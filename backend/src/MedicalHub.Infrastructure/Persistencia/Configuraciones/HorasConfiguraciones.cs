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
