using Microsoft.EntityFrameworkCore;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Prompt;
using RodajeIA.Web.Servicios;

namespace RodajeIA.Tests.Servicios;

public sealed class TomasServiceTests : IDisposable
{
    private static readonly ConfiguracionRodaje _config = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto);
    private readonly BaseDatosEnMemoria _base = new();
    private readonly TomasService _servicio;
    private readonly ArmadorPrompt _armador;
    private readonly int _escenaId;

    public TomasServiceTests()
    {
        _servicio = new TomasService(_base, _config.Vocabulario);
        _armador = new ArmadorPrompt(_base, _config.Molde, _config.Estilo);
        var (_, _, _, episodio) = _base.CrearSerieDeEjemplo();
        _escenaId = EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.EscenaAnexoA).GetAwaiter().GetResult()[0];
        EscenaDePrueba.MarcarGeneradaAsync(_base, _escenaId, (clip, toma) =>
        {
            toma.Accion = clip.Numero == 1 ? "Ana sonríe." : toma.Accion;
            toma.Angulo = "picado";
        }).GetAwaiter().GetResult();
    }

    public void Dispose() => _base.Dispose();

    private async Task<int> TomaDelClipAsync(int clip)
    {
        await using var db = _base.CreateDbContext();
        return await db.Tomas.Where(t => t.Bloque!.Clip!.EscenaId == _escenaId && t.Bloque.Clip.Numero == clip).Select(t => t.Id).SingleAsync();
    }

    private async Task<string> PromptAsync() => (await _armador.ArmarAsync(_escenaId))!;

    [Fact]
    [Trait("AC", "AC-07a")]
    public async Task Muestra_todos_los_bloques_con_todas_sus_tomas_y_sus_dialogos()
    {
        var bloques = await _servicio.ObtenerBloquesAsync(_escenaId);

        Assert.Equal([1, 2], bloques.Select(b => b.Clip));
        var toma = Assert.Single(bloques[0].Tomas);
        Assert.Equal(("plano medio", "50mm", "picado"), (toma.Plano, toma.Optica, toma.Angulo));
        Assert.Equal(["ANA: \"Mentiroso.\"", "JOSÉ DANIEL (sonriendo): \"Bueno... pasó parecido.\""], toma.Dialogos);
        Assert.Empty(toma.FueraDeVocabulario);
    }

    [Fact]
    [Trait("AC", "AC-07b")]
    public async Task Editar_el_plano_cambia_el_prompt()
    {
        var resultado = await _servicio.EditarCampoAsync(await TomaDelClipAsync(1), Vocabulario.Plano, "primer plano");

        Assert.True(resultado.Ok);
        var bloque1 = (await PromptAsync()).Split("Bloque 2:")[0].Split("Bloque 1:")[1];
        Assert.Contains("primer plano", bloque1, StringComparison.Ordinal);
        Assert.DoesNotContain("plano medio", bloque1, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-07c")]
    public async Task Editar_la_accion_cambia_el_prompt()
    {
        await _servicio.EditarAccionAsync(await TomaDelClipAsync(1), "Ana mira por la ventana.");

        var prompt = await PromptAsync();
        Assert.Contains("Ana mira por la ventana.", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("Ana sonríe.", prompt, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-07d")]
    public async Task Un_valor_fuera_del_vocabulario_no_se_acepta()
    {
        var tomaId = await TomaDelClipAsync(1);

        var resultado = await _servicio.EditarCampoAsync(tomaId, Vocabulario.Plano, "plano cinemático");

        Assert.Equal("«plano cinemático» no está en el vocabulario de plano.", resultado.Motivo);
        Assert.Equal("plano medio", (await _servicio.ObtenerBloquesAsync(_escenaId))[0].Tomas[0].Plano);
    }

    [Fact]
    public async Task Un_campo_obligatorio_no_se_puede_vaciar()
    {
        var resultado = await _servicio.EditarCampoAsync(await TomaDelClipAsync(1), Vocabulario.Optica, null);

        Assert.Equal("La óptica es obligatoria.", resultado.Motivo);
    }

    [Fact]
    [Trait("AC", "AC-07e")]
    public async Task La_edicion_queda_guardada_al_reiniciar()
    {
        var tomaId = await TomaDelClipAsync(1);
        await _servicio.EditarCampoAsync(tomaId, Vocabulario.Plano, "primer plano");

        var otraInstancia = new TomasService(_base, _config.Vocabulario);

        Assert.Equal("primer plano", (await otraInstancia.ObtenerBloquesAsync(_escenaId))[0].Tomas[0].Plano);
    }

    [Fact]
    [Trait("AC", "AC-07f")]
    public async Task Vaciar_el_angulo_lo_omite_del_prompt()
    {
        var tomaId = await TomaDelClipAsync(1);
        Assert.Contains("plano medio, picado, óptica 50mm.", await PromptAsync(), StringComparison.Ordinal);

        var resultado = await _servicio.EditarCampoAsync(tomaId, Vocabulario.Angulo, null);

        Assert.True(resultado.Ok);
        Assert.Null((await _servicio.ObtenerBloquesAsync(_escenaId))[0].Tomas[0].Angulo);
        Assert.Contains("Bloque 1:\nplano medio, óptica 50mm. Ana sonríe.", await PromptAsync(), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-S30")]
    public async Task Un_valor_que_ya_no_esta_en_el_vocabulario_se_conserva_y_se_marca()
    {
        var tomaId = await TomaDelClipAsync(1);
        await using (var db = _base.CreateDbContext())
        {
            await db.Tomas.Where(t => t.Id == tomaId).ExecuteUpdateAsync(s => s.SetProperty(t => t.Plano, "plano americano"));
        }

        var toma = (await _servicio.ObtenerBloquesAsync(_escenaId))[0].Tomas[0];

        Assert.Equal("plano americano", toma.Plano);
        Assert.Equal([Vocabulario.Plano], toma.FueraDeVocabulario);
        Assert.Contains("plano americano, picado, óptica 50mm.", await PromptAsync(), StringComparison.Ordinal);
        Assert.DoesNotContain("plano americano", _servicio.Opciones(Vocabulario.Plano));
    }

    [Theory]
    [Trait("AC", "AC-S37")]
    [InlineData("")]
    [InlineData("   \n ")]
    public async Task La_accion_no_puede_quedar_vacia(string texto)
    {
        var tomaId = await TomaDelClipAsync(1);

        var resultado = await _servicio.EditarAccionAsync(tomaId, texto);

        Assert.Equal("La acción no puede quedar vacía.", resultado.Motivo);
        Assert.Equal("Ana sonríe.", (await _servicio.ObtenerBloquesAsync(_escenaId))[0].Tomas[0].Accion);
    }

    [Fact]
    [Trait("AC", "AC-S37")]
    public async Task La_accion_editada_admite_comillas_y_se_guarda_recortada()
    {
        var tomaId = await TomaDelClipAsync(1);

        await _servicio.EditarAccionAsync(tomaId, "  Ana lee el cartel “Cerrado”.  ");

        Assert.Equal("Ana lee el cartel “Cerrado”.", (await _servicio.ObtenerBloquesAsync(_escenaId))[0].Tomas[0].Accion);
    }

    [Fact]
    [Trait("AC", "AC-S37")]
    public async Task No_hay_operacion_para_editar_las_referencias_de_dialogo()
    {
        var metodos = typeof(TomasService).GetMethods().Select(m => m.Name);

        Assert.DoesNotContain(metodos, m => m.Contains("Dialogo", StringComparison.Ordinal));
    }

    [Theory]
    [Trait("AC", "AC-S37")]
    [InlineData(EstadoEscena.Generando)]
    [InlineData(EstadoEscena.Error)]
    public async Task Solo_se_editan_tomas_de_escenas_generadas(EstadoEscena estado)
    {
        var tomaId = await TomaDelClipAsync(1);
        await using (var db = _base.CreateDbContext())
        {
            await db.Escenas.Where(e => e.Id == _escenaId).ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, estado));
        }

        var campo = await _servicio.EditarCampoAsync(tomaId, Vocabulario.Plano, "primer plano");
        var accion = await _servicio.EditarAccionAsync(tomaId, "Otra.");

        Assert.Equal(TomasService.SoloGenerada, campo.Motivo);
        Assert.Equal(TomasService.SoloGenerada, accion.Motivo);
    }
}
