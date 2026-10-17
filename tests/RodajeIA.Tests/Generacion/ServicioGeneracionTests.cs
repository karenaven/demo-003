using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Generacion;

public sealed class ServicioGeneracionTests : IAsyncLifetime
{
    private readonly EntornoGeneracion _entorno = new();
    private readonly ServicioGeneracion _servicio;
    private int _episodioId;
    private int _serieId;

    public ServicioGeneracionTests()
    {
        _servicio = new ServicioGeneracion(_entorno.Base, _entorno.Generador, _entorno.Notificador, NullLogger<ServicioGeneracion>.Instance);
    }

    public async ValueTask InitializeAsync()
    {
        var (serie, _, _, episodio) = _entorno.Base.CrearSerieDeEjemplo();
        (_serieId, _episodioId) = (serie.Id, episodio.Id);
        await _servicio.StartAsync(CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await _servicio.StopAsync(CancellationToken.None);
        _servicio.Dispose();
        _entorno.Dispose();
    }

    private async Task<IReadOnlyList<int>> CargarAsync(int escenas, int? episodioId = null) =>
        await EscenaDePrueba.CargarAsync(_entorno.Base, episodioId ?? _episodioId, Ejemplos.Guion(escenas, 2));

    private async Task<Dictionary<int, Escena>> EscenasAsync(IReadOnlyList<int> ids)
    {
        var resultado = new Dictionary<int, Escena>();
        foreach (var id in ids)
        {
            resultado[id] = await EscenaDePrueba.LeerAsync(_entorno.Base, id);
        }

        return resultado;
    }

    private void ResponderSiempreBien() =>
        _entorno.Ia.SiempreResponde(instruccion => RespuestaPara(instruccion));

    /// <summary>Respuesta válida para la escena de <see cref="Ejemplos.Guion"/> con 2 clips y un diálogo cada uno.</summary>
    private static string RespuestaPara(string instruccion) =>
        EscenaDePrueba.Respuesta([(1, ["c1-d1"]), (2, ["c2-d1"])]);

    private Task EsperarAsync() => _entorno.AvanzarHastaAsync(_servicio.EsperarInactivoAsync());

    [Fact]
    [Trait("AC", "AC-06f")]
    public async Task Generar_deja_las_escenas_pendientes_en_generada()
    {
        var ids = await CargarAsync(2);
        ResponderSiempreBien();

        Assert.True(await _servicio.TryIniciarEpisodioAsync(_episodioId));
        await EsperarAsync();

        Assert.All((await EscenasAsync(ids)).Values, e => Assert.Equal(EstadoEscena.Generada, e.Estado));
        Assert.False(_servicio.EnCurso);
        Assert.Null(_servicio.EpisodioEnCurso);
    }

    [Fact]
    [Trait("AC", "AC-06k")]
    public async Task Solo_se_generan_las_escenas_pendientes()
    {
        var ids = await CargarAsync(2);
        await using (var db = _entorno.Base.CreateDbContext())
        {
            await db.Escenas.Where(e => e.Id == ids[0]).ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, EstadoEscena.Generada));
        }

        ResponderSiempreBien();

        await _servicio.TryIniciarEpisodioAsync(_episodioId);
        await EsperarAsync();

