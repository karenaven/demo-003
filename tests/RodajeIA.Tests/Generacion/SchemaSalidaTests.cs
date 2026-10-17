using System.Text.Json.Nodes;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Generacion;

public class SchemaSalidaTests
{
    private static readonly Vocabulario _vocabulario = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto).Vocabulario;

    private static JsonObject Toma(JsonObject schema) =>
        schema["properties"]!["bloques"]!["items"]!["properties"]!["tomas"]!["items"]!.AsObject();

    [Fact]
    [Trait("AC", "AC-06a")]
    public void Cada_categoria_es_un_enum_con_los_valores_del_archivo_en_orden()
    {
        var toma = Toma(new SchemaSalida(_vocabulario).Json);

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
        var toma = Toma(new SchemaSalida(_vocabulario).Json);

        Assert.Equal(["plano", "optica", "iluminacion", "accion", "dialogos"], toma["required"]!.AsArray().Select(v => (string)v!));
        Assert.Equal("array", (string?)toma["properties"]!["dialogos"]!["type"]);
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

        var toma = Toma(new SchemaSalida(otro).Json);

        Assert.Equal(["plano raro"], toma["properties"]!["plano"]!["enum"]!.AsArray().Select(v => (string)v!));
        Assert.Equal(["dron"], toma["properties"]!["movimiento"]!["enum"]!.AsArray().Select(v => (string)v!));
    }

    [Fact]
    public void El_schema_no_se_comparte_entre_llamadas()
    {
        var schema = new SchemaSalida(_vocabulario);

        schema.Json["properties"]!.AsObject().Remove("bloques");

        Assert.NotNull(schema.Json["properties"]!["bloques"]);
    }
}
