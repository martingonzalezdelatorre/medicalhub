using MedicalHub.Domain.Catalogos;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Profesionales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalHub.Infrastructure.Persistencia.Configuraciones;

internal sealed class CuadranteConfiguracion : IEntityTypeConfiguration<Cuadrante>
{
    public void Configure(EntityTypeBuilder<Cuadrante> b)
    {
        b.ToTable("Cuadrantes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.EsEditable);
        b.Ignore(x => x.Dias);
        b.HasOne<Servicio>().WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ServicioId, x.Desde, x.Hasta });
    }
}

internal sealed class TurnoConfiguracion : IEntityTypeConfiguration<Turno>
{
    public void Configure(EntityTypeBuilder<Turno> b)
    {
        b.ToTable("Turnos");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Ignore(x => x.DuracionHoras);
        b.HasOne<Cuadrante>().WithMany().HasForeignKey(x => x.CuadranteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Servicio>().WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TipoTurno>().WithMany().HasForeignKey(x => x.TipoTurnoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CuadranteId, x.TipoTurnoId, x.Fecha }).IsUnique();
        b.HasIndex(x => new { x.ServicioId, x.InicioUtc });
        b.HasIndex(x => new { x.OrganizacionId, x.Fecha });
    }
}

internal sealed class NecesidadConfiguracion : IEntityTypeConfiguration<NecesidadCobertura>
{
    public void Configure(EntityTypeBuilder<NecesidadCobertura> b)
    {
        b.ToTable("NecesidadesCobertura");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasOne<Turno>().WithMany().HasForeignKey(x => x.TurnoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CategoriaProfesional>().WithMany().HasForeignKey(x => x.CategoriaProfesionalId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TurnoId, x.CategoriaProfesionalId }).IsUnique();
    }
}

internal sealed class AsignacionConfiguracion : IEntityTypeConfiguration<Asignacion>
{
    public void Configure(EntityTypeBuilder<Asignacion> b)
    {
        b.ToTable("Asignaciones");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(25);
        b.Ignore(x => x.EstaActiva);
        b.Property(x => x.MotivoCancelacion).HasMaxLength(200);
        b.HasOne<NecesidadCobertura>().WithMany().HasForeignKey(x => x.NecesidadId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Turno>().WithMany().HasForeignKey(x => x.TurnoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Profesional>().WithMany().HasForeignKey(x => x.ProfesionalId).OnDelete(DeleteBehavior.Restrict);
        // Respaldo de la regla: un profesional no ocupa dos veces la misma necesidad (las canceladas no cuentan).
        b.HasIndex(x => new { x.NecesidadId, x.ProfesionalId }).IsUnique().HasFilter("[Estado] <> 'Cancelada'");
        b.HasIndex(x => new { x.ProfesionalId, x.InicioUtc, x.FinUtc });
        b.HasIndex(x => x.TurnoId);
    }
}

internal sealed class VersionCuadranteConfiguracion : IEntityTypeConfiguration<VersionCuadrante>
{
    public void Configure(EntityTypeBuilder<VersionCuadrante> b)
    {
        b.ToTable("VersionesCuadrante");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasOne<Cuadrante>().WithMany().HasForeignKey(x => x.CuadranteId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CuadranteId, x.Numero }).IsUnique();
    }
}

internal sealed class PlantillaConfiguracion : IEntityTypeConfiguration<PlantillaCuadrante>
{
    public void Configure(EntityTypeBuilder<PlantillaCuadrante> b)
    {
        b.ToTable("PlantillasCuadrante");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.HasOne<Servicio>().WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ServicioId, x.Nombre }).IsUnique();

        b.OwnsMany(x => x.Lineas, l =>
        {
            l.ToTable("PlantillaLineas");
            l.WithOwner().HasForeignKey("PlantillaId");
            l.Property<int>("Id").ValueGeneratedOnAdd();
            l.HasKey("Id");
            l.HasOne<TipoTurno>().WithMany().HasForeignKey(x => x.TipoTurnoId).OnDelete(DeleteBehavior.Restrict);
            l.HasOne<CategoriaProfesional>().WithMany().HasForeignKey(x => x.CategoriaProfesionalId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Navigation(x => x.Lineas).HasField("_lineas").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
