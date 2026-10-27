namespace MedicalHub.Api.Seguridad;

/// <summary>
/// Cookie de refresco para la web. El token de refresco nunca llega al JavaScript del navegador: viaja en una cookie
/// <c>HttpOnly; SameSite=Strict</c> limitada a la ruta de autenticación. Es opt-in por petición (cabecera
/// <c>X-Sesion-Cookie: 1</c>), de modo que los clientes móviles y de escritorio siguen usando el cuerpo de la respuesta.
/// </summary>
/// <remarks>
/// Defensa frente a CSRF: la cookie solo se usa si la petición trae la cabecera personalizada (un formulario o una imagen de otro
/// sitio no puede añadirla y un <c>fetch</c> con ella exige pasar el CORS), y además es SameSite=Strict.
/// </remarks>
public static class SesionCookie
{
    public const string Cabecera = "X-Sesion-Cookie";
    public const string Nombre = "mh_refresh";
    public const string Ruta = "/api/autenticacion";

    public static bool EsModoCookie(HttpRequest r) => r.Headers[Cabecera] == "1";

    public static string? Leer(HttpRequest r) => r.Cookies.TryGetValue(Nombre, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

    public static void Escribir(HttpResponse r, string token, DateTimeOffset expira, bool segura)
        => r.Cookies.Append(Nombre, token, Opciones(segura, expira));

    public static void Borrar(HttpResponse r, bool segura)
        => r.Cookies.Delete(Nombre, Opciones(segura, null));

    private static CookieOptions Opciones(bool segura, DateTimeOffset? expira) => new()
    {
        HttpOnly = true,
        Secure = segura,
        SameSite = SameSiteMode.Strict,
        Path = Ruta,
        IsEssential = true,
        Expires = expira
    };
}
