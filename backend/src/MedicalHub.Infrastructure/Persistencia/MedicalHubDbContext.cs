using System.Text.Json;
using MedicalHub.Application.Comun;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Identidad;
using MedicalHub.Domain.Organizaciones;
using MedicalHub.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MedicalHub.Infrastructure.Persistencia;

public sealed class MedicalHubDbContext(
    DbContextOptions<MedicalHubDbContext> opciones,
    IContextoActual contexto,
    TimeProvider reloj) : DbContext(opciones), IAppDbContext
{
    public DbSet<Organizacion> Organizaciones => Set<Organizacion>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TokenRefresco> TokensRefresco => Set<TokenRefresco>();
    public DbSet<EventoSeguridad> EventosSeguridad => Set<EventoSeguridad>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    /// <summary>
    /// Organización de la petición. Es null para usuarios de plataforma y para contextos sin sesión;
    /// en ambos casos el filtro solo deja ver filas sin organización (comportamiento seguro por defecto).
    /// </summary>
    private Guid? OrganizacionActualId => contexto.OrganizacionId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MedicalHubDbContext).Assembly);

        // Aislamiento multiempresa: TODA entidad IPerteneceAOrganizacion necesita su filtro aquí.
        modelBuilder.Entity<Usuario>().HasQueryFilter(u => u.OrganizacionId == OrganizacionActualId);
        modelBuilder.Entity<RegistroAuditoria>().HasQueryFilter(a => a.OrganizacionId == OrganizacionActualId);

        foreach (var tipo in modelBuilder.Model.GetEntityTypes().Where(t => !t.IsOwned() && typeof(Entidad).IsAssignableFrom(t.ClrType)))
            modelBuilder.Entity(tipo.ClrType).Property(nameof(Entidad.VersionFila)).IsRowVersion();
    }

    public override Task<int> SaveChangesAsync(bool aceptarCambios, CancellationToken ct = default)
    {
        ValidarAislamiento();
        AuditarCambios();
        return base.SaveChangesAsync(aceptarCambios, ct);
    }

    public override int SaveChanges(bool aceptarCambios)
    {
        ValidarAislamiento();
        AuditarCambios();
        return base.SaveChanges(aceptarCambios);
    }

    /// <summary>Defensa en profundidad: un usuario de sociedad nunca puede escribir filas de otra sociedad.</summary>
    private void ValidarAislamiento()
    {
        if (contexto.OrganizacionId is not { } organizacion) return;
        var ajena = ChangeTracker.Entries<IPerteneceAOrganizacion>()
            .Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                      && e.Entity.OrganizacionId != organizacion);
        if (ajena) throw new ExcepcionProhibido("Operación no permitida sobre datos de otra organización.");
    }

    // Se auditan por eventos de seguridad, no aquí: contadores de acceso y paso TOTP.
    private static readonly HashSet<string> Excluidas =
        [nameof(Entidad.Id), nameof(Entidad.VersionFila), nameof(Usuario.IntentosFallidos), nameof(Usuario.BloqueadoHasta), nameof(Usuario.MfaUltimoPaso)];
    private static readonly HashSet<string> Sensibles =
        [nameof(Usuario.HashContrasena), nameof(Usuario.MfaSecretoProtegido)];

    private void AuditarCambios()
    {
        var ahora = reloj.GetUtcNow();
        var nuevos = new List<RegistroAuditoria>();

        foreach (var e in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
        {
            if (e.Entity is IAuditable)
            {
                var cambios = ExtraerCambios(e);
                if (e.State == EntityState.Modified && cambios.Count == 0) continue;
                var orgId = e.Entity switch
                {
                    IPerteneceAOrganizacion p => p.OrganizacionId,
                    Organizacion o => o.Id,
                    _ => null
                };
                nuevos.Add(Nuevo(ahora, orgId, e.Metadata.ClrType.Name, ((Entidad)e.Entity).Id.ToString(), e.State, cambios));
            }
            else if (e.Entity is UsuarioRol rol && e.State is EntityState.Added or EntityState.Deleted)
            {
                var usuarioId = (Guid)e.Property("UsuarioId").CurrentValue!;
                var orgId = Usuarios.Local.FirstOrDefault(u => u.Id == usuarioId)?.OrganizacionId;
                nuevos.Add(Nuevo(ahora, orgId, nameof(UsuarioRol), usuarioId.ToString(), e.State,
                    new Dictionary<string, object?> { ["Rol"] = ((UsuarioRol)e.Entity).Rol.ToString() }));
            }
        }
        RegistrosAuditoria.AddRange(nuevos);
    }

    private RegistroAuditoria Nuevo(DateTimeOffset ahora, Guid? orgId, string entidad, string id, EntityState estado,
        Dictionary<string, object?> cambios)
        => new(orgId, contexto.UsuarioId, entidad, id,
            estado switch { EntityState.Added => "Alta", EntityState.Deleted => "Baja", _ => "Modificación" },
            cambios.Count == 0 ? null : JsonSerializer.Serialize(cambios), contexto.IdCorrelacion, ahora);

    private static Dictionary<string, object?> ExtraerCambios(EntityEntry e)
    {
        var resultado = new Dictionary<string, object?>();
        foreach (var p in e.Properties)
        {
            var nombre = p.Metadata.Name;
            if (Excluidas.Contains(nombre) || p.Metadata.IsShadowProperty()) continue;
            if (e.State == EntityState.Modified && !p.IsModified) continue;
            if (e.State == EntityState.Deleted) continue;

            if (Sensibles.Contains(nombre)) resultado[nombre] = "(protegido)";
            else if (e.State == EntityState.Modified) resultado[nombre] = new { antes = p.OriginalValue, despues = p.CurrentValue };
            else resultado[nombre] = p.CurrentValue;
        }
        return resultado;
    }
}
