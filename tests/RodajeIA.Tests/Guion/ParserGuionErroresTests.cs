using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Guion;

namespace RodajeIA.Tests.Guion;

/// <summary>
/// Catálogo de errores E00 a E17 de contracts/guion.md. Líneas de la escena del Anexo A:
/// 1 ESCENA · 2 LOCACIÓN · 3 ILUMINACIÓN · 4 PUESTA EN ESCENA · 5 AUDIO · 6 CLIP 1 · 7 acción ·
/// 8 ANA · 9 JOSÉ DANIEL · 10 acción · 11 CLIP 2 · 12 acción.
/// </summary>
public class ParserGuionErroresTests
{
    private const string EscenaDos = """
        ESCENA 2 — EXT. CALLE — DÍA
        LOCACIÓN: Calle angosta.
        ILUMINACIÓN: Sol de mediodía.
        PUESTA EN ESCENA: Ana camina sola.
        AUDIO: Tránsito lejano.
        CLIP 1
        Ana camina.
        """;

    private static IReadOnlyList<ErrorGuion> Errores(string texto)
    {
        var resultado = ParserGuion.Parsear(texto);
        return Assert.IsType<ResultadoGuion.Invalido>(resultado).Errores;
    }

    private static string Reemplazar(string viejo, string nuevo) =>
        Ejemplos.EscenaAnexoA.Replace(viejo, nuevo, StringComparison.Ordinal);

    private static string Quitar(string linea) => Reemplazar(linea + "\n", "");

    private static string LineaN(string texto, int numero) => texto.Split('\n')[numero - 1];

    [Fact]
    [Trait("AC", "AC-S05")]
    public void E00_mas_de_cien_mil_caracteres_da_un_unico_error()
    {
        var texto = new string('x', 100_001);

        var error = Assert.Single(Errores(texto));

        Assert.Equal(new ErrorGuion(1, "E00", "El guion supera los 100.000 caracteres."), error);
    }

    [Fact]
    [Trait("AC", "AC-S05")]
    public void Cien_mil_caracteres_exactos_no_disparan_E00()
    {
        var texto = Ejemplos.EscenaAnexoA + new string(' ', 100_000 - Ejemplos.EscenaAnexoA.Length);

        Assert.IsType<ResultadoGuion.Valido>(ParserGuion.Parsear(texto));
    }

    [Theory]
    [Trait("AC", "AC-03u")]
    [Trait("AC", "AC-03b")]
    [InlineData("")]
    [InlineData("\n\n  \n")]
    [InlineData("Ana entra al bar.\nJosé Daniel la mira.")]
    public void E01_guion_vacio_o_sin_escenas_da_un_error_en_la_linea_1(string texto)
    {
        var error = Assert.Single(Errores(texto));

        Assert.Equal(1, error.Linea);
        Assert.Equal("E01", error.Codigo);
        Assert.Equal("El guion no tiene ninguna escena; debe empezar con un encabezado como \"ESCENA 1 — INT. LUGAR — NOCHE\".", error.Motivo);
    }

    [Fact]
    [Trait("AC", "AC-03k")]
    public void E02_texto_antes_de_la_primera_escena()
    {
        var error = Assert.Single(Errores("Título: La cena\n" + Ejemplos.EscenaAnexoA));

        Assert.Equal((1, "E02"), (error.Linea, error.Codigo));
    }

    [Fact]
    public void E03_encabezado_mal_formado()
    {
        var error = Assert.Single(Errores(Reemplazar("ESCENA 1 — INT. RESTAURANTE — NOCHE", "ESCENA 1 INT. RESTAURANTE")));

        Assert.Equal((1, "E03"), (error.Linea, error.Codigo));
        Assert.Equal("El encabezado de escena no tiene el formato \"ESCENA N — INT. LUGAR — MOMENTO DEL DÍA\".", error.Motivo);
    }

    [Theory]
    [Trait("AC", "AC-03n")]
    [InlineData("I/E", "ESCENA 1 — I/E RESTAURANTE — NOCHE")]
    [InlineData("INT", "ESCENA 1 — INT RESTAURANTE — NOCHE")]
    public void E04_int_ext_escrito_de_otra_forma(string valor, string encabezado)
    {
        var error = Assert.Single(Errores(Reemplazar("ESCENA 1 — INT. RESTAURANTE — NOCHE", encabezado)));

        Assert.Equal((1, "E04"), (error.Linea, error.Codigo));
        Assert.Equal($"En el encabezado de la escena 1, \"{valor}\" debe escribirse \"INT.\" o \"EXT.\", con punto.", error.Motivo);
    }

