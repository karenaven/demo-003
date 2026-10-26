using System.Text.Json.Nodes;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Generacion;
using RodajeIA.Web.Personajes;

namespace RodajeIA.Tests.Generacion;

public class ValidadorRespuestaTests
{
    private static readonly ConfiguracionRodaje _config = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto);

    private static readonly ValidadorRespuesta _validador = new(_config.Vocabulario, _config.Molde);

    private static readonly HojaEfectiva _ana = HojaEfectiva.Calcular(Ejemplos.HojaAna(), null);

    /// <summary>Escena de 3 clips: el 1 con dos líneas, el 2 con una, el 3 sin diálogo.</summary>
    private static readonly IReadOnlyList<ClipEsperado> _tresClips =
    [
        new(1, ["c1-d1", "c1-d2"]),
        new(2, ["c2-d1"]),
        new(3, []),
    ];

    private static JsonObject Toma(params string[] dialogos) => new()
    {
        ["plano"] = "plano medio",
        ["optica"] = "50mm",
        ["iluminacion"] = "continuidad con la luz base",
        ["accion"] = "Ana mira.",
        ["dialogos"] = new JsonArray(dialogos.Select(d => (JsonNode)d).ToArray()),
    };

    private static JsonObject Bloque(int clip, params JsonObject[] tomas) => new()
    {
        ["clip"] = clip,
        ["tomas"] = new JsonArray(tomas.Select(t => (JsonNode)t).ToArray()),
    };

    private static string Respuesta(params JsonObject[] bloques) =>
        new JsonObject { ["bloques"] = new JsonArray(bloques.Select(b => (JsonNode)b).ToArray()) }.ToJsonString();

    private static JsonObject[] BloquesValidos() =>
    [
        Bloque(1, Toma("c1-d1", "c1-d2")),
        Bloque(2, Toma("c2-d1")),
        Bloque(3, Toma()),
    ];

    private static string Motivo(string json, params HojaEfectiva[] hojas) =>
        Assert.IsType<ResultadoValidacion.Invalida>(_validador.Validar(json, _tresClips, hojas)).Motivo;

    private static string ConAccion(string accion)
    {
        var bloques = BloquesValidos();
        var toma = Toma("c2-d1");
        toma["accion"] = accion;
        bloques[1] = Bloque(2, toma);
        return Respuesta(bloques);
    }

    [Fact]
    [Trait("AC", "AC-06a")]
    public void Una_respuesta_valida_trae_un_bloque_por_clip_con_valores_del_vocabulario()
    {
        var resultado = Assert.IsType<ResultadoValidacion.Valida>(_validador.Validar(Respuesta(BloquesValidos()), _tresClips, []));

        Assert.Equal([1, 2, 3], resultado.Bloques.Select(b => b.Clip));
        var toma = resultado.Bloques[0].Tomas[0];
        Assert.Equal(("plano medio", "50mm", "continuidad con la luz base"), (toma.Plano, toma.Optica, toma.Iluminacion));
        Assert.Equal(["c1-d1", "c1-d2"], toma.Dialogos);
    }

    [Fact]
    [Trait("AC", "AC-06b")]
    public void Falta_una_linea_de_dialogo()
    {
        var bloques = BloquesValidos();
        bloques[0] = Bloque(1, Toma("c1-d1"));

        Assert.Equal("falta el diálogo c1-d2 en el clip 1", Motivo(Respuesta(bloques)));
    }

    [Fact]
    [Trait("AC", "AC-06c")]
    public void Una_linea_referenciada_en_dos_tomas()
    {
        var bloques = BloquesValidos();
        bloques[1] = Bloque(2, Toma("c2-d1"), Toma("c2-d1"));

        Assert.Equal("el diálogo c2-d1 se usa dos veces", Motivo(Respuesta(bloques)));
    }

    [Theory]
    [Trait("AC", "AC-06d")]
    [InlineData("c1-d1", "el id c1-d1 no es del clip 2")]
    [InlineData("c9-d9", "el id c9-d9 no es del clip 2")]
    public void Un_id_de_otro_clip_o_inexistente(string id, string motivo)
    {
        var bloques = BloquesValidos();
        bloques[1] = Bloque(2, Toma("c2-d1", id));

        Assert.Equal(motivo, Motivo(Respuesta(bloques)));
    }

    [Fact]
    [Trait("AC", "AC-06e")]
    public void Menos_bloques_que_clips()
    {
        var bloques = BloquesValidos();

        Assert.Equal("falta el bloque del clip 3", Motivo(Respuesta(bloques[0], bloques[1])));
    }

    [Fact]
    [Trait("AC", "AC-06g")]
    public void Un_clip_repetido()
    {
        var bloques = BloquesValidos();

        Assert.Equal("el clip 1 aparece dos veces", Motivo(Respuesta(bloques[0], Bloque(1, Toma("c1-d1", "c1-d2")), bloques[2])));
    }

    [Fact]
    public void Un_clip_que_no_existe_en_la_escena()
    {
        Assert.Equal("el clip 4 no existe en la escena", Motivo(Respuesta([.. BloquesValidos(), Bloque(4, Toma())])));
    }

    [Fact]
    [Trait("AC", "AC-06h")]
    public void Un_bloque_sin_tomas()
    {
        var bloques = BloquesValidos();
        bloques[2] = Bloque(3);

        Assert.Equal("el bloque del clip 3 no tiene tomas", Motivo(Respuesta(bloques)));
    }

    [Theory]
    [Trait("AC", "AC-06i")]
    [Trait("AC", "AC-S19")]
    [InlineData("")]
    [InlineData("  \n\t ")]
    public void Una_accion_vacia_o_solo_con_espacios(string accion)
    {
        var bloques = BloquesValidos();
        var toma = Toma();
        toma["accion"] = accion;
        bloques[2] = Bloque(3, toma);

        Assert.Equal("la toma 1 del clip 3 no tiene acción", Motivo(Respuesta(bloques)));
    }

    [Theory]
    [Trait("AC", "AC-S17")]
    [InlineData("no es json")]
    [InlineData("[]")]
    [InlineData("{\"bloques\": {}}")]
    [InlineData("{\"bloques\": [{\"clip\": \"uno\", \"tomas\": []}]}")]
    [InlineData("{\"bloques\": [], \"extra\": 1}")]
    public void Una_respuesta_sin_el_formato_pedido(string json)
    {
        Assert.Equal("la respuesta no tiene el formato pedido", Motivo(json));
    }

    [Fact]
    [Trait("AC", "AC-S17")]
    public void Un_campo_desconocido_en_la_toma()
    {
        var bloques = BloquesValidos();
        var toma = Toma();
        toma["camara"] = "ARRI";
        bloques[2] = Bloque(3, toma);

        Assert.Equal("la respuesta no tiene el formato pedido", Motivo(Respuesta(bloques)));
    }

    [Fact]
    [Trait("AC", "AC-S17")]
    [Trait("AC", "SC-003")]
    public void Un_plano_fuera_del_vocabulario()
    {
        var bloques = BloquesValidos();
        var toma = Toma();
        toma["plano"] = "plano cinemático";
        bloques[2] = Bloque(3, toma);

        Assert.Equal("el plano «plano cinemático» de la toma 1 del clip 3 no está en el vocabulario", Motivo(Respuesta(bloques)));
    }

    [Fact]
    [Trait("AC", "AC-S18")]
    public void Dialogos_fuera_de_orden_entre_tomas()
    {
        var bloques = BloquesValidos();
        bloques[0] = Bloque(1, Toma("c1-d2"), Toma("c1-d1"));

        Assert.Equal("diálogos fuera de orden en el clip 1", Motivo(Respuesta(bloques)));
    }

    [Fact]
    [Trait("AC", "AC-S18")]
    public void Dentro_de_una_toma_el_orden_de_la_lista_no_importa()
    {
        var bloques = BloquesValidos();
        bloques[0] = Bloque(1, Toma("c1-d2", "c1-d1"));

        Assert.IsType<ResultadoValidacion.Valida>(_validador.Validar(Respuesta(bloques), _tresClips, []));
    }

    [Fact]
    [Trait("AC", "AC-S19")]
    public void Angulo_y_movimiento_vacios_o_nulos_equivalen_a_sin_valor()
    {
        var bloques = BloquesValidos();
        var toma = Toma();
        toma["angulo"] = "";
        toma["movimiento"] = null;
        bloques[2] = Bloque(3, toma);

        var valida = Assert.IsType<ResultadoValidacion.Valida>(_validador.Validar(Respuesta(bloques), _tresClips, []));

        Assert.Null(valida.Bloques[2].Tomas[0].Angulo);
        Assert.Null(valida.Bloques[2].Tomas[0].Movimiento);
    }

    [Theory]
    [Trait("AC", "AC-S19")]
    [InlineData("")]
    [InlineData(null)]
    public void Una_optica_vacia_o_nula_hace_fallida_la_respuesta(string? optica)
    {
        var bloques = BloquesValidos();
        var toma = Toma();
        toma["optica"] = optica;
        bloques[2] = Bloque(3, toma);

        Assert.Equal("la toma 1 del clip 3 no tiene óptica", Motivo(Respuesta(bloques)));
    }

    [Fact]
    [Trait("AC", "AC-S19")]
    public void Un_plano_ausente_hace_fallida_la_respuesta()
    {
        var bloques = BloquesValidos();
        var toma = Toma();
        toma.Remove("plano");
        bloques[2] = Bloque(3, toma);

        Assert.Equal("la toma 1 del clip 3 no tiene plano", Motivo(Respuesta(bloques)));
    }

    [Theory]
    [Trait("AC", "AC-S20")]
    [InlineData("Ana dice \"hola\".")]
    [InlineData("Ana dice “hola”.")]
    public void Una_accion_con_comillas(string accion)
    {
        var bloques = BloquesValidos();
        var toma = Toma("c2-d1");
        toma["accion"] = accion;
        bloques[1] = Bloque(2, toma);

        Assert.Equal("la acción de la toma 1 del clip 2 tiene comillas", Motivo(Respuesta(bloques)));
    }

    [Fact]
    [Trait("AC", "AC-S21")]
    public void Con_varias_fallas_el_motivo_informa_solo_la_primera_segun_la_prioridad()
    {
        var accionVacia = Toma("c1-d1", "c1-d2");
        accionVacia["accion"] = "";

        var motivo = Motivo(Respuesta(Bloque(1, accionVacia), Bloque(2, Toma("c2-d1"))));

        Assert.Equal("falta el bloque del clip 3", motivo);
    }

    [Fact]
    public void La_accion_valida_se_devuelve_recortada()
    {
        var bloques = BloquesValidos();
        bloques[2]["tomas"]![0]!["accion"] = "  Ana mira.\n";

        var valida = Assert.IsType<ResultadoValidacion.Valida>(_validador.Validar(Respuesta(bloques), _tresClips, []));

        Assert.Equal("Ana mira.", valida.Bloques[2].Tomas[0].Accion);
    }

    [Fact]
    [Trait("AC", "AC-S40")]
    public void Una_accion_que_copia_la_hoja_renderizada()
    {
        var accion = _ana.Renderizar(_config.Molde) + " espera bajo el toldo.";

        Assert.Equal("la acción de la toma 1 del clip 2 copia la etiqueta «Peinado:» de la hoja", Motivo(ConAccion(accion), _ana));
    }

    [Theory]
    [Trait("AC", "AC-S40")]
    [InlineData("Ana, mujer de piel morena, espera bajo el toldo.", "la descripción física")]
    [InlineData("Ana, irónica y observadora, espera bajo el toldo.", "la personalidad")]
    [InlineData("Ana se acomoda el pelo negro largo.", "el peinado")]
    public void Una_accion_que_copia_un_campo_de_la_hoja(string accion, string campo)
    {
        Assert.Equal($"la acción de la toma 1 del clip 2 copia {campo} de la hoja de Ana", Motivo(ConAccion(accion), _ana));
    }

    [Fact]
    [Trait("AC", "AC-S40")]
    public void Una_accion_con_una_etiqueta_de_campo_del_molde()
    {
        Assert.Equal("la acción de la toma 1 del clip 2 copia la etiqueta «Rol:» de la hoja", Motivo(ConAccion("Rol: Protagonista. Ana espera."), _ana));
    }

    [Theory]
    [InlineData("Ana se ajusta la chaqueta negra y espera.")]
    [InlineData("Ana revisa el control: nada cambia.")]
    [InlineData("Ana espera, irónica, bajo el toldo.")]
    public void Una_accion_que_menciona_rasgos_sueltos_es_valida(string accion)
    {
        Assert.IsType<ResultadoValidacion.Valida>(_validador.Validar(ConAccion(accion), _tresClips, [_ana]));
    }

    private static readonly HojaEfectiva _joseDaniel = HojaEfectiva.Calcular(Ejemplos.HojaJoseDaniel(), null);

    private static readonly HojaEfectiva _luz = HojaEfectiva.Calcular(Ejemplos.Hoja("Luz"), null);

    [Theory]
    [Trait("AC", "AC-S41")]
    [InlineData("Jose Daniel sonríe pícaramente.", "Jose Daniel", "José Daniel")]
    [InlineData("Ana mira a Jose Daniel.", "Jose Daniel", "José Daniel")]
    [InlineData("José Daniel mira a Ána.", "Ána", "Ana")]
    public void Una_accion_con_un_nombre_sin_sus_tildes(string accion, string escrito, string nombre)
    {
        Assert.Equal(
            $"la acción de la toma 1 del clip 2 escribe «{escrito}» en lugar de «{nombre}»",
            Motivo(ConAccion(accion), _ana, _joseDaniel));
    }

    [Theory]
    [Trait("AC", "AC-S41")]
    [InlineData("José Daniel sonríe pícaramente.")]
    [InlineData("Josefina mira a José Daniel.")]
    [InlineData("Luz enciende la luz de la mesa.")]
    [InlineData("Ana sonríe 🙂 a José Daniel.")]
    public void Una_accion_con_los_nombres_como_en_la_hoja_es_valida(string accion)
    {
        Assert.IsType<ResultadoValidacion.Valida>(_validador.Validar(ConAccion(accion), _tresClips, [_ana, _joseDaniel, _luz]));
    }
}
