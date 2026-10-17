using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Components;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Generacion;
using RodajeIA.Web.Prompt;
using RodajeIA.Web.Servicios;

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

// Generación (research R-05 a R-08, R-13).
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SchemaSalida>();
builder.Services.AddSingleton<ArmadorInstruccion>();
builder.Services.AddSingleton<ValidadorRespuesta>();
builder.Services.AddSingleton<NotificadorGeneracion>();
builder.Services.AddSingleton(sp => new RegistroIntentos(
    Path.Combine(builder.Environment.ContentRootPath, "logs"),
    sp.GetRequiredService<TimeProvider>(),
    sp.GetRequiredService<ILogger<RegistroIntentos>>()));
builder.Services.AddSingleton<IClienteIa, ClienteGemini>();
builder.Services.AddSingleton<GeneradorEscena>();
builder.Services.AddSingleton<ServicioGeneracion>();
builder.Services.AddSingleton<IEstadoGeneracion>(sp => sp.GetRequiredService<ServicioGeneracion>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<ServicioGeneracion>());

builder.Services.AddSingleton<CargaGuionService>();

// Prompt final (contracts/prompt.md).
builder.Services.AddSingleton<ArmadorPrompt>();
builder.Services.AddScoped<CopiaPrompt>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options => options.MaximumReceiveMessageSize = 1024 * 1024);

var app = builder.Build();

try
{
    // Las escenas que quedaron en "generando" pasan a "error" antes de aceptar pedidos (FR-021).
    await RecuperacionArranque.RecuperarAsync(app.Services.GetRequiredService<IDbContextFactory<RodajeDbContext>>(), CancellationToken.None);
}
catch (InvalidOperationException ex) when (ex.Message == RecuperacionArranque.BaseSinMigrar)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

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

await app.RunAsync();
return 0;
