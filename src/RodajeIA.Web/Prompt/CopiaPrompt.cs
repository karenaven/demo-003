using Microsoft.JSInterop;

namespace RodajeIA.Web.Prompt;

public sealed record ResultadoCopia(bool Copiado, string Mensaje);

/// <summary>Copia el prompt al portapapeles con <c>wwwroot/js/portapapeles.js</c> (FR-028, FR-028a).</summary>
public sealed class CopiaPrompt(IJSRuntime js)
{
    public const string Copiado = "Copiado";
    public const string NoSePudo = "No se pudo copiar; seleccioná el texto y copialo a mano";

    public async Task<ResultadoCopia> CopiarAsync(string prompt)
    {
        try
        {
            var ok = await js.InvokeAsync<bool>("rodajeia.copiar", prompt);
            return ok ? new ResultadoCopia(true, Copiado) : new ResultadoCopia(false, NoSePudo);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or TaskCanceledException or InvalidOperationException)
        {
            return new ResultadoCopia(false, NoSePudo);
        }
    }
}
