namespace AutoGest.Services;

public class GestionState
{
    public event Action? OnChange;
    public string? Mensaje { get; private set; }
    public void NotificarCambio(string? mensaje = null)
    {
        Mensaje = mensaje;
        OnChange?.Invoke();
    }
    public void LimpiarMensaje() => Mensaje = null;
}
