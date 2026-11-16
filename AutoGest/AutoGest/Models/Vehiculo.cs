using System.ComponentModel.DataAnnotations;

namespace AutoGest.Models;

public class Vehiculo : IValidatableObject
{
    public int Id { get; set; }


    [Required(ErrorMessage = "Ingresá el modelo.")]
    [StringLength(80, MinimumLength = 2,
        ErrorMessage = "El modelo debe tener entre 2 y 80 caracteres.")]
    [RegularExpression(
        @"^[\p{L}\p{N}]+(?: [\p{L}\p{N}]+)*$",
        ErrorMessage = "El modelo solo puede contener letras, números y espacios entre palabras.")]
    public string Modelo { get; set; } = string.Empty;



    [Range(1900, 2100)]
    public int Anio { get; set; } = DateTime.Today.Year;

    public decimal Precio { get; set; }

    public bool EsUsado { get; set; }


    [Range(0, int.MaxValue,
        ErrorMessage = "El kilometraje no puede ser negativo.")]
    public int Kilometraje { get; set; }


    [StringLength(12)]
    [RegularExpression(@"^[A-Za-z0-9-]{5,12}$",
        ErrorMessage = "La patente no es válida.")]
    public string? Patente { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Seleccioná una marca.")]
    public int MarcaId { get; set; }

    public Marca? Marca { get; set; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext context)
    {
        if (Anio > DateTime.Today.Year + 1)
        {
            yield return new ValidationResult(
                "El año no puede superar al próximo año.",
                new[] { nameof(Anio) });
        }

        if (EsUsado && string.IsNullOrWhiteSpace(Patente))
        {
            yield return new ValidationResult(
                "Un vehículo usado debe tener patente.",
                new[] { nameof(Patente) });
        }

        if (EsUsado && Kilometraje == 0)
        {
            yield return new ValidationResult(
                "Un vehículo usado debe tener kilometraje mayor a cero.",
                new[] { nameof(Kilometraje) });
        }

        if (Precio < 1m || Precio > 999999999.99m)
        {
            yield return new ValidationResult(
                "El precio debe estar entre $1 y $999.999.999,99.",
                new[] { nameof(Precio) });
        }

    }
}
