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

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<Compra> Compras => Set<Compra>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>().HasIndex(c => c.Documento).IsUnique();
        modelBuilder.Entity<Venta>().HasIndex(v => v.VehiculoId).IsUnique();
        modelBuilder.Entity<Compra>().HasIndex(c => c.VehiculoId).IsUnique();
        modelBuilder.Entity<Venta>().Property(v => v.Importe).HasPrecision(12, 2);
        modelBuilder.Entity<Compra>().Property(c => c.Importe).HasPrecision(12, 2);
        modelBuilder.Entity<Venta>().HasOne(v => v.Cliente).WithMany()
            .HasForeignKey(v => v.ClienteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Venta>().HasOne(v => v.Vehiculo).WithMany()
            .HasForeignKey(v => v.VehiculoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Compra>().HasOne(c => c.Cliente).WithMany()
            .HasForeignKey(c => c.ClienteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Compra>().HasOne(c => c.Vehiculo).WithMany()
            .HasForeignKey(c => c.VehiculoId).OnDelete(DeleteBehavior.Restrict);

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
