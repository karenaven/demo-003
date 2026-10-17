using System.Collections.Concurrent;
using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Web.Generacion;

/// <param name="Intento">El intento en curso mientras la escena está en "generando"; nulo en los demás estados.</param>
public sealed record CambioEstadoEscena(int EscenaId, int EpisodioId, EstadoEscena Estado, int? Intento, string? Motivo);

/// <summary>
/// Avisa en memoria los cambios de la generación a las vistas abiertas, en todas las pestañas
/// (FR-019b, research R-08). Un suscriptor que falla no corta a los demás.
/// </summary>
public sealed class NotificadorGeneracion
{
    private readonly ConcurrentDictionary<int, int> _intentos = new();

    public event Action<CambioEstadoEscena>? EstadoEscenaCambiado;

    public event Action<bool, int?>? GeneracionEnCursoCambiada;

    /// <summary>El intento en curso de una escena que se está generando, para quien abre la vista después.</summary>
    public int? IntentoEnCurso(int escenaId) => _intentos.TryGetValue(escenaId, out var intento) ? intento : null;

    public void NotificarEscena(CambioEstadoEscena cambio)
    {
        if (cambio is { Estado: EstadoEscena.Generando, Intento: { } intento })
        {
            _intentos[cambio.EscenaId] = intento;
        }
        else
        {
            _intentos.TryRemove(cambio.EscenaId, out _);
        }

        Invocar(EstadoEscenaCambiado, manejador => manejador(cambio));
    }

    public void NotificarGeneracionEnCurso(bool enCurso, int? episodioId) =>
        Invocar(GeneracionEnCursoCambiada, manejador => manejador(enCurso, episodioId));

    private static void Invocar<T>(T? evento, Action<T> llamar)
        where T : Delegate
    {
        foreach (var manejador in evento?.GetInvocationList().Cast<T>() ?? [])
        {
            try
            {
                llamar(manejador);
            }
            catch (Exception)
            {
                // Una vista cerrada o con error no debe afectar a la generación ni a las demás vistas.
            }
        }
    }
}
