using MedicalHub.Application.Comun;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Identidad;
using MedicalHub.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Identidad;

public sealed class AutenticacionServicio(
    IAppDbContext db,
    IHasherContrasena hasher,
    IEmisorTokens emisor,
    IServicioTotp totp,
    IProtectorSecretos protector,
    IContextoActual contexto,
    TimeProvider reloj,
    OpcionesSeguridad opciones)
{
    private const string MensajeCredenciales = "Credenciales no válidas o cuenta bloqueada temporalmente.";
    private string? _hashFicticio;

    // ───────────── Inicio de sesión ─────────────

    public async Task<RespuestaSesion> LoginAsync(SolicitudLogin s, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        string email;
        try { email = Usuario.NormalizarEmail(s.Email); }
        catch (ExcepcionDominio) { throw await Fallo(null, s.Email, "Correo con formato no válido", ct); }

        // Búsqueda previa a la autenticación: no hay organización todavía, por eso se ignoran los filtros.
        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email, ct);
        if (usuario is null)
        {
            // Igualar el coste temporal para no revelar si el correo existe.
            hasher.Verificar(HashFicticio(), s.Contrasena ?? "");
            throw await Fallo(null, email, "Usuario inexistente", ct);
        }

        var org = usuario.OrganizacionId is { } oid
            ? await db.Organizaciones.FirstOrDefaultAsync(o => o.Id == oid, ct) : null;

        if (!usuario.Activo || (usuario.OrganizacionId is not null && org is not { Activa: true }))
            throw await Fallo(usuario, email, "Cuenta o sociedad inactiva", ct);

        if (usuario.EstaBloqueado(ahora))
            throw await Fallo(usuario, email, "Cuenta bloqueada", ct, contarFallo: false);

        if (!hasher.Verificar(usuario.HashContrasena, s.Contrasena ?? ""))
            throw await Fallo(usuario, email, "Contraseña incorrecta", ct);

        if (usuario.MfaActivado)
        {
            if (string.IsNullOrWhiteSpace(s.CodigoMfa))
            {
                Registrar(TipoEventoSeguridad.MfaRequerido, usuario, email);
                await db.SaveChangesAsync(ct);
                throw new ExcepcionNoAutenticado("Introduzca el código de autenticación en dos pasos.", "mfa_requerido");
            }
            var secreto = protector.Desproteger(usuario.MfaSecretoProtegido!);
            if (!totp.Validar(secreto, s.CodigoMfa, ahora, usuario.MfaUltimoPaso, out var paso))
            {
                Registrar(TipoEventoSeguridad.MfaFallido, usuario, email);
                throw await Fallo(usuario, email, "Código MFA incorrecto", ct);
            }
            usuario.RegistrarPasoMfa(paso);
        }

        usuario.RegistrarAccesoCorrecto();
        Registrar(TipoEventoSeguridad.InicioSesionCorrecto, usuario, email);
        return await CrearSesionAsync(usuario, org, Guid.NewGuid(), ct);
    }

    private async Task<Exception> Fallo(Usuario? usuario, string email, string detalle, CancellationToken ct, bool contarFallo = true)
    {
        var ahora = reloj.GetUtcNow();
        if (usuario is not null && contarFallo)
        {
            var estabaBloqueado = usuario.EstaBloqueado(ahora);
            usuario.RegistrarFallo(ahora, opciones.MaxIntentosFallidos, TimeSpan.FromMinutes(opciones.MinutosBloqueo));
            if (!estabaBloqueado && usuario.EstaBloqueado(ahora))
                Registrar(TipoEventoSeguridad.CuentaBloqueada, usuario, email);
        }
        Registrar(TipoEventoSeguridad.InicioSesionFallido, usuario, email, detalle);
        await db.SaveChangesAsync(ct);
        return new ExcepcionNoAutenticado(MensajeCredenciales);
    }

    // ───────────── Refresco y cierre de sesión ─────────────

    public async Task<RespuestaSesion> RefrescarAsync(SolicitudRefresco s, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        if (string.IsNullOrWhiteSpace(s.TokenRefresco)) throw new ExcepcionNoAutenticado("Sesión no válida.");

        var hash = emisor.HashearTokenRefresco(s.TokenRefresco);
        var token = await db.TokensRefresco.FirstOrDefaultAsync(t => t.HashToken == hash, ct)
            ?? throw new ExcepcionNoAutenticado("Sesión no válida.");

        if (token.EstaRevocado)
        {
            // Reutilización de un token ya rotado: posible robo. Se invalida toda la sesión.
            await RevocarFamiliaAsync(token.FamiliaId, ct);
            Registrar(TipoEventoSeguridad.TokenRefrescoReutilizado, null, null, $"Familia {token.FamiliaId}", token.UsuarioId);
            await db.SaveChangesAsync(ct);
            throw new ExcepcionNoAutenticado("Sesión no válida.");
        }
        if (token.HaExpirado(ahora)) throw new ExcepcionNoAutenticado("La sesión ha caducado.");

        var usuario = await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == token.UsuarioId, ct);
        var org = usuario?.OrganizacionId is { } oid
            ? await db.Organizaciones.FirstOrDefaultAsync(o => o.Id == oid, ct) : null;
        if (usuario is null || !usuario.Activo || (usuario.OrganizacionId is not null && org is not { Activa: true }))
            throw new ExcepcionNoAutenticado("Sesión no válida.");

        token.Revocar(ahora); // la concurrencia optimista (rowversion) impide dos refrescos simultáneos del mismo token
        return await CrearSesionAsync(usuario, org, token.FamiliaId, ct);
    }

    public async Task CerrarSesionAsync(SolicitudRefresco s, CancellationToken ct)
    {
        var usuarioId = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        var hash = emisor.HashearTokenRefresco(s.TokenRefresco ?? "");
        var token = await db.TokensRefresco.FirstOrDefaultAsync(t => t.HashToken == hash && t.UsuarioId == usuarioId, ct);
        if (token is not null) await RevocarFamiliaAsync(token.FamiliaId, ct);
        Registrar(TipoEventoSeguridad.CierreSesion, null, null, null, usuarioId);
        await db.SaveChangesAsync(ct);
    }

    private async Task RevocarFamiliaAsync(Guid familiaId, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var activos = await db.TokensRefresco.Where(t => t.FamiliaId == familiaId && t.RevocadoEn == null).ToListAsync(ct);
        foreach (var t in activos) t.Revocar(ahora);
    }

    private async Task RevocarTodosAsync(Guid usuarioId, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var activos = await db.TokensRefresco.Where(t => t.UsuarioId == usuarioId && t.RevocadoEn == null).ToListAsync(ct);
        foreach (var t in activos) t.Revocar(ahora);
    }

    // ───────────── Perfil y contraseña ─────────────

    public async Task<PerfilUsuario> ObtenerPerfilAsync(CancellationToken ct)
    {
        var usuario = await UsuarioActualAsync(ct);
        var org = await OrganizacionDeAsync(usuario, ct);
        return Perfil(usuario, org);
    }

    public async Task CambiarContrasenaAsync(SolicitudCambioContrasena s, CancellationToken ct)
    {
        var usuario = await UsuarioActualAsync(ct);
        var v = new Validador();
        PoliticaContrasena.Validar(v, nameof(s.ContrasenaNueva), s.ContrasenaNueva, usuario.Email, opciones.LongitudMinimaContrasena);
        v.LanzarSiHayErrores();

        if (!hasher.Verificar(usuario.HashContrasena, s.ContrasenaActual ?? ""))
        {
            Registrar(TipoEventoSeguridad.InicioSesionFallido, usuario, usuario.Email, "Cambio de contraseña con contraseña actual incorrecta");
            await db.SaveChangesAsync(ct);
            throw new ExcepcionNoAutenticado("La contraseña actual no es correcta.");
        }

        usuario.CambiarContrasena(hasher.Hash(s.ContrasenaNueva));
        await RevocarTodosAsync(usuario.Id, ct);
        Registrar(TipoEventoSeguridad.ContrasenaCambiada, usuario, usuario.Email);
        await db.SaveChangesAsync(ct);
    }

    // ───────────── MFA ─────────────

    public async Task<ConfiguracionMfa> ConfigurarMfaAsync(CancellationToken ct)
    {
        var usuario = await UsuarioActualAsync(ct);
        var secreto = totp.GenerarSecreto();
        usuario.IniciarConfiguracionMfa(protector.Proteger(secreto));
        await db.SaveChangesAsync(ct);
        return new ConfiguracionMfa(secreto, totp.ConstruirUri("MedicalHub", usuario.Email, secreto));
    }

    public async Task<RespuestaSesion> ActivarMfaAsync(SolicitudActivarMfa s, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var usuario = await UsuarioActualAsync(ct);
        if (usuario.MfaSecretoProtegido is null || usuario.MfaActivado)
            throw new ExcepcionConflicto("No hay una configuración de autenticación en dos pasos pendiente.");

        var secreto = protector.Desproteger(usuario.MfaSecretoProtegido);
        if (!totp.Validar(secreto, s.Codigo ?? "", ahora, usuario.MfaUltimoPaso, out var paso))
        {
            Registrar(TipoEventoSeguridad.MfaFallido, usuario, usuario.Email, "Activación");
            await db.SaveChangesAsync(ct);
            throw new ExcepcionValidacion(new Dictionary<string, string[]> { [nameof(s.Codigo)] = ["El código no es correcto."] });
        }

        usuario.ActivarMfa(paso);
        await RevocarTodosAsync(usuario.Id, ct);
        Registrar(TipoEventoSeguridad.MfaActivado, usuario, usuario.Email);
        var org = await OrganizacionDeAsync(usuario, ct);
        return await CrearSesionAsync(usuario, org, Guid.NewGuid(), ct);
    }

    // ───────────── Utilidades ─────────────

    private async Task<RespuestaSesion> CrearSesionAsync(Usuario usuario, Domain.Organizaciones.Organizacion? org, Guid familiaId, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var mfaPendiente = usuario.RequiereMfa(org?.MfaObligatorio ?? true) && !usuario.MfaActivado;
        var acceso = emisor.CrearTokenAcceso(usuario, mfaPendiente, ahora);
        var (refresco, hash) = emisor.GenerarTokenRefresco();
        var duracion = TimeSpan.FromDays(opciones.DiasTokenRefresco);
        db.TokensRefresco.Add(new TokenRefresco(usuario.Id, familiaId, hash, ahora, duracion));
        await db.SaveChangesAsync(ct);
        return new RespuestaSesion(acceso.Token, acceso.Expira, refresco, ahora + duracion, mfaPendiente, Perfil(usuario, org, mfaPendiente));
    }

    private static PerfilUsuario Perfil(Usuario u, Domain.Organizaciones.Organizacion? org, bool? mfaPendiente = null)
        => new(u.Id, u.OrganizacionId, org?.Nombre, u.Email, u.NombreCompleto, u.ListaRoles.ToList(),
               u.MfaActivado, mfaPendiente ?? (u.RequiereMfa(org?.MfaObligatorio ?? true) && !u.MfaActivado), org?.ZonaHoraria);

    private async Task<Usuario> UsuarioActualAsync(CancellationToken ct)
    {
        var id = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        return await db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new ExcepcionNoAutenticado("Sesión no válida.");
    }

    private Task<Domain.Organizaciones.Organizacion?> OrganizacionDeAsync(Usuario u, CancellationToken ct)
        => u.OrganizacionId is { } oid
            ? db.Organizaciones.FirstOrDefaultAsync(o => o.Id == oid, ct)
            : Task.FromResult<Domain.Organizaciones.Organizacion?>(null);

    private string HashFicticio() => _hashFicticio ??= hasher.Hash("contrasena-ficticia-para-igualar-tiempos");

    private void Registrar(TipoEventoSeguridad tipo, Usuario? usuario, string? email, string? detalle = null, Guid? usuarioId = null)
        => db.EventosSeguridad.Add(new EventoSeguridad(tipo, reloj.GetUtcNow(),
            usuario?.OrganizacionId ?? contexto.OrganizacionId, usuario?.Id ?? usuarioId, email, contexto.Ip, detalle));
}
