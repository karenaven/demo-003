using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Web.Generacion;

/// <param name="Falla">La categoría del último intento fallido; nula si la escena quedó generada.</param>
public sealed record ResultadoEscena(EstadoEscena Estado, CategoriaFalla? Falla);

/// <summary>
/// Genera una escena (FR-016 a FR-019a): hasta 3 intentos con timeout de 60 s por llamada y esperas
/// de 10 s y 30 s (FR-018); los errores de configuración e internos no se reintentan (FR-018a).
/// Los bloques se guardan en una sola transacción con el paso a "generada" (FR-019).
/// </summary>
public sealed class GeneradorEscena(
    IDbContextFactory<RodajeDbContext> fabrica,
    IClienteIa ia,
    ArmadorInstruccion armador,
    SchemaSalida schema,
    ValidadorRespuesta validador,
    RegistroIntentos registro,
    NotificadorGeneracion notificador,
    TimeProvider tiempo)
{
    public const int MaximoIntentos = 3;

    public static readonly TimeSpan TiempoMaximoPorLlamada = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan[] _esperas = [TimeSpan.Zero, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)];

    public async Task<ResultadoEscena> GenerarAsync(int escenaId, CancellationToken ct)
    {
        var (episodioId, numero) = await MarcarGenerandoAsync(escenaId, ct);

        CategoriaFalla falla = CategoriaFalla.Interno;
        string? detalle = null;
        for (var intento = 1; intento <= MaximoIntentos; intento++)
        {
            notificador.NotificarEscena(new CambioEstadoEscena(escenaId, episodioId, EstadoEscena.Generando, intento, null));
            if (_esperas[intento - 1] > TimeSpan.Zero)
            {
                await Task.Delay(_esperas[intento - 1], tiempo, ct);
            }

            var inicio = tiempo.GetTimestamp();
            string? json = null;
            try
            {
                // La hoja efectiva se calcula al iniciar cada llamada (FR-003a).
                var escena = await CargarAsync(escenaId, ct);
                var instruccion = armador.Armar(escena);
                var clips = ClipsEsperados(escena);
                json = await LlamarAsync(instruccion, schema.Para(clips), ct);

                var validacion = validador.Validar(json, clips);
                if (validacion is ResultadoValidacion.Invalida invalida)
                {
                    (falla, detalle) = (CategoriaFalla.RespuestaInvalida, invalida.Motivo);
                }
                else
                {
                    await GuardarAsync(escenaId, ((ResultadoValidacion.Valida)validacion).Bloques, ct);
                    Registrar(episodioId, numero, intento, inicio, ResultadoIntento.Ok, null, null);
                    notificador.NotificarEscena(new CambioEstadoEscena(escenaId, episodioId, EstadoEscena.Generada, null, null));
                    return new ResultadoEscena(EstadoEscena.Generada, null);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                (falla, detalle) = (CategoriaFalla.Timeout, null);
            }
            catch (ErrorIa error)
            {
                (falla, detalle) = (error.Categoria, error.Detalle);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                (falla, detalle) = (CategoriaFalla.Interno, null);
            }

            Registrar(episodioId, numero, intento, inicio, Resultado(falla), MotivosGeneracion.Para(falla, detalle), json);
            if (falla is CategoriaFalla.Configuracion or CategoriaFalla.Interno)
            {
                break;
            }
        }

        var motivo = MotivosGeneracion.Para(falla, detalle);
        await MarcarErrorAsync(escenaId, motivo, ct);
        notificador.NotificarEscena(new CambioEstadoEscena(escenaId, episodioId, EstadoEscena.Error, null, motivo));
        return new ResultadoEscena(EstadoEscena.Error, falla);
    }

    private async Task<string> LlamarAsync(string instruccion, JsonObject schemaEscena, CancellationToken ct)
    {
        using var timeout = new CancellationTokenSource(TiempoMaximoPorLlamada, tiempo);
        using var enlazado = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
        return (await ia.GenerarAsync(instruccion, schemaEscena, enlazado.Token)).Json;
    }

    private async Task<(int EpisodioId, int Numero)> MarcarGenerandoAsync(int escenaId, CancellationToken ct)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var escena = await db.Escenas.SingleAsync(e => e.Id == escenaId, ct);
        escena.Estado = EstadoEscena.Generando;
        escena.MotivoError = null;
        await db.SaveChangesAsync(ct);
        return (escena.EpisodioId, escena.Numero);
    }

    private async Task MarcarErrorAsync(int escenaId, string motivo, CancellationToken ct)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        await db.Escenas.Where(e => e.Id == escenaId).ExecuteUpdateAsync(
            s => s.SetProperty(e => e.Estado, EstadoEscena.Error).SetProperty(e => e.MotivoError, motivo),
            ct);
    }

    private async Task<Escena> CargarAsync(int escenaId, CancellationToken ct)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        return await db.Escenas
            .Include(e => e.Clips).ThenInclude(c => c.Lineas)
            .Include(e => e.Personajes).ThenInclude(p => p.Hoja)
            .Include(e => e.Variantes)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleAsync(e => e.Id == escenaId, ct);
    }

    private async Task GuardarAsync(int escenaId, IReadOnlyList<BloqueValidado> bloques, CancellationToken ct)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var escena = await db.Escenas.Include(e => e.Clips).ThenInclude(c => c.Lineas).SingleAsync(e => e.Id == escenaId, ct);
        var lineas = escena.Clips.SelectMany(c => c.Lineas.Select(l => (Id: LineaDialogo.IdIaDe(c.Numero, l.Posicion), Linea: l)))
            .ToDictionary(x => x.Id, x => x.Linea.Id);

        foreach (var bloque in bloques)
        {
            var clip = escena.Clips.Single(c => c.Numero == bloque.Clip);
            db.Bloques.Add(new Bloque
            {
                ClipId = clip.Id,
                Tomas = bloque.Tomas.Select((t, i) => new Toma
                {
                    Orden = i + 1,
                    Plano = t.Plano,
                    Optica = t.Optica,
                    Iluminacion = t.Iluminacion,
                    Angulo = t.Angulo,
                    Movimiento = t.Movimiento,
                    Accion = t.Accion,
                    Dialogos = t.Dialogos.Select(id => new TomaDialogo { LineaDialogoId = lineas[id] }).ToList(),
                }).ToList(),
            });
        }

        escena.Estado = EstadoEscena.Generada;
        escena.MotivoError = null;
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
    }

    private static IReadOnlyList<ClipEsperado> ClipsEsperados(Escena escena) =>
        escena.Clips.OrderBy(c => c.Numero)
            .Select(c => new ClipEsperado(c.Numero, c.Lineas.OrderBy(l => l.Posicion).Select(l => LineaDialogo.IdIaDe(c.Numero, l.Posicion)).ToList()))
            .ToList();

    private void Registrar(int episodioId, int escena, int intento, long inicio, ResultadoIntento resultado, string? motivo, string? json) =>
        registro.Registrar(new IntentoGeneracion(episodioId, escena, intento, tiempo.GetElapsedTime(inicio), resultado, motivo, json));

    private static ResultadoIntento Resultado(CategoriaFalla falla) => falla switch
    {
        CategoriaFalla.Timeout => ResultadoIntento.Timeout,
        CategoriaFalla.ErrorIa => ResultadoIntento.ErrorIa,
        CategoriaFalla.RespuestaInvalida => ResultadoIntento.Invalida,
        CategoriaFalla.Configuracion => ResultadoIntento.Configuracion,
        _ => ResultadoIntento.ErrorInterno,
    };
}
