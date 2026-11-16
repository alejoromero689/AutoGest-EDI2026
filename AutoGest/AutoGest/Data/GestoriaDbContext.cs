using AutoGest.Models;
using Microsoft.EntityFrameworkCore;

//Configura entidades, relaciones y restricciones
//se preparando dos requisitos de la Unidad 3: entidades relacionadas y datos iniciales (seed).

namespace AutoGest.Data;

public class GestoriaDbContext : DbContext
{
    public GestoriaDbContext(
        DbContextOptions<GestoriaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Marca> Marcas => Set<Marca>();

    public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relación: una marca tiene muchos vehículos.
        modelBuilder.Entity<Vehiculo>()
            .HasOne(v => v.Marca)
            .WithMany(m => m.Vehiculos)
            .HasForeignKey(v => v.MarcaId)
            .OnDelete(DeleteBehavior.Restrict);

        // El precio se guarda con dos decimales.
        modelBuilder.Entity<Vehiculo>()
            .Property(v => v.Precio)
            .HasPrecision(12, 2);

        // No permite patentes repetidas.
        modelBuilder.Entity<Vehiculo>()
            .HasIndex(v => v.Patente)
            .IsUnique();

        // Datos iniciales para la tabla Marcas.
        modelBuilder.Entity<Marca>().HasData(
            new Marca { Id = 1, Nombre = "Toyota" },
            new Marca { Id = 2, Nombre = "Ford" },
            new Marca { Id = 3, Nombre = "Volkswagen" },
            new Marca { Id = 4, Nombre = "Chevrolet" }
        );
    }
}
