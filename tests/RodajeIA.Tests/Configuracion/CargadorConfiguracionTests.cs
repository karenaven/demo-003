using System.Text.Json.Nodes;
using RodajeIA.Web.Configuracion;

namespace RodajeIA.Tests.Configuracion;

public sealed class CargadorConfiguracionTests : IDisposable
{
    private static readonly string _directorioReal = Path.Combine(AppContext.BaseDirectory, "config");
    private readonly string _directorio;

    public CargadorConfiguracionTests()
    {
        _directorio = Path.Combine(Path.GetTempPath(), "rodajeia-config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directorio);
        foreach (var archivo in Directory.GetFiles(_directorioReal))
        {
            File.Copy(archivo, Path.Combine(_directorio, Path.GetFileName(archivo)));
        }
    }

    public void Dispose() => Directory.Delete(_directorio, recursive: true);

    private string Ruta(string archivo) => Path.Combine(_directorio, archivo);

    private void ModificarJson(string archivo, Action<JsonNode> cambio)
    {
        var nodo = JsonNode.Parse(File.ReadAllText(Ruta(archivo)))!;
        cambio(nodo);
        File.WriteAllText(Ruta(archivo), nodo.ToJsonString());
    }

    private IReadOnlyList<string> Problemas()
    {
        var ex = Assert.Throws<ConfiguracionInvalidaException>(() => CargadorConfiguracion.Cargar(_directorio));
        return ex.Problemas;
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Los_archivos_reales_del_repositorio_son_validos()
    {
        var config = CargadorConfiguracion.Cargar(_directorioReal);

        Assert.Equal(8, config.Vocabulario.Categoria("plano").Valores.Count);
        Assert.False(config.Vocabulario.Categoria("angulo").Obligatorio);
        Assert.NotEmpty(config.Estilo.Texto);
        Assert.Contains("{{escena}}", config.Instruccion.Plantilla);
        Assert.Equal(" Corte a ", config.Molde.Bloques.SeparadorTomas);
    }

    [Theory]
    [Trait("AC", "AC-S29")]
    [InlineData("vocabulario.json")]
    [InlineData("estilo.txt")]
    [InlineData("instruccion-generacion.txt")]
    [InlineData("molde-prompt.json")]
    public void Un_archivo_faltante_se_informa(string archivo)
    {
        File.Delete(Ruta(archivo));

        Assert.Contains($"{archivo}: no existe", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_vocabulario_con_json_invalido_se_informa_con_su_linea()
    {
        File.WriteAllText(Ruta("vocabulario.json"), "{\n  \"categorias\": {\n    \"plano\": [\n");

        Assert.Contains(Problemas(), p => p.StartsWith("vocabulario.json: no es JSON válido (línea", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Falta_una_categoria_del_vocabulario()
    {
        ModificarJson("vocabulario.json", n => n["categorias"]!.AsObject().Remove("optica"));

        Assert.Contains("vocabulario.json: falta la categoría optica", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Una_categoria_desconocida_del_vocabulario()
    {
        ModificarJson("vocabulario.json", n => n["categorias"]!["camara"] = new JsonObject
        {
            ["obligatorio"] = false,
            ["valores"] = new JsonArray("ARRI"),
        });

        Assert.Contains("vocabulario.json: categoría desconocida camara", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Una_categoria_sin_valores()
    {
        ModificarJson("vocabulario.json", n => n["categorias"]!["angulo"]!.AsObject().Remove("valores"));

        Assert.Contains("vocabulario.json: la categoría angulo no tiene 'valores'", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Una_categoria_obligatoria_marcada_como_opcional()
    {
        ModificarJson("vocabulario.json", n => n["categorias"]!["plano"]!["obligatorio"] = false);

        Assert.Contains("vocabulario.json: plano debe ser obligatoria", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Una_categoria_opcional_marcada_como_obligatoria()
    {
        ModificarJson("vocabulario.json", n => n["categorias"]!["movimiento"]!["obligatorio"] = true);

        Assert.Contains("vocabulario.json: movimiento debe ser opcional", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_valor_repetido_en_una_categoria()
    {
        ModificarJson("vocabulario.json", n => n["categorias"]!["movimiento"]!["valores"]!.AsArray().Add("paneo"));

        Assert.Contains("vocabulario.json: valor repetido 'paneo' en movimiento", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_valor_vacio_en_una_categoria()
    {
        ModificarJson("vocabulario.json", n => n["categorias"]!["optica"]!["valores"]!.AsArray().Add(" "));

        Assert.Contains("vocabulario.json: valor vacío en optica", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_estilo_vacio()
    {
        File.WriteAllText(Ruta("estilo.txt"), "  \n");

        Assert.Contains("estilo.txt: está vacío", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Una_instruccion_sin_el_marcador_de_escena()
    {
        File.WriteAllText(Ruta("instruccion-generacion.txt"), "Reglas.\n{{personajes}}");

        Assert.Contains("instruccion-generacion.txt: falta el marcador {{escena}}", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Una_instruccion_con_un_marcador_repetido_o_desconocido()
    {
        File.WriteAllText(Ruta("instruccion-generacion.txt"), "{{escena}} {{escena}} {{personajes}} {{xyz}}");

        var problemas = Problemas();
        Assert.Contains("instruccion-generacion.txt: el marcador {{escena}} aparece más de una vez", problemas);
        Assert.Contains("instruccion-generacion.txt: marcador desconocido {{xyz}}", problemas);
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_molde_con_json_invalido()
    {
        File.WriteAllText(Ruta("molde-prompt.json"), "{ \"secciones\": ");

        Assert.Contains(Problemas(), p => p.StartsWith("molde-prompt.json: no es JSON válido", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_molde_sin_la_seccion_de_audio()
    {
        ModificarJson("molde-prompt.json", n =>
        {
            var secciones = n["secciones"]!.AsArray();
            secciones.Remove(secciones.First(s => (string?)s!["id"] == "audio"));
        });

        Assert.Contains("molde-prompt.json: falta la sección audio", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_molde_con_secciones_fuera_de_orden()
    {
        ModificarJson("molde-prompt.json", n =>
        {
            var secciones = n["secciones"]!.AsArray();
            var estilo = secciones[0]!;
            secciones.RemoveAt(0);
            secciones.Add(estilo);
        });

        Assert.Contains("molde-prompt.json: sección fuera de orden", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_molde_sin_corte_a()
    {
        ModificarJson("molde-prompt.json", n => Bloques(n)["separadorTomas"] = " Luego ");

        Assert.Contains("molde-prompt.json: falta la cadena de contrato 'Corte a'", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_molde_sin_el_titulo_de_bloque_de_contrato()
    {
        ModificarJson("molde-prompt.json", n => Bloques(n)["tituloBloque"] = "Clip {{bloque.numero}}:");

        Assert.Contains("molde-prompt.json: falta la cadena de contrato 'Bloque N:'", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_molde_sin_el_cierre_de_contrato()
    {
        ModificarJson("molde-prompt.json", n => Bloques(n)["cierreSinTextoEnPantalla"] = "Sin textos.");

        Assert.Contains("molde-prompt.json: falta la cadena de contrato 'Sin subtítulos ni texto en pantalla.'", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_molde_con_una_variable_desconocida()
    {
        ModificarJson("molde-prompt.json", n => Bloques(n)["toma"] = "{{toma.camara}}, óptica {{toma.optica}}.");

        Assert.Contains("molde-prompt.json: variable desconocida {{toma.camara}}", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Un_molde_con_un_corchete_sin_cerrar()
    {
        ModificarJson("molde-prompt.json", n => Bloques(n)["toma"] = "{{toma.plano}}[, {{toma.angulo}}, óptica {{toma.optica}}.");

        Assert.Contains("molde-prompt.json: corchete sin cerrar en 'toma'", Problemas());
    }

    [Fact]
    [Trait("AC", "AC-S29")]
    public void Se_informan_todos_los_archivos_invalidos_a_la_vez()
    {
        File.WriteAllText(Ruta("estilo.txt"), "");
        File.Delete(Ruta("molde-prompt.json"));

        var problemas = Problemas();
        Assert.Contains("estilo.txt: está vacío", problemas);
        Assert.Contains("molde-prompt.json: no existe", problemas);
    }

    private static JsonNode Bloques(JsonNode molde) =>
        molde["secciones"]!.AsArray().First(s => (string?)s!["id"] == "bloques")!;
}
