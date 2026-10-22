using MedicalHub.Application.Comun;
using MedicalHub.Domain.Comun;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace MedicalHub.Api.Middleware;

/// <summary>Convierte excepciones en respuestas ProblemDetails (RFC 9457) sin filtrar detalles internos.</summary>
public sealed class ManejadorExcepciones(ILogger<ManejadorExcepciones> logger, IProblemDetailsService problemas) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        string? codigo = null;
        var (estado, titulo, detalle) = excepcion switch
        {
            ExcepcionValidacion v => (StatusCodes.Status400BadRequest, "Datos no válidos", v.Message),
            ExcepcionDominio d => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida", d.Message),
            ExcepcionNoAutenticado a => (StatusCodes.Status401Unauthorized, "No autenticado", a.Message),
            ExcepcionProhibido p => (StatusCodes.Status403Forbidden, "Acceso denegado", p.Message),
            ExcepcionNoEncontrado n => (StatusCodes.Status404NotFound, "No encontrado", n.Message),
            ExcepcionConflicto c => (StatusCodes.Status409Conflict, "Conflicto", c.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflicto de concurrencia",
                "Otro usuario ha modificado estos datos. Vuelva a cargarlos e inténtelo de nuevo."),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } => (StatusCodes.Status409Conflict, "Conflicto",
                "Ya existe un registro con esos datos."),
            _ => (StatusCodes.Status500InternalServerError, "Error interno", "Se ha producido un error inesperado.")
        };
        if (excepcion is ExcepcionNoAutenticado { Codigo: { } c2 }) codigo = c2;

        if (estado >= 500) logger.LogError(excepcion, "Error no controlado");
        else logger.LogInformation("Solicitud rechazada ({Estado}): {Mensaje}", estado, excepcion.Message);

        contexto.Response.StatusCode = estado;
        var problema = new ProblemDetails { Status = estado, Title = titulo, Detail = detalle };
        if (excepcion is ExcepcionValidacion val) problema.Extensions["errores"] = val.Errores;
        if (codigo is not null) problema.Extensions["codigo"] = codigo;

        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = problema,
            Exception = excepcion
        });
    }
}
