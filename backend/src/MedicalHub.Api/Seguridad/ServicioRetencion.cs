using MedicalHub.Application.Retencion;
using Microsoft.Extensions.Options;

namespace MedicalHub.Api.Seguridad;

/// <summary>Ejecuta periódicamente la depuración de datos caducados según la política de retención configurada.</summary>
public sealed class ServicioRetencion(IServiceScopeFactory ambitos, IOptions<OpcionesRetencion> opciones, ILogger<ServicioRetencion> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var o = opciones.Value;
        if (!o.Habilitada) return;
        // Pequeña espera inicial para no competir con el arranque (migraciones, alta del administrador).
        try { await Task.Delay(TimeSpan.FromMinutes(1), ct); } catch (OperationCanceledException) { return; }

        using var temporizador = new PeriodicTimer(TimeSpan.FromHours(Math.Max(1, o.IntervaloHoras)));
        do
        {
            try
            {
                using var ambito = ambitos.CreateScope();
                var r = await ambito.ServiceProvider.GetRequiredService<RetencionServicio>().DepurarAsync(o, ct);
                if (r.Total > 0) log.LogInformation("Depuración de retención: {@Resultado}", r);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Fallo en la depuración de datos caducados");
            }
        }
        while (await SiguienteAsync(temporizador, ct));
    }

    private static async Task<bool> SiguienteAsync(PeriodicTimer t, CancellationToken ct)
    {
        try { return await t.WaitForNextTickAsync(ct); } catch (OperationCanceledException) { return false; }
    }
}
