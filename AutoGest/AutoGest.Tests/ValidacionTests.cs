using System.ComponentModel.DataAnnotations;
using AutoGest.Models;

namespace AutoGest.Tests;

public class ValidacionTests
{
    [Theory]
    [InlineData("es-AR")]
    [InlineData("en-US")]
    public void ImporteSeValidaConCulturaArgentinaOInglesa(string cultura)
    {
        var anterior = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new(cultura);
            Assert.Empty(Validar(new OperacionForm { ClienteId = 1, VehiculoId = 1, Importe = 12500000.50m }));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = anterior; }
    }

    private static List<ValidationResult> Validar(object modelo)
    {
        var resultados = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, true);
        return resultados;
    }

    [Fact]
    public void ClienteExigeUnMedioDeContacto()
    {
        var errores = Validar(new Cliente { Nombre = "Ana Pérez", Documento = "30123456" });
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(Cliente.Email)));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("30.123.456")]
    [InlineData("123456789")]
    public void RechazaDocumentosInvalidos(string documento) => Assert.NotEmpty(Validar(
        new Cliente { Nombre = "Ana Pérez", Documento = documento, Email = "ana@example.com" }));

    [Fact]
    public void OperacionRechazaFechasFuturasYImportesNegativos()
    {
        var form = new OperacionForm { ClienteId = 1, VehiculoId = 1, Importe = -10, Fecha = DateTime.Today.AddDays(1) };
        Assert.NotEmpty(Validar(form));
        form.Importe = 100;
        Assert.Contains(Validar(form), e => e.MemberNames.Contains(nameof(OperacionForm.Fecha)));
    }

    [Fact]
    public void VehiculoUsadoExigePatenteYKilometraje()
    {
        Assert.NotEmpty(Validar(new Vehiculo { MarcaId = 1, Modelo = "Corolla", Precio = 1000, EsUsado = true }));
    }
}
