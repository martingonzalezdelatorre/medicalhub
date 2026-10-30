using MedicalHub.Domain.Estructura;

namespace MedicalHub.Application.Planificacion;

// ── Catálogos ──
public sealed record CatalogoDto(Guid Id, string Nombre, string? Descripcion, bool Activa);
public sealed record SolicitudCatalogo(string Nombre, string? Descripcion);

// ── Centros ──
public sealed record CentroDto(Guid Id, string Nombre, TipoCentro Tipo, string? Direccion, string ZonaHoraria, bool Activo,
    IReadOnlyList<Guid> ResponsablesIds);
public sealed record SolicitudCentro(string Nombre, TipoCentro Tipo, string? Direccion, string ZonaHoraria);
public sealed record SolicitudResponsables(IReadOnlyList<Guid> UsuariosIds);

// ── Servicios ──
public sealed record TipoTurnoDto(Guid Id, string Nombre, TimeOnly HoraInicio, TimeOnly HoraFin, bool EsGuardia, bool Activo,
    bool CruzaMedianoche, double DuracionHoras);
public sealed record RequisitoDto(Guid Id, Guid TipoTurnoId, int NumeroProfesionales, Guid? CategoriaProfesionalId);
public sealed record ServicioDto(Guid Id, Guid CentroId, string CentroNombre, string Nombre, string? Descripcion,
    Guid? CoordinadorUsuarioId, string? ConfiguracionReglas, bool Activo,
    IReadOnlyList<Guid> CategoriasAdmitidasIds, IReadOnlyList<Guid> CompetenciasRequeridasIds,
    IReadOnlyList<TipoTurnoDto> TiposTurno, IReadOnlyList<RequisitoDto> Requisitos);
public sealed record SolicitudServicio(Guid CentroId, string Nombre, string? Descripcion, Guid? CoordinadorUsuarioId,
    string? ConfiguracionReglas, IReadOnlyList<Guid>? CategoriasAdmitidasIds, IReadOnlyList<Guid>? CompetenciasRequeridasIds);
public sealed record SolicitudTipoTurno(string Nombre, TimeOnly HoraInicio, TimeOnly HoraFin, bool EsGuardia);
public sealed record LineaRequisito(Guid TipoTurnoId, int NumeroProfesionales, Guid? CategoriaProfesionalId);
public sealed record SolicitudRequisitos(IReadOnlyList<LineaRequisito> Requisitos);
public sealed record ElegibilidadDto(Guid ProfesionalId, string NombreCompleto, bool Elegible, IReadOnlyList<string> Motivos);

// ── Tarifas ──
public sealed record TarifaDto(Guid TipoTurnoId, Guid CategoriaProfesionalId, decimal Importe);
public sealed record LineaTarifa(Guid TipoTurnoId, Guid CategoriaProfesionalId, decimal Importe);
public sealed record SolicitudTarifas(IReadOnlyList<LineaTarifa> Tarifas);

// ── Profesionales ──
public sealed record CompetenciaProfesionalDto(Guid CompetenciaId, DateOnly? VigenteHasta);
public sealed record ProfesionalDto(Guid Id, Guid? UsuarioId, string Nombre, string Apellidos, string? Email, string? Telefono,
    string? NumeroColegiado, Guid CategoriaId, IReadOnlyList<Guid> EspecialidadesIds,
    IReadOnlyList<CompetenciaProfesionalDto> Competencias, IReadOnlyList<Guid> ServiciosIds, bool Activo);
public sealed record SolicitudProfesional(Guid? UsuarioId, string Nombre, string Apellidos, string? Email, string? Telefono,
    string? NumeroColegiado, Guid CategoriaId, IReadOnlyList<Guid>? EspecialidadesIds,
    IReadOnlyList<CompetenciaProfesionalDto>? Competencias, IReadOnlyList<Guid>? ServiciosIds);
public sealed record FiltroProfesionales(bool? Activo, Guid? CategoriaId, Guid? ServicioId, string? Texto);
public sealed record CambioHistorialDto(DateTimeOffset Fecha, string? Usuario, string Entidad, string Accion, string? Cambios);
