using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Web.Generacion;

/// <summary>
/// Ejecuta la generación en segundo plano, de a una escena y con una sola generación en curso en
/// toda la aplicación (FR-014, FR-014a, FR-020, research R-07). No encola pedidos: si hay una
/// generación en curso, los rechaza.
/// </summary>
public sealed class ServicioGeneracion(
    IDbContextFactory<RodajeDbContext> fabrica,
    GeneradorEscena generador,
    NotificadorGeneracion notificador,
    ILogger<ServicioGeneracion> logger) : BackgroundService, IEstadoGeneracion
{
    private sealed record Pedido(int EpisodioId, IReadOnlyList<int> Escenas);

    private readonly Channel<Pedido> _canal = Channel.CreateUnbounded<Pedido>();
    private readonly Lock _bloqueo = new();
    private Pedido? _actual;
    private TaskCompletionSource _inactivo = Completado();

    public bool EnCurso
    {
        get
        {
            lock (_bloqueo)
            {
                return _actual is not null;
            }
        }
    }

    public int? EpisodioEnCurso
    {
        get
        {
            lock (_bloqueo)
            {
                return _actual?.EpisodioId;
            }
        }
    }

    /// <summary>Genera todas las escenas en "pendiente" del episodio, en orden de número.</summary>
    public async Task<bool> TryIniciarEpisodioAsync(int episodioId, CancellationToken ct = default)
    {
        if (EnCurso)
        {
            return false;
        }

        await using var db = await fabrica.CreateDbContextAsync(ct);
        var escenas = await db.Escenas
            .Where(e => e.EpisodioId == episodioId && e.Estado == EstadoEscena.Pendiente)
            .OrderBy(e => e.Numero)
            .Select(e => e.Id)
            .ToListAsync(ct);

        return escenas.Count > 0 && Encolar(new Pedido(episodioId, escenas));
    }

    /// <summary>Reintenta solo esa escena, si está en "error".</summary>
    public async Task<bool> TryReintentarAsync(int escenaId, CancellationToken ct = default)
    {
        if (EnCurso)
        {
            return false;
        }

        await using var db = await fabrica.CreateDbContextAsync(ct);
        var escena = await db.Escenas.SingleOrDefaultAsync(e => e.Id == escenaId && e.Estado == EstadoEscena.Error, ct);
        return escena is not null && Encolar(new Pedido(escena.EpisodioId, [escena.Id]));
    }

    /// <summary>Termina cuando no hay ninguna generación en curso.</summary>
    public Task EsperarInactivoAsync()
    {
        lock (_bloqueo)
        {
            return _inactivo.Task;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var pedido in _canal.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                foreach (var escenaId in pedido.Escenas)
                {
                    var resultado = await generador.GenerarAsync(escenaId, stoppingToken);
                    if (resultado.Falla == CategoriaFalla.Configuracion)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Falló la generación del episodio {Episodio}.", pedido.EpisodioId);
            }
            finally
            {
                TaskCompletionSource inactivo;
                lock (_bloqueo)
                {
                    _actual = null;
                    inactivo = _inactivo;
                }

                notificador.NotificarGeneracionEnCurso(false, null);
                inactivo.TrySetResult();
            }
        }
    }

    private bool Encolar(Pedido pedido)
    {
        lock (_bloqueo)
        {
            if (_actual is not null)
            {
                return false;
            }

            _actual = pedido;
            _inactivo = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        notificador.NotificarGeneracionEnCurso(true, pedido.EpisodioId);
        _canal.Writer.TryWrite(pedido);
        return true;
    }

    private static TaskCompletionSource Completado()
    {
        var tcs = new TaskCompletionSource();
        tcs.SetResult();
        return tcs;
    }
}
