using System.ComponentModel.DataAnnotations;

namespace AutoGest.Models;

public class Cliente : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ingresá el nombre o razón social.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 120 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingresá el DNI o CUIT.")]
    [RegularExpression(@"^(?:\d{7,8}|\d{11})$", ErrorMessage = "Usá un DNI de 7 u 8 dígitos o un CUIT de 11, sin puntos ni guiones.")]
    [StringLength(11)]
    public string Documento { get; set; } = "";

    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [StringLength(160)]
    public string? Email { get; set; }

    [StringLength(30)]
    [RegularExpression(@"^[+\d\s()\-]{6,30}$", ErrorMessage = "Ingresá un teléfono válido.")]
    public string? Telefono { get; set; }

    [StringLength(200)]
    public string? Direccion { get; set; }

    [ConcurrencyCheck]
    public Guid Version { get; set; } = Guid.NewGuid();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(Telefono))
            yield return new ValidationResult("Ingresá al menos un correo o teléfono de contacto.", [nameof(Email), nameof(Telefono)]);
        if (string.IsNullOrWhiteSpace(Nombre) || Nombre.Trim().Length < 2)
            yield return new ValidationResult("El nombre debe tener al menos dos caracteres.", [nameof(Nombre)]);
    }
}
