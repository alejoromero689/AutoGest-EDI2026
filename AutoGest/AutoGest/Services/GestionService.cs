using System.ComponentModel.DataAnnotations;
using System.Data;
using AutoGest.Data;
using AutoGest.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoGest.Services;

// Cada operación usa su propio contexto. Los circuitos Blazor no comparten un DbContext.
public class GestionService(IDbContextFactory<GestoriaDbContext> factory, GestionState state)
{
    public async Task<Pagina<Cliente>> ClientesAsync(string texto, int pagina)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Clientes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(texto))
        {
            texto = texto.Trim();
            query = query.Where(c => c.Nombre.Contains(texto) || c.Documento.Contains(texto) ||
                (c.Email != null && c.Email.Contains(texto)));
        }
        return await Paginar(query.OrderBy(c => c.Nombre).ThenBy(c => c.Id), pagina);
    }

    public async Task<Cliente?> ClienteAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Clientes.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
    }

    public async Task GuardarClienteAsync(Cliente cliente)
    {
        cliente.Nombre = cliente.Nombre.Trim();
        cliente.Documento = cliente.Documento.Trim();
        cliente.Email = Normalizar(cliente.Email);
        cliente.Telefono = Normalizar(cliente.Telefono);
        cliente.Direccion = Normalizar(cliente.Direccion);
        Validator.ValidateObject(cliente, new ValidationContext(cliente), true);
        await using var db = await factory.CreateDbContextAsync();
        if (await db.Clientes.AnyAsync(c => c.Documento == cliente.Documento && c.Id != cliente.Id))
            throw new ReglaNegocioException("Ya existe un cliente con ese DNI o CUIT.");
        if (cliente.Id == 0) db.Clientes.Add(cliente);
        else
        {
            var existente = await db.Clientes.FindAsync(cliente.Id)
                ?? throw new ReglaNegocioException("El cliente ya no existe.");
            db.Entry(existente).Property(c => c.Version).OriginalValue = cliente.Version;
            db.Entry(existente).CurrentValues.SetValues(cliente);
            existente.Version = Guid.NewGuid();
        }
        await db.SaveChangesAsync();
        state.NotificarCambio("Cliente guardado correctamente.");
    }

    public async Task EliminarClienteAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        if (await db.Ventas.AnyAsync(v => v.ClienteId == id) || await db.Compras.AnyAsync(c => c.ClienteId == id))
            throw new ReglaNegocioException("Este cliente tiene operaciones asociadas y no se puede eliminar.");
        var cliente = await db.Clientes.FindAsync(id)
            ?? throw new ReglaNegocioException("El cliente ya no existe.");
        db.Clientes.Remove(cliente);
        await db.SaveChangesAsync();
        state.NotificarCambio("Cliente eliminado.");
    }

    public async Task<Pagina<OperacionFila>> OperacionesAsync(bool venta, string texto, int pagina)
    {
        await using var db = await factory.CreateDbContextAsync();
        // Proyección directa a DTO: no se cargan entidades completas para las tablas.
        var query = venta
            ? db.Ventas.AsNoTracking().Select(v => new OperacionFila { Id = v.Id, VehiculoId = v.VehiculoId,
                Vehiculo = v.Vehiculo!.Marca!.Nombre + " " + v.Vehiculo.Modelo, Patente = v.Vehiculo.Patente,
                Cliente = v.Cliente!.Nombre, Fecha = v.Fecha, Importe = v.Importe })
            : db.Compras.AsNoTracking().Select(c => new OperacionFila { Id = c.Id, VehiculoId = c.VehiculoId,
                Vehiculo = c.Vehiculo!.Marca!.Nombre + " " + c.Vehiculo.Modelo, Patente = c.Vehiculo.Patente,
                Cliente = c.Cliente!.Nombre, Fecha = c.Fecha, Importe = c.Importe });
        if (!string.IsNullOrWhiteSpace(texto))
        {
            texto = texto.Trim();
            query = query.Where(o => o.Vehiculo.Contains(texto) || o.Cliente.Contains(texto) ||
                (o.Patente != null && o.Patente.Contains(texto)));
        }
        return await Paginar(query.OrderByDescending(o => o.Fecha).ThenByDescending(o => o.Id), pagina);
    }

    public async Task<OperacionForm?> OperacionAsync(bool venta, int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        IOperacion? operacion = venta
            ? await db.Ventas.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id)
            : await db.Compras.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
        return operacion == null ? null : new OperacionForm
        {
            Id = operacion.Id, ClienteId = operacion.ClienteId, VehiculoId = operacion.VehiculoId,
            Fecha = operacion.Fecha, Importe = operacion.Importe,
            Observaciones = operacion.Observaciones, Version = operacion.Version
        };
    }

    public async Task<(List<Cliente> Clientes, List<Vehiculo> Vehiculos)> OpcionesAsync(bool venta, int? incluirVehiculo = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var clientes = await db.Clientes.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();
        var query = db.Vehiculos.AsNoTracking().Include(v => v.Marca)
            .Where(v => v.Id == incluirVehiculo || !db.Ventas.Any(o => o.VehiculoId == v.Id));
        if (!venta)
            query = query.Where(v => v.Id == incluirVehiculo || !db.Compras.Any(o => o.VehiculoId == v.Id));
        return (clientes, await query.OrderBy(v => v.Marca!.Nombre).ThenBy(v => v.Modelo).ToListAsync());
    }

    public async Task GuardarOperacionAsync(bool venta, OperacionForm form)
    {
        Validator.ValidateObject(form, new ValidationContext(form), true);
        await using var db = await factory.CreateDbContextAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        // Se bloquea la unidad antes de leer su compra/venta. Esto serializa incluso
        // escrituras desde dos circuitos distintos y conserva la coherencia del stock.
        var vehiculo = await db.Vehiculos.FromSqlInterpolated(
            $"SELECT * FROM Vehiculos WHERE Id = {form.VehiculoId} FOR UPDATE").SingleOrDefaultAsync()
            ?? throw new ReglaNegocioException("El vehículo ya no existe.");
        if (!await db.Clientes.AnyAsync(c => c.Id == form.ClienteId))
            throw new ReglaNegocioException("El cliente o proveedor ya no existe.");
        var compra = await db.Compras.SingleOrDefaultAsync(c => c.VehiculoId == vehiculo.Id);
        var ventaActual = await db.Ventas.SingleOrDefaultAsync(v => v.VehiculoId == vehiculo.Id);
        IOperacion operacion;
        if (venta)
        {
            if (ventaActual != null && ventaActual.Id != form.Id)
                throw new ReglaNegocioException("Este vehículo ya está vendido.");
            if (compra != null && form.Fecha.Date < compra.Fecha.Date)
                throw new ReglaNegocioException("La venta no puede ser anterior a la compra.");
            operacion = form.Id == 0 ? new Venta() : await db.Ventas.FindAsync(form.Id)
                ?? throw new ReglaNegocioException("La venta ya no existe.");
        }
        else
        {
            if (compra != null && compra.Id != form.Id)
                throw new ReglaNegocioException("Este vehículo ya tiene una compra registrada.");
            if (form.Id == 0 && ventaActual != null)
                throw new ReglaNegocioException("No se puede registrar la compra de un vehículo vendido.");
            if (ventaActual != null && form.Fecha.Date > ventaActual.Fecha.Date)
                throw new ReglaNegocioException("La compra no puede ser posterior a la venta.");
            operacion = form.Id == 0 ? new Compra() : await db.Compras.FindAsync(form.Id)
                ?? throw new ReglaNegocioException("La compra ya no existe.");
        }
        if (form.Id != 0)
        {
            if (operacion.VehiculoId != form.VehiculoId)
                throw new ReglaNegocioException("No se puede cambiar el vehículo de una operación. Eliminá la operación y volvé a registrarla.");
            db.Entry(operacion).Property(nameof(IOperacion.Version)).OriginalValue = form.Version;
        }
        operacion.ClienteId = form.ClienteId;
        operacion.VehiculoId = form.VehiculoId;
        operacion.Fecha = form.Fecha.Date;
        operacion.Importe = form.Importe;
        operacion.Observaciones = Normalizar(form.Observaciones);
        operacion.Version = Guid.NewGuid();
        if (form.Id == 0) db.Add(operacion);
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        state.NotificarCambio(venta ? "Venta guardada. Stock actualizado." : "Compra guardada correctamente.");
    }

    public async Task EliminarOperacionAsync(bool venta, int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var vehiculoId = venta
            ? await db.Ventas.Where(v => v.Id == id).Select(v => (int?)v.VehiculoId).SingleOrDefaultAsync()
            : await db.Compras.Where(c => c.Id == id).Select(c => (int?)c.VehiculoId).SingleOrDefaultAsync();
        if (vehiculoId == null) throw new ReglaNegocioException("La operación ya no existe.");
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await db.Vehiculos.FromSqlInterpolated(
            $"SELECT * FROM Vehiculos WHERE Id = {vehiculoId.Value} FOR UPDATE").SingleAsync();
        IOperacion? operacion = venta ? await db.Ventas.FindAsync(id) : await db.Compras.FindAsync(id);
        if (operacion == null) throw new ReglaNegocioException("La operación ya no existe.");
        if (!venta && await db.Ventas.AnyAsync(v => v.VehiculoId == vehiculoId))
            throw new ReglaNegocioException("Primero eliminá la venta asociada a este vehículo.");
        db.Remove(operacion);
        await db.SaveChangesAsync();
        await transaccion.CommitAsync();
        state.NotificarCambio(venta ? "Venta eliminada. El vehículo vuelve al stock disponible." : "Compra eliminada. El vehículo permanece en el inventario.");
    }

    public async Task<Pagina<StockFila>> StockAsync(string texto, string estado, int pagina)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.Vehiculos.AsNoTracking().Select(v => new StockFila { Id = v.Id,
            Marca = v.Marca!.Nombre, Modelo = v.Modelo, Anio = v.Anio, Patente = v.Patente, Precio = v.Precio,
            Costo = db.Compras.Where(c => c.VehiculoId == v.Id).Select(c => (decimal?)c.Importe).FirstOrDefault(),
            Vendido = db.Ventas.Any(o => o.VehiculoId == v.Id),
            FechaVenta = db.Ventas.Where(o => o.VehiculoId == v.Id).Select(o => (DateTime?)o.Fecha).FirstOrDefault() });
        if (!string.IsNullOrWhiteSpace(texto))
        {
            texto = texto.Trim();
            query = query.Where(v => v.Modelo.Contains(texto) || v.Marca.Contains(texto) || (v.Patente != null && v.Patente.Contains(texto)));
        }
        if (estado == "disponible") query = query.Where(v => !v.Vendido);
        if (estado == "vendido") query = query.Where(v => v.Vendido);
        return await Paginar(query.OrderByDescending(v => v.Id), pagina);
    }

    public async Task<Resumen> ResumenAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var inicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var fin = inicio.AddMonths(1);
        var disponibles = db.Vehiculos.Where(v => !db.Ventas.Any(o => o.VehiculoId == v.Id));
        return new Resumen(await db.Clientes.CountAsync(), await disponibles.CountAsync(), await db.Ventas.CountAsync(),
            await disponibles.SumAsync(v => (decimal?)v.Precio) ?? 0,
            await db.Ventas.Where(v => v.Fecha >= inicio && v.Fecha < fin).SumAsync(v => (decimal?)v.Importe) ?? 0,
            await db.Compras.Where(c => c.Fecha >= inicio && c.Fecha < fin).SumAsync(c => (decimal?)c.Importe) ?? 0);
    }

    private static string? Normalizar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    private static async Task<Pagina<T>> Paginar<T>(IQueryable<T> query, int pagina)
    {
        const int tamano = 8;
        var total = await query.CountAsync();
        pagina = Math.Clamp(pagina, 1, Math.Max(1, (int)Math.Ceiling(total / (double)tamano)));
        return new Pagina<T>(await query.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync(), total, pagina, tamano);
    }
}
