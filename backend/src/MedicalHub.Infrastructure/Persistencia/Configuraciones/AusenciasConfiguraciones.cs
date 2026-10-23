using MedicalHub.Domain.Ausencias;
using MedicalHub.Domain.Estructura;
using MedicalHub.Domain.Notificaciones;
using MedicalHub.Domain.Planificacion;
using MedicalHub.Domain.Profesionales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalHub.Infrastructure.Persistencia.Configuraciones;

internal sealed class AusenciaConfiguracion : IEntityTypeConfiguration<Ausencia>
{
    public void Configure(EntityTypeBuilder<Ausencia> b)
    {
        b.ToTable("Ausencias");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Comentario).HasMaxLength(Ausencia.MaxComentario);
        b.Ignore(x => x.Dias);
        b.Ignore(x => x.EstaVigente);
        b.HasOne<Profesional>().WithMany().HasForeignKey(x => x.ProfesionalId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ProfesionalId, x.Desde, x.Hasta });
        b.HasIndex(x => new { x.OrganizacionId, x.Estado });
    }
}

internal sealed class DecisionAusenciaConfiguracion : IEntityTypeConfiguration<DecisionAusencia>
{
    public void Configure(EntityTypeBuilder<DecisionAusencia> b)
    {
        b.ToTable("DecisionesAusencia");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Decision).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Motivo).HasMaxLength(500);
        b.HasOne<Ausencia>().WithMany().HasForeignKey(x => x.AusenciaId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.AusenciaId);
    }
}

internal sealed class DisponibilidadConfiguracion : IEntityTypeConfiguration<Disponibilidad>
{
    public void Configure(EntityTypeBuilder<Disponibilidad> b)
    {
        b.ToTable("Disponibilidades");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Comentario).HasMaxLength(300);
        b.Property(x => x.MotivoDecision).HasMaxLength(500);
        b.HasOne<Profesional>().WithMany().HasForeignKey(x => x.ProfesionalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<TipoTurno>().WithMany().HasForeignKey(x => x.TipoTurnoId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ProfesionalId, x.Desde, x.Hasta });
        b.HasIndex(x => new { x.OrganizacionId, x.Estado });
    }
}

internal sealed class VacanteConfiguracion : IEntityTypeConfiguration<Vacante>
{
    public void Configure(EntityTypeBuilder<Vacante> b)
    {
        b.ToTable("Vacantes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Motivo).HasMaxLength(200).IsRequired();
        b.HasOne<NecesidadCobertura>().WithMany().HasForeignKey(x => x.NecesidadId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Turno>().WithMany().HasForeignKey(x => x.TurnoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Asignacion>().WithMany().HasForeignKey(x => x.AsignacionOriginalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Ausencia>().WithMany().HasForeignKey(x => x.AusenciaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Profesional>().WithMany().HasForeignKey(x => x.ProfesionalAusenteId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.AsignacionOriginalId).IsUnique();
        b.HasIndex(x => new { x.OrganizacionId, x.Estado });
    }
}

internal sealed class OfertaSustitucionConfiguracion : IEntityTypeConfiguration<OfertaSustitucion>
{
    public void Configure(EntityTypeBuilder<OfertaSustitucion> b)
    {
        b.ToTable("OfertasSustitucion");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.MotivoRespuesta).HasMaxLength(OfertaSustitucion.MaxMotivo);
        b.Property(x => x.MotivoDecision).HasMaxLength(OfertaSustitucion.MaxMotivo);
        b.Ignore(x => x.EstaAbierta);
        b.HasOne<Vacante>().WithMany().HasForeignKey(x => x.VacanteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Profesional>().WithMany().HasForeignKey(x => x.ProfesionalId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Asignacion>().WithMany().HasForeignKey(x => x.AsignacionId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.VacanteId, x.ProfesionalId });
        b.HasIndex(x => new { x.ProfesionalId, x.Estado });
        b.HasIndex(x => new { x.OrganizacionId, x.Estado });
    }
}

internal sealed class NotificacionConfiguracion : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> b)
    {
        b.ToTable("Notificaciones");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Titulo).HasMaxLength(Notificacion.MaxTitulo).IsRequired();
        b.Property(x => x.Mensaje).HasMaxLength(Notificacion.MaxMensaje).IsRequired();
        b.Ignore(x => x.Leida);
        b.HasIndex(x => new { x.UsuarioId, x.FechaLectura, x.Fecha });
    }
}
