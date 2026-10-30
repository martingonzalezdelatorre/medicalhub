using MedicalHub.Domain.Catalogos;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Profesionales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalHub.Infrastructure.Persistencia.Configuraciones;

internal abstract class CatalogoConfiguracion<T>(string tabla) : IEntityTypeConfiguration<T> where T : EntidadCatalogo
{
    public void Configure(EntityTypeBuilder<T> b)
    {
        b.ToTable(tabla);
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(500);
        b.HasIndex(x => new { x.OrganizacionId, x.Nombre }).IsUnique();
    }
}

internal sealed class EspecialidadConfiguracion() : CatalogoConfiguracion<Especialidad>("Especialidades");
internal sealed class CategoriaProfesionalConfiguracion() : CatalogoConfiguracion<CategoriaProfesional>("CategoriasProfesionales");
internal sealed class CompetenciaConfiguracion() : CatalogoConfiguracion<Competencia>("Competencias");

internal sealed class CentroConfiguracion : IEntityTypeConfiguration<Centro>
{
    public void Configure(EntityTypeBuilder<Centro> b)
    {
        b.ToTable("Centros");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Direccion).HasMaxLength(300);
        b.Property(x => x.ZonaHoraria).HasMaxLength(64).IsRequired();
        b.HasIndex(x => new { x.OrganizacionId, x.Nombre }).IsUnique();

        b.OwnsMany(x => x.Responsables, r =>
        {
            r.ToTable("CentroResponsables");
            r.WithOwner().HasForeignKey("CentroId");
            r.Property<Guid>("CentroId");
            r.HasKey("CentroId", nameof(CentroResponsable.UsuarioId));
            r.Property(x => x.UsuarioId).ValueGeneratedNever();
            r.HasIndex(nameof(CentroResponsable.UsuarioId));
        });
        b.Navigation(x => x.Responsables).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ServicioConfiguracion : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> b)
    {
        b.ToTable("Servicios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(1000);
        b.Property(x => x.ConfiguracionReglas).HasMaxLength(4000);
        b.HasOne<Centro>().WithMany().HasForeignKey(x => x.CentroId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CentroId, x.Nombre }).IsUnique();

        b.OwnsMany(x => x.CategoriasAdmitidas, r =>
        {
            r.ToTable("ServicioCategorias");
            r.WithOwner().HasForeignKey("ServicioId");
            r.Property<Guid>("ServicioId");
            r.HasKey("ServicioId", nameof(ServicioCategoria.CategoriaProfesionalId));
            r.Property(x => x.CategoriaProfesionalId).ValueGeneratedNever();
            r.HasOne<CategoriaProfesional>().WithMany().HasForeignKey(x => x.CategoriaProfesionalId).OnDelete(DeleteBehavior.Restrict);
        });
        b.OwnsMany(x => x.CompetenciasRequeridas, r =>
        {
            r.ToTable("ServicioCompetencias");
            r.WithOwner().HasForeignKey("ServicioId");
            r.Property<Guid>("ServicioId");
            r.HasKey("ServicioId", nameof(ServicioCompetencia.CompetenciaId));
            r.Property(x => x.CompetenciaId).ValueGeneratedNever();
            r.HasOne<Competencia>().WithMany().HasForeignKey(x => x.CompetenciaId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Navigation(x => x.CategoriasAdmitidas).HasField("_categorias").UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.CompetenciasRequeridas).HasField("_competencias").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class TipoTurnoConfiguracion : IEntityTypeConfiguration<TipoTurno>
{
    public void Configure(EntityTypeBuilder<TipoTurno> b)
    {
        b.ToTable("TiposTurno");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Ignore(x => x.CruzaMedianoche);
        b.Ignore(x => x.Duracion);
        b.HasOne<Servicio>().WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ServicioId, x.Nombre }).IsUnique();
    }
}

internal sealed class RequisitoCoberturaConfiguracion : IEntityTypeConfiguration<RequisitoCobertura>
{
    public void Configure(EntityTypeBuilder<RequisitoCobertura> b)
    {
        b.ToTable("RequisitosCobertura");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasOne<Servicio>().WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TipoTurno>().WithMany().HasForeignKey(x => x.TipoTurnoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CategoriaProfesional>().WithMany().HasForeignKey(x => x.CategoriaProfesionalId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TipoTurnoId, x.CategoriaProfesionalId }).IsUnique();
    }
}

internal sealed class TarifaTurnoConfiguracion : IEntityTypeConfiguration<TarifaTurno>
{
    public void Configure(EntityTypeBuilder<TarifaTurno> b)
    {
        b.ToTable("TarifasTurno");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Importe).HasPrecision(12, 2);
        b.HasOne<TipoTurno>().WithMany().HasForeignKey(x => x.TipoTurnoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CategoriaProfesional>().WithMany().HasForeignKey(x => x.CategoriaProfesionalId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TipoTurnoId, x.CategoriaProfesionalId }).IsUnique();
    }
}

internal sealed class ProfesionalConfiguracion :IEntityTypeConfiguration<Profesional>
{
    public void Configure(EntityTypeBuilder<Profesional> b)
    {
        b.ToTable("Profesionales");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        b.Property(x => x.Apellidos).HasMaxLength(150).IsRequired();
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.Telefono).HasMaxLength(30);
        b.Property(x => x.NumeroColegiado).HasMaxLength(30);
        b.Ignore(x => x.NombreCompleto);
        b.HasOne<CategoriaProfesional>().WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OrganizacionId, x.NumeroColegiado }).IsUnique().HasFilter("[NumeroColegiado] IS NOT NULL");
        b.HasIndex(x => x.UsuarioId).IsUnique().HasFilter("[UsuarioId] IS NOT NULL");
        b.HasIndex(x => new { x.OrganizacionId, x.Apellidos, x.Nombre });

        b.OwnsMany(x => x.Especialidades, r =>
        {
            r.ToTable("ProfesionalEspecialidades");
            r.WithOwner().HasForeignKey("ProfesionalId");
            r.Property<Guid>("ProfesionalId");
            r.HasKey("ProfesionalId", nameof(ProfesionalEspecialidad.EspecialidadId));
            r.Property(x => x.EspecialidadId).ValueGeneratedNever();
            r.HasOne<Especialidad>().WithMany().HasForeignKey(x => x.EspecialidadId).OnDelete(DeleteBehavior.Restrict);
        });
        b.OwnsMany(x => x.Competencias, r =>
        {
            r.ToTable("ProfesionalCompetencias");
            r.WithOwner().HasForeignKey("ProfesionalId");
            r.Property<Guid>("ProfesionalId");
            r.HasKey("ProfesionalId", nameof(ProfesionalCompetencia.CompetenciaId));
            r.Property(x => x.CompetenciaId).ValueGeneratedNever();
            r.HasOne<Competencia>().WithMany().HasForeignKey(x => x.CompetenciaId).OnDelete(DeleteBehavior.Restrict);
        });
        b.OwnsMany(x => x.ServiciosAutorizados, r =>
        {
            r.ToTable("ProfesionalServicios");
            r.WithOwner().HasForeignKey("ProfesionalId");
            r.Property<Guid>("ProfesionalId");
            r.HasKey("ProfesionalId", nameof(ProfesionalServicio.ServicioId));
            r.Property(x => x.ServicioId).ValueGeneratedNever();
            r.HasOne<Servicio>().WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Navigation(x => x.Especialidades).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Competencias).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.ServiciosAutorizados).HasField("_servicios").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
