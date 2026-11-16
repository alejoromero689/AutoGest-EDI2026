
using System.ComponentModel.DataAnnotations;

namespace AutoGest.Models;

public class Marca
{
    public int Id { get; set; }

    [Required]
    [StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    public List<Vehiculo> Vehiculos { get; set; } = new();
}
