using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Components;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos;

ConfiguracionRodaje configuracion;
try
{
    // Los archivos de config/ se leen y validan una sola vez, al iniciar (FR-015a, FR-015c, FR-015d).
    configuracion = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto);
}
catch (ConfiguracionInvalidaException ex)
{
    foreach (var problema in ex.Problemas)
    {
        Console.Error.WriteLine($"Configuración inválida: {problema}");
    }

    return 1;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(configuracion);
builder.Services.AddSingleton(configuracion.Vocabulario);
builder.Services.AddSingleton(configuracion.Estilo);
builder.Services.AddSingleton(configuracion.Instruccion);
builder.Services.AddSingleton(configuracion.Molde);

builder.Services.AddDbContextFactory<RodajeDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Rodaje") ?? "Data Source=rodajeia.db"));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 1024 * 1024);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
return 0;
