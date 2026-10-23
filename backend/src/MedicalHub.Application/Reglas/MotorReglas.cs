using System.Globalization;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Reglas;

namespace MedicalHub.Application.Reglas;

/// <summary>
/// Motor de reglas de planificación: lógica pura (sin acceso a datos). Dado un profesional, un turno y sus demás
/// asignaciones, devuelve las infracciones. La severidad y la posibilidad de excepción las fija cada sociedad.
/// </summary>
public static class MotorReglas
{
    private const string Solape = "Solape";
    private const string ProfesionalInactivo = "ProfesionalInactivo";
    private const string ServicioInactivo = "ServicioInactivo";

    // ───────────── Selección de reglas ─────────────

    /// <summary>
    /// Reglas aplicables a un servicio. Si la sociedad no ha configurado ninguna de un tipo con comportamiento por defecto, se usa el
    /// de por defecto; si ha configurado alguna (aunque esté inactiva), solo cuentan las activas (así se puede desactivar una regla base).
    /// </summary>
    public static IReadOnlyList<ReglaEfectiva> Efectivas(IEnumerable<ReglaPlanificacion> todas, Guid servicioId)
    {
        var lista = todas.ToList();
        var resultado = new List<ReglaEfectiva>();
        foreach (var tipo in Enum.GetValues<TipoRegla>())
        {
            var delTipo = lista.Where(r => r.Tipo == tipo && (r.ServicioId == null || r.ServicioId == servicioId)).ToList();
            if (delTipo.Count == 0)
            {
                if (ParametrosRegla.TieneComportamientoPorDefecto(tipo)) resultado.Add(PorDefecto(tipo));
                continue;
            }
            resultado.AddRange(delTipo.Where(r => r.Activa).Select(r =>
                new ReglaEfectiva(r.Id, r.Tipo, r.Nombre, r.Severidad, r.PermiteExcepcion, r.ExcepcionRequiereAdministrador, r.ParametrosJson)));
        }
        return resultado;
    }

    private static ReglaEfectiva PorDefecto(TipoRegla tipo) => tipo switch
    {
        TipoRegla.CoberturaMinima => new(null, tipo, "Cobertura mínima (por defecto)", SeveridadRegla.Advertencia, false, false, """{"porcentajeMinimo":100}"""),
        TipoRegla.AsignacionPendiente => new(null, tipo, "Asignaciones pendientes (por defecto)", SeveridadRegla.Advertencia, false, false, null),
        TipoRegla.PreferenciaEvitar => new(null, tipo, "Preferencia por evitar el turno (por defecto)", SeveridadRegla.Advertencia, false, false, null),
        _ => new(null, tipo, ParametrosRegla.Definicion(tipo).Nombre + " (por defecto)", SeveridadRegla.Bloqueante, true, false, null)
    };

    /// <summary>Días alrededor de un turno que hay que cargar para evaluar los límites configurados.</summary>
    public static int HorizonteDias(IEnumerable<ReglaPlanificacion> todas)
    {
        var dias = 3;
        foreach (var r in todas.Where(r => r.Activa))
        {
            switch (r.Tipo)
            {
                case TipoRegla.MaxHorasPeriodo: dias = Math.Max(dias, (ParametrosRegla.Leer<ParamHoras>(r.ParametrosJson)?.VentanaDias ?? 0) + 2); break;
                case TipoRegla.MaxGuardiasPeriodo: dias = Math.Max(dias, (ParametrosRegla.Leer<ParamGuardias>(r.ParametrosJson)?.VentanaDias ?? 0) + 2); break;
                case TipoRegla.MaxDiasConsecutivos: dias = Math.Max(dias, (ParametrosRegla.Leer<ParamDias>(r.ParametrosJson)?.MaxDias ?? 0) + 3); break;
                case TipoRegla.DescansoMinimo: dias = Math.Max(dias, (ParametrosRegla.Leer<ParamDescanso>(r.ParametrosJson)?.Horas ?? 0) / 24 + 2); break;
                case TipoRegla.IncompatibilidadServicios: dias = Math.Max(dias, (ParametrosRegla.Leer<ParamIncompatibilidad>(r.ParametrosJson)?.MargenHoras ?? 0) / 24 + 2); break;
            }
        }
        return dias;
    }

