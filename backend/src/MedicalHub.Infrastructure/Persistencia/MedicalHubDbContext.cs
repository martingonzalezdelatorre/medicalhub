using MedicalHub.Domain.Organizaciones;
using Microsoft.EntityFrameworkCore;

namespace MedicalHub.Infrastructure.Persistencia;

public sealed class MedicalHubDbContext(DbContextOptions<MedicalHubDbContext> opciones) : DbContext(opciones)
{
    public DbSet<Organizacion> Organizaciones => Set<Organizacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(MedicalHubDbContext).Assembly);
}