    [Fact]
    [Trait("AC", "AC-03i")]
    public void E05_escena_que_saltea_un_numero()
    {
        var texto = Ejemplos.EscenaAnexoA + "\n" + EscenaDos.Replace("ESCENA 2", "ESCENA 3", StringComparison.Ordinal);

        var error = Assert.Single(Errores(texto));

        Assert.Equal((13, "E05"), (error.Linea, error.Codigo));
        Assert.Equal("La escena 3 debería ser la escena 2: los números empiezan en 1 y no se saltean ni repiten.", error.Motivo);
    }

    [Fact]
    public void E05_la_primera_escena_debe_ser_la_1()
    {
        var error = Assert.Single(Errores(Reemplazar("ESCENA 1 —", "ESCENA 2 —")));

        Assert.Equal((1, "E05"), (error.Linea, error.Codigo));
    }

    [Fact]
    [Trait("AC", "AC-03q")]
    public void E06_campo_repetido()
    {
        var texto = Reemplazar("ILUMINACIÓN:", "LOCACIÓN: Otra.\nILUMINACIÓN:");

        var error = Assert.Single(Errores(texto));

        Assert.Equal((3, "E06"), (error.Linea, error.Codigo));
        Assert.Equal("El campo LOCACIÓN: de la escena 1 está repetido; el orden es LOCACIÓN, ILUMINACIÓN, PUESTA EN ESCENA, AUDIO, antes del primer CLIP.", error.Motivo);
    }

