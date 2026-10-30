using MedicalHub.Application.Comun;
using MedicalHub.Domain.Estructura;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

/// <summary>Coste por turno de cada perfil profesional. Se edita por servicio: cada guardado sustituye las tarifas de sus tipos de turno.</summary>
public sealed class TarifasServicio(IAppDbContext db, IContextoActual contexto)
{
    public async Task<IReadOnlyList<TarifaDto>> ListarAsync(Guid servicioId, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        await ComprobarServicioAsync(servicioId, ct);
        var tipos = db.TiposTurno.Where(t => t.ServicioId == servicioId).Select(t => t.Id);
        return await db.TarifasTurno.Where(t => tipos.Contains(t.TipoTurnoId))
            .Select(t => new TarifaDto(t.TipoTurnoId, t.CategoriaProfesionalId, t.Importe)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TarifaDto>> ReemplazarAsync(Guid servicioId, SolicitudTarifas s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        await ComprobarServicioAsync(servicioId, ct);
        var lineas = s.Tarifas ?? [];

        var v = new Validador();
        v.Cuando(lineas.GroupBy(l => (l.TipoTurnoId, l.CategoriaProfesionalId)).Any(g => g.Count() > 1),
            nameof(s.Tarifas), "Hay tarifas duplicadas para el mismo turno y perfil.");
        v.Cuando(lineas.Any(l => l.Importe < 0 || l.Importe > TarifaTurno.ImporteMaximo),
            nameof(s.Tarifas), $"Los importes deben estar entre 0 y {TarifaTurno.ImporteMaximo:N0}.");
        var tiposServicio = await db.TiposTurno.Where(t => t.ServicioId == servicioId).Select(t => t.Id).ToListAsync(ct);
        v.Cuando(lineas.Any(l => !tiposServicio.Contains(l.TipoTurnoId)), nameof(s.Tarifas), "Todos los turnos deben pertenecer al servicio.");
        await db.CategoriasProfesionales.ExigirExistentesAsync(lineas.Select(l => l.CategoriaProfesionalId), nameof(s.Tarifas), v, ct);
        v.LanzarSiHayErrores();

        var existentes = await db.TarifasTurno.Where(t => tiposServicio.Contains(t.TipoTurnoId)).ToListAsync(ct);
        foreach (var t in existentes.Where(t => !lineas.Any(l => l.TipoTurnoId == t.TipoTurnoId && l.CategoriaProfesionalId == t.CategoriaProfesionalId)))
            db.TarifasTurno.Remove(t);
        foreach (var l in lineas)
        {
            var actual = existentes.FirstOrDefault(t => t.TipoTurnoId == l.TipoTurnoId && t.CategoriaProfesionalId == l.CategoriaProfesionalId);
            if (actual is null) db.TarifasTurno.Add(new TarifaTurno(org, l.TipoTurnoId, l.CategoriaProfesionalId, l.Importe));
            else actual.CambiarImporte(l.Importe);
        }
        await db.SaveChangesAsync(ct);
        return await ListarAsync(servicioId, ct);
    }

    private async Task ComprobarServicioAsync(Guid servicioId, CancellationToken ct)
    {
        if (!await db.Servicios.AnyAsync(s => s.Id == servicioId, ct)) throw new ExcepcionNoEncontrado("Servicio no encontrado.");
    }
}
