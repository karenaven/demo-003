using System.Text.Json.Nodes;

namespace RodajeIA.Web.Generacion;

/// <summary>Categorías de falla de un intento (FR-019a, research R-06).</summary>
public enum CategoriaFalla
{
    /// <summary>La IA no respondió en 60 s. Se reintenta.</summary>
    Timeout,

    /// <summary>La IA devolvió un error o no está disponible (429, 5xx, red). Se reintenta.</summary>
    ErrorIa,

    /// <summary>La respuesta no cumplió las reglas. Se reintenta.</summary>
    RespuestaInvalida,

    /// <summary>Falta la clave, fue rechazada o el modelo no existe. No se reintenta y detiene la cola (FR-018a).</summary>
    Configuracion,

    /// <summary>Cualquier otra falla, por ejemplo al guardar. No se reintenta (FR-018a).</summary>
    Interno,
}

/// <summary>El JSON crudo que devolvió la IA; se valida antes de usarse.</summary>
public sealed record RespuestaIa(string Json);

/// <summary>Falla de la llamada a la IA ya clasificada. <see cref="Detalle"/> nunca incluye la clave.</summary>
public sealed class ErrorIa(CategoriaFalla categoria, string detalle, Exception? causa = null)
    : Exception(detalle, causa)
{
    public CategoriaFalla Categoria { get; } = categoria;

    public string Detalle { get; } = detalle;
}

/// <summary>Acceso a la IA de texto, detrás de una abstracción para poder usar un doble en los tests (constitución V).</summary>
public interface IClienteIa
{
    /// <summary>
    /// Envía la instrucción con el schema de salida de esa escena (<see cref="SchemaSalida.Para"/>)
    /// y devuelve el JSON de la respuesta, o lanza <see cref="ErrorIa"/>.
    /// </summary>
    Task<RespuestaIa> GenerarAsync(string instruccion, JsonObject schema, CancellationToken ct);
}
