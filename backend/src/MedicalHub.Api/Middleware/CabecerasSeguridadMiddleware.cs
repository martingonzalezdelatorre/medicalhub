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
        return siguiente(contexto);
    }
}
