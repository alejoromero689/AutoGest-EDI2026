namespace AutoGest.Services;

public record Pagina<T>(IReadOnlyList<T> Items, int Total, int Numero, int Tamano = 8)
{
    public int Paginas => Math.Max(1, (int)Math.Ceiling(Total / (double)Tamano));
}

public record OperacionFila
{
    public int Id { get; init; }
    public int VehiculoId { get; init; }
    public string Vehiculo { get; init; } = "";
    public string? Patente { get; init; }
    public string Cliente { get; init; } = "";
    public DateTime Fecha { get; init; }
    public decimal Importe { get; init; }
}
public record StockFila
{
    public int Id { get; init; }
    public string Marca { get; init; } = "";
    public string Modelo { get; init; } = "";
    public int Anio { get; init; }
    public string? Patente { get; init; }
    public decimal Precio { get; init; }
    public decimal? Costo { get; init; }
    public bool Vendido { get; init; }
    public DateTime? FechaVenta { get; init; }
}
public record Resumen(int Clientes, int Disponibles, int Vendidos, decimal ValorStock,
    decimal VentasMes, decimal ComprasMes);
public class ReglaNegocioException(string mensaje) : Exception(mensaje);
