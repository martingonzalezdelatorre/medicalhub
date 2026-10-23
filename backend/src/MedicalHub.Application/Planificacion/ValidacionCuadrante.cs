using MedicalHub.Application.Comun;
using MedicalHub.Application.Reglas;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Reglas;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

public sealed partial class CuadrantesServicio
{
    /// <summary>
    /// Evalúa el cuadrante con el motor de reglas. Una infracción bloqueante impide publicar salvo que tenga una
    /// excepción autorizada registrada; las advertencias son revisables y no bloquean.
    /// </summary>
    private async Task<ValidacionDto> ValidarInternoAsync(Cuadrante cuadrante, CancellationToken ct)
    {
        var incidencias = new List<IncidenciaDto>();
        var turnos = await db.Turnos.Where(t => t.CuadranteId == cuadrante.Id).OrderBy(t => t.InicioUtc).ToListAsync(ct);
        if (turnos.Count == 0)
            return new ValidacionDto(false, [new(Severidad.Bloqueante, "CuadranteVacio", "El cuadrante no tiene turnos.", null, null)]);

        var datos = await evaluacion.CargarDatosAsync(ct);
        var reglas = MotorReglas.Efectivas(datos.Reglas, cuadrante.ServicioId);
        var turnoIds = turnos.Select(t => t.Id).ToList();
        var tipos = await db.TiposTurno.Where(t => t.ServicioId == cuadrante.ServicioId).ToDictionaryAsync(t => t.Id, ct);
        var necesidades = await db.Necesidades.Where(n => turnoIds.Contains(n.TurnoId)).ToListAsync(ct);
        var asignaciones = await db.Asignaciones.Where(a => turnoIds.Contains(a.TurnoId) && a.Estado != EstadoAsignacion.Cancelada).ToListAsync(ct);
        var servicio = await db.Servicios.FirstAsync(s => s.Id == cuadrante.ServicioId, ct);
        var profIds = asignaciones.Select(a => a.ProfesionalId).Distinct().ToList();
        var profesionales = await db.Profesionales.Where(p => profIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);
        var asignacionIds = asignaciones.Select(a => a.Id).ToList();
        var excepciones = (await db.Excepciones.Where(e => asignacionIds.Contains(e.AsignacionId)).ToListAsync(ct))
            .ToDictionary(e => (e.AsignacionId, e.ClaveRegla));
        var autores = await db.Usuarios.Where(u => excepciones.Values.Select(e => e.AutorizadaPorUsuarioId).Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.NombreCompleto, ct);

        TurnoLigero Ligero(Turno t) => new(t.Id, t.ServicioId, t.Fecha, t.InicioUtc, t.FinUtc, tipos[t.TipoTurnoId].EsGuardia,
            EvaluacionServicio.Etiqueta(tipos[t.TipoTurnoId].Nombre, t.Fecha, servicio.Nombre));

        // 1. Cobertura y turnos sin necesidades
        foreach (var t in turnos)
        {
            var etiqueta = EvaluacionServicio.Etiqueta(tipos[t.TipoTurnoId].Nombre, t.Fecha, servicio.Nombre);
            var ns = necesidades.Where(n => n.TurnoId == t.Id).ToList();
            if (ns.Count == 0)
                incidencias.Add(new(Severidad.Advertencia, "TurnoSinNecesidades", $"{etiqueta} no tiene necesidades de cobertura definidas.", t.Id, null));
            foreach (var n in ns)
                foreach (var (regla, mensaje) in MotorReglas.EvaluarCobertura(n.NumeroPlazas, asignaciones.Count(a => a.NecesidadId == n.Id), etiqueta, reglas))
                    incidencias.Add(new(regla.Severidad.ToString(), regla.Tipo.ToString(), mensaje, t.Id, null));
        }

        // 2. Reglas de cada asignación frente al resto de turnos del profesional (incluidos otros cuadrantes)
        var entorno = await evaluacion.AsignacionesAsync(profIds, turnos.Min(t => t.InicioUtc) - datos.Horizonte, turnos.Max(t => t.FinUtc) + datos.Horizonte, datos, ct);
        foreach (var a in asignaciones)
        {
            if (!profesionales.TryGetValue(a.ProfesionalId, out var p)) continue;
            var turno = turnos.First(t => t.Id == a.TurnoId);
            var ligero = Ligero(turno);

            foreach (var (regla, mensaje) in MotorReglas.EvaluarPendiente(a.Estado, p.NombreCompleto, ligero.Etiqueta, reglas))
                incidencias.Add(new(regla.Severidad.ToString(), regla.Tipo.ToString(), mensaje, turno.Id, p.Id));

            var violaciones = MotorReglas.EvaluarAsignacion(new ContextoEvaluacion
            {
                Profesional = p, Servicio = servicio, Necesidad = necesidades.FirstOrDefault(n => n.Id == a.NecesidadId), Turno = ligero,
                Otras = (entorno.GetValueOrDefault(p.Id) ?? []).Where(o => EsAnterior(o, a)).ToList(),
                Competencias = datos.Competencias, Servicios = datos.Servicios
            }, reglas);

            foreach (var v in violaciones)
            {
                var texto = $"{p.NombreCompleto} en {ligero.Etiqueta}: {v.Mensaje}";
                if (v.Bloquea && excepciones.TryGetValue((a.Id, v.Clave), out var ex))
                    incidencias.Add(new(Severidad.Bloqueante, v.Codigo, texto, turno.Id, p.Id, true, ex.Justificacion, autores.GetValueOrDefault(ex.AutorizadaPorUsuarioId)));
                else
                    incidencias.Add(new(v.Severidad.ToString(), v.Codigo, texto, turno.Id, p.Id));
            }
        }

        var distintas = incidencias.DistinctBy(i => (i.Codigo, i.Mensaje)).ToList();
        return new ValidacionDto(distintas.All(i => i.Severidad != Severidad.Bloqueante || i.Excepcionada), distintas);
    }
}

public sealed partial class CuadrantesServicio
{
    /// <summary>
    /// Las infracciones entre pares (descanso, ventanas, incompatibilidades…) se atribuyen a la asignación MÁS RECIENTE del par:
    /// es la que se hizo teniendo en cuenta a la otra y, por tanto, la que lleva la excepción si la hubo. Así cada infracción se
    /// cuenta una sola vez y coincide exactamente con lo que se evaluó al asignar.
    /// </summary>
    internal static bool EsAnterior(AsignacionLigera otra, Asignacion actual)
        => otra.AsignacionId != actual.Id
           && (otra.FechaAsignacion < actual.FechaAsignacion
               || (otra.FechaAsignacion == actual.FechaAsignacion && otra.AsignacionId.CompareTo(actual.Id) < 0));
}
