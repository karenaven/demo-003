using System.Text.Json.Nodes;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Generacion;

public class SchemaSalidaTests
{
    private static readonly Vocabulario _vocabulario = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto).Vocabulario;

    /// <summary>Escena de 3 clips: el 1 con dos líneas, el 2 con una, el 3 sin diálogo.</summary>
    private static readonly IReadOnlyList<ClipEsperado> _tresClips =
    [
        new(1, ["c1-d1", "c1-d2"]),
        new(2, ["c2-d1"]),
        new(3, []),
    ];

    private static JsonObject Bloques(JsonObject schema) => schema["properties"]!["bloques"]!.AsObject();

    private static JsonObject Toma(JsonObject schema) =>
        Bloques(schema)["items"]!["properties"]!["tomas"]!["items"]!.AsObject();

    [Fact]
    [Trait("AC", "AC-06a")]
    public void Cada_categoria_es_un_enum_con_los_valores_del_archivo_en_orden()
    {
        var toma = Toma(new SchemaSalida(_vocabulario).Para(_tresClips));

        foreach (var categoria in _vocabulario.Categorias)
        {
            var propiedad = toma["properties"]![categoria.Nombre]!;
            Assert.Equal("string", (string?)propiedad["type"]);
            Assert.Equal(categoria.Valores, propiedad["enum"]!.AsArray().Select(v => (string)v!));
        }
    }

    [Fact]
    public void Required_tiene_solo_las_categorias_obligatorias_mas_accion_y_dialogos()
    {
        var toma = Toma(new SchemaSalida(_vocabulario).Para(_tresClips));

        Assert.Equal(["plano", "optica", "iluminacion", "accion", "dialogos"], toma["required"]!.AsArray().Select(v => (string)v!));
        Assert.Equal("array", (string?)toma["properties"]!["dialogos"]!["type"]);
    }

    [Fact]
    public void Los_dialogos_son_un_enum_con_los_ids_de_la_escena_en_orden()
    {
        var dialogos = Toma(new SchemaSalida(_vocabulario).Para(_tresClips))["properties"]!["dialogos"]!;

        Assert.Equal(["c1-d1", "c1-d2", "c2-d1"], dialogos["items"]!["enum"]!.AsArray().Select(v => (string)v!));
        Assert.Null(dialogos["maxItems"]);
    }

    [Fact]
    public void Una_escena_sin_dialogos_no_admite_ids()
    {
        var dialogos = Toma(new SchemaSalida(_vocabulario).Para([new(1, []), new(2, [])]))["properties"]!["dialogos"]!;

        Assert.Equal(0, (int)dialogos["maxItems"]!);
        Assert.Null(dialogos["items"]!["enum"]);
    }

    [Fact]
    public void Acota_el_numero_de_clip_y_pide_al_menos_una_toma_sin_fijar_la_cantidad_de_bloques()
    {
        var bloques = Bloques(new SchemaSalida(_vocabulario).Para(_tresClips));

        // Gemini rechaza minItems/maxItems en bloques con 20 clips (HTTP 400); RN-08 lo valida.
        Assert.Null(bloques["minItems"]);
        Assert.Null(bloques["maxItems"]);
        var clip = bloques["items"]!["properties"]!["clip"]!;
        Assert.Equal((1, 3), ((int)clip["minimum"]!, (int)clip["maximum"]!));
        Assert.Equal(1, (int)bloques["items"]!["properties"]!["tomas"]!["minItems"]!);
    }

    [Fact]
    public void Un_vocabulario_distinto_cambia_el_schema_sin_tocar_el_codigo()
    {
        var otro = new Vocabulario(
        [
            new CategoriaVocabulario("plano", true, ["plano raro"]),
            new CategoriaVocabulario("optica", true, ["12mm"]),
            new CategoriaVocabulario("iluminacion", true, ["neón"]),
            new CategoriaVocabulario("angulo", false, ["holandés"]),
            new CategoriaVocabulario("movimiento", false, ["dron"]),
        ]);

        var toma = Toma(new SchemaSalida(otro).Para(_tresClips));

        Assert.Equal(["plano raro"], toma["properties"]!["plano"]!["enum"]!.AsArray().Select(v => (string)v!));
        Assert.Equal(["dron"], toma["properties"]!["movimiento"]!["enum"]!.AsArray().Select(v => (string)v!));
    }

    [Fact]
    public void El_schema_no_se_comparte_entre_llamadas()
    {
        var schema = new SchemaSalida(_vocabulario);

        schema.Para(_tresClips)["properties"]!.AsObject().Remove("bloques");

        Assert.NotNull(schema.Para([new(1, [])])["properties"]!["bloques"]);
        Assert.Null(Toma(schema.Para([new(1, [])]))["properties"]!["dialogos"]!["items"]!["enum"]);
    }
}