    [Fact]
    [Trait("AC", "AC-03r")]
    public void E06_campo_fuera_de_orden_se_informa_en_el_que_se_adelanto()
    {
        var lineas = Ejemplos.EscenaAnexoA.Split('\n');
        (lineas[1], lineas[2]) = (lineas[2], lineas[1]);

        var error = Assert.Single(Errores(string.Join('\n', lineas)));

        Assert.Equal((2, "E06"), (error.Linea, error.Codigo));
        Assert.Contains("ILUMINACIÓN: de la escena 1 está fuera de orden", error.Motivo, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-03t")]
    [Trait("AC", "AC-S01")]
    public void E06_y_E07_campo_despues_del_primer_clip()
    {
        var texto = Quitar(LineaN(Ejemplos.EscenaAnexoA, 5)).Replace("CLIP 1\n", "CLIP 1\nAUDIO: Murmullo.\n", StringComparison.Ordinal);

        var errores = Errores(texto);

        Assert.Equal(2, errores.Count);
        Assert.Equal(new ErrorGuion(1, "E07", "Falta el campo AUDIO: en la escena 1."), errores[0]);
        Assert.Equal((6, "E06"), (errores[1].Linea, errores[1].Codigo));
        Assert.Contains("está después del primer CLIP", errores[1].Motivo, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-03a")]
    [Trait("AC", "AC-S02")]
    public void E07_y_E11_cada_uno_en_el_encabezado_de_su_escena()
    {
        var escenaUno = Quitar(LineaN(Ejemplos.EscenaAnexoA, 5));
        var escenaDosSinClips = EscenaDos[..EscenaDos.IndexOf("CLIP 1", StringComparison.Ordinal)];

        var errores = Errores(escenaUno + "\n" + escenaDosSinClips);

        Assert.Equal(2, errores.Count);
        Assert.Equal(new ErrorGuion(1, "E07", "Falta el campo AUDIO: en la escena 1."), errores[0]);
        Assert.Equal(new ErrorGuion(12, "E11", "La escena 2 no tiene ningún clip; agregá al menos un \"CLIP 1\"."), errores[1]);
    }

    [Fact]
    [Trait("AC", "AC-03s")]
    public void E08_campo_sin_valor()
    {
        var error = Assert.Single(Errores(Reemplazar(LineaN(Ejemplos.EscenaAnexoA, 5), "AUDIO:   ")));

        Assert.Equal(new ErrorGuion(5, "E08", "El campo AUDIO: de la escena 1 no tiene valor."), error);
    }

    [Fact]
    [Trait("AC", "AC-03l")]
    public void E09_linea_que_no_es_un_campo_antes_del_primer_clip()
    {
        var error = Assert.Single(Errores(Reemplazar("CLIP 1\n", "Ana espera.\nCLIP 1\n")));

        Assert.Equal((6, "E09"), (error.Linea, error.Codigo));
    }

    [Fact]
    [Trait("AC", "AC-S04")]
    public void E09_un_campo_en_dos_lineas()
    {
        var error = Assert.Single(Errores(Reemplazar("vela en portavelas de cristal.\n", "vela en portavelas\nde cristal.\n")));

        Assert.Equal((3, "E09"), (error.Linea, error.Codigo));
    }

    [Fact]
    [Trait("AC", "AC-S06")]
    public void Un_marcador_sin_tilde_no_es_marcador()
    {
        var errores = Errores(Reemplazar("LOCACIÓN:", "Locacion:"));

        Assert.Equal([(1, "E07"), (2, "E09")], errores.Select(e => (e.Linea, e.Codigo)));
        Assert.Equal("Falta el campo LOCACIÓN: en la escena 1.", errores[0].Motivo);
    }

    [Fact]
    [Trait("AC", "AC-03g")]
    public void E10_dialogo_sin_comillas()
    {
        var error = Assert.Single(Errores(Reemplazar("ANA: \"Mentiroso.\"", "ANA: Mentiroso.")));

        Assert.Equal(new ErrorGuion(8, "E10", "El diálogo de ANA debe ir entre comillas: ANA: \"texto\"."), error);
    }

    [Fact]
    [Trait("AC", "AC-03j")]
    public void E12_clip_repetido()
    {
        var error = Assert.Single(Errores(Reemplazar("CLIP 2", "CLIP 1")));

        Assert.Equal((11, "E12"), (error.Linea, error.Codigo));
        Assert.Equal("El clip 1 de la escena 1 debería ser el clip 2: los números empiezan en 1 y no se saltean ni repiten.", error.Motivo);
    }

    [Fact]
    [Trait("AC", "AC-03m")]
    public void E13_clip_sin_contenido_al_final_y_en_el_medio()
    {
        var alFinal = Ejemplos.EscenaAnexoA[..Ejemplos.EscenaAnexoA.LastIndexOf('\n')];
        var enElMedio = Reemplazar("CLIP 2\n", "CLIP 2\n\nCLIP 3\n");

        Assert.Equal(new ErrorGuion(11, "E13", "El clip 2 de la escena 1 no tiene contenido."), Assert.Single(Errores(alFinal)));
        var error = Assert.Single(Errores(enElMedio));
        Assert.Equal((11, "E13"), (error.Linea, error.Codigo));
    }

    [Fact]
    [Trait("AC", "AC-03h")]
    public void E14_texto_en_pantalla_sin_comillas()
    {
        var error = Assert.Single(Errores(Ejemplos.EscenaAnexoA + "\nTEXTO EN PANTALLA: Hola"));

        Assert.Equal(new ErrorGuion(13, "E14", "El texto en pantalla debe ir entre comillas: TEXTO EN PANTALLA: \"texto\"."), error);
    }

    [Fact]
    [Trait("AC", "AC-03e")]
    public void E15_dos_textos_en_pantalla_en_un_clip()
    {
        var texto = Ejemplos.EscenaAnexoA + "\nTEXTO EN PANTALLA: \"Uno\"\nTEXTO EN PANTALLA: \"Dos\"";

        var error = Assert.Single(Errores(texto));

        Assert.Equal(new ErrorGuion(14, "E15", "El clip 2 de la escena 1 tiene más de un TEXTO EN PANTALLA; se permite uno solo."), error);
    }

    [Fact]
    [Trait("AC", "AC-03c")]
    public void E16_mas_de_diez_escenas()
    {
        var texto = Ejemplos.Guion(11, 1);
        var lineaEscena11 = texto.Split('\n').ToList().FindIndex(l => l.StartsWith("ESCENA 11 ", StringComparison.Ordinal)) + 1;

        var error = Assert.Single(Errores(texto));

        Assert.Equal(new ErrorGuion(lineaEscena11, "E16", "El episodio supera el máximo de 10 escenas."), error);
    }

    [Fact]
    [Trait("AC", "AC-03d")]
    public void E17_mas_de_veinte_clips()
    {
        var texto = Ejemplos.Guion(1, 21);
        var lineaClip21 = texto.Split('\n').ToList().FindIndex(l => l == "CLIP 21") + 1;

        var error = Assert.Single(Errores(texto));

        Assert.Equal(new ErrorGuion(lineaClip21, "E17", "La escena 1 supera el máximo de 20 clips."), error);
    }

    [Fact]
    public void Los_limites_exactos_son_validos()
    {
        Assert.IsType<ResultadoGuion.Valido>(ParserGuion.Parsear(Ejemplos.Guion(10, 20)));
    }

    [Fact]
    public void Los_errores_se_ordenan_por_linea_y_codigo()
    {
        var texto = Reemplazar("ANA: \"Mentiroso.\"", "ANA: Mentiroso.").Replace(LineaN(Ejemplos.EscenaAnexoA, 3), "ILUMINACIÓN:", StringComparison.Ordinal);

        var errores = Errores(texto);

        Assert.Equal([(3, "E08"), (8, "E10")], errores.Select(e => (e.Linea, e.Codigo)));
    }

    [Fact]
    public void Un_dialogo_antes_del_primer_clip_es_E09()
    {
        var error = Assert.Single(Errores(Reemplazar("CLIP 1\n", "ANA: \"Hola.\"\nCLIP 1\n")));

        Assert.Equal((6, "E09"), (error.Linea, error.Codigo));
    }
}
