using System.Text.Json;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Profesionales;
using MedicalHub.Domain.Reglas;

namespace MedicalHub.Application.Reglas;

/// <summary>Turno reducido a lo que necesita el motor de reglas.</summary>
public sealed record TurnoLigero(Guid TurnoId, Guid ServicioId, DateOnly Fecha, DateTimeOffset InicioUtc, DateTimeOffset FinUtc, bool EsGuardia, string Etiqueta,
    DateOnly? FechaFinLocal = null, Guid? TipoTurnoId = null)
{
    public double Horas => (FinUtc - InicioUtc).TotalHours;

    /// <summary>Último día local que ocupa el turno (una noche que termina a las 08:00 ocupa también el día siguiente).</summary>
    public DateOnly FechaFin => FechaFinLocal ?? Fecha;
}

/// <summary>Ausencia aprobada reducida a lo que necesita el motor.</summary>
public sealed record AusenciaLigera(MedicalHub.Domain.Ausencias.TipoAusencia Tipo, DateOnly Desde, DateOnly Hasta);

/// <summary>Disponibilidad aprobada (disponible, bloqueo o preferencia).</summary>
public sealed record DisponibilidadLigera(MedicalHub.Domain.Ausencias.TipoDisponibilidad Tipo, DateOnly Desde, DateOnly Hasta, int Dias, Guid? TipoTurnoId, string? Comentario)
{
    public bool Afecta(TurnoLigero t) => Desde <= t.FechaFin && t.Fecha <= Hasta
        && (Dias & MedicalHub.Domain.Ausencias.DiasSemana.Bit(t.Fecha.DayOfWeek)) != 0 && (TipoTurnoId is null || TipoTurnoId == t.TipoTurnoId);
}

public sealed record AsignacionLigera(Guid AsignacionId, EstadoAsignacion Estado, TurnoLigero Turno, DateTimeOffset FechaAsignacion);

/// <summary>Regla ya resuelta para un servicio: una configurada por la sociedad o la de por defecto.</summary>
public sealed record ReglaEfectiva(Guid? Id, TipoRegla Tipo, string Nombre, SeveridadRegla Severidad, bool PermiteExcepcion,
    bool RequiereAdministrador, string? ParametrosJson);

public sealed record Violacion(string Codigo, Guid? ReglaId, string NombreRegla, SeveridadRegla Severidad, string Mensaje,
    bool PermiteExcepcion, bool RequiereAdministrador)
{
    /// <summary>Identifica la regla incumplida (el id de la regla configurada o el nombre del tipo si es la de por defecto).</summary>
    public string Clave => ReglaId?.ToString() ?? Codigo;
    public bool Bloquea => Severidad == SeveridadRegla.Bloqueante;
}

public sealed class ContextoEvaluacion
{
    public required Profesional Profesional { get; init; }
    public required Servicio Servicio { get; init; }
    public NecesidadCobertura? Necesidad { get; init; }
    public required TurnoLigero Turno { get; init; }
    /// <summary>Asignaciones activas del profesional (sin incluir la evaluada) en el entorno del turno.</summary>
    public required IReadOnlyList<AsignacionLigera> Otras { get; init; }
    public required IReadOnlyDictionary<Guid, string> Competencias { get; init; }
    public required IReadOnlyDictionary<Guid, string> Servicios { get; init; }
    /// <summary>Ausencias APROBADAS del profesional en el entorno del turno.</summary>
    public IReadOnlyList<AusenciaLigera> Ausencias { get; init; } = [];
    /// <summary>Disponibilidades APROBADAS del profesional en el entorno del turno.</summary>
    public IReadOnlyList<DisponibilidadLigera> Disponibilidades { get; init; } = [];
}

public sealed record ParametroRegla(string Clave, string Etiqueta, string Tipo, int? Minimo, int? Maximo);

public sealed record DefinicionTipoRegla(TipoRegla Tipo, string Nombre, string Descripcion, string Nivel, string ComportamientoPorDefecto,
    IReadOnlyList<ParametroRegla> Parametros);

// ── Parámetros tipados (JSON en camelCase) ──
public sealed record ParamDescanso(int Horas);
public sealed record ParamHoras(int VentanaDias, int MaxHoras);
public sealed record ParamGuardias(int VentanaDias, int MaxGuardias);
public sealed record ParamDias(int MaxDias);
public sealed record ParamIncompatibilidad(Guid ServicioAId, Guid ServicioBId, int MargenHoras);
public sealed record ParamCobertura(int PorcentajeMinimo);

