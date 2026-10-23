namespace MedicalHub.Api.IntegrationTests;

public static class EscenarioExtensiones
{
    /// <summary>Profesional elegible para el servicio, con usuario propio para entrar en la aplicación.</summary>
    public static async Task<(Guid profesionalId, HttpClient cliente, string email, string contrasena)> ProfesionalConUsuarioAsync(
        this Escenario e, string? nombre = null, Guid? categoria = null)
    {
        var pwd = AyudaSesion.ContrasenaValida("pu");
        var usuario = await Ayudas.CrearUsuarioAsync(e.Fabrica, e.Admin, "ProfesionalSanitario", contrasena: pwd);
        var id = await Planif.CrearProfesionalAsync(e.C, Planif.Profesional(categoria ?? e.Adjunto,
            competencias: [new { competenciaId = e.Competencia, vigenteHasta = (string?)null }], servicios: [e.Servicio], usuarioId: usuario.Id, nombre: nombre));
        var cliente = (await AyudaSesion.LoginAsync(e.Fabrica.CreateClient(), usuario.Email, pwd)).Cliente(e.Fabrica);
        return (id, cliente, usuario.Email, pwd);
    }
}
