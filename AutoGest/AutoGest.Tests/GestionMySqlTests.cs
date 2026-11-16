using AutoGest.Data;
using AutoGest.Models;
using AutoGest.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySqlConnector;

namespace AutoGest.Tests;

public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AUTOGEST_TEST_CONNECTION_STRING")))
            Skip = "Definí AUTOGEST_TEST_CONNECTION_STRING para ejecutar las pruebas contra MySQL 8.4.";
    }
}

// La colección evita interferencias entre pruebas que comparten la base descartable.
[CollectionDefinition("MySQL", DisableParallelization = true)]
public class MySqlCollection;

[Collection("MySQL")]
public class GestionMySqlTests : IAsyncLifetime
{
    private IDbContextFactory<GestoriaDbContext> factory = default!;
    private GestionService servicio = default!;
    private readonly GestionState state = new();

    public async Task InitializeAsync()
    {
        var conexion = Environment.GetEnvironmentVariable("AUTOGEST_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(conexion)) return;
        var builder = new MySqlConnectionStringBuilder(conexion);
        if (!builder.Database.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Las pruebas requieren una base descartable cuyo nombre termine en _test.");
        factory = new GestoriaFactory(new DbContextOptionsBuilder<GestoriaDbContext>()
            .UseMySql(conexion, new MySqlServerVersion(new Version(8, 4, 0))).Options);
        servicio = new GestionService(factory, state);
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
        await db.Ventas.ExecuteDeleteAsync();
        await db.Compras.ExecuteDeleteAsync();
        await db.Vehiculos.ExecuteDeleteAsync();
        await db.Clientes.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Cliente Cliente, Vehiculo Vehiculo)> Preparar()
    {
        var cliente = new Cliente { Nombre = "Ana Pérez", Documento = "30123456", Email = "ana@example.com" };
        await servicio.GuardarClienteAsync(cliente);
        await using var db = await factory.CreateDbContextAsync();
        var vehiculo = new Vehiculo { MarcaId = 1, Modelo = "Corolla", Precio = 10000, Anio = 2024 };
        db.Vehiculos.Add(vehiculo);
        await db.SaveChangesAsync();
        return (cliente, vehiculo);
    }

    private static OperacionForm Form(Cliente c, Vehiculo v, decimal importe = 9000) =>
        new() { ClienteId = c.Id, VehiculoId = v.Id, Importe = importe, Fecha = DateTime.Today };

    [MySqlFact]
    public async Task MigracionConservaVehiculosDelProyectoOriginal()
    {
        await using var db = await factory.CreateDbContextAsync();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261114204603_AgregarKilometrajeVehiculo");
        var vehiculo = new Vehiculo { MarcaId = 1, Modelo = "Unidad existente", Anio = 2023, Precio = 10000 };
        db.Vehiculos.Add(vehiculo);
        await db.SaveChangesAsync();
        await migrator.MigrateAsync();
        db.ChangeTracker.Clear();
        Assert.Equal("Unidad existente", (await db.Vehiculos.SingleAsync(v => v.Id == vehiculo.Id)).Modelo);
        Assert.Equal(1, (await servicio.ResumenAsync()).Disponibles);
    }

    [MySqlFact]
    public async Task ClientePermiteCrudBuscaYPagina()
    {
        for (int i = 0; i < 10; i++)
            await servicio.GuardarClienteAsync(new Cliente { Nombre = $"Cliente {i:D2}", Documento = $"400000{i:D2}", Telefono = "1122334455" });
        var pagina = await servicio.ClientesAsync("Cliente", 2);
        Assert.Equal(10, pagina.Total);
        Assert.Equal(2, pagina.Items.Count);
        var cliente = pagina.Items[0];
        cliente.Nombre = "Editado";
        await servicio.GuardarClienteAsync(cliente);
        Assert.Single((await servicio.ClientesAsync("Editado", 1)).Items);
        await servicio.EliminarClienteAsync(cliente.Id);
        Assert.Null(await servicio.ClienteAsync(cliente.Id));
    }

    [MySqlFact]
    public async Task DocumentoDuplicadoYEdicionSimultaneaSeRechazan()
    {
        var (cliente, _) = await Preparar();
        await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.GuardarClienteAsync(
            new Cliente { Nombre = "Duplicado", Documento = cliente.Documento, Email = "otro@example.com" }));
        var primera = (await servicio.ClienteAsync(cliente.Id))!;
        var segunda = (await servicio.ClienteAsync(cliente.Id))!;
        primera.Nombre = "Nombre actualizado";
        await servicio.GuardarClienteAsync(primera);
        segunda.Nombre = "Edición obsoleta";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => servicio.GuardarClienteAsync(segunda));
    }

