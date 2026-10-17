using System.Globalization;
using System.Text.Json.Nodes;

namespace RodajeIA.Web.Generacion;

public enum ResultadoIntento
{
    Ok,
    Timeout,
    ErrorIa,
    Invalida,
    Configuracion,
    ErrorInterno,
}

/// <param name="RespuestaCruda">Solo se registra si el resultado es <see cref="ResultadoIntento.Invalida"/>.</param>
public sealed record IntentoGeneracion(
    int EpisodioId,
    int Escena,
    int Intento,
    TimeSpan Duracion,
    ResultadoIntento Resultado,
    string? Motivo,
    string? RespuestaCruda);

/// <summary>
/// Agrega una línea JSON por intento a <c>logs/generacion-AAAA-MM-DD.jsonl</c> (FR-019c). No registra la
/// instrucción, la configuración de la IA ni la clave.
/// </summary>
public sealed class RegistroIntentos(string directorio, TimeProvider tiempo, ILogger<RegistroIntentos> logger)
{
    private readonly Lock _bloqueo = new();

    public void Registrar(IntentoGeneracion intento)
    {
        var ahora = tiempo.GetUtcNow();
        var linea = new JsonObject
        {
            ["fecha"] = ahora.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture),
            ["episodioId"] = intento.EpisodioId,
            ["escena"] = intento.Escena,
            ["intento"] = intento.Intento,
            ["duracionMs"] = (long)intento.Duracion.TotalMilliseconds,
            ["resultado"] = Nombre(intento.Resultado),
            ["motivo"] = intento.Motivo,
        };
        if (intento.Resultado == ResultadoIntento.Invalida)
        {
            linea["respuestaCruda"] = intento.RespuestaCruda;
        }

        try
        {
            lock (_bloqueo)
            {
                Directory.CreateDirectory(directorio);
                var archivo = Path.Combine(directorio, $"generacion-{ahora:yyyy-MM-dd}.jsonl");
                File.AppendAllText(archivo, linea.ToJsonString() + "\n");
            }
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "No se pudo escribir el registro de intentos en {Directorio}.", directorio);
        }

        logger.LogInformation(
            "Generación: episodio {Episodio}, escena {Escena}, intento {Intento}: {Resultado} en {Duracion} ms. {Motivo}",
            intento.EpisodioId, intento.Escena, intento.Intento, Nombre(intento.Resultado), (long)intento.Duracion.TotalMilliseconds, intento.Motivo);
    }

    private static string Nombre(ResultadoIntento resultado) => resultado switch
    {
        ResultadoIntento.Ok => "ok",
        ResultadoIntento.Timeout => "timeout",
        ResultadoIntento.ErrorIa => "error_ia",
        ResultadoIntento.Invalida => "invalida",
        ResultadoIntento.Configuracion => "configuracion",
        _ => "error_interno",
    };
}
