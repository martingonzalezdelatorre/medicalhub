using MedicalHub.Application.Comun;
using MedicalHub.Domain.Comun;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MedicalHub.Api.Middleware;

/// <summary>Convierte excepciones en respuestas ProblemDetails (RFC 9457) sin filtrar detalles internos.</summary>
public sealed class ManejadorExcepciones(ILogger<ManejadorExcepciones> logger, IProblemDetailsService problemas) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        var (estado, titulo, detalle) = excepcion switch
        {
            ExcepcionValidacion v => (StatusCodes.Status400BadRequest, "Datos no válidos", v.Message),
            ExcepcionDominio d => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida", d.Message),
            ExcepcionNoEncontrado n => (StatusCodes.Status404NotFound, "No encontrado", n.Message),
            ExcepcionConflicto c => (StatusCodes.Status409Conflict, "Conflicto", c.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno", "Se ha producido un error inesperado.")
        };

        if (estado >= 500) logger.LogError(excepcion, "Error no controlado");
        else logger.LogInformation("Solicitud rechazada ({Estado}): {Mensaje}", estado, excepcion.Message);

        contexto.Response.StatusCode = estado;
        var problema = new ProblemDetails { Status = estado, Title = titulo, Detail = detalle };
        if (excepcion is ExcepcionValidacion val) problema.Extensions["errores"] = val.Errores;

        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = problema,
            Exception = excepcion
        });
    }
}