    [MySqlFact]
    public async Task CompraVentaYEliminacionConservanStock()
    {
        var (cliente, vehiculo) = await Preparar();
        await servicio.GuardarOperacionAsync(false, Form(cliente, vehiculo));
        var stock = await servicio.StockAsync("Toyota", "disponible", 1);
        Assert.Single(stock.Items);
        Assert.Equal(9000m, stock.Items[0].Costo);
        Assert.Equal(1, (await servicio.ResumenAsync()).Disponibles);
        await servicio.GuardarOperacionAsync(true, Form(cliente, vehiculo, 12000));
        Assert.Empty((await servicio.StockAsync("", "disponible", 1)).Items);
        Assert.Single((await servicio.StockAsync("", "vendido", 1)).Items);
        await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.EliminarClienteAsync(cliente.Id));
        var compra = (await servicio.OperacionesAsync(false, "Ana", 1)).Items.Single();
        await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.EliminarOperacionAsync(false, compra.Id));
        var venta = (await servicio.OperacionesAsync(true, "Corolla", 1)).Items.Single();
        await servicio.EliminarOperacionAsync(true, venta.Id);
        Assert.Equal(1, (await servicio.ResumenAsync()).Disponibles);
        await servicio.EliminarOperacionAsync(false, compra.Id);
        Assert.Single((await servicio.StockAsync("", "disponible", 1)).Items);
        Assert.Null((await servicio.StockAsync("", "", 1)).Items.Single().Costo);
        await servicio.EliminarClienteAsync(cliente.Id);
    }

    [MySqlFact]
    public async Task FechasDeCompraYVentaSonCoherentes()
    {
        var (cliente, vehiculo) = await Preparar();
        await servicio.GuardarOperacionAsync(false, Form(cliente, vehiculo));
        var venta = Form(cliente, vehiculo);
        venta.Fecha = DateTime.Today.AddDays(-1);
        await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.GuardarOperacionAsync(true, venta));
        await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.GuardarOperacionAsync(false, Form(cliente, vehiculo)));
    }

    [MySqlFact]
    public async Task OperacionesPermitenEdicionSinCambiarVehiculo()
    {
        var (cliente, vehiculo) = await Preparar();
        await servicio.GuardarOperacionAsync(false, Form(cliente, vehiculo));
        var id = (await servicio.OperacionesAsync(false, "", 1)).Items.Single().Id;
        var compra = (await servicio.OperacionAsync(false, id))!;
        compra.Importe = 8000;
        await servicio.GuardarOperacionAsync(false, compra);
        Assert.Equal(8000m, (await servicio.OperacionAsync(false, id))!.Importe);
        await servicio.GuardarOperacionAsync(true, Form(cliente, vehiculo));
        id = (await servicio.OperacionesAsync(true, "", 1)).Items.Single().Id;
        var venta = (await servicio.OperacionAsync(true, id))!;
        venta.Importe = 15000;
        await servicio.GuardarOperacionAsync(true, venta);
        Assert.Equal(15000m, (await servicio.OperacionAsync(true, id))!.Importe);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => servicio.GuardarOperacionAsync(true, venta));
        await using var db = await factory.CreateDbContextAsync();
        var otraUnidad = new Vehiculo { MarcaId = 1, Modelo = "Otra unidad", Anio = 2024, Precio = 10000 };
        db.Vehiculos.Add(otraUnidad);
        await db.SaveChangesAsync();
        venta = (await servicio.OperacionAsync(true, id))!;
        venta.VehiculoId = otraUnidad.Id;
        await Assert.ThrowsAsync<ReglaNegocioException>(() => servicio.GuardarOperacionAsync(true, venta));
    }

    [MySqlFact]
    public async Task DosVentasSimultaneasDeLaMismaUnidadSoloPermitenUna()
    {
        var (cliente, vehiculo) = await Preparar();
        async Task<bool> Intentar()
        {
            try { await servicio.GuardarOperacionAsync(true, Form(cliente, vehiculo)); return true; }
            catch (ReglaNegocioException) { return false; }
        }
        var resultados = await Task.WhenAll(Intentar(), Intentar());
        Assert.Single(resultados, r => r);
        Assert.Equal(1, (await servicio.ResumenAsync()).Vendidos);
        Assert.Equal(0, (await servicio.ResumenAsync()).Disponibles);
    }

    [MySqlFact]
    public async Task VehiculoRelacionadoNoSePuedeEliminar()
    {
        var (cliente, vehiculo) = await Preparar();
        await servicio.GuardarOperacionAsync(true, Form(cliente, vehiculo));
        await using var db = await factory.CreateDbContextAsync();
        db.Vehiculos.Remove((await db.Vehiculos.FindAsync(vehiculo.Id))!);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [MySqlFact]
    public async Task OpcionesExcluyenVendidosYComprasDuplicadas()
    {
        var (cliente, vehiculo) = await Preparar();
        Assert.Single((await servicio.OpcionesAsync(false)).Vehiculos);
        await servicio.GuardarOperacionAsync(false, Form(cliente, vehiculo));
        Assert.Empty((await servicio.OpcionesAsync(false)).Vehiculos);
        Assert.Single((await servicio.OpcionesAsync(true)).Vehiculos);
        await servicio.GuardarOperacionAsync(true, Form(cliente, vehiculo));
        Assert.Empty((await servicio.OpcionesAsync(true)).Vehiculos);
        Assert.Single((await servicio.OpcionesAsync(true, vehiculo.Id)).Vehiculos);
    }

    private sealed class GestoriaFactory(DbContextOptions<GestoriaDbContext> opciones) : IDbContextFactory<GestoriaDbContext>
    {
        public GestoriaDbContext CreateDbContext() => new(opciones);
    }
}
