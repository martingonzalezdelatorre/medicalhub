using System.Text.Json;
using System.Text.Json.Serialization;
using MedicalHub.Application.Comun;
using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Horas;
using MedicalHub.Domain.Notificaciones;
using MedicalHub.Domain.Catalogos;
using MedicalHub.Domain.Comun;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Identidad;
using MedicalHub.Domain.Organizaciones;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Reglas;
using MedicalHub.Domain.Profesionales;
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
    public DbSet<Especialidad> Especialidades => Set<Especialidad>();
    public DbSet<CategoriaProfesional> CategoriasProfesionales => Set<CategoriaProfesional>();
    public DbSet<Competencia> Competencias => Set<Competencia>();
    public DbSet<Centro> Centros => Set<Centro>();
    public DbSet<Servicio> Servicios => Set<Servicio>();
    public DbSet<TipoTurno> TiposTurno => Set<TipoTurno>();
    public DbSet<RequisitoCobertura> RequisitosCobertura => Set<RequisitoCobertura>();
    public DbSet<Profesional> Profesionales => Set<Profesional>();
    public DbSet<Cuadrante> Cuadrantes => Set<Cuadrante>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<NecesidadCobertura> Necesidades => Set<NecesidadCobertura>();
    public DbSet<Asignacion> Asignaciones => Set<Asignacion>();
    public DbSet<VersionCuadrante> VersionesCuadrante => Set<VersionCuadrante>();
    public DbSet<PlantillaCuadrante> Plantillas => Set<PlantillaCuadrante>();
    public DbSet<ReglaPlanificacion> Reglas => Set<ReglaPlanificacion>();
    public DbSet<ExcepcionRegla> Excepciones => Set<ExcepcionRegla>();
    public DbSet<Ausencia> Ausencias => Set<Ausencia>();
    public DbSet<DecisionAusencia> DecisionesAusencia => Set<DecisionAusencia>();
    public DbSet<Disponibilidad> Disponibilidades => Set<Disponibilidad>();
    public DbSet<Vacante> Vacantes => Set<Vacante>();
    public DbSet<OfertaSustitucion> Ofertas => Set<OfertaSustitucion>();
    public DbSet<RegistroHoras> RegistrosHoras => Set<RegistroHoras>();
    public DbSet<AjusteHoras> AjustesHoras => Set<AjusteHoras>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    /// <summary>
    /// Organización de la petición. Es null para usuarios de plataforma y para contextos sin sesión;
    /// en ambos casos el filtro solo deja ver filas sin organización (comportamiento seguro por defecto).
    /// </summary>
    private Guid? OrganizacionActualId => contexto.OrganizacionId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MedicalHubDbContext).Assembly);

        // Aislamiento multiempresa: se aplica automáticamente a TODA entidad IPerteneceAOrganizacion
        // (y una prueba verifica que ninguna se quede sin filtro).
        var aplicar = typeof(MedicalHubDbContext).GetMethod(nameof(AplicarFiltroOrganizacion),
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        foreach (var tipo in modelBuilder.Model.GetEntityTypes()
                     .Where(t => !t.IsOwned() && t.BaseType is null && typeof(IPerteneceAOrganizacion).IsAssignableFrom(t.ClrType))
                     .Select(t => t.ClrType).ToList())
            aplicar.MakeGenericMethod(tipo).Invoke(this, [modelBuilder]);

        foreach (var tipo in modelBuilder.Model.GetEntityTypes().Where(t => !t.IsOwned() && typeof(Entidad).IsAssignableFrom(t.ClrType)))
            modelBuilder.Entity(tipo.ClrType).Property(nameof(Entidad.VersionFila)).IsRowVersion();
    }

    private void AplicarFiltroOrganizacion<T>(ModelBuilder modelBuilder) where T : class, IPerteneceAOrganizacion
        => modelBuilder.Entity<T>().HasQueryFilter(e => e.OrganizacionId == OrganizacionActualId);

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

    public Task<T> EnTransaccionAsync<T>(Func<CancellationToken, Task<T>> operacion, CancellationToken ct = default)
        => Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            ChangeTracker.Clear(); // cada intento parte de un estado limpio
            await using var tx = await Database.BeginTransactionAsync(ct);
            var resultado = await operacion(ct);
            await tx.CommitAsync(ct);
            return resultado;
        });

    public async Task BloquearAsync(string recurso, CancellationToken ct = default)
    {
        var resultado = await Database
            .SqlQuery<int>($"DECLARE @r int; EXEC @r = sp_getapplock @Resource = {recurso}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; SELECT @r AS Value")
            .ToListAsync(ct); // SQL no componible: se materializa tal cual
        if (resultado.Single() < 0) throw new ExcepcionConflicto("La operación está siendo realizada por otro usuario. Inténtelo de nuevo.");
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

    private static readonly JsonSerializerOptions OpcionesJson = new() { Converters = { new JsonStringEnumConverter() } };

    private void AuditarCambios()
    {
        var ahora = reloj.GetUtcNow();
        var nuevos = new List<RegistroAuditoria>();
        var entradas = ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();

        foreach (var e in entradas)
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
            else if (e.Metadata.IsOwned() && e.State is EntityState.Added or EntityState.Deleted)
            {
                // Colecciones poseídas (roles, especialidades, competencias…): se auditan bajo el id de su propietario.
                var propiedad = e.Metadata.FindOwnership()!;
                var propietarioId = (Guid)e.Property(propiedad.Properties[0].Name).CurrentValue!;
                var propietario = entradas.Concat(ChangeTracker.Entries())
                    .FirstOrDefault(x => x.Metadata == propiedad.PrincipalEntityType && x.Entity is Entidad ent && ent.Id == propietarioId);
                if (propietario?.Entity is not IAuditable) continue;

                var cambios = new Dictionary<string, object?>();
                foreach (var p in e.Properties.Where(p => !p.Metadata.IsShadowProperty()))
                    cambios[p.Metadata.Name] = p.CurrentValue;
                nuevos.Add(Nuevo(ahora, (propietario.Entity as IPerteneceAOrganizacion)?.OrganizacionId,
                    e.Metadata.ClrType.Name, propietarioId.ToString(), e.State, cambios));
            }
        }
        RegistrosAuditoria.AddRange(nuevos);
    }

    private RegistroAuditoria Nuevo(DateTimeOffset ahora, Guid? orgId, string entidad, string id, EntityState estado,
        Dictionary<string, object?> cambios)
        => new(orgId, contexto.UsuarioId, entidad, id,
            estado switch { EntityState.Added => "Alta", EntityState.Deleted => "Baja", _ => "Modificación" },
            cambios.Count == 0 ? null : JsonSerializer.Serialize(cambios, OpcionesJson), contexto.IdCorrelacion, ahora);

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
