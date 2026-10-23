using MedicalHub.Domain.Horas;

namespace MedicalHub.Application.Horas;

/// <summary>Posición del dispositivo. Ambas coordenadas o ninguna; sin ubicación el fichaje se admite pero queda como «sin ubicación».</summary>
public sealed record SolicitudFichaje(double? Latitud, double? Longitud, double? PrecisionMetros);

public sealed record SolicitudGeovalla(double? Latitud, double? Longitud, int? RadioMetros);

public sealed record GeovallaDto(Guid CentroId, double? Latitud, double? Longitud, int? RadioMetros);

public sealed record PausaFichajeDto(DateTimeOffset InicioUtc, DateTimeOffset? FinUtc);

public sealed record FichajeDto(Guid Id, Guid AsignacionId, EstadoFichaje Estado, string Turno, string Centro, bool CentroConGeovalla, DateTimeOffset InicioUtc,
    DateTimeOffset? FinUtc, int MinutosPausa, int MinutosTrabajados, bool? EntradaFueraDeZona, bool? SalidaFueraDeZona, bool EntradaSinUbicacion,
    bool? SalidaSinUbicacion, IReadOnlyList<PausaFichajeDto> Pausas);

/// <summary>Asignación sobre la que se fichará si se pulsa «entrada» ahora.</summary>
public sealed record TurnoFichableDto(Guid AsignacionId, string Turno, string Centro, bool CentroConGeovalla, DateTimeOffset InicioUtc, DateTimeOffset FinUtc);

public sealed record EstadoFichajeDto(FichajeDto? Actual, TurnoFichableDto? Fichable);

/// <summary>Fichaje con alguna marca fuera de zona, para su revisión por la coordinación.</summary>
public sealed record FichajeFueraDeZonaDto(Guid Id, Guid ProfesionalId, string Profesional, string Servicio, string Centro, DateTimeOffset InicioUtc,
    DateTimeOffset? FinUtc, bool? EntradaFueraDeZona, bool? SalidaFueraDeZona);