    // ───────────── Evaluación de una asignación ─────────────

    public static List<Violacion> EvaluarAsignacion(ContextoEvaluacion c, IReadOnlyList<ReglaEfectiva> reglas)
    {
        var v = new List<Violacion>();

        // Reglas intrínsecas: imposibilidades físicas o administrativas. No se configuran ni admiten excepción.
        if (!c.Profesional.Activo)
            v.Add(new(ProfesionalInactivo, null, "Profesional activo", SeveridadRegla.Bloqueante, "El profesional está inactivo.", false, false));
        if (!c.Servicio.Activo)
            v.Add(new(ServicioInactivo, null, "Servicio activo", SeveridadRegla.Bloqueante, "El servicio está inactivo.", false, false));
        var solapada = c.Otras.FirstOrDefault(o => CalculadoraHorario.Solapan(o.Turno.InicioUtc, o.Turno.FinUtc, c.Turno.InicioUtc, c.Turno.FinUtc));
        if (solapada is not null)
            v.Add(new(Solape, null, "Sin solapamientos", SeveridadRegla.Bloqueante,
                $"Ya tiene otro turno que se solapa con este horario: {solapada.Turno.Etiqueta}.", false, false));

        foreach (var r in reglas)
            foreach (var mensaje in Evaluar(r, c))
                v.Add(new(r.Tipo.ToString(), r.Id, r.Nombre, r.Severidad, mensaje, r.PermiteExcepcion, r.RequiereAdministrador));
        return v;
    }

