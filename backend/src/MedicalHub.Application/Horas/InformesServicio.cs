using System.Globalization;
using System.Text;
using MedicalHub.Application.Comun;
using MedicalHub.Application.Planificacion;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Horas;

/// <summary>Informes calculados en el servidor con el alcance del usuario. Las exportaciones CSV quedan registradas como eventos de seguridad.</summary>
public sealed class InformesServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj, HorasServicio horas)
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-ES");

    // ───────────── Horas por profesional ─────────────

    public async Task<InformeHorasDto> HorasAsync(FiltroHoras f, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        HorasServicio.ValidarRango(f.Desde, f.Hasta);
        var lineas = await horas.LineasAsync(f, null, ct);
        var filas = lineas.GroupBy(l => (l.ProfesionalId, l.Profesional)).Select(g => Fila(g.Key.ProfesionalId, g.Key.Profesional, g)).OrderBy(x => x.Profesional).ToList();
        return new InformeHorasDto(f.Desde, f.Hasta, filas, Fila(Guid.Empty, "Total", lineas));
    }

    private static FilaInformeHorasDto Fila(Guid id, string nombre, IEnumerable<LineaHorasDto> lineas)
    {
        var l = lineas.ToList();
        var registradas = l.Where(x => x.RegistroId is not null).ToList();
        return new FilaInformeHorasDto(id, nombre, l.Count, l.Count(x => x.EsGuardia), l.Sum(x => x.PlanificadasMin), l.Sum(x => x.NocturnasPlanMin),
            registradas.Count, registradas.Sum(x => x.RealesMin ?? 0), registradas.Sum(x => x.NocturnasRealMin ?? 0), registradas.Sum(x => x.AjustesMin),
            registradas.Sum(x => x.EfectivasMin ?? 0));
    }

    public async Task<ArchivoExportado> HorasCsvAsync(FiltroHoras f, CancellationToken ct)
    {
        var informe = await HorasAsync(f, ct);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', "Profesional", "Turnos", "Guardias", "Horas planificadas", "Nocturnas planificadas", "Turnos con registro", "Horas registradas",
            "Nocturnas registradas", "Ajustes (h)", "Horas efectivas"));
        foreach (var x in informe.Filas.Append(informe.Total))
            sb.AppendLine(string.Join(';', new[]
            {
                x.Profesional, x.Turnos.ToString(Es), x.Guardias.ToString(Es), H(x.PlanificadasMin), H(x.NocturnasPlanMin), x.TurnosRegistrados.ToString(Es),
                H(x.RealesMin), H(x.NocturnasRealMin), H(x.AjustesMin), H(x.EfectivasMin)
            }.Select(CuadrantesServicio.CeldaCsv)));
        return await ExportarAsync($"informe-horas-{f.Desde:yyyyMMdd}-{f.Hasta:yyyyMMdd}.csv", sb, "horas", f.Desde, f.Hasta, ct);
    }

    // ───────────── Cobertura por servicio ─────────────

    public async Task<InformeCoberturaDto> CoberturaAsync(DateOnly desde, DateOnly hasta, Guid? servicioId, Guid? centroId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        HorasServicio.ValidarRango(desde, hasta);
        var servicios = AlcancePlanificacion.Servicios(db, contexto);
        if (servicioId is { } sid) servicios = servicios.Where(s => s.Id == sid);
        if (centroId is { } cid) servicios = servicios.Where(s => s.CentroId == cid);

        var baseTurnos = from t in db.Turnos
                         where t.Fecha >= desde && t.Fecha <= hasta
                         join c in db.Cuadrantes on t.CuadranteId equals c.Id
                         where c.Estado != EstadoCuadrante.Archivado
                         join s in servicios on t.ServicioId equals s.Id
                         select t;

        var plazas = await (from t in baseTurnos join n in db.Necesidades on t.Id equals n.TurnoId group n by t.ServicioId into g
                            select new { ServicioId = g.Key, Total = g.Sum(n => n.NumeroPlazas) }).ToDictionaryAsync(x => x.ServicioId, x => x.Total, ct);
        var cubiertas = await (from t in baseTurnos join a in db.Asignaciones on t.Id equals a.TurnoId where a.Estado != EstadoAsignacion.Cancelada
                               group a by t.ServicioId into g select new { ServicioId = g.Key, Total = g.Count() }).ToDictionaryAsync(x => x.ServicioId, x => x.Total, ct);
        var vacantes = await (from t in baseTurnos join v in db.Vacantes on t.Id equals v.TurnoId where v.Estado == EstadoVacante.Abierta
                              group v by t.ServicioId into g select new { ServicioId = g.Key, Total = g.Count() }).ToDictionaryAsync(x => x.ServicioId, x => x.Total, ct);
        var excepciones = await (from t in baseTurnos join e in db.Excepciones on t.Id equals e.TurnoId
                                 group e by t.ServicioId into g select new { ServicioId = g.Key, Total = g.Count() }).ToDictionaryAsync(x => x.ServicioId, x => x.Total, ct);

        var nombres = await (from s in servicios join c in db.Centros on s.CentroId equals c.Id orderby c.Nombre, s.Nombre select new { s.Id, Servicio = s.Nombre, Centro = c.Nombre })
            .ToListAsync(ct);
        var filas = nombres.Where(n => plazas.ContainsKey(n.Id)).Select(n => new FilaInformeCoberturaDto(n.Id, n.Servicio, n.Centro, plazas.GetValueOrDefault(n.Id),
            Math.Min(cubiertas.GetValueOrDefault(n.Id), plazas.GetValueOrDefault(n.Id)), vacantes.GetValueOrDefault(n.Id), excepciones.GetValueOrDefault(n.Id))).ToList();
        var total = new FilaInformeCoberturaDto(Guid.Empty, "Total", "", filas.Sum(x => x.Plazas), filas.Sum(x => x.Cubiertas), filas.Sum(x => x.VacantesAbiertas),
            filas.Sum(x => x.Excepciones));
        return new InformeCoberturaDto(desde, hasta, filas, total);
    }

    public async Task<ArchivoExportado> CoberturaCsvAsync(DateOnly desde, DateOnly hasta, Guid? servicioId, Guid? centroId, CancellationToken ct)
    {
        var informe = await CoberturaAsync(desde, hasta, servicioId, centroId, ct);
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', "Centro", "Servicio", "Plazas", "Cubiertas", "Libres", "% cobertura", "Vacantes abiertas", "Excepciones"));
        foreach (var x in informe.Filas.Append(informe.Total))
            sb.AppendLine(string.Join(';', new[]
            {
                x.Centro, x.Servicio, x.Plazas.ToString(Es), x.Cubiertas.ToString(Es), x.Libres.ToString(Es), x.PorcentajeCobertura.ToString("0.0", Es),
                x.VacantesAbiertas.ToString(Es), x.Excepciones.ToString(Es)
            }.Select(CuadrantesServicio.CeldaCsv)));
        return await ExportarAsync($"informe-cobertura-{desde:yyyyMMdd}-{hasta:yyyyMMdd}.csv", sb, "cobertura", desde, hasta, ct);
    }

    // ───────────── Internos ─────────────

    private async Task<ArchivoExportado> ExportarAsync(string nombre, StringBuilder sb, string informe, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        db.EventosSeguridad.Add(new EventoSeguridad(TipoEventoSeguridad.ExportacionDatos, reloj.GetUtcNow(), contexto.OrganizacionId, contexto.UsuarioId, null,
            contexto.Ip, $"Exportación CSV del informe de {informe} ({desde:yyyy-MM-dd} a {hasta:yyyy-MM-dd})"));
        await db.SaveChangesAsync(ct);
        var contenido = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return new ArchivoExportado(nombre, "text/csv; charset=utf-8", contenido);
    }

    private static string H(int minutos) => (minutos / 60.0).ToString("0.00", Es);
}
