using AutoGest.Components;
using AutoGest.Data;
using AutoGest.Services;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("GestoriaDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configurá ConnectionStrings:GestoriaDb con tu conexión MySQL. Consultá el README.");
builder.Services.AddDbContextFactory<GestoriaDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 4, 0))));
builder.Services.AddScoped<VehiculosState>();
builder.Services.AddScoped<GestionState>(sp => sp.GetRequiredService<VehiculosState>());
builder.Services.AddScoped<GestionService>();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.Configure<RequestLocalizationOptions>(options =>
    options.SetDefaultCulture("es-AR").AddSupportedCultures("es-AR").AddSupportedUICultures("es-AR"));

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRequestLocalization();
app.UseStaticFiles();
app.UseAntiforgery();
// Optativo: en producción también se puede aplicar la migración con dotnet ef.
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GestoriaDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}
if (builder.Configuration.GetValue<bool>("Database:SeedDemo"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GestoriaDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await DatosDemo.CargarAsync(db);
}
app.MapGet("/health", () => Results.Ok(new { estado = "ok" }));
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
