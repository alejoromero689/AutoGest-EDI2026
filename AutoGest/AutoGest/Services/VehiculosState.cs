namespace AutoGest.Services;

public class VehiculosState
{
    public event Action? OnChange;

    public void NotificarCambio()
    {
        OnChange?.Invoke();
    }
}
