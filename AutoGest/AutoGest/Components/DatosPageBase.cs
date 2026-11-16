using AutoGest.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace AutoGest.Components;

public abstract class DatosPageBase : ComponentBase, IDisposable
{
    [Inject] protected GestionService Gestion { get; set; } = default!;
    [Inject] protected GestionState Estado { get; set; } = default!;
    [Inject] private ILogger<DatosPageBase> Logger { get; set; } = default!;
    protected bool Cargando { get; private set; }
    protected bool Guardando { get; private set; }
    protected string? Error { get; set; }
    private bool disposed;
    protected virtual bool Reactivo => true;
    protected override void OnInitialized() => Estado.OnChange += Cambio;
    protected override Task OnParametersSetAsync() => Recargar();
    protected abstract Task CargarAsync();
    protected async Task Recargar()
    {
        if (Cargando || disposed) return;
        Cargando = true;
        Error = null;
        try { await CargarAsync(); }
        catch (Exception ex) { Registrar(ex); }
        finally { Cargando = false; }
    }
    protected async Task Ejecutar(Func<Task> accion)
    {
        if (Guardando) return;
        Error = null;
        Guardando = true;
        try { await accion(); }
        catch (Exception ex) { Registrar(ex); }
        finally { Guardando = false; }
    }
    private void Registrar(Exception ex)
    {
        Logger.LogError(ex, "Error en la gestión de datos");
        Error = ex switch
        {
            ReglaNegocioException => ex.Message,
            System.ComponentModel.DataAnnotations.ValidationException => ex.Message,
            DbUpdateConcurrencyException => "Otro usuario modificó este registro. Recargá la página antes de guardar.",
            DbUpdateException => "No se pudo guardar. Revisá los datos duplicados o las operaciones relacionadas y recargá la página.",
            _ => "No se pudieron cargar o guardar los datos. Revisá la conexión con MySQL y que las migraciones estén aplicadas."
        };
    }
    private void Cambio() { if (!disposed && Reactivo) _ = InvokeAsync(async () => { await Recargar(); if (!disposed) StateHasChanged(); }); }
    public void Dispose() { disposed = true; Estado.OnChange -= Cambio; GC.SuppressFinalize(this); }
}
