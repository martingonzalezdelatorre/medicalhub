using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Estructura;

namespace MedicalHub.Domain.Profesionales;

public sealed class ProfesionalEspecialidad
{
    private ProfesionalEspecialidad() { }
    public ProfesionalEspecialidad(Guid id) => EspecialidadId = id;
    public Guid EspecialidadId { get; private set; }
}

public sealed class ProfesionalCompetencia
{
    private ProfesionalCompetencia() { }
    public ProfesionalCompetencia(Guid competenciaId, DateOnly? vigenteHasta)
    {
        CompetenciaId = competenciaId;
        VigenteHasta = vigenteHasta;
    }

    public Guid CompetenciaId { get; private set; }
    /// <summary>Fecha de caducidad de la habilitación (inclusive). null = sin caducidad.</summary>
    public DateOnly? VigenteHasta { get; private set; }

    public bool VigenteEn(DateOnly fecha) => VigenteHasta is null || VigenteHasta >= fecha;
}

public sealed class ProfesionalServicio
{
    private ProfesionalServicio() { }
    public ProfesionalServicio(Guid servicioId) => ServicioId = servicioId;
    public Guid ServicioId { get; private set; }
}

/// <summary>
/// Profesional sanitario. Solo se guardan los datos de contacto estrictamente necesarios.
/// No tiene por qué tener usuario de acceso (UsuarioId opcional).
/// </summary>
public sealed class Profesional : EntidadDeOrganizacion, IAuditable
{
    private readonly List<ProfesionalEspecialidad> _especialidades = [];
    private readonly List<ProfesionalCompetencia> _competencias = [];
    private readonly List<ProfesionalServicio> _servicios = [];

    public Guid? UsuarioId { get; private set; }
    public string Nombre { get; private set; } = "";
    public string Apellidos { get; private set; } = "";
    public string? Email { get; private set; }
    public string? Telefono { get; private set; }
    public string? NumeroColegiado { get; private set; }
    public Guid CategoriaId { get; private set; }
    public bool Activo { get; private set; } = true;
    public DateTimeOffset FechaCreacion { get; private set; }

    public IReadOnlyCollection<ProfesionalEspecialidad> Especialidades => _especialidades;
    public IReadOnlyCollection<ProfesionalCompetencia> Competencias => _competencias;
    public IReadOnlyCollection<ProfesionalServicio> ServiciosAutorizados => _servicios;

    public string NombreCompleto => $"{Nombre} {Apellidos}".Trim();

    private Profesional() { }

    public Profesional(Guid organizacionId, string nombre, string apellidos, Guid categoriaId, DateTimeOffset ahora)
        : base(organizacionId)
    {
        Nombre = Texto(nombre, "El nombre", 100);
        Apellidos = Texto(apellidos, "Los apellidos", 150);
        CategoriaId = ValidarCategoria(categoriaId);
        FechaCreacion = ahora;
    }

    private static Guid ValidarCategoria(Guid id)
        => id == Guid.Empty ? throw new ExcepcionDominio("La categoría profesional es obligatoria.") : id;

    public void ActualizarDatos(string nombre, string apellidos, string? email, string? telefono, string? numeroColegiado, Guid categoriaId)
    {
        Nombre = Texto(nombre, "El nombre", 100);
        Apellidos = Texto(apellidos, "Los apellidos", 150);
        Email = TextoOpcional(email, "El correo", 254)?.ToLowerInvariant();
        if (Email is not null && !Email.Contains('@')) throw new ExcepcionDominio("El correo de contacto no es válido.");
        Telefono = TextoOpcional(telefono, "El teléfono", 30);
        NumeroColegiado = TextoOpcional(numeroColegiado, "El número de colegiado", 30);
        CategoriaId = ValidarCategoria(categoriaId);
    }

    public void VincularUsuario(Guid? usuarioId) => UsuarioId = usuarioId;

    public void EstablecerEspecialidades(IEnumerable<Guid> ids)
    {
        var nuevos = ids.Distinct().ToList();
        _especialidades.RemoveAll(e => !nuevos.Contains(e.EspecialidadId));
        foreach (var id in nuevos.Where(n => _especialidades.All(e => e.EspecialidadId != n)))
            _especialidades.Add(new ProfesionalEspecialidad(id));
    }

    public void EstablecerCompetencias(IEnumerable<(Guid CompetenciaId, DateOnly? VigenteHasta)> competencias)
    {
        var nuevas = competencias.GroupBy(c => c.CompetenciaId).Select(g => g.Last()).ToList();
        _competencias.RemoveAll(c => nuevas.All(n => n.CompetenciaId != c.CompetenciaId)
                                     || nuevas.First(n => n.CompetenciaId == c.CompetenciaId).VigenteHasta != c.VigenteHasta);
        foreach (var n in nuevas.Where(n => _competencias.All(c => c.CompetenciaId != n.CompetenciaId)))
            _competencias.Add(new ProfesionalCompetencia(n.CompetenciaId, n.VigenteHasta));
    }

    public void EstablecerServiciosAutorizados(IEnumerable<Guid> ids)
    {
        var nuevos = ids.Distinct().ToList();
        _servicios.RemoveAll(s => !nuevos.Contains(s.ServicioId));
        foreach (var id in nuevos.Where(n => _servicios.All(s => s.ServicioId != n)))
            _servicios.Add(new ProfesionalServicio(id));
    }

    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;
}

public sealed record ResultadoElegibilidad(bool Elegible, IReadOnlyList<string> Motivos);

/// <summary>
/// Regla de dominio: un profesional solo puede ser asignado a un servicio si cumple estas condiciones.
/// (Solapamientos, ausencias y descansos los valida el motor de reglas de planificación, E4.)
/// </summary>
public static class EvaluadorElegibilidad
{
    public static ResultadoElegibilidad Evaluar(Profesional profesional, Servicio servicio, DateOnly fecha,
        IReadOnlyDictionary<Guid, string>? nombresCompetencias = null)
    {
        var motivos = new List<string>();
        if (!profesional.Activo) motivos.Add("El profesional está inactivo.");
        if (!servicio.Activo) motivos.Add("El servicio está inactivo.");
        if (profesional.ServiciosAutorizados.All(s => s.ServicioId != servicio.Id))
            motivos.Add("No está autorizado para este servicio.");
        if (servicio.CategoriasAdmitidas.Count > 0
            && servicio.CategoriasAdmitidas.All(c => c.CategoriaProfesionalId != profesional.CategoriaId))
            motivos.Add("Su categoría profesional no está admitida en este servicio.");

        foreach (var req in servicio.CompetenciasRequeridas)
        {
            var nombre = nombresCompetencias?.GetValueOrDefault(req.CompetenciaId) ?? req.CompetenciaId.ToString();
            var tiene = profesional.Competencias.FirstOrDefault(c => c.CompetenciaId == req.CompetenciaId);
            if (tiene is null) motivos.Add($"Falta la competencia «{nombre}».");
            else if (!tiene.VigenteEn(fecha)) motivos.Add($"La competencia «{nombre}» está caducada.");
        }
        return new ResultadoElegibilidad(motivos.Count == 0, motivos);
    }
}
