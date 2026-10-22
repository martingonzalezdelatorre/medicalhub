using MedicalHub.Domain.Organizaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalHub.Infrastructure.Persistencia.Configuraciones;

internal sealed class OrganizacionConfiguracion : IEntityTypeConfiguration<Organizacion>
{
    public void Configure(EntityTypeBuilder<Organizacion> b)
    {
        b.ToTable("Organizaciones");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        b.Property(x => x.ZonaHoraria).HasMaxLength(64).IsRequired();
        b.Property(x => x.VersionFila).IsRowVersion();
        b.HasIndex(x => x.Nombre).IsUnique();
    }
}
