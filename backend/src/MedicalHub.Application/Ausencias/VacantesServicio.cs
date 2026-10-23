using MedicalHub.Application.Comun;
using MedicalHub.Application.Planificacion;
using MedicalHub.Application.Reglas;
using MedicalHub.Domain.Planificacion;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Ausencias;

/// <summary>Plazas que han quedado sin cubrir (p. ej. por una ausencia aprobada) y su cobertura directa por el coordinador.</summary>
public sealed class VacantesServicio(IAppDbContext db, IContextoActual contexto, AsignacionesServicio asignaciones)
{
    public async Task<IReadOnlyList<VacanteDto>> ListarAsync(EstadoVacante? estado, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var q = db.Vacantes.AsQueryable();
        if (estado is { } e) q = q.Where(v => v.Estado == e);

        var filas = await (
            from v in q
            join t in db.Turnos on v.TurnoId equals t.Id
            join tt in db.TiposTurno on t.TipoTurnoId equals tt.Id
            join s in db.Servicios on t.ServicioId equals s.Id
            join c in db.Centros on s.CentroId equals c.Id
            join cu in db.Cuadrantes on t.CuadranteId equals cu.Id
            join n in db.Necesidades on v.NecesidadId equals n.Id
            join p in db.Profesionales on v.ProfesionalAusenteId equals p.Id
            orderby v.Estado, t.InicioUtc
            select new
            {
                v.Id, v.Estado, v.Motivo, v.FechaCreacion, v.FechaCierre, v.NecesidadId, v.TurnoId, v.AusenciaId, t.Fecha, t.InicioUtc, t.FinUtc,
                Tipo = tt.Nombre, Servicio = s.Nombre, Centro = c.Nombre, c.ZonaHoraria, CuadranteEstado = cu.Estado,
                n.CategoriaProfesionalId, Ausente = p.Nombre + " " + p.Apellidos
            }).Take(300).ToListAsync(ct);

        var categorias = await db.CategoriasProfesionales.ToDictionaryAsync(c => c.Id, c => c.Nombre, ct);
        return filas.Select(f => new VacanteDto(f.Id, f.Estado, f.Motivo, f.FechaCreacion, f.FechaCierre, f.NecesidadId, f.TurnoId, f.Fecha,
            $"{f.Tipo} {CalculadoraHorario.ALocal(f.ZonaHoraria, f.InicioUtc):HH\\:mm}–{CalculadoraHorario.ALocal(f.ZonaHoraria, f.FinUtc):HH\\:mm}",
            f.Servicio, f.Centro, f.CategoriaProfesionalId is { } cat ? categorias.GetValueOrDefault(cat) : null, f.Ausente, f.AusenciaId, f.CuadranteEstado)).ToList();
    }

    /// <summary>Asigna a un profesional la plaza de la vacante con las mismas reglas y excepciones que cualquier asignación.</summary>
    public async Task<ResultadoAsignacionDto> CubrirAsync(Guid vacanteId, SolicitudCubrirVacante s, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var vacante = await db.Vacantes.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vacanteId, ct) ?? throw new ExcepcionNoEncontrado("Vacante no encontrada.");
        return await asignaciones.AsignarAsync(vacante.NecesidadId, s.ProfesionalId, s.Excepcion, ct, vacanteId);
    }
}
