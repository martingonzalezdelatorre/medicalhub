using MedicalHub.Api.Seguridad;
using MedicalHub.Application.Ausencias;
using MedicalHub.Application.Notificaciones;
using MedicalHub.Application.Reglas;
using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Planificacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalHub.Api.Controllers;

[ApiController]
public sealed class AusenciasController(AusenciasServicio servicio) : ControllerBase
{
    // ── Del propio profesional ──

    [HttpGet("api/mis-ausencias")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<IReadOnlyList<AusenciaDto>>> Mias(CancellationToken ct) => Ok(await servicio.MisAusenciasAsync(ct));

    /// <summary>Solicita una ausencia. No indique diagnósticos ni datos de salud en el comentario.</summary>
    [HttpPost("api/mis-ausencias")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<AusenciaDto>> Solicitar(SolicitudAusencia solicitud, CancellationToken ct)
        => Created("/api/mis-ausencias", await servicio.SolicitarPropiaAsync(solicitud, ct));

    [HttpPost("api/mis-ausencias/{id:guid}/cancelar")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<AusenciaDto>> CancelarPropia(Guid id, CancellationToken ct) => await servicio.CancelarPropiaAsync(id, ct);

    // ── Gestión ──

    [HttpGet("api/ausencias")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<AusenciaDto>>> Listar([FromQuery] EstadoAusencia? estado, [FromQuery] Guid? profesionalId,
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
        => Ok(await servicio.ListarAsync(estado, profesionalId, desde, hasta, ct));

    [HttpGet("api/ausencias/{id:guid}")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<AusenciaDto>> Obtener(Guid id, CancellationToken ct) => await servicio.ObtenerAsync(id, ct);

    [HttpPost("api/ausencias")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<ResultadoAusenciaDto>> Registrar(SolicitudAusenciaDeProfesional solicitud, CancellationToken ct)
        => Created("/api/ausencias", await servicio.RegistrarAsync(solicitud, ct));

    /// <summary>Turnos futuros del profesional que caen dentro de la ausencia.</summary>
    [HttpGet("api/ausencias/{id:guid}/comprometidos")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<TurnoComprometidoDto>>> Comprometidos(Guid id, CancellationToken ct) => Ok(await servicio.ComprometidosAsync(id, ct));

    /// <summary>Aprueba la ausencia: cancela las asignaciones afectadas y abre una vacante por cada plaza liberada.</summary>
    [HttpPost("api/ausencias/{id:guid}/aprobar")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<ResultadoAusenciaDto>> Aprobar(Guid id, SolicitudDecision? solicitud, CancellationToken ct)
        => await servicio.AprobarAsync(id, solicitud?.Motivo, ct);

    [HttpPost("api/ausencias/{id:guid}/rechazar")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<ResultadoAusenciaDto>> Rechazar(Guid id, SolicitudDecision solicitud, CancellationToken ct)
        => await servicio.RechazarAsync(id, solicitud.Motivo, ct);

    [HttpPost("api/ausencias/{id:guid}/cancelar")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<AusenciaDto>> Cancelar(Guid id, SolicitudDecision? solicitud, CancellationToken ct)
        => await servicio.CancelarAsync(id, solicitud?.Motivo, ct);
}

[ApiController]
public sealed class DisponibilidadController(DisponibilidadServicio servicio) : ControllerBase
{
    [HttpGet("api/mi-disponibilidad")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<IReadOnlyList<DisponibilidadDto>>> Mia(CancellationToken ct) => Ok(await servicio.MiDisponibilidadAsync(ct));

    [HttpPost("api/mi-disponibilidad")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<DisponibilidadDto>> Declarar(SolicitudDisponibilidad solicitud, CancellationToken ct)
        => Created("/api/mi-disponibilidad", await servicio.DeclararAsync(solicitud, ct));

    [HttpDelete("api/mi-disponibilidad/{id:guid}")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<IActionResult> EliminarPropia(Guid id, CancellationToken ct)
    {
        await servicio.EliminarPropiaAsync(id, ct);
        return NoContent();
    }

    [HttpGet("api/mis-tipos-turno")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<IReadOnlyList<TipoTurnoPropioDto>>> MisTiposTurno(CancellationToken ct) => Ok(await servicio.MisTiposTurnoAsync(ct));

    [HttpGet("api/disponibilidades")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<DisponibilidadDto>>> Listar([FromQuery] EstadoDisponibilidad? estado, [FromQuery] Guid? profesionalId, CancellationToken ct)
        => Ok(await servicio.ListarAsync(estado, profesionalId, ct));

    [HttpPost("api/disponibilidades")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<DisponibilidadDto>> Registrar(SolicitudDisponibilidadDeProfesional solicitud, CancellationToken ct)
        => Created("/api/disponibilidades", await servicio.RegistrarAsync(solicitud, ct));

    [HttpPost("api/disponibilidades/{id:guid}/aprobar")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<DisponibilidadDto>> Aprobar(Guid id, SolicitudDecision? solicitud, CancellationToken ct)
        => await servicio.AprobarAsync(id, solicitud?.Motivo, ct);

    [HttpPost("api/disponibilidades/{id:guid}/rechazar")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<DisponibilidadDto>> Rechazar(Guid id, SolicitudDecision? solicitud, CancellationToken ct)
        => await servicio.RechazarAsync(id, solicitud?.Motivo, ct);

    [HttpDelete("api/disponibilidades/{id:guid}")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken ct)
    {
        await servicio.EliminarAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/vacantes")]
public sealed class VacantesController(VacantesServicio servicio) : ControllerBase
{
    [HttpGet]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<VacanteDto>>> Listar([FromQuery] EstadoVacante? estado, CancellationToken ct)
        => Ok(await servicio.ListarAsync(estado, ct));

    /// <summary>Cubre la vacante asignando a un profesional (también en cuadrantes ya publicados). Aplica las reglas y excepciones.</summary>
    [HttpPost("{id:guid}/cubrir")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<ResultadoAsignacionDto>> Cubrir(Guid id, SolicitudCubrirVacante solicitud, CancellationToken ct)
        => await servicio.CubrirAsync(id, solicitud, ct);
}

[ApiController]
public sealed class OfertasController(OfertasServicio servicio) : ControllerBase
{
    // ── Coordinación ──

    [HttpGet("api/ofertas")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<OfertaDto>>> Listar([FromQuery] EstadoOferta? estado, [FromQuery] Guid? vacanteId, CancellationToken ct)
        => Ok(await servicio.ListarAsync(estado, vacanteId, ct));

    /// <summary>Ofrece la vacante a uno o varios profesionales (solo a quienes podrían ser asignados).</summary>
    [HttpPost("api/vacantes/{id:guid}/ofertas")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<IReadOnlyList<OfertaDto>>> Ofrecer(Guid id, SolicitudOfrecer solicitud, CancellationToken ct)
        => Created("/api/ofertas", await servicio.OfrecerAsync(id, solicitud, ct));

    [HttpPost("api/ofertas/{id:guid}/retirar")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> Retirar(Guid id, SolicitudDecision? solicitud, CancellationToken ct)
    {
        await servicio.RetirarAsync(id, solicitud?.Motivo, ct);
        return NoContent();
    }

    [HttpPost("api/ofertas/{id:guid}/denegar")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<IActionResult> Denegar(Guid id, SolicitudDenegarOferta solicitud, CancellationToken ct)
    {
        await servicio.DenegarAsync(id, solicitud.Motivo, ct);
        return NoContent();
    }

    /// <summary>Confirma la sustitución creando la asignación. Aplica las reglas y excepciones como cualquier otra asignación.</summary>
    [HttpPost("api/ofertas/{id:guid}/aprobar")]
    [Authorize(Politicas.GestionPlanificacion)]
    public async Task<ActionResult<ResultadoAsignacionDto>> Aprobar(Guid id, SolicitudAprobarOferta? solicitud, CancellationToken ct)
        => await servicio.AprobarAsync(id, solicitud?.Excepcion, ct);

    // ── Del propio profesional ──

    [HttpGet("api/mis-ofertas")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<IReadOnlyList<OfertaDto>>> Mias(CancellationToken ct) => Ok(await servicio.MisOfertasAsync(ct));

    [HttpPost("api/mis-ofertas/{id:guid}/aceptar")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<OfertaDto>> Aceptar(Guid id, SolicitudRespuestaOferta? solicitud, CancellationToken ct)
        => await servicio.AceptarAsync(id, solicitud?.Motivo, ct);

    [HttpPost("api/mis-ofertas/{id:guid}/rechazar")]
    [Authorize(Politicas.UsuarioSociedad)]
    public async Task<ActionResult<OfertaDto>> Rechazar(Guid id, SolicitudRespuestaOferta? solicitud, CancellationToken ct)
        => await servicio.RechazarAsync(id, solicitud?.Motivo, ct);
}

[ApiController]
[Route("api/notificaciones")]
[Authorize(Politicas.UsuarioSociedad)]
public sealed class NotificacionesController(NotificacionesServicio servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificacionDto>>> Listar([FromQuery] bool soloNoLeidas, CancellationToken ct)
        => Ok(await servicio.ListarAsync(soloNoLeidas, ct));

    [HttpGet("resumen")]
    public async Task<ActionResult<ResumenNotificacionesDto>> Resumen(CancellationToken ct) => await servicio.ResumenAsync(ct);

    [HttpPost("{id:guid}/leer")]
    public async Task<IActionResult> Leer(Guid id, CancellationToken ct)
    {
        await servicio.MarcarLeidaAsync(id, ct);
        return NoContent();
    }

    [HttpPost("leer-todas")]
    public async Task<IActionResult> LeerTodas(CancellationToken ct)
    {
        await servicio.MarcarTodasLeidasAsync(ct);
        return NoContent();
    }
}