        Assert.Single(_entorno.Ia.Llamadas);
        Assert.Equal(EstadoEscena.Generada, (await EscenasAsync(ids))[ids[1]].Estado);
    }

    [Fact]
    [Trait("AC", "AC-10b")]
    [Trait("AC", "SC-009")]
    public async Task Una_falla_en_la_escena_3_conserva_los_bloques_de_las_otras()
    {
        var ids = await CargarAsync(3);
        _entorno.Ia
            .Responde(RespuestaPara)
            .Responde(RespuestaPara)
            .Falla(CategoriaFalla.ErrorIa).Falla(CategoriaFalla.ErrorIa).Falla(CategoriaFalla.ErrorIa);

        await _servicio.TryIniciarEpisodioAsync(_episodioId);
        await EsperarAsync();

        var escenas = await EscenasAsync(ids);
        Assert.Equal(EstadoEscena.Generada, escenas[ids[0]].Estado);
        Assert.Equal(EstadoEscena.Generada, escenas[ids[1]].Estado);
        Assert.All(escenas[ids[0]].Clips.Concat(escenas[ids[1]].Clips), c => Assert.NotNull(c.Bloque));
        Assert.Equal(EstadoEscena.Error, escenas[ids[2]].Estado);
        Assert.Equal(5, _entorno.Ia.Llamadas.Count);
    }

    [Fact]
    [Trait("AC", "AC-10d")]
    public async Task Reintentar_una_escena_en_error_la_deja_generada()
    {
        var ids = await CargarAsync(1);
        _entorno.Ia.Falla(CategoriaFalla.ErrorIa).Falla(CategoriaFalla.ErrorIa).Falla(CategoriaFalla.ErrorIa);
        await _servicio.TryIniciarEpisodioAsync(_episodioId);
        await EsperarAsync();
        Assert.Equal(EstadoEscena.Error, (await EscenasAsync(ids))[ids[0]].Estado);

        ResponderSiempreBien();
        Assert.True(await _servicio.TryReintentarAsync(ids[0]));
        await EsperarAsync();

        var escena = (await EscenasAsync(ids))[ids[0]];
        Assert.Equal(EstadoEscena.Generada, escena.Estado);
        Assert.Null(escena.MotivoError);
        Assert.All(escena.Clips, c => Assert.NotNull(c.Bloque));
    }

    [Fact]
    [Trait("AC", "AC-S13")]
    public async Task Las_escenas_se_generan_de_a_una_y_las_que_esperan_siguen_pendientes()
    {
        var ids = await CargarAsync(3);
        var estadosDurante = new List<EstadoEscena[]>();
        _entorno.Ia.SiempreResponde(instruccion =>
        {
            estadosDurante.Add(EscenasAsync(ids).GetAwaiter().GetResult().Values.Select(e => e.Estado).ToArray());
            return RespuestaPara(instruccion);
        });

        await _servicio.TryIniciarEpisodioAsync(_episodioId);
        await EsperarAsync();

        Assert.Equal(
            [
                [EstadoEscena.Generando, EstadoEscena.Pendiente, EstadoEscena.Pendiente],
                [EstadoEscena.Generada, EstadoEscena.Generando, EstadoEscena.Pendiente],
                [EstadoEscena.Generada, EstadoEscena.Generada, EstadoEscena.Generando],
            ],
            estadosDurante);
    }

    [Fact]
    [Trait("AC", "AC-S14")]
    public async Task Con_una_generacion_en_curso_no_se_inicia_otra_en_ningun_episodio()
    {
        var ids = await CargarAsync(1);
        int otroEpisodio;
        await using (var db = _entorno.Base.CreateDbContext())
        {
            var episodio = new Episodio { SerieId = _serieId, Numero = 2, Titulo = "Otro" };
            db.Episodios.Add(episodio);
            await db.SaveChangesAsync();
            otroEpisodio = episodio.Id;
        }

        await CargarAsync(1, otroEpisodio);
        var liberar = new TaskCompletionSource<string>();
        var enCursoDurante = (EnCurso: false, Episodio: (int?)null, OtroEpisodio: true, Reintento: true);
        _entorno.Ia.Ejecuta(async (_, _) =>
        {
            enCursoDurante = (
                _servicio.EnCurso,
                _servicio.EpisodioEnCurso,
                await _servicio.TryIniciarEpisodioAsync(otroEpisodio),
                await _servicio.TryReintentarAsync(ids[0]));
            return await liberar.Task;
        });

        await _servicio.TryIniciarEpisodioAsync(_episodioId);
        while (_entorno.Ia.Llamadas.Count == 0 || enCursoDurante.Episodio is null)
        {
            await Task.Delay(5);
        }

        liberar.SetResult(RespuestaPara(""));
        await EsperarAsync();

        Assert.Equal((true, (int?)_episodioId, false, false), enCursoDurante);
        Assert.Single(_entorno.Ia.Llamadas);
    }

    [Fact]
    [Trait("AC", "AC-S23")]
    public async Task Un_error_de_configuracion_detiene_la_cola()
    {
        var ids = await CargarAsync(3);
        _entorno.Ia.Falla(CategoriaFalla.Configuracion, "falta la clave (Gemini:ApiKey)");

        await _servicio.TryIniciarEpisodioAsync(_episodioId);
        await EsperarAsync();

        var escenas = await EscenasAsync(ids);
        Assert.Single(_entorno.Ia.Llamadas);
        Assert.Equal(EstadoEscena.Error, escenas[ids[0]].Estado);
        Assert.StartsWith("Error de configuración de la IA", escenas[ids[0]].MotivoError, StringComparison.Ordinal);
        Assert.Equal(EstadoEscena.Pendiente, escenas[ids[1]].Estado);
        Assert.Equal(EstadoEscena.Pendiente, escenas[ids[2]].Estado);
    }

    [Fact]
    public async Task Un_error_interno_no_detiene_la_cola()
    {
        var ids = await CargarAsync(2);
        _entorno.Ia
            .Ejecuta(async (instruccion, _) =>
            {
                await using var db = _entorno.Base.CreateDbContext();
                await db.LineasDialogo.Where(l => l.Clip!.EscenaId == ids[0] && l.Clip.Numero == 2).ExecuteDeleteAsync();
                return RespuestaPara(instruccion);
            })
            .Responde(RespuestaPara);

        await _servicio.TryIniciarEpisodioAsync(_episodioId);
        await EsperarAsync();

        var escenas = await EscenasAsync(ids);
        Assert.Equal("Error interno.", escenas[ids[0]].MotivoError);
        Assert.Equal(EstadoEscena.Generada, escenas[ids[1]].Estado);
    }

    [Fact]
    [Trait("AC", "AC-S28")]
    public async Task Reintentar_genera_solo_esa_escena()
    {
        var ids = await CargarAsync(2);
        await using (var db = _entorno.Base.CreateDbContext())
        {
            await db.Escenas.Where(e => e.Id == ids[0]).ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Estado, EstadoEscena.Error)
                .SetProperty(e => e.MotivoError, "La IA no respondió a tiempo."));
        }

        ResponderSiempreBien();

        Assert.True(await _servicio.TryReintentarAsync(ids[0]));
        await EsperarAsync();

        var escenas = await EscenasAsync(ids);
        Assert.Single(_entorno.Ia.Llamadas);
        Assert.Equal(EstadoEscena.Generada, escenas[ids[0]].Estado);
        Assert.Equal(EstadoEscena.Pendiente, escenas[ids[1]].Estado);
    }

    [Fact]
    public async Task Reintentar_una_escena_que_no_esta_en_error_no_hace_nada()
    {
        var ids = await CargarAsync(1);

        Assert.False(await _servicio.TryReintentarAsync(ids[0]));
        Assert.False(await _servicio.TryIniciarEpisodioAsync(_episodioId + 100));
        Assert.Empty(_entorno.Ia.Llamadas);
    }

    [Fact]
    public async Task Avisa_cuando_empieza_y_termina_la_generacion()
    {
        await CargarAsync(1);
        ResponderSiempreBien();
        var cambios = new List<bool>();
        _entorno.Notificador.GeneracionEnCursoCambiada += (enCurso, _) => cambios.Add(enCurso);

        await _servicio.TryIniciarEpisodioAsync(_episodioId);
        await EsperarAsync();

        Assert.Equal([true, false], cambios);
    }
}
