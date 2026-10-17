using Google.GenAI;
using Google.GenAI.Types;

namespace RodajeIA.Web.Generacion;

/// <summary>
/// Llama a Gemini con salida estructurada y el schema generado desde el vocabulario (research R-05).
/// Los reintentos del SDK se desactivan: los reintentos son los de FR-018.
/// </summary>
public sealed class ClienteGemini(IConfiguration configuracion, SchemaSalida schema) : IClienteIa, IDisposable
{
    public const string ModeloPorDefecto = "gemini-3.1-flash-lite";

    private readonly string? _clave = configuracion["Gemini:ApiKey"];
    private readonly string _modelo = configuracion["Gemini:Modelo"] is { Length: > 0 } modelo ? modelo : ModeloPorDefecto;
    private Client? _cliente;

    public async Task<RespuestaIa> GenerarAsync(string instruccion, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_clave))
        {
            throw new ErrorIa(CategoriaFalla.Configuracion, "falta la clave (Gemini:ApiKey)");
        }

        _cliente ??= new Client(apiKey: _clave);
        var config = new GenerateContentConfig
        {
            ResponseMimeType = "application/json",
            ResponseJsonSchema = schema.Json,
            HttpOptions = new HttpOptions { RetryOptions = new HttpRetryOptions { Attempts = 1 } },
        };

        try
        {
            var respuesta = await _cliente.Models.GenerateContentAsync(_modelo, instruccion, config, ct);
            return new RespuestaIa(respuesta.Text ?? "");
        }
        catch (ApiException ex)
        {
            throw Clasificar(ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ErrorIa(CategoriaFalla.ErrorIa, "no se pudo conectar", ex);
        }
    }

    public void Dispose() => _cliente?.Dispose();

    private ErrorIa Clasificar(ApiException ex) => ex.StatusCode switch
    {
        401 or 403 => new ErrorIa(CategoriaFalla.Configuracion, $"la clave fue rechazada (HTTP {ex.StatusCode})", ex),
        404 => new ErrorIa(CategoriaFalla.Configuracion, $"el modelo {_modelo} no existe (HTTP 404)", ex),
        400 when EsClaveInvalida(ex) => new ErrorIa(CategoriaFalla.Configuracion, "la clave no es válida (HTTP 400)", ex),
        _ => new ErrorIa(CategoriaFalla.ErrorIa, $"HTTP {ex.StatusCode}", ex),
    };

    private static bool EsClaveInvalida(ApiException ex) =>
        ex.Message.Contains("API_KEY_INVALID", StringComparison.Ordinal)
        || ex.Message.Contains("API key not valid", StringComparison.OrdinalIgnoreCase);
}
