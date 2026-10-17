using Microsoft.JSInterop;
using RodajeIA.Web.Prompt;

namespace RodajeIA.Tests.Prompt;

public class CopiaPromptTests
{
    private sealed class JsFalso(Func<object?[]?, object?> respuesta) : IJSRuntime
    {
        public List<(string Identificador, object?[]? Argumentos)> Llamadas { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Llamadas.Add((identifier, args));
            return ValueTask.FromResult((TValue)respuesta(args)!);
        }
    }

    [Fact]
    [Trait("AC", "AC-08d")]
    public async Task Copia_exactamente_el_prompt_y_confirma()
    {
        var js = new JsFalso(_ => true);
        const string prompt = "Estilo…\n\nBloque 1:\nplano medio, óptica 50mm. ANA: “Hola”.";

        var resultado = await new CopiaPrompt(js).CopiarAsync(prompt);

        var llamada = Assert.Single(js.Llamadas);
        Assert.Equal("rodajeia.copiar", llamada.Identificador);
        Assert.Equal(prompt, Assert.Single(llamada.Argumentos!));
        Assert.True(resultado.Copiado);
        Assert.Equal("Copiado", resultado.Mensaje);
    }

    [Fact]
    [Trait("AC", "AC-S36")]
    public async Task Si_el_portapapeles_falla_avisa_para_copiar_a_mano()
    {
        var resultado = await new CopiaPrompt(new JsFalso(_ => false)).CopiarAsync("texto");

        Assert.False(resultado.Copiado);
        Assert.Equal("No se pudo copiar; seleccioná el texto y copialo a mano", resultado.Mensaje);
    }

    [Fact]
    [Trait("AC", "AC-S36")]
    public async Task Si_la_llamada_al_navegador_lanza_tambien_avisa()
    {
        var resultado = await new CopiaPrompt(new JsFalso(_ => throw new JSException("sin permiso"))).CopiarAsync("texto");

        Assert.False(resultado.Copiado);
    }
}
