using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Identidad;

namespace MedicalHub.Domain.Tests;

public class UsuarioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Org = Guid.NewGuid();

    private static Usuario Nuevo(Guid? org = null, params Rol[] roles)
        => new(org ?? Org, "  Ana@Sociedad.ES ", "Ana", "hash", roles.Length == 0 ? [Rol.ProfesionalSanitario] : roles, Ahora);

    [Fact]
    public void El_correo_se_normaliza()
        => Assert.Equal("ana@sociedad.es", Nuevo().Email);

    [Theory]
    [InlineData("")]
    [InlineData("sin-arroba")]
    [InlineData("@x.es")]
    public void Correo_invalido_falla(string email)
        => Assert.Throws<ExcepcionDominio>(() => new Usuario(Org, email, "Ana", "h", [Rol.ProfesionalSanitario], Ahora));

    [Fact]
    public void Un_usuario_de_sociedad_sin_organizacion_falla()
        => Assert.Throws<ExcepcionDominio>(() => new Usuario(null, "a@b.es", "Ana", "h", [Rol.CoordinadorMedico], Ahora));

    [Fact]
    public void El_administrador_de_plataforma_no_puede_tener_organizacion_ni_otros_roles()
    {
        Assert.Throws<ExcepcionDominio>(() => new Usuario(Org, "a@b.es", "Ana", "h", [Rol.AdministradorPlataforma], Ahora));
        Assert.Throws<ExcepcionDominio>(() => new Usuario(null, "a@b.es", "Ana", "h", [Rol.AdministradorPlataforma, Rol.CoordinadorMedico], Ahora));
        Assert.NotNull(new Usuario(null, "a@b.es", "Ana", "h", [Rol.AdministradorPlataforma], Ahora));
    }

    [Fact]
    public void Sin_roles_falla()
        => Assert.Throws<ExcepcionDominio>(() => new Usuario(Org, "a@b.es", "Ana", "h", [], Ahora));

    [Fact]
    public void Cambiar_roles_conserva_los_existentes_y_retira_los_demas()
    {
        var u = Nuevo(null, Rol.ProfesionalSanitario, Rol.CoordinadorMedico);
        u.EstablecerRoles([Rol.CoordinadorMedico, Rol.ResponsableCentro]);
        Assert.Equal([Rol.CoordinadorMedico, Rol.ResponsableCentro], u.ListaRoles.OrderBy(r => r));
    }

    [Fact]
    public void Tras_el_maximo_de_fallos_la_cuenta_se_bloquea_y_se_desbloquea_con_el_tiempo()
    {
        var u = Nuevo();
        for (var i = 0; i < 4; i++) u.RegistrarFallo(Ahora, 5, TimeSpan.FromMinutes(15));
        Assert.False(u.EstaBloqueado(Ahora));

        u.RegistrarFallo(Ahora, 5, TimeSpan.FromMinutes(15));
        Assert.True(u.EstaBloqueado(Ahora.AddMinutes(14)));
        Assert.False(u.EstaBloqueado(Ahora.AddMinutes(16)));
    }

    [Fact]
    public void Un_acceso_correcto_reinicia_el_contador()
    {
        var u = Nuevo();
        u.RegistrarFallo(Ahora, 5, TimeSpan.FromMinutes(15));
        u.RegistrarAccesoCorrecto();
        Assert.Equal(0, u.IntentosFallidos);
    }

    [Theory]
    [InlineData(Rol.AdministradorSociedad, true, true)]
    [InlineData(Rol.AdministradorSociedad, false, false)]
    [InlineData(Rol.CoordinadorMedico, true, true)]
    [InlineData(Rol.ProfesionalSanitario, true, false)]
    [InlineData(Rol.ResponsableCentro, true, false)]
    [InlineData(Rol.RecursosHumanos, true, true)]
    public void La_exigencia_de_mfa_depende_del_rol_y_de_la_politica(Rol rol, bool politica, bool esperado)
        => Assert.Equal(esperado, Nuevo(null, rol).RequiereMfa(politica));

    [Fact]
    public void El_administrador_de_plataforma_siempre_requiere_mfa()
        => Assert.True(new Usuario(null, "a@b.es", "A", "h", [Rol.AdministradorPlataforma], Ahora).RequiereMfa(false));

    [Fact]
    public void Activar_mfa_sin_configuracion_previa_falla()
        => Assert.Throws<ExcepcionDominio>(() => Nuevo().ActivarMfa(5));
}
