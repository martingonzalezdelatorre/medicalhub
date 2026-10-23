using System.Net.Http.Json;
using MedicalHub.Application.Comun;
using MedicalHub.Application.Retencion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalHub.Api.IntegrationTests;

[Collection(ColeccionApi.Nombre)]
public class RetencionTests(FabricaApi fabrica)
{
    private sealed class RelojAdelantado(TimeSpan desfase) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + desfase;
    }

    private static async Task<(int Notificaciones, int Eventos)> ContarAsync(IAppDbContext db, Guid org)
        => (await db.Notificaciones.IgnoreQueryFilters().CountAsync(n => n.OrganizacionId == org),
            await db.EventosSeguridad.CountAsync(e => e.OrganizacionId == org));

    [Fact]
    public async Task La_depuracion_borra_solo_lo_que_supera_su_plazo_y_respeta_los_desactivados()
    {
        var e = await Escenario.CrearAsync(fabrica);
        await e.CuadranteAsync("2026-11-02", "2026-11-08");
        var (prof, cliente, _, _) = await e.ProfesionalConUsuarioAsync();
        await e.AsignarOkAsync(await e.NecesidadAsync("2026-11-03", e.Manana), prof);
        await cliente.PostAsJsonAsync("/api/mis-ausencias", new { tipo = "Vacaciones", desde = "2026-11-03", hasta = "2026-11-03" });   // genera notificaciones
        await e.C.GetAsync("/api/cuadrantes");                                                                                          // asegura eventos de sesión

        using var ambito = fabrica.Services.CreateScope();
        var db = ambito.ServiceProvider.GetRequiredService<IAppDbContext>();
        var orgId = await db.Notificaciones.IgnoreQueryFilters().OrderByDescending(n => n.Fecha).Select(n => n.OrganizacionId!.Value).FirstAsync();
        var (notifAntes, eventosAntes) = await ContarAsync(db, orgId);
        Assert.True(notifAntes > 0);
        Assert.True(eventosAntes > 0);

        // Ahora mismo no hay nada que haya superado su plazo
        var ahora = await new RetencionServicio(db, TimeProvider.System).DepurarAsync(new OpcionesRetencion(), CancellationToken.None);
        Assert.Equal(0, ahora.Notificaciones);
        Assert.Equal((notifAntes, eventosAntes), await ContarAsync(db, orgId));

        // Con todos los plazos a 0 no se borra nada aunque pase el tiempo
        var desactivado = new OpcionesRetencion { NotificacionesLeidasDias = 0, NotificacionesDias = 0, EventosSeguridadDias = 0, TokensRefrescoDias = 0, AuditoriaDias = 0 };
        Assert.Equal(0, (await new RetencionServicio(db, new RelojAdelantado(TimeSpan.FromDays(4000))).DepurarAsync(desactivado, CancellationToken.None)).Total);
        Assert.Equal((notifAntes, eventosAntes), await ContarAsync(db, orgId));

        // Dentro de dos años: las notificaciones (365 d) y los eventos de seguridad (365 d) ya han caducado
        var futuro = await new RetencionServicio(db, new RelojAdelantado(TimeSpan.FromDays(800))).DepurarAsync(new OpcionesRetencion { TokensRefrescoDias = 0 }, CancellationToken.None);   // sin tocar tokens: otras pruebas reutilizan sesiones
        Assert.True(futuro.Notificaciones >= notifAntes);
        Assert.True(futuro.EventosSeguridad >= eventosAntes);
        Assert.Equal(0, futuro.TokensRefresco);
        Assert.Equal(0, (await ContarAsync(db, orgId)).Notificaciones);
    }
}
