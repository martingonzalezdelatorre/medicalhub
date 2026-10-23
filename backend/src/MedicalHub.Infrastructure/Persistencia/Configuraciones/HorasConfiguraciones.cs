using MedicalHub.Domain.Horas;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Profesionales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalHub.Infrastructure.Persistencia.Configuraciones;

internal sealed class RegistroHorasConfiguracion : IEntityTypeConfiguration<RegistroHoras>
{
    public void Configure(EntityTypeBuilder<RegistroHoras> b)
    {
        b.ToTable("RegistrosHoras");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Ignore(x => x.Minutos);
        b.HasOne<Asignacion>().WithMany().HasForeignKey(x => x.AsignacionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Profesional>().WithMany().HasForeignKey(x => x.ProfesionalId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.AsignacionId).IsUnique();
        b.HasIndex(x => new { x.OrganizacionId, x.ProfesionalId });
    }
}

internal sealed class FichajeConfiguracion : IEntityTypeConfiguration<Fichaje>
{
    public void Configure(EntityTypeBuilder<Fichaje> b)
    {
        b.ToTable("Fichajes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.EstaAbierto);
        b.Ignore(x => x.FueraDeZona);
        b.HasOne<Asignacion>().WithMany().HasForeignKey(x => x.AsignacionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Profesional>().WithMany().HasForeignKey(x => x.ProfesionalId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OrganizacionId, x.ProfesionalId, x.InicioUtc });
        b.HasIndex(x => x.AsignacionId);
        // Una sola jornada abierta por profesional, garantizada también en base de datos.
        b.HasIndex(x => x.ProfesionalId).IsUnique().HasFilter("[Estado] IN ('EnCurso', 'EnPausa')");

        b.OwnsMany(x => x.Pausas, p =>
        {
            p.ToTable("PausasFichaje");
            p.WithOwner().HasForeignKey("FichajeId");
            p.Property<int>("Id").ValueGeneratedOnAdd();
            p.HasKey("Id");
        });
        b.Navigation(x => x.Pausas).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AjusteHorasConfiguracion : IEntityTypeConfiguration<AjusteHoras>
{
    public void Configure(EntityTypeBuilder<AjusteHoras> b)
    {
        b.ToTable("AjustesHoras");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Motivo).HasMaxLength(AjusteHoras.MaxMotivo).IsRequired();
        b.HasOne<RegistroHoras>().WithMany().HasForeignKey(x => x.RegistroHorasId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.RegistroHorasId);
    }
}
