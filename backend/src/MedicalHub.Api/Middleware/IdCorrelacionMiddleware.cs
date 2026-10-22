using Serilog.Context;

namespace MedicalHub.Api.Middleware;

public sealed class IdCorrelacionMiddleware(RequestDelegate siguiente)
{
    public const string Cabecera = "X-Correlation-Id";
    public const string ClaveItem = "IdCorrelacion";

    public async Task InvokeAsync(HttpContext contexto)
    {
        var id = contexto.Request.Headers[Cabecera].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64) id = Guid.NewGuid().ToString("N");

        contexto.Items[ClaveItem] = id;
        contexto.Response.Headers[Cabecera] = id;
        using (LogContext.PushProperty("IdCorrelacion", id))
            await siguiente(contexto);
    }
}
