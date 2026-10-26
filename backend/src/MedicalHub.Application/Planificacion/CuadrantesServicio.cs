using System.Globalization;
using System.Text;
using System.Text.Json;
using MedicalHub.Application.Comun;
using MedicalHub.Application.Reglas;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Profesionales;
using MedicalHub.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

public sealed partial class CuadrantesServicio(
    IAppDbContext db,
    IContextoActual contexto,
    TimeProvider reloj,
    CalendarioServicio calendario,
    TurnosServicio turnosServicio,
    EvaluacionServicio evaluacion,
    AutoAsignacionServicio autoAsignacion)
{
    // ───────────── Consulta ─────────────

    public async Task<IReadOnlyList<CuadranteDto>> ListarAsync(Guid? servicioId, EstadoCuadrante? estado, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var visibles = AlcancePlanificacion.Servicios(db, contexto).Select(s => s.Id);
        var q = db.Cuadrantes.Where(c => visibles.Contains(c.ServicioId));
        if (servicioId is { } s) q = q.Where(c => c.ServicioId == s);
        if (estado is { } e) q = q.Where(c => c.Estado == e);
        var lista = await q.OrderByDescending(c => c.Desde).ThenBy(c => c.Nombre).Take(300).ToListAsync(ct);
        return await ComponerAsync(lista, ct);
    }

    public async Task<CuadranteDto> ObtenerAsync(Guid id, CancellationToken ct)
        => (await ComponerAsync([await BuscarAsync(id, ct)], ct)).Single();

    // ───────────── Alta y baja ─────────────

    public async Task<CuadranteDto> CrearAsync(SolicitudCuadrante s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var servicio = await ServicioVisibleAsync(s.ServicioId, ct);
        if (!servicio.Activo) throw new ExcepcionConflicto("El servicio está inactivo.");
        var nombre = string.IsNullOrWhiteSpace(s.Nombre) ? $"{servicio.Nombre} {s.Desde:dd/MM/yyyy}–{s.Hasta:dd/MM/yyyy}" : s.Nombre;
        var cuadrante = new Cuadrante(org, servicio.Id, nombre, s.Desde, s.Hasta, reloj.GetUtcNow());
        var nuevos = new List<Guid>();
        db.Cuadrantes.Add(cuadrante);

        if (s.GenerarTurnos)
        {
            var requisitos = await db.RequisitosCobertura.Where(r => r.ServicioId == servicio.Id).ToListAsync(ct);
            var tipos = await db.TiposTurno.Where(t => t.ServicioId == servicio.Id && t.Activo).ToListAsync(ct);
            var zona = await turnosServicio.ZonaDelServicioAsync(servicio.Id, ct);
            var yaExisten = await turnosServicio.TurnosExistentesAsync(servicio.Id, s.Desde, s.Hasta, ct);
            for (var dia = s.Desde; dia <= s.Hasta; dia = dia.AddDays(1))
                foreach (var tipo in tipos.Where(t => requisitos.Any(r => r.TipoTurnoId == t.Id)))
                {
                    if (yaExisten.Contains((tipo.Id, dia))) continue; // ya está en otro cuadrante del servicio
                    var turno = TurnosServicio.NuevoTurno(org, cuadrante, tipo, dia, zona);
                    db.Turnos.Add(turno);
                    nuevos.Add(turno.Id);
                    foreach (var r in requisitos.Where(r => r.TipoTurnoId == tipo.Id))
                        db.Necesidades.Add(new NecesidadCobertura(org, turno.Id, r.NumeroProfesionales, r.CategoriaProfesionalId));
                }
        }
        await db.SaveChangesAsync(ct);
        await autoAsignacion.AsignarTurnosAsync(nuevos, null, ct);
        return await ObtenerAsync(cuadrante.Id, ct);
    }

    public async Task RenombrarAsync(Guid id, SolicitudRenombrar s, CancellationToken ct)
    {
        new Validador().Requerido(nameof(s.Nombre), s.Nombre, 150).LanzarSiHayErrores();
        var c = await BuscarAsync(id, ct);
        c.Renombrar(s.Nombre);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Elimina un borrador que nunca se publicó. Los publicados se archivan para conservar su historial.</summary>
    public async Task EliminarBorradorAsync(Guid id, CancellationToken ct)
    {
        var cuadrante = await BuscarAsync(id, ct);
        if (!cuadrante.EsEditable || cuadrante.Version > 0)
            throw new ExcepcionConflicto("Solo se pueden eliminar borradores que nunca se han publicado. Archive el resto.");

        await db.EnTransaccionAsync(async token =>
        {
            db.Asignaciones.RemoveRange(await db.Asignaciones.Where(a => db.Turnos.Any(t => t.Id == a.TurnoId && t.CuadranteId == id)).ToListAsync(token));
            db.Necesidades.RemoveRange(await db.Necesidades.Where(n => db.Turnos.Any(t => t.Id == n.TurnoId && t.CuadranteId == id)).ToListAsync(token));
            db.Turnos.RemoveRange(await db.Turnos.Where(t => t.CuadranteId == id).ToListAsync(token));
            db.Cuadrantes.Remove(await db.Cuadrantes.FirstAsync(c => c.Id == id, token));
            await db.SaveChangesAsync(token);
            return true;
        }, ct);
    }

    // ───────────── Validación y publicación ─────────────

    /// <summary>
    /// Comprobaciones previas a publicar. Las bloqueantes impiden publicar; las advertencias son revisables.
    /// (El motor de reglas configurable de E4 ampliará este conjunto.)
    /// </summary>
    public async Task<ValidacionDto> ValidarAsync(Guid id, CancellationToken ct)
    {
        var cuadrante = await BuscarAsync(id, ct);
        return await ValidarInternoAsync(cuadrante, ct);
    }

    public async Task<CuadranteDto> PublicarAsync(Guid id, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");

        await db.EnTransaccionAsync(async token =>
        {
            await db.BloquearAsync($"cuadrante:{id}", token);
            var cuadrante = await BuscarAsync(id, token);
            if (!cuadrante.EsEditable) throw new ExcepcionConflicto("El cuadrante ya está publicado.");

            var validacion = await ValidarInternoAsync(cuadrante, token);
            if (!validacion.PuedePublicarse)
                throw new ExcepcionDominio("No se puede publicar: " + string.Join(" ", validacion.Incidencias.Where(i => i.Severidad == Severidad.Bloqueante).Select(i => i.Mensaje)));

            var ahora = reloj.GetUtcNow();
            cuadrante.Publicar(uid, ahora);

            var turnos = await calendario.TurnosDeCuadranteAsync(id, token);
            var nombreUsuario = await db.Usuarios.Where(u => u.Id == uid).Select(u => u.NombreCompleto).FirstOrDefaultAsync(token);
            var json = JsonSerializer.Serialize(new
            {
                cuadrante = new { cuadrante.Id, cuadrante.Nombre, cuadrante.Desde, cuadrante.Hasta, version = cuadrante.Version, publicadoPor = nombreUsuario, fecha = ahora },
                incidencias = validacion.Incidencias,
                turnos = turnos.Select(t => new
                {
                    t.Id, t.Fecha, t.InicioLocal, t.FinLocal, t.InicioUtc, t.FinUtc, t.Centro, t.Servicio, t.TipoTurno, t.EsGuardia,
                    necesidades = t.Necesidades.Select(n => new
                    {
                        n.Id, n.NumeroPlazas, categoria = n.Categoria,
                        asignaciones = n.Asignaciones.Select(a => new { a.Id, a.ProfesionalId, a.Profesional })
                    })
                })
            });
            db.VersionesCuadrante.Add(new VersionCuadrante(cuadrante.OrganizacionId!.Value, id, cuadrante.Version, ahora, uid,
                turnos.Count, turnos.Sum(t => t.PlazasTotales), turnos.Sum(t => t.PlazasCubiertas), json));
            await db.SaveChangesAsync(token);
            return true;
        }, ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task<CuadranteDto> ReabrirAsync(Guid id, CancellationToken ct)
    {
        var c = await BuscarAsync(id, ct);
        if (c.Estado != EstadoCuadrante.Publicado) throw new ExcepcionConflicto("Solo se puede reabrir un cuadrante publicado.");
        c.Reabrir();
        await db.SaveChangesAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task<CuadranteDto> ArchivarAsync(Guid id, CancellationToken ct)
    {
        var c = await BuscarAsync(id, ct);
        if (c.Estado != EstadoCuadrante.Publicado) throw new ExcepcionConflicto("Solo se puede archivar un cuadrante publicado.");
        c.Archivar();
        await db.SaveChangesAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task<IReadOnlyList<VersionResumenDto>> VersionesAsync(Guid id, CancellationToken ct)
    {
        await BuscarAsync(id, ct);
        var versiones = await db.VersionesCuadrante.Where(v => v.CuadranteId == id).OrderByDescending(v => v.Numero).ToListAsync(ct);
        var usuarios = await UsuariosAsync(versiones.Select(v => v.UsuarioId), ct);
        return versiones.Select(v => Resumen(v, usuarios)).ToList();
    }

    public async Task<VersionDetalleDto> VersionAsync(Guid id, int numero, CancellationToken ct)
    {
        await BuscarAsync(id, ct);
        var v = await db.VersionesCuadrante.FirstOrDefaultAsync(x => x.CuadranteId == id && x.Numero == numero, ct)
            ?? throw new ExcepcionNoEncontrado("Versión no encontrada.");
        var usuarios = await UsuariosAsync([v.UsuarioId], ct);
        return new VersionDetalleDto(Resumen(v, usuarios), v.ContenidoJson);
    }

    // ───────────── Plantillas ─────────────

    public async Task<IReadOnlyList<PlantillaDto>> PlantillasAsync(Guid? servicioId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var visibles = AlcancePlanificacion.Servicios(db, contexto).Select(s => s.Id);
        var q = db.Plantillas.Where(p => visibles.Contains(p.ServicioId));
        if (servicioId is { } s) q = q.Where(p => p.ServicioId == s);
        return (await q.OrderBy(p => p.Nombre).ToListAsync(ct)).Select(p => new PlantillaDto(p.Id, p.ServicioId, p.Nombre, p.DiasPeriodo, p.Lineas.Count)).ToList();
    }

    /// <summary>Guarda la estructura del cuadrante (turnos y necesidades, sin profesionales) como plantilla reutilizable.</summary>
    public async Task<PlantillaDto> GuardarComoPlantillaAsync(Guid id, SolicitudPlantilla s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        new Validador().Requerido(nameof(s.Nombre), s.Nombre, 150).LanzarSiHayErrores();
        var cuadrante = await BuscarAsync(id, ct);
        if (await db.Plantillas.AnyAsync(p => p.ServicioId == cuadrante.ServicioId && p.Nombre == s.Nombre.Trim(), ct))
            throw new ExcepcionConflicto("Ya existe una plantilla con ese nombre en el servicio.");

        var turnos = await db.Turnos.Where(t => t.CuadranteId == id).ToListAsync(ct);
        var turnoIds = turnos.Select(t => t.Id).ToList();
        var necesidades = await db.Necesidades.Where(n => turnoIds.Contains(n.TurnoId)).ToListAsync(ct);
        var lineas = (from n in necesidades
                      join t in turnos on n.TurnoId equals t.Id
                      select new LineaPlantilla(t.Fecha.DayNumber - cuadrante.Desde.DayNumber, t.TipoTurnoId, n.NumeroPlazas, n.CategoriaProfesionalId)).ToList();
        if (lineas.Count == 0) throw new ExcepcionConflicto("El cuadrante no tiene turnos con necesidades que guardar.");

        var plantilla = new PlantillaCuadrante(org, cuadrante.ServicioId, s.Nombre, cuadrante.Dias, lineas, reloj.GetUtcNow());
        db.Plantillas.Add(plantilla);
        await db.SaveChangesAsync(ct);
        return new PlantillaDto(plantilla.Id, plantilla.ServicioId, plantilla.Nombre, plantilla.DiasPeriodo, plantilla.Lineas.Count);
    }

    /// <summary>Crea un cuadrante en borrador repitiendo la estructura de una plantilla desde la fecha indicada.</summary>
    public async Task<CuadranteDto> CrearDesdePlantillaAsync(SolicitudDesdePlantilla s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        var visibles = AlcancePlanificacion.Servicios(db, contexto).Select(x => x.Id);
        var plantilla = await db.Plantillas.FirstOrDefaultAsync(p => p.Id == s.PlantillaId && visibles.Contains(p.ServicioId), ct)
            ?? throw new ExcepcionNoEncontrado("Plantilla no encontrada.");
        var servicio = await ServicioVisibleAsync(plantilla.ServicioId, ct);
        if (!servicio.Activo) throw new ExcepcionConflicto("El servicio está inactivo.");

        var hasta = s.Desde.AddDays(plantilla.DiasPeriodo - 1);
        var tiposActivos = await db.TiposTurno.Where(t => t.ServicioId == servicio.Id && t.Activo).ToDictionaryAsync(t => t.Id, ct);
        if (plantilla.Lineas.Any(l => !tiposActivos.ContainsKey(l.TipoTurnoId)))
            throw new ExcepcionConflicto("La plantilla usa tipos de turno que ya no están activos en el servicio.");

        var nombre = string.IsNullOrWhiteSpace(s.Nombre) ? $"{servicio.Nombre} {s.Desde:dd/MM/yyyy}–{hasta:dd/MM/yyyy}" : s.Nombre;
        var cuadrante = new Cuadrante(org, servicio.Id, nombre, s.Desde, hasta, reloj.GetUtcNow());
        db.Cuadrantes.Add(cuadrante);

        var zona = await turnosServicio.ZonaDelServicioAsync(servicio.Id, ct);
        var yaExisten = await turnosServicio.TurnosExistentesAsync(servicio.Id, s.Desde, hasta, ct);
        var nuevos = new List<Guid>();
        foreach (var grupo in plantilla.Lineas.GroupBy(l => (l.DiaOffset, l.TipoTurnoId)))
        {
            if (yaExisten.Contains((grupo.Key.TipoTurnoId, s.Desde.AddDays(grupo.Key.DiaOffset)))) continue; // ya está en otro cuadrante del servicio
            var turno = TurnosServicio.NuevoTurno(org, cuadrante, tiposActivos[grupo.Key.TipoTurnoId], s.Desde.AddDays(grupo.Key.DiaOffset), zona);
            db.Turnos.Add(turno);
            nuevos.Add(turno.Id);
            foreach (var l in grupo)
                db.Necesidades.Add(new NecesidadCobertura(org, turno.Id, l.NumeroPlazas, l.CategoriaProfesionalId));
        }
        await db.SaveChangesAsync(ct);
        await autoAsignacion.AsignarTurnosAsync(nuevos, null, ct);
        return await ObtenerAsync(cuadrante.Id, ct);
    }

    public async Task EliminarPlantillaAsync(Guid id, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var plantilla = await db.Plantillas.FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Plantilla no encontrada.");
        db.Plantillas.Remove(plantilla);
        await db.SaveChangesAsync(ct);
    }

    // ───────────── Exportación ─────────────

    /// <summary>CSV compatible con Excel en español (UTF-8 con BOM, separador «;»). Cada exportación queda registrada.</summary>
    public async Task<ArchivoExportado> ExportarCsvAsync(Guid id, CancellationToken ct)
    {
        var cuadrante = await BuscarAsync(id, ct);
        var turnos = await calendario.TurnosDeCuadranteAsync(id, ct);

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', "Fecha", "Inicio", "Fin", "Centro", "Servicio", "Turno", "Categoría requerida", "Plaza", "Profesional", "Estado del cuadrante"));
        foreach (var t in turnos)
            foreach (var n in t.Necesidades)
                for (var plaza = 0; plaza < n.NumeroPlazas; plaza++)
                {
                    var a = plaza < n.Asignaciones.Count ? n.Asignaciones[plaza] : null;
                    sb.AppendLine(string.Join(';', new[]
                    {
                        t.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), t.InicioLocal, t.FinLocal, t.Centro, t.Servicio, t.TipoTurno,
                        n.Categoria ?? "Cualquiera", (plaza + 1).ToString(CultureInfo.InvariantCulture), a?.Profesional ?? "SIN CUBRIR", cuadrante.Estado.ToString()
                    }.Select(CeldaCsv)));
                }

        db.EventosSeguridad.Add(new EventoSeguridad(TipoEventoSeguridad.ExportacionDatos, reloj.GetUtcNow(), contexto.OrganizacionId,
            contexto.UsuarioId, null, contexto.Ip, $"Exportación CSV del cuadrante {id}"));
        await db.SaveChangesAsync(ct);

        var contenido = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return new ArchivoExportado($"cuadrante-{cuadrante.Desde:yyyyMMdd}-{cuadrante.Hasta:yyyyMMdd}.csv", "text/csv; charset=utf-8", contenido);
    }

    /// <summary>Evita la inyección de fórmulas en hojas de cálculo y escapa separadores, comillas y saltos de línea.</summary>
    public static string CeldaCsv(string? valor)
    {
        var v = valor ?? "";
        if (v.Length > 0 && v[0] is '=' or '+' or '-' or '@' or '\t' or '\r') v = "'" + v;
        return v.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }

    // ───────────── Internos ─────────────

    private async Task<Cuadrante> BuscarAsync(Guid id, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var visibles = AlcancePlanificacion.Servicios(db, contexto).Select(s => s.Id);
        return await db.Cuadrantes.FirstOrDefaultAsync(c => c.Id == id && visibles.Contains(c.ServicioId), ct)
            ?? throw new ExcepcionNoEncontrado("Cuadrante no encontrado.");
    }

    private async Task<Domain.Estructura.Servicio> ServicioVisibleAsync(Guid id, CancellationToken ct)
        => await AlcancePlanificacion.Servicios(db, contexto).FirstOrDefaultAsync(s => s.Id == id, ct)
           ?? throw new ExcepcionNoEncontrado("Servicio no encontrado.");

    private async Task<Dictionary<Guid, string>> UsuariosAsync(IEnumerable<Guid?> ids, CancellationToken ct)
    {
        var lista = ids.Where(i => i != null).Select(i => i!.Value).Distinct().ToList();
        return await db.Usuarios.Where(u => lista.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.NombreCompleto, ct);
    }

    private static VersionResumenDto Resumen(VersionCuadrante v, Dictionary<Guid, string> usuarios)
        => new(v.Numero, v.FechaPublicacion, v.UsuarioId is { } u ? usuarios.GetValueOrDefault(u) : null, v.TotalTurnos, v.PlazasTotales, v.PlazasCubiertas);

    private async Task<IReadOnlyList<CuadranteDto>> ComponerAsync(List<Cuadrante> cuadrantes, CancellationToken ct)
    {
        var ids = cuadrantes.Select(c => c.Id).ToList();
        var servicioIds = cuadrantes.Select(c => c.ServicioId).Distinct().ToList();
        var servicios = await db.Servicios.Where(s => servicioIds.Contains(s.Id)).Select(s => new { s.Id, s.Nombre, s.CentroId }).ToDictionaryAsync(s => s.Id, ct);
        var centroIds = servicios.Values.Select(s => s.CentroId).Distinct().ToList();
        var centros = await db.Centros.Where(c => centroIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);

        var turnos = await db.Turnos.Where(t => ids.Contains(t.CuadranteId)).Select(t => new { t.Id, t.CuadranteId }).ToListAsync(ct);
        var turnoIds = turnos.Select(t => t.Id).ToList();
        var necesidades = await db.Necesidades.Where(n => turnoIds.Contains(n.TurnoId)).Select(n => new { n.Id, n.TurnoId, n.NumeroPlazas }).ToListAsync(ct);
        var ocupadas = await db.Asignaciones.Where(a => turnoIds.Contains(a.TurnoId) && a.Estado != EstadoAsignacion.Cancelada)
            .GroupBy(a => a.NecesidadId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, ct);
        var turnoCuadrante = turnos.ToDictionary(t => t.Id, t => t.CuadranteId);

        return cuadrantes.Select(c =>
        {
            var ns = necesidades.Where(n => turnoCuadrante[n.TurnoId] == c.Id).ToList();
            var s = servicios[c.ServicioId];
            return new CuadranteDto(c.Id, c.ServicioId, s.Nombre, s.CentroId, centros.GetValueOrDefault(s.CentroId, ""), c.Nombre, c.Desde, c.Hasta,
                c.Estado, c.Version, c.FechaPublicacion, turnos.Count(t => t.CuadranteId == c.Id), ns.Sum(n => n.NumeroPlazas),
                ns.Sum(n => Math.Min(n.NumeroPlazas, ocupadas.GetValueOrDefault(n.Id))));
        }).ToList();
    }
}
