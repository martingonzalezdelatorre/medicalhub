using System.Text.Json;
using MedicalHub.Application.Comun;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Reglas;

namespace MedicalHub.Application.Reglas;

public sealed record ViolacionDto(string Codigo, Guid? ReglaId, string Regla, string Severidad, string Mensaje, bool PermiteExcepcion, bool RequiereAdministrador)
{
    public static ViolacionDto De(Violacion v) => new(v.Codigo, v.ReglaId, v.NombreRegla, v.Severidad.ToString(), v.Mensaje, v.PermiteExcepcion, v.RequiereAdministrador);
}

public sealed record SolicitudExcepcion(string Justificacion);

public sealed record SolicitudAsignacionConReglas(Guid ProfesionalId, SolicitudExcepcion? Excepcion);

/// <param name="Advertencias">Infracciones no bloqueantes: la asignación se ha hecho pero conviene revisarlas.</param>
/// <param name="Excepciones">Infracciones bloqueantes que se han saltado con una excepción justificada.</param>
public sealed record ResultadoAsignacionDto(Guid Id, Guid ProfesionalId, string Profesional, EstadoAsignacion Estado,
    IReadOnlyList<ViolacionDto> Advertencias, IReadOnlyList<ViolacionDto> Excepciones);

public sealed record ReglaDto(Guid Id, TipoRegla Tipo, string Nombre, string? Descripcion, Guid? ServicioId, string? ServicioNombre,
    SeveridadRegla Severidad, bool Activa, bool PermiteExcepcion, bool ExcepcionRequiereAdministrador, JsonElement? Parametros,
    string? Fundamento, DateTimeOffset? RevisadaEl, string? RevisadaPor, bool PendienteDeRevision);

public sealed record SolicitudRegla(TipoRegla Tipo, string Nombre, string? Descripcion, Guid? ServicioId, SeveridadRegla Severidad,
    bool PermiteExcepcion, bool ExcepcionRequiereAdministrador, JsonElement? Parametros, string? Fundamento);

public sealed record ExcepcionDto(Guid Id, DateTimeOffset Fecha, Guid ProfesionalId, string Profesional, Guid TurnoId, string Turno,
    string Codigo, string Regla, string Mensaje, string Justificacion, string AutorizadaPor, bool AsignacionActiva);

/// <summary>Se lanza cuando una asignación incumple reglas. Incluye el detalle para que el cliente pueda ofrecer la excepción.</summary>
public sealed class ExcepcionReglas(string mensaje, IReadOnlyList<ViolacionDto> violaciones, string codigo, bool prohibida = false) : Exception(mensaje)
{
    /// <summary>"regla_incumplida" (no admite excepción) o "requiere_excepcion" (se puede saltar con justificación).</summary>
    public string Codigo { get; } = codigo;
    public IReadOnlyList<ViolacionDto> Violaciones { get; } = violaciones;
    /// <summary>true si el usuario no tiene autoridad para autorizar la excepción (403).</summary>
    public bool Prohibida { get; } = prohibida;
}
