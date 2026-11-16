using AutoGest.Models;
using Microsoft.EntityFrameworkCore;
namespace AutoGest.Data;

public static class DatosDemo
{
    // Se ejecuta solo por opción explícita y nunca duplica documentos ni patentes.
    public static async Task CargarAsync(GestoriaDbContext db)
    {
        if (!await db.Clientes.AnyAsync(c => c.Documento == "20000000001"))
            db.Clientes.Add(new Cliente { Nombre = "Cliente de demostración", Documento = "20000000001", Email = "cliente@example.com", Telefono = "+54 11 5555 0100", Direccion = "Dirección de ejemplo" });
        if (!await db.Vehiculos.AnyAsync(v => v.Patente == "DEMO001"))
            db.Vehiculos.Add(new Vehiculo { MarcaId = 1, Modelo = "Corolla", Anio = 2023, Precio = 24500000m, EsUsado = true, Kilometraje = 32000, Patente = "DEMO001" });
        await db.SaveChangesAsync();
    }
}
