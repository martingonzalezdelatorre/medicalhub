using MedicalHub.Application.Comun;
using MedicalHub.Application.Planificacion;
using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Identidad;
using MedicalHub.Domain.Planificacion;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Horas;

/// <summary>Indicadores de la pantalla de inicio según el rol. Todo se calcula en el servidor y respeta el alcance del usuario.</summary>
public sealed class DashboardServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj)
{
    public const int DiasVentana = 14;
    public const int MaxSinCubrir = 8;

    public async Task<DashboardDto> ObtenerAsync(CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        var ahora = reloj.GetUtcNow();
        var hoy = DateOnly.FromDateTime(ahora.UtcDateTime);

        var lectura = contexto.TieneRol(Rol.AdministradorSociedad) || contexto.TieneRol(Rol.CoordinadorMedico) || contexto.TieneRol(Rol.ResponsableCentro);
        var gestion = lectura ? await GestionAsync(hoy, ahora, ct) : null;

        var profesional = await db.Profesionales.Where(p => p.UsuarioId == uid).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
        var propio = profesional is { } pid ? await ProfesionalAsync(pid, hoy, ahora, ct) : null;
        return new DashboardDto(gestion, propio);
    }

    private async Task<DashboardGestionDto> GestionAsync(DateOnly hoy, DateTimeOffset ahora, CancellationToken ct)
    {
        var hasta = hoy.AddDays(DiasVentana - 1);
        var servicios = AlcancePlanificacion.Servicios(db, contexto);

        var turnos = from t in db.Turnos
                     where t.Fecha >= hoy && t.Fecha <= hasta
                     join c in db.Cuadrantes on t.CuadranteId equals c.Id
                     where c.Estado != EstadoCuadrante.Archivado
                     join s in servicios on t.ServicioId equals s.Id
                     select t;

        var plazas = await (from t in turnos join n in db.Necesidades on t.Id equals n.TurnoId group n by t.Id into g select new { TurnoId = g.Key, Total = g.Sum(n => n.NumeroPlazas) })
            .ToDictionaryAsync(x => x.TurnoId, x => x.Total, ct);
        var cubiertas = await (from t in turnos join a in db.Asignaciones on t.Id equals a.TurnoId where a.Estado != EstadoAsignacion.Cancelada
                               group a by t.Id into g select new { TurnoId = g.Key, Total = g.Count() }).ToDictionaryAsync(x => x.TurnoId, x => x.Total, ct);
        var libres = plazas.Select(p => (p.Key, Libres: p.Value - Math.Min(cubiertas.GetValueOrDefault(p.Key), p.Value))).Where(x => x.Libres > 0).ToDictionary(x => x.Key, x => x.Libres);

        var detalle = libres.Count == 0 ? [] : await (
            from t in db.Turnos where libres.Keys.Contains(t.Id)
            join s in db.Servicios on t.ServicioId equals s.Id
            join c in db.Centros on s.CentroId equals c.Id
            join tt in db.TiposTurno on t.TipoTurnoId equals tt.Id
            orderby t.InicioUtc
            select new { t.Id, t.Fecha, t.InicioUtc, t.FinUtc, Tipo = tt.Nombre, Servicio = s.Nombre, Centro = c.Nombre, c.ZonaHoraria }).Take(MaxSinCubrir).ToListAsync(ct);
        var sinCubrir = detalle.Select(d => new TurnoSinCubrirDto(d.Id, d.Fecha,
            $"{d.Tipo} {CalculadoraHorario.ALocal(d.ZonaHoraria, d.InicioUtc):HH\\:mm}–{CalculadoraHorario.ALocal(d.ZonaHoraria, d.FinUtc):HH\\:mm}", d.Servicio, d.Centro, libres[d.Id])).ToList();

        var borradores = await (from c in db.Cuadrantes where c.Estado == EstadoCuadrante.Borrador && c.Hasta >= hoy join s in servicios on c.ServicioId equals s.Id select c.Id).CountAsync(ct);

        PendientesGestionDto? pendientes = null;
        if (contexto.TieneRol(Rol.AdministradorSociedad) || contexto.TieneRol(Rol.CoordinadorMedico))
        {
            var desde30 = ahora.AddDays(-30);
            pendientes = new PendientesGestionDto(
                await db.Vacantes.CountAsync(v => v.Estado == EstadoVacante.Abierta, ct),
                await db.Ausencias.CountAsync(a => a.Estado == EstadoAusencia.Solicitada, ct),
                await db.Ofertas.CountAsync(o => o.Estado == EstadoOferta.Aceptada, ct),
                await db.Disponibilidades.CountAsync(d => d.Estado == EstadoDisponibilidad.Declarada, ct),
                await db.Excepciones.CountAsync(e => e.Fecha >= desde30, ct));
        }
        return new DashboardGestionDto(hoy, hasta, plazas.Values.Sum(), plazas.Keys.Sum(k => Math.Min(cubiertas.GetValueOrDefault(k), plazas[k])), borradores, sinCubrir, pendientes);
    }

    private async Task<DashboardProfesionalDto> ProfesionalAsync(Guid profesionalId, DateOnly hoy, DateTimeOffset ahora, CancellationToken ct)
    {
        var proximos = await (
            from a in db.Asignaciones
            where a.ProfesionalId == profesionalId && a.Estado != EstadoAsignacion.Cancelada && a.FinUtc > ahora
            join t in db.Turnos on a.TurnoId equals t.Id
            join cu in db.Cuadrantes on t.CuadranteId equals cu.Id
            join s in db.Servicios on t.ServicioId equals s.Id
            join c in db.Centros on s.CentroId equals c.Id
            join tt in db.TiposTurno on t.TipoTurnoId equals tt.Id
            orderby a.InicioUtc
            select new { a.Id, t.Fecha, a.InicioUtc, a.FinUtc, Tipo = tt.Nombre, Servicio = s.Nombre, Centro = c.Nombre, c.ZonaHoraria, Estado = cu.Estado }).Take(5).ToListAsync(ct);

        var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
        var finMes = inicioMes.AddMonths(1).AddDays(-1);
        var mes = await (from a in db.Asignaciones where a.ProfesionalId == profesionalId && a.Estado != EstadoAsignacion.Cancelada
                         join t in db.Turnos on a.TurnoId equals t.Id where t.Fecha >= inicioMes && t.Fecha <= finMes select new { a.InicioUtc, a.FinUtc }).ToListAsync(ct);

        return new DashboardProfesionalDto(
            proximos.Select(p => new ProximoTurnoDto(p.Id, p.Fecha,
                $"{p.Tipo} {CalculadoraHorario.ALocal(p.ZonaHoraria, p.InicioUtc):HH\\:mm}–{CalculadoraHorario.ALocal(p.ZonaHoraria, p.FinUtc):HH\\:mm}", p.Servicio, p.Centro,
                p.InicioUtc, p.FinUtc, p.Estado)).ToList(),
            mes.Sum(m => (int)(m.FinUtc - m.InicioUtc).TotalMinutes),
            await db.Ofertas.CountAsync(o => o.ProfesionalId == profesionalId && o.Estado == EstadoOferta.Pendiente && (o.Caduca == null || o.Caduca > ahora), ct),
            await db.Ausencias.CountAsync(a => a.ProfesionalId == profesionalId && a.Estado == EstadoAusencia.Solicitada, ct));
    }
}
