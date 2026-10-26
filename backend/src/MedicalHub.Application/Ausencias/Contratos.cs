using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Planificacion;

namespace MedicalHub.Application.Ausencias;

// ── Ausencias ──
public sealed record DecisionDto(EstadoAusencia Decision, string Usuario, DateTimeOffset Fecha, string? Motivo);

public sealed record AusenciaDto(Guid Id, Guid ProfesionalId, string Profesional, TipoAusencia Tipo, DateOnly Desde, DateOnly Hasta, int Dias,
    string? Comentario, EstadoAusencia Estado, DateTimeOffset FechaSolicitud, string SolicitadaPor, IReadOnlyList<DecisionDto> Decisiones);

public sealed record SolicitudAusencia(TipoAusencia Tipo, DateOnly Desde, DateOnly Hasta, string? Comentario);

/// <param name="Aprobar">El coordinador la registra ya aprobada (p. ej. una baja comunicada).</param>
public sealed record SolicitudAusenciaDeProfesional(Guid ProfesionalId, TipoAusencia Tipo, DateOnly Desde, DateOnly Hasta, string? Comentario, bool Aprobar);

public sealed record SolicitudDecision(string? Motivo);

/// <summary>Asignación futura que cae dentro de la ausencia.</summary>
public sealed record TurnoComprometidoDto(Guid AsignacionId, Guid TurnoId, Guid NecesidadId, DateOnly Fecha, string Turno, string Servicio,
    string Centro, DateTimeOffset InicioUtc, DateTimeOffset FinUtc, EstadoCuadrante CuadranteEstado);

/// <param name="Vacantes">Turnos que han quedado sin cubrir al aprobar la ausencia.</param>
public sealed record ResultadoAusenciaDto(AusenciaDto Ausencia, IReadOnlyList<TurnoComprometidoDto> Vacantes);

// ── Disponibilidad ──
public sealed record DisponibilidadDto(Guid Id, Guid ProfesionalId, string Profesional, TipoDisponibilidad Tipo, DateOnly Desde, DateOnly Hasta,
    int Dias, Guid? TipoTurnoId, string? TipoTurno, string? Comentario, EstadoDisponibilidad Estado, DateTimeOffset FechaDeclaracion,
    string? MotivoDecision, Guid? CentroId = null, string? Centro = null, int? TurnosAsignados = null);

/// <summary>Varios bloques (hospital · días · turno) declarados a la vez: se guardan todos o ninguno.</summary>
public sealed record SolicitudDisponibilidadLote(IReadOnlyList<SolicitudDisponibilidad> Bloques);

/// <param name="Dias">Máscara de días (lunes = 1 … domingo = 64). Null = todos.</param>
public sealed record SolicitudDisponibilidad(TipoDisponibilidad Tipo, DateOnly Desde, DateOnly Hasta, int? Dias, Guid? TipoTurnoId, string? Comentario, Guid? CentroId = null);

public sealed record SolicitudDisponibilidadDeProfesional(Guid ProfesionalId, TipoDisponibilidad Tipo, DateOnly Desde, DateOnly Hasta, int? Dias,
    Guid? TipoTurnoId, string? Comentario, bool Aprobar, Guid? CentroId = null);

public sealed record TipoTurnoPropioDto(Guid Id, string Nombre, string Franja, string Servicio, string Centro, Guid? CentroId = null);

// ── Vacantes ──
public sealed record VacanteDto(Guid Id, EstadoVacante Estado, string Motivo, DateTimeOffset FechaCreacion, DateTimeOffset? FechaCierre, Guid NecesidadId,
    Guid TurnoId, DateOnly Fecha, string Turno, string Servicio, string Centro, string? Categoria, string ProfesionalAusente, Guid? AusenciaId,
    EstadoCuadrante CuadranteEstado);

public sealed record SolicitudCubrirVacante(Guid ProfesionalId, MedicalHub.Application.Reglas.SolicitudExcepcion? Excepcion);
