using MedicalHub.Application.Comun;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Retencion;

/// <summary>
/// Plazos de conservación (minimización de datos). Un valor 0 desactiva la depuración de ese tipo de dato.
/// Los plazos por defecto son orientativos: cada sociedad debe fijarlos según su política de retención.
/// </summary>
public sealed class OpcionesRetencion
{
    public const string Seccion = "Retencion";

    public bool Habilitada { get; set; } = true;
    /// <summary>Cada cuántas horas se ejecuta la depuración.</summary>
    public int IntervaloHoras { get; set; } = 24;
    /// <summary>Notificaciones ya leídas.</summary>
    public int NotificacionesLeidasDias { get; set; } = 90;
    /// <summary>Cualquier notificación, leída o no.</summary>
    public int NotificacionesDias { get; set; } = 365;
    /// <summary>Eventos de seguridad (inicios de sesión, exportaciones…).</summary>
    public int EventosSeguridadDias { get; set; } = 365;
    /// <summary>Tokens de refresco, contados desde su caducidad.</summary>
    public int TokensRefrescoDias { get; set; } = 30;
    /// <summary>Registros de auditoría de cambios.</summary>
    public int AuditoriaDias { get; set; } = 1825;
}

public sealed record ResultadoDepuracion(int Notificaciones, int EventosSeguridad, int TokensRefresco, int Auditoria)
{
    public int Total => Notificaciones + EventosSeguridad + TokensRefresco + Auditoria;
}

/// <summary>Borra de forma definitiva los datos que han superado su plazo de conservación, para todas las sociedades.</summary>
public sealed class RetencionServicio(IAppDbContext db, TimeProvider reloj)
{
    public async Task<ResultadoDepuracion> DepurarAsync(OpcionesRetencion o, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        int notif = 0, eventos = 0, tokens = 0, auditoria = 0;

        // Proceso de mantenimiento del sistema: no hay sociedad actual, así que se ignora el filtro de organización.
        if (o.NotificacionesLeidasDias > 0)
        {
            var corte = ahora.AddDays(-o.NotificacionesLeidasDias);
            notif += await db.Notificaciones.IgnoreQueryFilters().Where(n => n.FechaLectura != null && n.FechaLectura < corte).ExecuteDeleteAsync(ct);
        }
        if (o.NotificacionesDias > 0)
        {
            var corte = ahora.AddDays(-o.NotificacionesDias);
            notif += await db.Notificaciones.IgnoreQueryFilters().Where(n => n.Fecha < corte).ExecuteDeleteAsync(ct);
        }
        if (o.EventosSeguridadDias > 0)
        {
            var corte = ahora.AddDays(-o.EventosSeguridadDias);
            eventos = await db.EventosSeguridad.Where(e => e.Fecha < corte).ExecuteDeleteAsync(ct);
        }
        if (o.TokensRefrescoDias > 0)
        {
            var corte = ahora.AddDays(-o.TokensRefrescoDias);
            tokens = await db.TokensRefresco.Where(t => t.Expira < corte).ExecuteDeleteAsync(ct);
        }
        if (o.AuditoriaDias > 0)
        {
            var corte = ahora.AddDays(-o.AuditoriaDias);
            auditoria = await db.RegistrosAuditoria.IgnoreQueryFilters().Where(a => a.Fecha < corte).ExecuteDeleteAsync(ct);
        }
        return new ResultadoDepuracion(notif, eventos, tokens, auditoria);
    }
}
