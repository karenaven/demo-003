using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Gemini;

/// <summary>
/// Mediciones con Gemini real (SC-006 y SC-002). Requieren <c>Gemini:ApiKey</c> en los user-secrets
/// de la app o en la variable <c>Gemini__ApiKey</c>, consumen cuota y no corren por defecto.
/// </summary>
[Trait("Categoria", "Gemini")]
public sealed class SpikeGeminiTests(ITestOutputHelper salida) : IDisposable
{
    private const int Generaciones = 10;
    private static readonly ConfiguracionRodaje _config = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto);
    private readonly BaseDatosEnMemoria _base = new();
    private readonly string _logs = Path.Combine(Path.GetTempPath(), "rodajeia-spike-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        _base.Dispose();
        if (Directory.Exists(_logs))
        {
            Directory.Delete(_logs, recursive: true);
        }
    }

    [Fact(Explicit = true)]
    public async Task SC006_p95_de_una_escena_de_20_clips_menor_a_30_segundos()
    {
        var (generador, cliente) = Crear();
        var (_, _, _, episodio) = _base.CrearSerieDeEjemplo();
        var escenaId = (await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.Guion(1, 20)))[0];

        for (var i = 0; i < Generaciones; i++)
        {
            await Reiniciar(escenaId);
            await generador.GenerarAsync(escenaId, TestContext.Current.CancellationToken);
        }

        var p95 = Percentil95(cliente.Exitosas);
        salida.WriteLine($"SC-006: {cliente.Exitosas.Count} llamadas exitosas; p95 = {p95.TotalSeconds:F1} s; todas: {string.Join(", ", cliente.Exitosas.Select(d => d.TotalSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)))}");
        Assert.True(p95 < TimeSpan.FromSeconds(30), $"p95 = {p95.TotalSeconds:F1} s");
    }

    [Fact(Explicit = true)]
    public async Task SC002_las_escenas_de_referencia_terminan_generadas_dentro_de_los_tres_intentos()
    {
        var (generador, _) = Crear();
        var (serie, _, _, episodio) = _base.CrearSerieDeEjemplo();
        var anexoA = (await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.EscenaAnexoA))[0];
        int otroEpisodio;
        await using (var db = _base.CreateDbContext())
        {
            var nuevo = new Episodio { SerieId = serie.Id, Numero = 2, Titulo = "Spike" };
            db.Episodios.Add(nuevo);
            await db.SaveChangesAsync();
            otroEpisodio = nuevo.Id;
        }

        var veinteClips = (await EscenaDePrueba.CargarAsync(_base, otroEpisodio, Ejemplos.Guion(1, 20)))[0];

        var fallidas = new List<string>();
        foreach (var escenaId in new[] { anexoA, veinteClips })
        {
            for (var i = 0; i < Generaciones; i++)
            {
                await Reiniciar(escenaId);
                var resultado = await generador.GenerarAsync(escenaId, TestContext.Current.CancellationToken);
                if (resultado.Estado != EstadoEscena.Generada)
                {
                    fallidas.Add($"escena {escenaId}, generación {i + 1}: {(await EscenaDePrueba.LeerAsync(_base, escenaId)).MotivoError}");
                }
            }
        }

        salida.WriteLine($"SC-002: {2 * Generaciones - fallidas.Count} de {2 * Generaciones} generadas.");
        Assert.Empty(fallidas);
    }

    private (GeneradorEscena Generador, ClienteMedido Cliente) Crear()
    {
        var configuracion = new ConfigurationBuilder()
            .AddUserSecrets("bbbe13ca-c173-41f8-b639-fc3921837a50")
            .AddEnvironmentVariables()
            .Build();
        if (string.IsNullOrWhiteSpace(configuracion["Gemini:ApiKey"]))
        {
            Assert.Skip("Falta Gemini:ApiKey (user-secrets de la app o variable Gemini__ApiKey).");
        }

        var cliente = new ClienteMedido(new ClienteGemini(configuracion, new SchemaSalida(_config.Vocabulario)));
        var generador = new GeneradorEscena(
            _base,
            cliente,
            new ArmadorInstruccion(_config.Instruccion, _config.Molde),
            new ValidadorRespuesta(_config.Vocabulario),
            new RegistroIntentos(_logs, TimeProvider.System, NullLogger<RegistroIntentos>.Instance),
            new NotificadorGeneracion(),
            TimeProvider.System);
        return (generador, cliente);
    }

    private async Task Reiniciar(int escenaId)
    {
        await using var db = _base.CreateDbContext();
        await db.Bloques.Where(b => b.Clip!.EscenaId == escenaId).ExecuteDeleteAsync();
        await db.Escenas.Where(e => e.Id == escenaId).ExecuteUpdateAsync(s => s
            .SetProperty(e => e.Estado, EstadoEscena.Pendiente)
            .SetProperty(e => e.MotivoError, (string?)null));
    }

    private static TimeSpan Percentil95(IReadOnlyList<TimeSpan> duraciones)
    {
        Assert.NotEmpty(duraciones);
        var ordenadas = duraciones.Order().ToList();
        return ordenadas[(int)Math.Ceiling(0.95 * ordenadas.Count) - 1];
    }

    /// <summary>Mide la duración de cada llamada exitosa, sin intentos fallidos ni esperas (SC-006).</summary>
    private sealed class ClienteMedido(IClienteIa interno) : IClienteIa
    {
        public List<TimeSpan> Exitosas { get; } = [];

        public async Task<RespuestaIa> GenerarAsync(string instruccion, CancellationToken ct)
        {
            var reloj = Stopwatch.StartNew();
            var respuesta = await interno.GenerarAsync(instruccion, ct);
            Exitosas.Add(reloj.Elapsed);
            return respuesta;
        }
    }
}
