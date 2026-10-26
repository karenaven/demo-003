using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Fakes;

/// <summary>Base de test, reloj falso, IA falsa y las piezas reales de la generación.</summary>
public sealed class EntornoGeneracion : IDisposable
{
    private static readonly ConfiguracionRodaje _config = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto);

    public EntornoGeneracion()
    {
        DirectorioLogs = Path.Combine(Path.GetTempPath(), "rodajeia-logs-" + Guid.NewGuid().ToString("N"));
        Ia = new ClienteIaFalso(Reloj);
        Registro = new RegistroIntentos(DirectorioLogs, Reloj, NullLogger<RegistroIntentos>.Instance);
        Generador = new GeneradorEscena(
            Base,
            Ia,
            new ArmadorInstruccion(_config.Instruccion, _config.Molde),
            new SchemaSalida(_config.Vocabulario),
            new ValidadorRespuesta(_config.Vocabulario, _config.Molde),
            Registro,
            Notificador,
            Reloj);
    }

    public BaseDatosEnMemoria Base { get; } = new();

    public FakeTimeProvider Reloj { get; } = new(new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero));

    public ClienteIaFalso Ia { get; }

    public NotificadorGeneracion Notificador { get; } = new();

    public RegistroIntentos Registro { get; }

    public GeneradorEscena Generador { get; }

    public string DirectorioLogs { get; }

    /// <summary>Espera la tarea avanzando el reloj falso de a un segundo.</summary>
    public async Task<T> ConRelojAsync<T>(Task<T> tarea)
    {
        await AvanzarHastaAsync(tarea);
        return await tarea;
    }

    public async Task AvanzarHastaAsync(Task tarea)
    {
        var limite = DateTime.UtcNow.AddSeconds(30);
        while (!tarea.IsCompleted)
        {
            await Task.Delay(2);
            if (!tarea.IsCompleted)
            {
                Reloj.Advance(TimeSpan.FromSeconds(1));
            }

            if (DateTime.UtcNow > limite)
            {
                throw new TimeoutException("La generación no terminó.");
            }
        }
    }

    public void Dispose()
    {
        Base.Dispose();
        if (Directory.Exists(DirectorioLogs))
        {
            Directory.Delete(DirectorioLogs, recursive: true);
        }
    }
}
