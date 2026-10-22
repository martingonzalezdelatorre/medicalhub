using MedicalHub.Domain.Identidad;
using MedicalHub.Domain.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalHub.Infrastructure.Persistencia.Configuraciones;

internal sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> b)
    {
        b.ToTable("Usuarios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Email).HasMaxLength(254).IsRequired();
        b.Property(x => x.NombreCompleto).HasMaxLength(200).IsRequired();
        b.Property(x => x.HashContrasena).HasMaxLength(512).IsRequired();
        b.Property(x => x.MfaSecretoProtegido).HasMaxLength(1024);
        // El correo es único en toda la plataforma (una persona = una sociedad en el MVP).
        b.HasIndex(x => x.Email).IsUnique();
        b.HasIndex(x => x.OrganizacionId);
        b.Ignore(x => x.ListaRoles);

        b.OwnsMany(x => x.Roles, r =>
        {
            r.ToTable("UsuarioRoles");
            r.WithOwner().HasForeignKey("UsuarioId");
            r.Property<Guid>("UsuarioId");
            r.Property(x => x.Rol).HasConversion<string>().HasMaxLength(40);
            r.HasKey("UsuarioId", nameof(UsuarioRol.Rol));
        });
        b.Navigation(x => x.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class TokenRefrescoConfiguracion : IEntityTypeConfiguration<TokenRefresco>
{
    public void Configure(EntityTypeBuilder<TokenRefresco> b)
    {
        b.ToTable("TokensRefresco");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.HashToken).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.HashToken).IsUnique();
        b.HasIndex(x => x.UsuarioId);
        b.HasIndex(x => x.FamiliaId);
    }
}

internal sealed class EventoSeguridadConfiguracion : IEntityTypeConfiguration<EventoSeguridad>
{
    public void Configure(EntityTypeBuilder<EventoSeguridad> b)
    {
        b.ToTable("EventosSeguridad");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Email).HasMaxLength(254);
        b.Property(x => x.Ip).HasMaxLength(64);
        b.Property(x => x.Detalle).HasMaxLength(500);
        b.HasIndex(x => x.Fecha);
        b.HasIndex(x => x.UsuarioId);
    }
}

internal sealed class RegistroAuditoriaConfiguracion : IEntityTypeConfiguration<RegistroAuditoria>
{
    public void Configure(EntityTypeBuilder<RegistroAuditoria> b)
    {
        b.ToTable("RegistrosAuditoria");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Entidad).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntidadId).HasMaxLength(64).IsRequired();
        b.Property(x => x.Accion).HasMaxLength(30).IsRequired();
        b.Property(x => x.IdCorrelacion).HasMaxLength(64);
        b.HasIndex(x => new { x.OrganizacionId, x.Fecha });
        b.HasIndex(x => new { x.Entidad, x.EntidadId });
    }
}
