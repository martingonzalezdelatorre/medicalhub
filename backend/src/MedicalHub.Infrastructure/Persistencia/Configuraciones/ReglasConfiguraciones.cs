using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Profesionales;
using MedicalHub.Domain.Reglas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalHub.Infrastructure.Persistencia.Configuraciones;

internal sealed class ReglaPlanificacionConfiguracion : IEntityTypeConfiguration<ReglaPlanificacion>
{
    public void Configure(EntityTypeBuilder<ReglaPlanificacion> b)
    {
        b.ToTable("ReglasPlanificacion");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Severidad).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.Property(x => x.Descripcion).HasMaxLength(500);
        b.Property(x => x.Fundamento).HasMaxLength(1000);
        b.Property(x => x.ParametrosJson).HasMaxLength(2000);
        b.HasOne<Servicio>().WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.OrganizacionId, x.Tipo });
    }
}

internal sealed class ExcepcionReglaConfiguracion : IEntityTypeConfiguration<ExcepcionRegla>
{
    public void Configure(EntityTypeBuilder<ExcepcionRegla> b)
    {
        b.ToTable("ExcepcionesRegla");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ClaveRegla).HasMaxLength(60).IsRequired();
        b.Property(x => x.Codigo).HasMaxLength(60).IsRequired();
        b.Property(x => x.Mensaje).HasMaxLength(500).IsRequired();
        b.Property(x => x.Justificacion).HasMaxLength(1000).IsRequired();
        b.HasOne<Asignacion>().WithMany().HasForeignKey(x => x.AsignacionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Turno>().WithMany().HasForeignKey(x => x.TurnoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Profesional>().WithMany().HasForeignKey(x => x.ProfesionalId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.AsignacionId, x.ClaveRegla }).IsUnique();
        b.HasIndex(x => new { x.OrganizacionId, x.Fecha });
    }
}
