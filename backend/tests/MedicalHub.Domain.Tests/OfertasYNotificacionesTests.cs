using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Notificaciones;
using MedicalHub.Domain.Planificacion;

namespace MedicalHub.Domain.Tests;

public class OfertasYNotificacionesTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 23, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.NewGuid();
    private static readonly Guid Usuario = Guid.NewGuid();

    private static OfertaSustitucion Oferta(DateTimeOffset? caduca = null) => new(Org, Guid.NewGuid(), Guid.NewGuid(), Usuario, Ahora, caduca);

    [Fact]
    public void Una_oferta_nueva_esta_pendiente_y_abierta()
    {
        var o = Oferta();
        Assert.Equal(EstadoOferta.Pendiente, o.Estado);
        Assert.True(o.EstaAbierta);
    }

    [Fact]
    public void La_caducidad_debe_ser_futura()
        => Assert.Throws<ExcepcionDominio>(() => Oferta(Ahora));

    [Fact]
    public void Una_oferta_pendiente_vencida_cuenta_como_caducada_y_no_puede_aceptarse()
    {
        var o = Oferta(Ahora.AddHours(2));
        var tarde = Ahora.AddHours(3);
        Assert.Equal(EstadoOferta.Pendiente, o.EstadoEfectivo(Ahora.AddHours(1)));
        Assert.Equal(EstadoOferta.Caducada, o.EstadoEfectivo(tarde));
        Assert.Throws<ExcepcionDominio>(() => o.Aceptar(tarde, null));
        o.Caducar(tarde);
        Assert.Equal(EstadoOferta.Caducada, o.Estado);
    }

    [Fact]
    public void Aceptada_puede_aprobarse_o_denegarse_pero_solo_una_vez()
    {
        var o = Oferta();
        o.Aceptar(Ahora, "Disponible");
        Assert.Equal("Disponible", o.MotivoRespuesta);
        var asignacion = Guid.NewGuid();
        o.Aprobar(Usuario, asignacion, Ahora);
        Assert.Equal(EstadoOferta.Aprobada, o.Estado);
        Assert.Equal(asignacion, o.AsignacionId);
        Assert.False(o.EstaAbierta);
        Assert.Throws<ExcepcionDominio>(() => o.Aprobar(Usuario, asignacion, Ahora));
        Assert.Throws<ExcepcionDominio>(() => o.Denegar(Usuario, Ahora, "x"));
    }

    [Fact]
    public void No_se_puede_aprobar_una_oferta_que_no_ha_sido_aceptada()
    {
        var o = Oferta();
        Assert.Throws<ExcepcionDominio>(() => o.Aprobar(Usuario, Guid.NewGuid(), Ahora));
        o.Rechazar(Ahora, null);
        Assert.Throws<ExcepcionDominio>(() => o.Aprobar(Usuario, Guid.NewGuid(), Ahora));
        Assert.Throws<ExcepcionDominio>(() => o.Aceptar(Ahora, null));
    }

    [Fact]
    public void Denegar_exige_motivo_y_retirar_solo_vale_si_esta_abierta()
    {
        var o = Oferta();
        o.Aceptar(Ahora, null);
        Assert.Throws<ExcepcionDominio>(() => o.Denegar(Usuario, Ahora, " "));
        o.Denegar(Usuario, Ahora, "Otro perfil");
        Assert.Equal("Otro perfil", o.MotivoDecision);
        Assert.Throws<ExcepcionDominio>(() => o.Retirar(Usuario, Ahora, null));
    }

    [Fact]
    public void Cerrar_ignora_las_ofertas_que_ya_no_estan_abiertas_y_limita_el_motivo()
    {
        var rechazada = Oferta();
        rechazada.Rechazar(Ahora, null);
        rechazada.Cerrar(Ahora, "La vacante se ha cubierto");
        Assert.Equal(EstadoOferta.Rechazada, rechazada.Estado);

        var pendiente = Oferta();
        Assert.Throws<ExcepcionDominio>(() => pendiente.Cerrar(Ahora, new string('x', OfertaSustitucion.MaxMotivo + 1)));
        pendiente.Cerrar(Ahora, "Cubierta");
        Assert.Equal(EstadoOferta.Cerrada, pendiente.Estado);
    }

    [Fact]
    public void Las_notificaciones_validan_sus_textos_y_se_marcan_leidas_una_sola_vez()
    {
        var n = new Notificacion(Org, Usuario, TipoNotificacion.OfertaRecibida, "Oferta", "Se le ofrece un turno.", Guid.NewGuid(), Ahora);
        Assert.False(n.Leida);
        n.MarcarLeida(Ahora);
        n.MarcarLeida(Ahora.AddHours(1));
        Assert.Equal(Ahora, n.FechaLectura);

        Assert.Throws<ExcepcionDominio>(() => new Notificacion(Org, Usuario, TipoNotificacion.OfertaRecibida, " ", "x", null, Ahora));
        Assert.Throws<ExcepcionDominio>(() => new Notificacion(Org, Usuario, TipoNotificacion.OfertaRecibida, "x", new string('x', Notificacion.MaxMensaje + 1), null, Ahora));
        Assert.Throws<ExcepcionDominio>(() => new Notificacion(Org, Usuario, (TipoNotificacion)999, "x", "y", null, Ahora));
    }
}
