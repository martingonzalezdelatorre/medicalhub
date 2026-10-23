using MedicalHub.Application.Comun;
using MedicalHub.Domain.Identidad;
using MedicalHub.Domain.Notificaciones;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Notificaciones;

public sealed record NotificacionDto(Guid Id, TipoNotificacion Tipo, string Titulo, string Mensaje, Guid? EntidadId, DateTimeOffset Fecha, bool Leida);

public sealed record ResumenNotificacionesDto(int NoLeidas);

/// <summary>
/// Avisos dentro de la aplicación. Los métodos «Crear…» solo añaden al contexto: el llamador los guarda en la misma
/// transacción que el hecho que notifican. Cada usuario solo ve y modifica las suyas.
/// </summary>
public sealed class NotificacionesServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj)
{
    public const int MaxListado = 100;

    // ───────────── Creación (desde otros servicios) ─────────────

    public void Crear(Guid organizacionId, Guid usuarioId, TipoNotificacion tipo, string titulo, string mensaje, Guid? entidadId)
        => db.Notificaciones.Add(new Notificacion(organizacionId, usuarioId, tipo, titulo, mensaje, entidadId, reloj.GetUtcNow()));

    /// <summary>Avisa a los usuarios con capacidad de gestión de la organización (administradores y coordinadores activos).</summary>
    public async Task CrearParaCoordinacionAsync(Guid organizacionId, TipoNotificacion tipo, string titulo, string mensaje, Guid? entidadId,
        CancellationToken ct, Guid? excepto = null)
    {
        var ids = await db.Usuarios
            .Where(u => u.Activo && u.Roles.Any(r => r.Rol == Rol.CoordinadorMedico || r.Rol == Rol.AdministradorSociedad))
            .Select(u => u.Id).ToListAsync(ct);
        foreach (var id in ids.Where(i => i != excepto))
            Crear(organizacionId, id, tipo, titulo, mensaje, entidadId);
    }

    /// <summary>Avisa al usuario vinculado a un profesional, si lo tiene.</summary>
    public async Task CrearParaProfesionalAsync(Guid organizacionId, Guid profesionalId, TipoNotificacion tipo, string titulo, string mensaje, Guid? entidadId,
        CancellationToken ct, Guid? excepto = null)
    {
        var usuarioId = await db.Profesionales.Where(p => p.Id == profesionalId).Select(p => p.UsuarioId).FirstOrDefaultAsync(ct);
        if (usuarioId is { } u && u != excepto) Crear(organizacionId, u, tipo, titulo, mensaje, entidadId);
    }

    // ───────────── Consulta (usuario actual) ─────────────

    public async Task<IReadOnlyList<NotificacionDto>> ListarAsync(bool soloNoLeidas, CancellationToken ct)
    {
        var uid = Usuario();
        var q = db.Notificaciones.Where(n => n.UsuarioId == uid);
        if (soloNoLeidas) q = q.Where(n => n.FechaLectura == null);
        var lista = await q.OrderByDescending(n => n.Fecha).Take(MaxListado).ToListAsync(ct);
        return lista.Select(Dto).ToList();
    }

    public async Task<ResumenNotificacionesDto> ResumenAsync(CancellationToken ct)
    {
        var uid = Usuario();
        return new ResumenNotificacionesDto(await db.Notificaciones.CountAsync(n => n.UsuarioId == uid && n.FechaLectura == null, ct));
    }

    public async Task MarcarLeidaAsync(Guid id, CancellationToken ct)
    {
        var uid = Usuario();
        var n = await db.Notificaciones.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == uid, ct) ?? throw new ExcepcionNoEncontrado("Notificación no encontrada.");
        n.MarcarLeida(reloj.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> MarcarTodasLeidasAsync(CancellationToken ct)
    {
        var uid = Usuario();
        var pendientes = await db.Notificaciones.Where(n => n.UsuarioId == uid && n.FechaLectura == null).Take(500).ToListAsync(ct);
        var ahora = reloj.GetUtcNow();
        foreach (var n in pendientes) n.MarcarLeida(ahora);
        await db.SaveChangesAsync(ct);
        return pendientes.Count;
    }

    private Guid Usuario()
    {
        contexto.ExigirOrganizacion();
        return contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
    }

    private static NotificacionDto Dto(Notificacion n) => new(n.Id, n.Tipo, n.Titulo, n.Mensaje, n.EntidadId, n.Fecha, n.Leida);
}
