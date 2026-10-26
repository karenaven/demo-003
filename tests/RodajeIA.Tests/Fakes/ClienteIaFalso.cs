using System.Text.Json.Nodes;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Fakes;

/// <summary>
/// Doble de <see cref="IClienteIa"/> con una cola de resultados programados. Registra cada
/// instrucción y schema recibidos y el momento de la llamada según el reloj del test.
/// </summary>
public sealed class ClienteIaFalso(TimeProvider tiempo) : IClienteIa
{
    private readonly Queue<Func<string, CancellationToken, Task<string>>> _programados = new();
    private Func<string, CancellationToken, Task<string>>? _porDefecto;

    public List<string> Instrucciones { get; } = [];

    public List<JsonObject> Schemas { get; } = [];

    public List<DateTimeOffset> Llamadas { get; } = [];

    public ClienteIaFalso Responde(Func<string, string> respuesta)
    {
        _programados.Enqueue((instruccion, _) => Task.FromResult(respuesta(instruccion)));
        return this;
    }

    public ClienteIaFalso Responde(string json) => Responde(_ => json);

    public ClienteIaFalso Ejecuta(Func<string, CancellationToken, Task<string>> accion)
    {
        _programados.Enqueue(accion);
        return this;
    }

    public ClienteIaFalso Falla(CategoriaFalla categoria, string detalle = "HTTP 503")
    {
        _programados.Enqueue((_, _) => Task.FromException<string>(new ErrorIa(categoria, detalle)));
        return this;
    }

    /// <summary>No responde nunca: solo termina cuando se cancela la llamada (timeout).</summary>
    public ClienteIaFalso NoResponde()
    {
        _programados.Enqueue(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return "";
        });
        return this;
    }

    /// <summary>Resultado para las llamadas que excedan la cola.</summary>
    public ClienteIaFalso SiempreResponde(Func<string, string> respuesta)
    {
        _porDefecto = (instruccion, _) => Task.FromResult(respuesta(instruccion));
        return this;
    }

    public Task<RespuestaIa> GenerarAsync(string instruccion, JsonObject schema, CancellationToken ct)
    {
        Instrucciones.Add(instruccion);
        Schemas.Add(schema);
        Llamadas.Add(tiempo.GetUtcNow());
        var accion = _programados.Count > 0 ? _programados.Dequeue() : _porDefecto
            ?? throw new InvalidOperationException("El ClienteIaFalso no tiene más resultados programados.");
        return Envolver(accion(instruccion, ct));
    }

    private static async Task<RespuestaIa> Envolver(Task<string> tarea) => new(await tarea);
}
