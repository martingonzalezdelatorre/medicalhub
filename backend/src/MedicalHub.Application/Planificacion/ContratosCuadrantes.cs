using MedicalHub.Application.Comun;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Planificacion;

namespace MedicalHub.Application.Planificacion;

// ── Cuadrantes ──
public sealed record CuadranteDto(Guid Id, Guid ServicioId, string ServicioNombre, Guid CentroId, string CentroNombre, string Nombre,
    DateOnly Desde, DateOnly Hasta, EstadoCuadrante Estado, int Version, DateTimeOffset? FechaPublicacion,
    int TotalTurnos, int PlazasTotales, int PlazasCubiertas);

/// <param name="GenerarTurnos">Si es true, crea un turno por día y tipo de turno con requisitos de cobertura definidos en el servicio.</param>
public sealed record SolicitudCuadrante(Guid ServicioId, string? Nombre, DateOnly Desde, DateOnly Hasta, bool GenerarTurnos);

public static class Severidad
{
    public const string Bloqueante = "Bloqueante";
    public const string Advertencia = "Advertencia";
}

public sealed record IncidenciaDto(string Severidad, string Codigo, string Mensaje, Guid? TurnoId, Guid? ProfesionalId);
public sealed record ValidacionDto(bool PuedePublicarse, IReadOnlyList<IncidenciaDto> Incidencias);

public sealed record VersionResumenDto(int Numero, DateTimeOffset Fecha, string? Usuario, int Turnos, int PlazasTotales, int PlazasCubiertas);
public sealed record VersionDetalleDto(VersionResumenDto Resumen, string ContenidoJson);

public sealed record PlantillaDto(Guid Id, Guid ServicioId, string Nombre, int DiasPeriodo, int Lineas);
public sealed record SolicitudPlantilla(string Nombre);
public sealed record SolicitudDesdePlantilla(Guid PlantillaId, DateOnly Desde, string? Nombre);
public sealed record SolicitudRenombrar(string Nombre);

// ── Turnos, necesidades y asignaciones ──
public sealed record LineaNecesidad(int NumeroPlazas, Guid? CategoriaProfesionalId);
/// <param name="Necesidades">Si va vacío, se toman los requisitos de cobertura del servicio para ese tipo de turno.</param>
public sealed record SolicitudTurno(DateOnly Fecha, Guid TipoTurnoId, IReadOnlyList<LineaNecesidad>? Necesidades);
public sealed record SolicitudNecesidades(IReadOnlyList<LineaNecesidad> Necesidades);
public sealed record SolicitudAsignacion(Guid ProfesionalId);

public sealed record AsignacionDto(Guid Id, Guid ProfesionalId, string Profesional, EstadoAsignacion Estado);

public sealed record NecesidadDto(Guid Id, int NumeroPlazas, Guid? CategoriaProfesionalId, string? Categoria,
    IReadOnlyList<AsignacionDto> Asignaciones)
{
    public int PlazasCubiertas => Asignaciones.Count;
}

public sealed record TurnoDto(Guid Id, Guid CuadranteId, EstadoCuadrante CuadranteEstado, DateOnly Fecha,
    string InicioLocal, string FinLocal, DateTimeOffset InicioUtc, DateTimeOffset FinUtc, bool CruzaMedianoche, double DuracionHoras,
    Guid CentroId, string Centro, string ZonaHoraria, Guid ServicioId, string Servicio,
    Guid TipoTurnoId, string TipoTurno, bool EsGuardia,
    IReadOnlyList<NecesidadDto> Necesidades, int PlazasTotales, int PlazasCubiertas)
{
    public bool SinCubrir => PlazasCubiertas < PlazasTotales;
}

public sealed record FiltroCalendario(DateOnly Desde, DateOnly Hasta, Guid? CentroId, Guid? ServicioId, Guid? ProfesionalId,
    Guid? CategoriaId, Guid? CuadranteId, bool SoloSinCubrir);

public sealed record CandidatoDto(Guid ProfesionalId, string NombreCompleto, bool Elegible, bool Libre, IReadOnlyList<string> Motivos);

public sealed record ArchivoExportado(string NombreArchivo, string TipoContenido, byte[] Contenido);

/// <summary>Alcance de datos de planificación según el rol del usuario.</summary>
public static class AlcancePlanificacion
{
    /// <summary>Servicios que el usuario puede consultar (el responsable de centro solo ve los de sus centros).</summary>
    public static IQueryable<Servicio> Servicios(IAppDbContext db, IContextoActual contexto)
    {
        if (!contexto.EsAlcanceRestringido()) return db.Servicios;
        var centros = CentrosServicio.Visibles(db, contexto).Select(c => c.Id);
        return db.Servicios.Where(s => centros.Contains(s.CentroId));
    }
}
