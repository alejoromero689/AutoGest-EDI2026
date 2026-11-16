using System.ComponentModel.DataAnnotations;

namespace AutoGest.Models;

public interface IOperacion
{
    int Id { get; set; }
    int ClienteId { get; set; }
    int VehiculoId { get; set; }
    DateTime Fecha { get; set; }
    decimal Importe { get; set; }
    string? Observaciones { get; set; }
    Guid Version { get; set; }
}

public class Venta : IOperacion
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public int VehiculoId { get; set; }
    public Vehiculo? Vehiculo { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Importe { get; set; }
    [StringLength(500)] public string? Observaciones { get; set; }
    [ConcurrencyCheck] public Guid Version { get; set; } = Guid.NewGuid();
}

public class Compra : IOperacion
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public int VehiculoId { get; set; }
    public Vehiculo? Vehiculo { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Importe { get; set; }
    [StringLength(500)] public string? Observaciones { get; set; }
    [ConcurrencyCheck] public Guid Version { get; set; } = Guid.NewGuid();
}

public class OperacionForm : IValidatableObject
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un cliente o proveedor.")]
    public int ClienteId { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "Seleccioná un vehículo.")]
    public int VehiculoId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;
    [Range(typeof(decimal), "1", "999999999.99", ParseLimitsInInvariantCulture = true, ErrorMessage = "El importe debe estar entre $1 y $999.999.999,99.")]
    public decimal Importe { get; set; }
    [StringLength(500, ErrorMessage = "Las observaciones admiten hasta 500 caracteres.")]
    public string? Observaciones { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Fecha.Date > DateTime.Today || Fecha.Year < 2000)
            yield return new ValidationResult("La fecha debe estar entre el año 2000 y hoy.", [nameof(Fecha)]);
    }
}
