using Microsoft.EntityFrameworkCore;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Generacion;

public sealed class GeneradorEscenaTests : IDisposable
{
    private readonly EntornoGeneracion _entorno = new();

    public void Dispose() => _entorno.Dispose();

    private async Task<(int EscenaId, Escena Escena, HojaPersonaje Ana)> EscenaDeEjemploAsync()
    {
        var (_, ana, _, episodio) = _entorno.Base.CrearSerieDeEjemplo();
        var ids = await EscenaDePrueba.CargarAsync(_entorno.Base, episodio.Id, Ejemplos.EscenaAnexoA);
        return (ids[0], await EscenaDePrueba.LeerAsync(_entorno.Base, ids[0]), ana);
    }

    private Task<Escena> LeerAsync(int escenaId) => EscenaDePrueba.LeerAsync(_entorno.Base, escenaId);

    private static double Segundos(DateTimeOffset desde, DateTimeOffset hasta) => (hasta - desde).TotalSeconds;

    [Fact]
    public async Task Una_respuesta_valida_guarda_bloques_tomas_y_dialogos_y_deja_la_escena_generada()
    {
        var (escenaId, escena, _) = await EscenaDeEjemploAsync();
        _entorno.Ia.Responde(EscenaDePrueba.RespuestaValida(escena));

        var resultado = await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        Assert.Equal(EstadoEscena.Generada, resultado.Estado);
        var guardada = await LeerAsync(escenaId);
        Assert.Equal(EstadoEscena.Generada, guardada.Estado);
        Assert.Null(guardada.MotivoError);
        Assert.All(guardada.Clips, c => Assert.Single(c.Bloque!.Tomas));
        var toma = guardada.Clips.Single(c => c.Numero == 1).Bloque!.Tomas[0];
        Assert.Equal(2, toma.Dialogos.Count);
        Assert.Single(_entorno.Ia.Instrucciones);
    }

    [Fact]
    public async Task El_schema_de_la_llamada_acota_los_clips_y_los_ids_de_dialogo_de_la_escena()
    {
        var (escenaId, escena, _) = await EscenaDeEjemploAsync();
        _entorno.Ia.Responde(EscenaDePrueba.RespuestaValida(escena));

        await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        var bloques = _entorno.Ia.Schemas.Single()["properties"]!["bloques"]!;
        var clips = escena.Clips.Count;
        Assert.Equal((clips, clips), ((int)bloques["minItems"]!, (int)bloques["maxItems"]!));
        var ids = escena.Clips.OrderBy(c => c.Numero)
            .SelectMany(c => c.Lineas.OrderBy(l => l.Posicion).Select(l => LineaDialogo.IdIaDe(c.Numero, l.Posicion)));
        var dialogos = bloques["items"]!["properties"]!["tomas"]!["items"]!["properties"]!["dialogos"]!;
        Assert.Equal(ids, dialogos["items"]!["enum"]!.AsArray().Select(v => (string)v!));
    }

    [Fact]
    [Trait("AC", "AC-10a")]
    public async Task Tres_errores_hacen_exactamente_tres_intentos_y_dejan_la_escena_en_error()
    {
        var (escenaId, _, _) = await EscenaDeEjemploAsync();
        _entorno.Ia.Falla(CategoriaFalla.ErrorIa).Falla(CategoriaFalla.ErrorIa).Falla(CategoriaFalla.ErrorIa);

        var resultado = await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        Assert.Equal(EstadoEscena.Error, resultado.Estado);
        Assert.Equal(3, _entorno.Ia.Llamadas.Count);
        var guardada = await LeerAsync(escenaId);
        Assert.Equal(EstadoEscena.Error, guardada.Estado);
        Assert.StartsWith("La IA devolvió un error o no está disponible", guardada.MotivoError, StringComparison.Ordinal);
        Assert.All(guardada.Clips, c => Assert.Null(c.Bloque));
    }

    [Fact]
    [Trait("AC", "AC-10c")]
    public async Task Un_error_y_despues_una_respuesta_valida_dejan_la_escena_generada()
    {
        var (escenaId, escena, _) = await EscenaDeEjemploAsync();
        _entorno.Ia.Falla(CategoriaFalla.ErrorIa).Responde(EscenaDePrueba.RespuestaValida(escena));

        var resultado = await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        Assert.Equal(EstadoEscena.Generada, resultado.Estado);
        Assert.Equal(2, _entorno.Ia.Llamadas.Count);
        Assert.InRange(Segundos(_entorno.Ia.Llamadas[0], _entorno.Ia.Llamadas[1]), 10, 12);
    }

    [Fact]
    [Trait("AC", "AC-06e")]
    public async Task Una_respuesta_invalida_se_reintenta()
    {
        var (escenaId, escena, _) = await EscenaDeEjemploAsync();
        _entorno.Ia.Responde(EscenaDePrueba.Respuesta([(1, ["c1-d1", "c1-d2"])])).Responde(EscenaDePrueba.RespuestaValida(escena));

        var resultado = await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        Assert.Equal(EstadoEscena.Generada, resultado.Estado);
        Assert.Equal(2, _entorno.Ia.Llamadas.Count);
    }

