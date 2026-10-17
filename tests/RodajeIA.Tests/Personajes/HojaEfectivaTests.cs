using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Personajes;

namespace RodajeIA.Tests.Personajes;

public class HojaEfectivaTests
{
    private static readonly MoldePrompt _molde = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto).Molde;

    [Fact]
    public void Sin_variante_es_la_hoja_base()
    {
        var hoja = Ejemplos.HojaAna();

        var efectiva = HojaEfectiva.Calcular(hoja, null);

        Assert.Equal(hoja.Vestuario, efectiva.Vestuario);
        Assert.Equal(hoja.Edad, efectiva.Edad);
        Assert.Null(efectiva.HeridasMarcas);
    }

    [Fact]
    [Trait("AC", "AC-09a")]
    public void Una_variante_reemplaza_solo_sus_campos()
    {
        var hoja = Ejemplos.HojaAna();
        var variante = new VarianteEscena
        {
            Edad = "40 años",
            Peinado = "rapado",
            Vestuario = "abrigo gris",
            ReemplazaHeridas = true,
            HeridasMarcas = "corte en la ceja",
        };

        var efectiva = HojaEfectiva.Calcular(hoja, variante);

        Assert.Equal("40 años", efectiva.Edad);
        Assert.Equal("rapado", efectiva.Peinado);
        Assert.Equal("abrigo gris", efectiva.Vestuario);
        Assert.Equal("corte en la ceja", efectiva.HeridasMarcas);
        Assert.Equal(hoja.DescripcionFisica, efectiva.DescripcionFisica);
        Assert.Equal(hoja.Personalidad, efectiva.Personalidad);
    }

    [Fact]
    public void Reemplazar_heridas_con_vacio_da_una_hoja_sin_heridas()
    {
        var hoja = Ejemplos.HojaJoseDaniel();
        Assert.NotNull(hoja.HeridasMarcas);

        var efectiva = HojaEfectiva.Calcular(hoja, new VarianteEscena { ReemplazaHeridas = true, HeridasMarcas = "" });

        Assert.Null(efectiva.HeridasMarcas);
        Assert.DoesNotContain("Heridas/marcas", efectiva.Renderizar(_molde));
    }

    [Fact]
    [Trait("AC", "AC-08c")]
    public void Renderizar_usa_la_plantilla_por_personaje_y_omite_heridas_vacias()
    {
        var texto = HojaEfectiva.Calcular(Ejemplos.HojaAna(), null).Renderizar(_molde);

        Assert.StartsWith("Ana, 32 años.", texto, StringComparison.Ordinal);
        Assert.Contains("Vestuario: chaqueta negra.", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("Heridas/marcas", texto, StringComparison.Ordinal);
    }
}