public static class ParametrosRegla
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static T? Leer<T>(string? json) where T : class
        => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<T>(json, Json);

    /// <summary>Catálogo de tipos de regla con la descripción de sus parámetros (la interfaz lo usa para construir los formularios).</summary>
    public static readonly IReadOnlyList<DefinicionTipoRegla> Catalogo =
    [
        new(TipoRegla.AutorizacionServicio, "Autorización para el servicio", "El profesional debe estar autorizado en el servicio.", "Asignación",
            "Bloqueante, con excepción posible", []),
        new(TipoRegla.CategoriaAdmitida, "Categoría admitida en el servicio", "La categoría del profesional debe estar admitida en el servicio.", "Asignación",
            "Bloqueante, con excepción posible", []),
        new(TipoRegla.CompetenciasRequeridas, "Competencias requeridas", "El profesional debe tener vigentes las competencias que exige el servicio.", "Asignación",
            "Bloqueante, con excepción posible", []),
        new(TipoRegla.CategoriaNecesidad, "Categoría de la necesidad", "El profesional debe ser de la categoría que exige la necesidad de cobertura.", "Asignación",
            "Bloqueante, con excepción posible", []),
        new(TipoRegla.DescansoMinimo, "Descanso mínimo entre turnos", "Horas mínimas entre el fin de un turno y el inicio del siguiente del mismo profesional.", "Asignación",
            "No se aplica hasta que la sociedad la configure", [new("horas", "Horas de descanso", "entero", 1, 72)]),
        new(TipoRegla.MaxHorasPeriodo, "Máximo de horas en un periodo", "Horas planificadas como máximo en cualquier ventana de días consecutivos.", "Asignación",
            "No se aplica hasta que la sociedad la configure", [new("ventanaDias", "Ventana (días)", "entero", 1, 93), new("maxHoras", "Máximo de horas", "entero", 1, 2000)]),
        new(TipoRegla.MaxGuardiasPeriodo, "Máximo de guardias en un periodo", "Número máximo de guardias en cualquier ventana de días consecutivos.", "Asignación",
            "No se aplica hasta que la sociedad la configure", [new("ventanaDias", "Ventana (días)", "entero", 1, 93), new("maxGuardias", "Máximo de guardias", "entero", 1, 93)]),
        new(TipoRegla.MaxDiasConsecutivos, "Máximo de días consecutivos", "Días seguidos con turnos asignados (el turno cuenta en el día en que empieza).", "Asignación",
            "No se aplica hasta que la sociedad la configure", [new("maxDias", "Máximo de días", "entero", 1, 93)]),
        new(TipoRegla.IncompatibilidadServicios, "Incompatibilidad entre servicios", "El profesional no puede tener turnos en dos servicios con menos de un margen de horas entre ellos.", "Asignación",
            "No se aplica hasta que la sociedad la configure",
            [new("servicioAId", "Servicio A", "servicio", null, null), new("servicioBId", "Servicio B", "servicio", null, null), new("margenHoras", "Margen mínimo (horas)", "entero", 0, 72)]),
        new(TipoRegla.AusenciaAprobada, "Ausencia aprobada", "El profesional tiene una ausencia aprobada que coincide con el turno.", "Asignación",
            "Bloqueante, con excepción posible", []),
        new(TipoRegla.DisponibilidadBloqueada, "Periodo bloqueado por el profesional", "El profesional ha bloqueado (y el coordinador ha aprobado) ese periodo.", "Asignación",
            "Bloqueante, con excepción posible", []),
        new(TipoRegla.PreferenciaEvitar, "Preferencia por evitar el turno", "El profesional ha indicado que prefiere evitar ese turno. Una preferencia no es una prohibición salvo que la sociedad la configure como bloqueante.", "Asignación",
            "Advertencia", []),
        new(TipoRegla.CoberturaMinima, "Cobertura mínima", "Porcentaje mínimo de plazas cubiertas en cada necesidad al validar un cuadrante.", "Cuadrante",
            "Advertencia si hay plazas sin cubrir (100 %)", [new("porcentajeMinimo", "Porcentaje mínimo", "entero", 1, 100)]),
        new(TipoRegla.AsignacionPendiente, "Asignaciones pendientes de aprobación", "Avisa de las asignaciones que aún no están confirmadas al validar un cuadrante.", "Cuadrante",
            "Advertencia", []),
    ];

    public static DefinicionTipoRegla Definicion(TipoRegla tipo) => Catalogo.First(d => d.Tipo == tipo);

    /// <summary>Tipos que tienen un comportamiento por defecto cuando la sociedad no configura ninguna regla.</summary>
    public static bool TieneComportamientoPorDefecto(TipoRegla t)
        => t is TipoRegla.AutorizacionServicio or TipoRegla.CategoriaAdmitida or TipoRegla.CompetenciasRequeridas
            or TipoRegla.CategoriaNecesidad or TipoRegla.CoberturaMinima or TipoRegla.AsignacionPendiente
            or TipoRegla.AusenciaAprobada or TipoRegla.DisponibilidadBloqueada or TipoRegla.PreferenciaEvitar;
}