    private static IEnumerable<string> Evaluar(ReglaEfectiva r, ContextoEvaluacion c)
    {
        switch (r.Tipo)
        {
            case TipoRegla.AutorizacionServicio:
                if (c.Profesional.ServiciosAutorizados.All(s => s.ServicioId != c.Servicio.Id))
                    yield return "No está autorizado para este servicio.";
                break;

            case TipoRegla.CategoriaAdmitida:
                if (c.Servicio.CategoriasAdmitidas.Count > 0 && c.Servicio.CategoriasAdmitidas.All(x => x.CategoriaProfesionalId != c.Profesional.CategoriaId))
                    yield return "Su categoría profesional no está admitida en este servicio.";
                break;

            case TipoRegla.CompetenciasRequeridas:
                foreach (var req in c.Servicio.CompetenciasRequeridas)
                {
                    var nombre = c.Competencias.GetValueOrDefault(req.CompetenciaId, req.CompetenciaId.ToString());
                    var tiene = c.Profesional.Competencias.FirstOrDefault(x => x.CompetenciaId == req.CompetenciaId);
                    if (tiene is null) yield return $"Falta la competencia «{nombre}».";
                    else if (!tiene.VigenteEn(c.Turno.Fecha)) yield return $"La competencia «{nombre}» está caducada.";
                }
                break;

            case TipoRegla.CategoriaNecesidad:
                if (c.Necesidad?.CategoriaProfesionalId is { } cat && c.Profesional.CategoriaId != cat)
                    yield return "Su categoría no es la que exige esta necesidad de cobertura.";
                break;

            case TipoRegla.AusenciaAprobada:
                if (c.Ausencias.FirstOrDefault(a => a.Desde <= c.Turno.FechaFin && c.Turno.Fecha <= a.Hasta) is { } au)
                    yield return $"Tiene una ausencia aprobada ({EtiquetaAusencia(au.Tipo)}) del {au.Desde:dd/MM/yyyy} al {au.Hasta:dd/MM/yyyy}.";
                break;

            case TipoRegla.DisponibilidadBloqueada:
                if (c.Disponibilidades.FirstOrDefault(d => d.Tipo == MedicalHub.Domain.Ausencias.TipoDisponibilidad.Bloqueo && d.Afecta(c.Turno)) is { } bl)
                    yield return $"Ha bloqueado ese periodo ({bl.Desde:dd/MM/yyyy} – {bl.Hasta:dd/MM/yyyy}).";
                break;

            case TipoRegla.PreferenciaEvitar:
                if (c.Disponibilidades.Any(d => d.Tipo == MedicalHub.Domain.Ausencias.TipoDisponibilidad.Evita && d.Afecta(c.Turno)))
                    yield return "Ha indicado que prefiere evitar este turno.";
                break;

            case TipoRegla.DescansoMinimo:
                if (ParametrosRegla.Leer<ParamDescanso>(r.ParametrosJson) is { } d)
                {
                    var peor = c.Otras.Where(o => !CalculadoraHorario.Solapan(o.Turno.InicioUtc, o.Turno.FinUtc, c.Turno.InicioUtc, c.Turno.FinUtc))
                        .Select(o => (Otro: o, Hueco: Hueco(o.Turno, c.Turno))).Where(x => x.Hueco < d.Horas)
                        .OrderBy(x => x.Hueco).FirstOrDefault();
                    if (peor.Otro is not null)
                        yield return $"Descanso insuficiente: {N(peor.Hueco)} h entre «{peor.Otro.Turno.Etiqueta}» y este turno (mínimo {d.Horas} h).";
                }
                break;

            case TipoRegla.MaxHorasPeriodo:
                if (ParametrosRegla.Leer<ParamHoras>(r.ParametrosJson) is { } mh)
                {
                    var (suma, inicio) = MayorVentana(c, mh.VentanaDias, t => t.Horas);
                    if (suma > mh.MaxHoras)
                        yield return $"{N(suma)} h planificadas entre el {inicio:dd/MM} y el {inicio.AddDays(mh.VentanaDias - 1):dd/MM} ({mh.VentanaDias} días); máximo: {mh.MaxHoras} h.";
                }
                break;

            case TipoRegla.MaxGuardiasPeriodo:
                if (ParametrosRegla.Leer<ParamGuardias>(r.ParametrosJson) is { } mg)
                {
                    var (cuenta, inicio) = MayorVentana(c, mg.VentanaDias, t => t.EsGuardia ? 1 : 0);
                    if (cuenta > mg.MaxGuardias)
                        yield return $"{N(cuenta)} guardias entre el {inicio:dd/MM} y el {inicio.AddDays(mg.VentanaDias - 1):dd/MM} ({mg.VentanaDias} días); máximo: {mg.MaxGuardias}.";
                }
                break;

            case TipoRegla.MaxDiasConsecutivos:
                if (ParametrosRegla.Leer<ParamDias>(r.ParametrosJson) is { } md)
                {
                    var dias = c.Otras.Select(o => o.Turno.Fecha).Append(c.Turno.Fecha).ToHashSet();
                    var desde = c.Turno.Fecha;
                    while (dias.Contains(desde.AddDays(-1))) desde = desde.AddDays(-1);
                    var hasta = c.Turno.Fecha;
                    while (dias.Contains(hasta.AddDays(1))) hasta = hasta.AddDays(1);
                    var largo = hasta.DayNumber - desde.DayNumber + 1;
                    if (largo > md.MaxDias)
                        yield return $"{largo} días consecutivos con turno (del {desde:dd/MM} al {hasta:dd/MM}); máximo: {md.MaxDias}.";
                }
                break;

            case TipoRegla.IncompatibilidadServicios:
                if (ParametrosRegla.Leer<ParamIncompatibilidad>(r.ParametrosJson) is { } inc
                    && (c.Servicio.Id == inc.ServicioAId || c.Servicio.Id == inc.ServicioBId))
                {
                    var opuesto = c.Servicio.Id == inc.ServicioAId ? inc.ServicioBId : inc.ServicioAId;
                    var choque = c.Otras.Where(o => o.Turno.ServicioId == opuesto)
                        .Select(o => (Otro: o, Hueco: Hueco(o.Turno, c.Turno))).Where(x => x.Hueco < inc.MargenHoras)
                        .OrderBy(x => x.Hueco).FirstOrDefault();
                    if (choque.Otro is not null)
                    {
                        var nombre = c.Servicios.GetValueOrDefault(opuesto, "otro servicio");
                        yield return $"Incompatible con {nombre}: «{choque.Otro.Turno.Etiqueta}» está a {N(choque.Hueco)} h (margen mínimo {inc.MargenHoras} h).";
                    }
                }
                break;
        }
    }

    // ───────────── Reglas de cuadrante ─────────────

