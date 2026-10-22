using MedicalHub.Infrastructure.Seguridad;

namespace MedicalHub.Api.IntegrationTests;

public class TotpTests
{
    // Vectores de la RFC 6238 (SHA-1, secreto ASCII "12345678901234567890"), truncados a 6 dígitos.
    private const string SecretoRfc = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    [Theory]
    [InlineData(59, "287082")]
    [InlineData(1111111109, "081804")]
    [InlineData(1234567890, "005924")]
    public void Genera_los_codigos_de_la_RFC_6238(long segundos, string esperado)
        => Assert.Equal(esperado, ServicioTotp.Generar(SecretoRfc, segundos / 30));

    [Fact]
    public void Rechaza_pasos_ya_usados_y_acepta_la_ventana_de_mas_menos_uno()
    {
        var s = new ServicioTotp();
        var ahora = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var paso = ServicioTotp.PasoDe(ahora);

        Assert.True(s.Validar(SecretoRfc, ServicioTotp.Generar(SecretoRfc, paso + 1), ahora, 0, out var aceptado));
        Assert.Equal(paso + 1, aceptado);
        Assert.False(s.Validar(SecretoRfc, ServicioTotp.Generar(SecretoRfc, paso + 1), ahora, aceptado, out _));
        Assert.False(s.Validar(SecretoRfc, ServicioTotp.Generar(SecretoRfc, paso + 2), ahora, 0, out _));
        Assert.False(s.Validar(SecretoRfc, "abc", ahora, 0, out _));
    }
}
