using MedicalHub.Domain.Comun;

namespace MedicalHub.Domain.Identidad;

/// <summary>Rol asignado a un usuario (tipo poseído; tabla UsuarioRoles).</summary>
public sealed class UsuarioRol
{
    private UsuarioRol() { }
    public UsuarioRol(Rol rol) => Rol = rol;
    public Rol Rol { get; private set; }
}

public sealed class Usuario : Entidad, IPerteneceAOrganizacion, IAuditable
{
    private readonly List<UsuarioRol> _roles = [];

    public Guid? OrganizacionId { get; private set; }
    public string Email { get; private set; }
    public string NombreCompleto { get; private set; }
    public string HashContrasena { get; private set; }
    public bool Activo { get; private set; } = true;
    public DateTimeOffset FechaCreacion { get; private set; }

    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }

    public bool MfaActivado { get; private set; }
    /// <summary>Secreto TOTP ya protegido (cifrado) por la infraestructura.</summary>
    public string? MfaSecretoProtegido { get; private set; }
    /// <summary>Último paso TOTP aceptado; impide reutilizar un código.</summary>
    public long MfaUltimoPaso { get; private set; }

    public IReadOnlyCollection<UsuarioRol> Roles => _roles;
    public IEnumerable<Rol> ListaRoles => _roles.Select(r => r.Rol);

    private Usuario() { Email = NombreCompleto = HashContrasena = ""; }

    public Usuario(Guid? organizacionId, string email, string nombreCompleto, string hashContrasena,
        IEnumerable<Rol> roles, DateTimeOffset ahora)
    {
        Email = NormalizarEmail(email);
        NombreCompleto = ValidarNombre(nombreCompleto);
        HashContrasena = hashContrasena;
        FechaCreacion = ahora;
        OrganizacionId = organizacionId;
        EstablecerRoles(roles);
    }

    public static string NormalizarEmail(string email)
    {
        var e = (email ?? "").Trim().ToLowerInvariant();
        if (e.Length is 0 or > 254 || !e.Contains('@') || e.StartsWith('@') || e.EndsWith('@'))
            throw new ExcepcionDominio("El correo electrónico no es válido.");
        return e;
    }

    private static string ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 200)
            throw new ExcepcionDominio("El nombre es obligatorio y no puede superar 200 caracteres.");
        return nombre.Trim();
    }

    /// <summary>
    /// Invariantes: el administrador de plataforma no pertenece a ninguna organización y no
    /// combina otros roles; el resto debe pertenecer a una organización.
    /// </summary>
    public void EstablecerRoles(IEnumerable<Rol> roles)
    {
        var nuevos = roles.Distinct().ToList();
        if (nuevos.Count == 0) throw new ExcepcionDominio("El usuario debe tener al menos un rol.");

        var esPlataforma = nuevos.Contains(Rol.AdministradorPlataforma);
        if (esPlataforma && (nuevos.Count > 1 || OrganizacionId is not null))
            throw new ExcepcionDominio("El administrador de plataforma no puede pertenecer a una organización ni tener otros roles.");
        if (!esPlataforma && OrganizacionId is null)
            throw new ExcepcionDominio("Los usuarios de una sociedad deben pertenecer a una organización.");

        _roles.RemoveAll(r => !nuevos.Contains(r.Rol));
        foreach (var rol in nuevos.Where(n => _roles.All(r => r.Rol != n)))
            _roles.Add(new UsuarioRol(rol));
    }

    public bool EstaBloqueado(DateTimeOffset ahora) => BloqueadoHasta is { } hasta && hasta > ahora;

    public void RegistrarFallo(DateTimeOffset ahora, int maxIntentos, TimeSpan duracionBloqueo)
    {
        if (EstaBloqueado(ahora)) return;
        IntentosFallidos++;
        if (IntentosFallidos >= maxIntentos)
        {
            BloqueadoHasta = ahora + duracionBloqueo;
            IntentosFallidos = 0;
        }
    }

    public void RegistrarAccesoCorrecto()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
    }

    public void CambiarNombre(string nombre) => NombreCompleto = ValidarNombre(nombre);

    public void CambiarContrasena(string nuevoHash) => HashContrasena = nuevoHash;

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;

    /// <summary>Inicia (o reinicia) el alta de MFA; no queda activo hasta confirmarlo con un código.</summary>
    public void IniciarConfiguracionMfa(string secretoProtegido)
    {
        if (MfaActivado) throw new ExcepcionDominio("La autenticación en dos pasos ya está activada.");
        MfaSecretoProtegido = secretoProtegido;
    }

    public void ActivarMfa(long paso)
    {
        if (MfaSecretoProtegido is null) throw new ExcepcionDominio("No hay una configuración de MFA pendiente.");
        MfaActivado = true;
        MfaUltimoPaso = paso;
    }

    public void RegistrarPasoMfa(long paso) => MfaUltimoPaso = paso;

    public void ReiniciarMfa()
    {
        MfaActivado = false;
        MfaSecretoProtegido = null;
        MfaUltimoPaso = 0;
    }

    /// <summary>Indica si, por rol y política de la organización, este usuario debe usar MFA.</summary>
    public bool RequiereMfa(bool politicaOrganizacionExigeMfa)
        => _roles.Any(r => r.Rol == Rol.AdministradorPlataforma)
           || (politicaOrganizacionExigeMfa && _roles.Any(r => r.Rol.ExigeMfa()));
}