    public static IEnumerable<(ReglaEfectiva Regla, string Mensaje)> EvaluarCobertura(int plazas, int cubiertas, string etiqueta, IReadOnlyList<ReglaEfectiva> reglas)
    {
        foreach (var r in reglas.Where(r => r.Tipo == TipoRegla.CoberturaMinima))
        {
            var minimo = ParametrosRegla.Leer<ParamCobertura>(r.ParametrosJson)?.PorcentajeMinimo ?? 100;
            var porcentaje = plazas == 0 ? 100.0 : cubiertas * 100.0 / plazas;
            if (porcentaje < minimo)
                yield return (r, $"{etiqueta}: {cubiertas} de {plazas} plazas cubiertas ({N(porcentaje)} %; mínimo {minimo} %).");
        }
    }

    public static IEnumerable<(ReglaEfectiva Regla, string Mensaje)> EvaluarPendiente(EstadoAsignacion estado, string profesional, string etiqueta, IReadOnlyList<ReglaEfectiva> reglas)
    {
        if (estado != EstadoAsignacion.PendienteAprobacion) yield break;
        foreach (var r in reglas.Where(r => r.Tipo == TipoRegla.AsignacionPendiente))
            yield return (r, $"{profesional} en {etiqueta}: asignación pendiente de aprobación.");
    }

    // ───────────── Utilidades ─────────────

    /// <summary>Horas entre dos turnos que no se solapan (0 si se solapan).</summary>
    private static double Hueco(TurnoLigero a, TurnoLigero b)
    {
        if (a.FinUtc <= b.InicioUtc) return (b.InicioUtc - a.FinUtc).TotalHours;
        if (b.FinUtc <= a.InicioUtc) return (a.InicioUtc - b.FinUtc).TotalHours;
        return 0;
    }

    /// <summary>Mayor valor acumulado en una ventana de N días consecutivos que incluya el día del turno evaluado.</summary>
    private static (double Valor, DateOnly Inicio) MayorVentana(ContextoEvaluacion c, int ventanaDias, Func<TurnoLigero, double> valor)
    {
        var todos = c.Otras.Select(o => o.Turno).Append(c.Turno).ToList();
        var mejor = (Valor: 0.0, Inicio: c.Turno.Fecha);
        for (var i = 0; i < ventanaDias; i++)
        {
            var inicio = c.Turno.Fecha.AddDays(-i);
            var fin = inicio.AddDays(ventanaDias - 1);
            var suma = todos.Where(t => t.Fecha >= inicio && t.Fecha <= fin).Sum(valor);
            if (suma > mejor.Valor) mejor = (suma, inicio);
        }
        return mejor;
    }

    private static string EtiquetaAusencia(MedicalHub.Domain.Ausencias.TipoAusencia tipo) => tipo switch
    {
        MedicalHub.Domain.Ausencias.TipoAusencia.Vacaciones => "vacaciones",
        MedicalHub.Domain.Ausencias.TipoAusencia.AsuntosPropios => "asuntos propios",
        MedicalHub.Domain.Ausencias.TipoAusencia.Permiso => "permiso",
        MedicalHub.Domain.Ausencias.TipoAusencia.Formacion => "formación",
        MedicalHub.Domain.Ausencias.TipoAusencia.BajaTemporal => "baja temporal",
        _ => "otra ausencia"
    };

    private static string N(double valor) => valor.ToString("0.#", CultureInfo.GetCultureInfo("es-ES"));
}

public static class NotasEvaluacion
{
    /// <summary>Información positiva (no son infracciones): el profesional ha declarado disponibilidad o prefiere el turno.</summary>
    public static IReadOnlyList<string> Positivas(ContextoEvaluacion c)
    {
        var notas = new List<string>();
        if (c.Disponibilidades.Any(d => d.Tipo == MedicalHub.Domain.Ausencias.TipoDisponibilidad.Prefiere && d.Afecta(c.Turno))) notas.Add("Prefiere este turno.");
        if (c.Disponibilidades.Any(d => d.Tipo == MedicalHub.Domain.Ausencias.TipoDisponibilidad.Disponible && d.Afecta(c.Turno))) notas.Add("Ha declarado disponibilidad para estas fechas.");
        return notas;
    }
}
