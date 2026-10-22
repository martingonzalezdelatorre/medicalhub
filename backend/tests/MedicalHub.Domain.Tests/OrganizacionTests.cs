using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Organizaciones;

namespace MedicalHub.Domain.Tests;

public class OrganizacionTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Crear_con_datos_validos_queda_activa()
    {
        var org = new Organizacion("  Anestesia Madrid  ", "Europe/Madrid", Ahora);

        Assert.Equal("Anestesia Madrid", org.Nombre);
        Assert.Equal("Europe/Madrid", org.ZonaHoraria);
        Assert.True(org.Activa);
        Assert.Equal(Ahora, org.FechaCreacion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_sin_nombre_falla(string nombre)
        => Assert.Throws<ExcepcionDominio>(() => new Organizacion(nombre, "Europe/Madrid", Ahora));

    [Theory]
    [InlineData("")]
    [InlineData("Marte/Olimpo")]
    public void Crear_con_zona_horaria_invalida_falla(string zona)
        => Assert.Throws<ExcepcionDominio>(() => new Organizacion("Sociedad", zona, Ahora));

    [Fact]
    public void Desactivar_marca_la_organizacion_como_inactiva()
    {
        var org = new Organizacion("Sociedad", "Atlantic/Canary", Ahora);
        org.Desactivar();
        Assert.False(org.Activa);
    }
}