    [Fact]
    [Trait("AC", "AC-S22")]
    public async Task Sin_respuesta_hay_tres_timeouts_de_60_segundos_con_esperas_de_10_y_30()
    {
        var (escenaId, _, _) = await EscenaDeEjemploAsync();
        _entorno.Ia.NoResponde().NoResponde().NoResponde();
        var intentos = new List<(EstadoEscena Estado, int? Intento)>();
        _entorno.Notificador.EstadoEscenaCambiado += c => intentos.Add((c.Estado, c.Intento));

        var resultado = await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        Assert.Equal(EstadoEscena.Error, resultado.Estado);
        var llamadas = _entorno.Ia.Llamadas;
        Assert.Equal(3, llamadas.Count);
        Assert.InRange(Segundos(llamadas[0], llamadas[1]), 69, 73);
        Assert.InRange(Segundos(llamadas[1], llamadas[2]), 89, 93);
        Assert.Contains((EstadoEscena.Generando, (int?)1), intentos);
        Assert.Contains((EstadoEscena.Generando, (int?)2), intentos);
        Assert.Contains((EstadoEscena.Generando, (int?)3), intentos);
        Assert.Equal(EstadoEscena.Error, intentos[^1].Estado);
        Assert.Equal("La IA no respondió a tiempo.", (await LeerAsync(escenaId)).MotivoError);
    }

    [Fact]
    [Trait("AC", "AC-S23")]
    public async Task Un_error_de_configuracion_no_se_reintenta()
    {
        var (escenaId, _, _) = await EscenaDeEjemploAsync();
        _entorno.Ia.Falla(CategoriaFalla.Configuracion, "falta la clave (Gemini:ApiKey)");

        var resultado = await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        Assert.Equal(CategoriaFalla.Configuracion, resultado.Falla);
        Assert.Single(_entorno.Ia.Llamadas);
        Assert.Equal("Error de configuración de la IA: falta la clave (Gemini:ApiKey).", (await LeerAsync(escenaId)).MotivoError);
    }

    [Fact]
    [Trait("AC", "AC-S24")]
    public async Task Si_falla_el_guardado_no_queda_ningun_bloque_y_el_motivo_es_error_interno()
    {
        var (escenaId, escena, _) = await EscenaDeEjemploAsync();
        _entorno.Ia.Ejecuta(async (_, _) =>
        {
            // Se borra una línea de diálogo: la referencia de la toma ya no existe y el guardado falla.
            await using var db = _entorno.Base.CreateDbContext();
            await db.LineasDialogo.Where(l => l.Posicion == 2).ExecuteDeleteAsync();
            return EscenaDePrueba.RespuestaValida(escena);
        });

        var resultado = await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        Assert.Equal(CategoriaFalla.Interno, resultado.Falla);
        Assert.Single(_entorno.Ia.Llamadas);
        var guardada = await LeerAsync(escenaId);
        Assert.Equal(EstadoEscena.Error, guardada.Estado);
        Assert.Equal("Error interno.", guardada.MotivoError);
        await using var db = _entorno.Base.CreateDbContext();
        Assert.Empty(await db.Bloques.ToListAsync());
        Assert.Empty(await db.Tomas.ToListAsync());
    }

    [Fact]
    [Trait("AC", "AC-S25")]
    public async Task El_motivo_se_borra_al_reintentar_con_exito_y_nunca_contiene_la_clave()
    {
        var (escenaId, escena, _) = await EscenaDeEjemploAsync();
        const string clave = "AIzaSECRETO123";
        _entorno.Ia.Falla(CategoriaFalla.Configuracion, "la clave fue rechazada (HTTP 401)");
        await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));
        var motivo = (await LeerAsync(escenaId)).MotivoError;
        Assert.NotNull(motivo);
        Assert.DoesNotContain(clave, motivo, StringComparison.Ordinal);

        _entorno.Ia.Responde(EscenaDePrueba.RespuestaValida(escena));
        await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        var guardada = await LeerAsync(escenaId);
        Assert.Equal(EstadoEscena.Generada, guardada.Estado);
        Assert.Null(guardada.MotivoError);
    }

    [Fact]
    public async Task La_hoja_efectiva_se_calcula_al_iniciar_cada_llamada()
    {
        var (escenaId, escena, ana) = await EscenaDeEjemploAsync();
        _entorno.Ia
            .Ejecuta(async (_, _) =>
            {
                await using var db = _entorno.Base.CreateDbContext();
                var hoja = await db.Hojas.SingleAsync(h => h.Id == ana.Id);
                hoja.Vestuario = "vestido rojo";
                await db.SaveChangesAsync();
                throw new ErrorIa(CategoriaFalla.ErrorIa, "HTTP 503");
            })
            .Responde(EscenaDePrueba.RespuestaValida(escena));

        await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        Assert.Contains("chaqueta negra", _entorno.Ia.Instrucciones[0], StringComparison.Ordinal);
        Assert.Contains("vestido rojo", _entorno.Ia.Instrucciones[1], StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-10e")]
    public async Task Mientras_espera_la_respuesta_la_escena_esta_en_generando()
    {
        var (escenaId, escena, _) = await EscenaDeEjemploAsync();
        EstadoEscena? durante = null;
        _entorno.Ia.Ejecuta(async (_, _) =>
        {
            durante = (await LeerAsync(escenaId)).Estado;
            return EscenaDePrueba.RespuestaValida(escena);
        });

        await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        Assert.Equal(EstadoEscena.Generando, durante);
    }
}
