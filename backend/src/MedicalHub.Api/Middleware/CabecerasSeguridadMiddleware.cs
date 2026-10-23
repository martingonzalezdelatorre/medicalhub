namespace MedicalHub.Api.Middleware;

public sealed class CabecerasSeguridadMiddleware(RequestDelegate siguiente)
{
    public Task InvokeAsync(HttpContext contexto)
    {
        var h = contexto.Response.Headers;
        h["X-Content-Type-Options"] = "nosniff";
        h["X-Frame-Options"] = "DENY";
        h["Referrer-Policy"] = "no-referrer";
        h["Cache-Control"] = "no-store";
        // La API solo devuelve JSON/CSV: no debe poder cargar ni ejecutar nada ni incrustarse en otra página.
        h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        h["Cross-Origin-Resource-Policy"] = "same-site";
        return siguiente(contexto);
    }
}
