using MedicalHub.Application.Comun;
using MedicalHub.Domain.Identidad;
using MedicalHub.Domain.Profesionales;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Application.Planificacion;

public sealed class ProfesionalesServicio(IAppDbContext db, IContextoActual contexto, TimeProvider reloj)
{
    public async Task<IReadOnlyList<ProfesionalDto>> ListarAsync(FiltroProfesionales f, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var q = Visibles();
        if (f.Activo is { } a) q = q.Where(p => p.Activo == a);
        if (f.CategoriaId is { } c) q = q.Where(p => p.CategoriaId == c);
        if (f.ServicioId is { } s) q = q.Where(p => p.ServiciosAutorizados.Any(x => x.ServicioId == s));
        if (!string.IsNullOrWhiteSpace(f.Texto))
        {
            var t = f.Texto.Trim();
            q = q.Where(p => p.Nombre.Contains(t) || p.Apellidos.Contains(t) || (p.NumeroColegiado != null && p.NumeroColegiado.Contains(t)));
        }
        var lista = await q.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombre).Take(500).ToListAsync(ct);
        return lista.Select(p => ADto(p, ocultarContacto: contexto.EsAlcanceRestringido())).ToList();
    }

    public async Task<ProfesionalDto> ObtenerAsync(Guid id, CancellationToken ct)
        => ADto(await BuscarAsync(id, ct), ocultarContacto: contexto.EsAlcanceRestringido());

    /// <summary>El profesional consulta su propia ficha.</summary>
    public async Task<ProfesionalDto> MiPerfilAsync(CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        var uid = contexto.UsuarioId ?? throw new ExcepcionNoAutenticado("No autenticado.");
        var p = await db.Profesionales.FirstOrDefaultAsync(x => x.UsuarioId == uid, ct)
            ?? throw new ExcepcionNoEncontrado("Su usuario no está vinculado a ninguna ficha profesional.");
        return ADto(p, false);
    }

    public async Task<ProfesionalDto> CrearAsync(SolicitudProfesional s, CancellationToken ct)
    {
        var org = contexto.ExigirOrganizacion();
        await ValidarAsync(s, null, ct);

        var p = new Profesional(org, s.Nombre, s.Apellidos, s.CategoriaId, reloj.GetUtcNow());
        Aplicar(p, s);
        db.Profesionales.Add(p);
        await db.SaveChangesAsync(ct);
        return ADto(p, false);
    }

    public async Task<ProfesionalDto> ActualizarAsync(Guid id, SolicitudProfesional s, CancellationToken ct)
    {
        var p = await BuscarAsync(id, ct);
        await ValidarAsync(s, id, ct);
        Aplicar(p, s);
        await db.SaveChangesAsync(ct);
        return ADto(p, false);
    }

    public async Task CambiarEstadoAsync(Guid id, bool activo, CancellationToken ct)
    {
        var p = await BuscarAsync(id, ct);
        if (activo) p.Activar(); else p.Desactivar();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Historial de cambios relevantes de la ficha (datos, especialidades, competencias y servicios).</summary>
    public async Task<IReadOnlyList<CambioHistorialDto>> HistorialAsync(Guid id, CancellationToken ct)
    {
        await BuscarAsync(id, ct);
        var clave = id.ToString();
        string[] entidades = [nameof(Profesional), nameof(ProfesionalEspecialidad), nameof(ProfesionalCompetencia), nameof(ProfesionalServicio)];
        var registros = await db.RegistrosAuditoria.Where(a => a.EntidadId == clave && entidades.Contains(a.Entidad))
            .OrderByDescending(a => a.Fecha).Take(200).ToListAsync(ct);

        var usuarioIds = registros.Where(r => r.UsuarioId != null).Select(r => r.UsuarioId!.Value).Distinct().ToList();
        var nombres = await db.Usuarios.Where(u => usuarioIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.NombreCompleto, ct);
        return registros.Select(r => new CambioHistorialDto(r.Fecha, r.UsuarioId is { } u ? nombres.GetValueOrDefault(u) : null,
            r.Entidad, r.Accion, r.Cambios)).ToList();
    }

    // ── Internos ──

    private IQueryable<Profesional> Visibles()
    {
        if (!contexto.EsAlcanceRestringido()) return db.Profesionales;
        var centros = CentrosServicio.Visibles(db, contexto).Select(c => c.Id);
        var servicios = db.Servicios.Where(s => centros.Contains(s.CentroId)).Select(s => s.Id);
        return db.Profesionales.Where(p => p.ServiciosAutorizados.Any(x => servicios.Contains(x.ServicioId)));
    }

    private async Task<Profesional> BuscarAsync(Guid id, CancellationToken ct)
    {
        contexto.ExigirOrganizacion();
        return await Visibles().FirstOrDefaultAsync(p => p.Id == id, ct) ?? throw new ExcepcionNoEncontrado("Profesional no encontrado.");
    }

    private async Task ValidarAsync(SolicitudProfesional s, Guid? idActual, CancellationToken ct)
    {
        var v = new Validador().Requerido(nameof(s.Nombre), s.Nombre, 100).Requerido(nameof(s.Apellidos), s.Apellidos, 150);
        v.Cuando(s.CategoriaId == Guid.Empty, nameof(s.CategoriaId), "Es obligatoria.");
        if (s.CategoriaId != Guid.Empty)
            await db.CategoriasProfesionales.ExigirExistentesAsync([s.CategoriaId], nameof(s.CategoriaId), v, ct);
        await db.Especialidades.ExigirExistentesAsync(s.EspecialidadesIds, nameof(s.EspecialidadesIds), v, ct);
        await db.Competencias.ExigirExistentesAsync(s.Competencias?.Select(c => c.CompetenciaId), nameof(s.Competencias), v, ct);
        await db.Servicios.ExigirExistentesAsync(s.ServiciosIds, nameof(s.ServiciosIds), v, ct);

        if (s.UsuarioId is { } uid)
        {
            if (!await db.Usuarios.AnyAsync(u => u.Id == uid && u.Roles.Any(r => r.Rol == Rol.ProfesionalSanitario), ct))
                v.Agregar(nameof(s.UsuarioId), "El usuario debe existir y tener el rol de profesional sanitario.");
            else if (await db.Profesionales.AnyAsync(p => p.UsuarioId == uid && p.Id != idActual, ct))
                v.Agregar(nameof(s.UsuarioId), "Ese usuario ya está vinculado a otra ficha.");
        }
        v.LanzarSiHayErrores();

        if (!string.IsNullOrWhiteSpace(s.NumeroColegiado))
        {
            var n = s.NumeroColegiado.Trim();
            if (await db.Profesionales.AnyAsync(p => p.NumeroColegiado == n && p.Id != idActual, ct))
                throw new ExcepcionConflicto("Ya existe un profesional con ese número de colegiado.");
        }
    }

    private static void Aplicar(Profesional p, SolicitudProfesional s)
    {
        p.ActualizarDatos(s.Nombre, s.Apellidos, s.Email, s.Telefono, s.NumeroColegiado, s.CategoriaId);
        p.VincularUsuario(s.UsuarioId);
        p.EstablecerEspecialidades(s.EspecialidadesIds ?? []);
        p.EstablecerCompetencias((s.Competencias ?? []).Select(c => (c.CompetenciaId, c.VigenteHasta)));
        p.EstablecerServiciosAutorizados(s.ServiciosIds ?? []);
    }

    private static ProfesionalDto ADto(Profesional p, bool ocultarContacto) => new(
        p.Id, p.UsuarioId, p.Nombre, p.Apellidos,
        ocultarContacto ? null : p.Email, ocultarContacto ? null : p.Telefono,
        p.NumeroColegiado, p.CategoriaId,
        p.Especialidades.Select(e => e.EspecialidadId).ToList(),
        p.Competencias.Select(c => new CompetenciaProfesionalDto(c.CompetenciaId, c.VigenteHasta)).ToList(),
        p.ServiciosAutorizados.Select(x => x.ServicioId).ToList(),
        p.Activo);
}
