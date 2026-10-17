using System.Diagnostics;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Guion;
using RodajeIA.Web.Personajes;
using RodajeIA.Web.Prompt;

namespace RodajeIA.Tests.Rendimiento;

/// <summary>
/// SC-004 y SC-005: p95 de 100 ejecuciones, descartando la primera (arranque en frío), en el equipo
/// local. No corren por defecto: anotá el equipo (CPU, RAM, sistema operativo) junto al resultado.
/// </summary>
[Trait("Categoria", "Rendimiento")]
public sealed class RendimientoTests(ITestOutputHelper salida) : IDisposable
{
    private const int Ejecuciones = 100;
    private readonly BaseDatosEnMemoria _base = new();

    public void Dispose() => _base.Dispose();

    [Fact(Explicit = true)]
    public void SC004_validar_y_dividir_un_guion_maximo_tarda_menos_de_1_segundo()
    {
        var guion = Ejemplos.Guion(ParserGuion.MaximoEscenas, ParserGuion.MaximoClips, dialogosPorClip: 2);
        HojaPersonaje[] hojas = [Ejemplos.HojaAna(), Ejemplos.HojaJoseDaniel()];
        hojas[0].Id = 1;
        hojas[1].Id = 2;

        var p95 = Medir(() =>
        {
            var resultado = (ResultadoGuion.Valido)ParserGuion.Parsear(guion);
            foreach (var escena in resultado.Guion.Escenas)
            {
                DetectorPersonajes.Detectar(escena, hojas);
            }
        });

        salida.WriteLine($"SC-004: p95 = {p95.TotalMilliseconds:F2} ms ({Environment.ProcessorCount} núcleos, {Environment.OSVersion}).");
        Assert.True(p95 < TimeSpan.FromSeconds(1), $"p95 = {p95.TotalMilliseconds:F2} ms");
    }

    [Fact(Explicit = true)]
    public async Task SC005_armar_el_prompt_de_una_escena_tarda_menos_de_1_segundo()
    {
        var config = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto);
        var (_, _, _, episodio) = _base.CrearSerieDeEjemplo();
        var escenaId = (await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.Guion(1, ParserGuion.MaximoClips, dialogosPorClip: 2)))[0];
        await EscenaDePrueba.MarcarGeneradaAsync(_base, escenaId);
        var armador = new ArmadorPrompt(_base, config.Molde, config.Estilo);

        var p95 = Medir(() => armador.ArmarAsync(escenaId).GetAwaiter().GetResult());

        salida.WriteLine($"SC-005: p95 = {p95.TotalMilliseconds:F2} ms ({Environment.ProcessorCount} núcleos, {Environment.OSVersion}).");
        Assert.True(p95 < TimeSpan.FromSeconds(1), $"p95 = {p95.TotalMilliseconds:F2} ms");
    }

    private static TimeSpan Medir(Action accion)
    {
        accion();
        var tiempos = new List<TimeSpan>(Ejecuciones);
        for (var i = 0; i < Ejecuciones; i++)
        {
            var reloj = Stopwatch.StartNew();
            accion();
            tiempos.Add(reloj.Elapsed);
        }

        tiempos.Sort();
        return tiempos[(int)Math.Ceiling(0.95 * tiempos.Count) - 1];
    }
}
