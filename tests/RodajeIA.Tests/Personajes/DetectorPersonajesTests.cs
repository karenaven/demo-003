using System.Text;
using RodajeIA.Tests.Fakes;
using RodajeIA.Tests.Guion;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Personajes;

namespace RodajeIA.Tests.Personajes;

public class DetectorPersonajesTests
{
    private static readonly HojaPersonaje _ana = ConId(Ejemplos.HojaAna(), 1);
    private static readonly HojaPersonaje _jose = ConId(Ejemplos.HojaJoseDaniel(), 2);

    private static HojaPersonaje ConId(HojaPersonaje hoja, int id)
    {
        hoja.Id = id;
        return hoja;
    }

    private const string Encabezado = """
        ESCENA 1 — INT. BAR — NOCHE
        LOCACIÓN: Bar.
        ILUMINACIÓN: Tenue.
        PUESTA EN ESCENA: {0}
        AUDIO: Música baja.
        CLIP 1
        """;

    private static DeteccionEscena Detectar(string puesta, string clip, params HojaPersonaje[] hojas)
    {
        var texto = string.Format(System.Globalization.CultureInfo.InvariantCulture, Encabezado, puesta) + "\n" + clip;
        var escena = ParserGuionValidoTests.Valido(texto).Escenas[0];
        return DetectorPersonajes.Detectar(escena, hojas.Length > 0 ? hojas : [_ana, _jose]);
    }

    private static IReadOnlySet<int> EnClip1(DeteccionEscena d) => d.PorClip[1];

    [Fact]
    [Trait("AC", "AC-05a")]
    public void Detecta_por_nombre_en_la_accion()
    {
        var d = Detectar("Una mesa.", "Ana mira la puerta.");

        Assert.Equal([1], EnClip1(d));
        Assert.Equal([1], d.PresentesEnOrden);
    }

    [Fact]
    [Trait("AC", "AC-05b")]
    public void Detecta_por_marcador_de_dialogo()
    {
        var d = Detectar("Una mesa.", "Alguien entra.\nANA: \"Claro.\"");

        Assert.Equal([1], EnClip1(d));
    }

    [Fact]
    [Trait("AC", "AC-05c")]
    public void No_detecta_a_un_personaje_sin_hoja_mencionado_en_la_accion()
    {
        var d = Detectar("Una mesa.", "Mesero sirve vino.");

        Assert.Empty(EnClip1(d));
        Assert.Empty(d.PresentesEnOrden);
    }

    [Fact]
    [Trait("AC", "AC-05d")]
    public void No_detecta_a_un_personaje_sin_hoja_con_dialogo()
    {
        var d = Detectar("Una mesa.", "Alguien entra.\nMESERO: \"¿Algo más?\"");

        Assert.Empty(EnClip1(d));
    }

    [Fact]
    [Trait("AC", "AC-05e")]
    public void Un_nombre_como_prefijo_de_otra_palabra_no_cuenta()
    {
        var d = Detectar("Una mesa.", "Anabel entra.");

        Assert.Empty(EnClip1(d));
    }

    [Fact]
    [Trait("AC", "AC-05f")]
    public void Un_nombre_sin_tilde_no_cuenta()
    {
        var d = Detectar("Una mesa.", "Jose Daniel entra.");

        Assert.Empty(EnClip1(d));
    }

    [Fact]
    [Trait("AC", "AC-05g")]
    public void Mencionado_solo_en_la_puesta_en_escena_esta_en_la_escena_pero_en_ningun_clip()
    {
        var d = Detectar("Ana espera en la barra.", "Alguien entra.");

        Assert.Empty(EnClip1(d));
        Assert.Equal([1], d.PresentesEnOrden);
    }

    [Fact]
    [Trait("AC", "AC-S10")]
    public void Gana_el_nombre_mas_largo_en_la_accion_y_en_la_puesta()
    {
        var jose = ConId(Ejemplos.Hoja("José"), 3);

        var soloLargo = Detectar("José Daniel en la barra.", "José Daniel sonríe.", jose, _jose);
        var ambos = Detectar("Una mesa.", "José Daniel sonríe. José entra.", jose, _jose);

        Assert.Equal([2], EnClip1(soloLargo));
        Assert.Equal([2], soloLargo.PresentesEnOrden);
        Assert.Equal([2, 3], EnClip1(ambos).Order());
    }

    [Theory]
    [Trait("AC", "AC-S11")]
    [InlineData("¡Ana! grita alguien.", true)]
    [InlineData("Llega Ana, cansada.", true)]
    [InlineData("(Ana) mira.", true)]
    [InlineData("Ana-María entra.", false)]
    [InlineData("Ana's bar.", false)]
    [InlineData("Ana’s bar.", false)]
    [InlineData("Ana2 entra.", false)]
    public void Limites_de_palabra(string accion, bool detecta)
    {
        var d = Detectar("Una mesa.", accion);

        Assert.Equal(detecta, EnClip1(d).Contains(1));
    }

    [Fact]
    [Trait("AC", "AC-S12")]
    public void Un_marcador_sin_tilde_o_un_nombre_en_mayusculas_en_la_accion_no_detectan()
    {
        var d = Detectar("Una mesa.", "JOSÉ DANIEL entra.\nJOSE DANIEL: \"Hola.\"");

        Assert.Empty(EnClip1(d));
    }

    [Fact]
    [Trait("AC", "AC-S12")]
    public void El_marcador_con_acotacion_detecta_al_personaje()
    {
        var d = Detectar("Una mesa.", "Alguien entra.\nJOSÉ DANIEL (serio): \"Hola.\"");

        Assert.Equal([2], EnClip1(d));
    }

    [Fact]
    public void Detecta_un_nombre_escrito_en_nfd()
    {
        var accionNfd = "José Daniel sonríe.".Normalize(NormalizationForm.FormD);
        Assert.NotEqual("José Daniel sonríe.", accionNfd);

        var d = Detectar("Una mesa.", accionNfd);

        Assert.Equal([2], EnClip1(d));
    }

    [Fact]
    [Trait("AC", "AC-S32")]
    public void El_orden_es_de_primera_aparicion_empezando_por_la_puesta_en_escena()
    {
        var d = Detectar("José Daniel espera.", "Ana entra y saluda a José Daniel.");

        Assert.Equal([2, 1], d.PresentesEnOrden);
    }

    [Fact]
    [Trait("AC", "AC-S32")]
    public void El_orden_sigue_las_lineas_del_clip_incluidos_los_dialogos()
    {
        var d = Detectar("Una mesa.", "Alguien entra.\nJOSÉ DANIEL: \"Hola.\"\nAna sonríe.");

        Assert.Equal([2, 1], d.PresentesEnOrden);
    }
}
