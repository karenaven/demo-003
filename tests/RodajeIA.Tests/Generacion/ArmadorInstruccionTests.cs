using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Generacion;

public sealed class ArmadorInstruccionTests : IDisposable
{
    private static readonly ConfiguracionRodaje _config = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto);
    private readonly BaseDatosEnMemoria _base = new();
    private readonly ArmadorInstruccion _armador = new(_config.Instruccion, _config.Molde);

    public void Dispose() => _base.Dispose();

    [Fact]
    [Trait("AC", "AC-S16")]
    public async Task Arma_la_escena_en_el_formato_del_contrato_con_ids_de_dialogo()
    {
        var (_, _, _, episodio) = _base.CrearSerieDeEjemplo();
        var ids = await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.EscenaAnexoA);
        var escena = await EscenaDePrueba.LeerAsync(_base, ids[0]);

        var texto = ArmadorInstruccion.ArmarEscena(escena);

        Assert.Equal("""
            ESCENA 1 — INT. RESTAURANTE — NOCHE
            Locación: Restaurante pequeño y acogedor; pared de ladrillo visto, mesas de madera oscura, vela en portavelas de cristal.
            Iluminación base: Luz cálida ambiental (aprox. 2700 K), principal suave cenital de la lámpara sobre la mesa, reflejo de la vela en los rostros.
            Puesta en escena: Sentados frente a frente en una mesa para dos. Ana siempre a la IZQUIERDA del encuadre, José Daniel siempre a la DERECHA.
            Audio: Ambiente tranquilo de restaurante, murmullo lejano y cubiertos; SIN música. Diálogo en español, acento latinoamericano neutro.

            CLIP 1
            Acción:
            Ana y José Daniel conversan en una mesa junto a la ventana. José Daniel sonríe pícaramente.
            Ana niega con la cabeza, divertida, con una sonrisa amplia.
            Diálogos:
            [c1-d1] ANA: "Mentiroso."
            [c1-d2] JOSÉ DANIEL (sonriendo): "Bueno... pasó parecido."
            Texto en pantalla: (ninguno)

            CLIP 2
            Acción:
            Un mesero deja dos platos sobre la mesa. Ana y José Daniel agradecen con gestos breves.
            Diálogos: (ninguno)
            Texto en pantalla: (ninguno)
            """, texto);
    }

    [Fact]
    [Trait("AC", "AC-S16")]
    public async Task La_instruccion_no_incluye_otras_escenas()
    {
        var (_, _, _, episodio) = _base.CrearSerieDeEjemplo();
        var ids = await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.Guion(2, 1));
        var escena = await EscenaDePrueba.LeerAsync(_base, ids[0]);

        var instruccion = _armador.Armar(escena);

        Assert.Contains("ESCENA 1 —", instruccion, StringComparison.Ordinal);
        Assert.DoesNotContain("ESCENA 2", instruccion, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", instruccion, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sin_accion_ni_personajes_usa_los_textos_de_ausencia()
    {
        var (_, _, _, episodio) = _base.CrearSerieDeEjemplo();
        var guion = """
            ESCENA 1 — EXT. CALLE — DÍA
            LOCACIÓN: Calle.
            ILUMINACIÓN: Sol.
            PUESTA EN ESCENA: Nadie.
            AUDIO: Viento.
            CLIP 1
            MESERO: "Hola."
            """;
        var ids = await EscenaDePrueba.CargarAsync(_base, episodio.Id, guion);
        var escena = await EscenaDePrueba.LeerAsync(_base, ids[0]);

        Assert.Contains("Acción: (ninguna)\nDiálogos:\n[c1-d1] MESERO: \"Hola.\"", ArmadorInstruccion.ArmarEscena(escena), StringComparison.Ordinal);
        Assert.Equal("(ninguno)", _armador.ArmarPersonajes(escena));
    }

    [Fact]
    public async Task Las_hojas_van_renderizadas_con_la_plantilla_del_molde_en_orden_de_aparicion()
    {
        var (_, ana, jose, episodio) = _base.CrearSerieDeEjemplo();
        var ids = await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.EscenaAnexoA);
        var escena = await EscenaDePrueba.LeerAsync(_base, ids[0]);

        var personajes = _armador.ArmarPersonajes(escena).Split('\n');

        Assert.Equal(2, personajes.Length);
        Assert.StartsWith("Ana, 32 años.", personajes[0], StringComparison.Ordinal);
        Assert.StartsWith("José Daniel, 35 años.", personajes[1], StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-09h")]
    public async Task La_instruccion_usa_la_hoja_efectiva_con_la_variante()
    {
        var (_, ana, _, episodio) = _base.CrearSerieDeEjemplo();
        var ids = await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.EscenaAnexoA);
        await using (var db = _base.CreateDbContext())
        {
            db.Variantes.Add(new VarianteEscena { EscenaId = ids[0], HojaId = ana.Id, Vestuario = "abrigo gris" });
            await db.SaveChangesAsync();
        }

        var instruccion = _armador.Armar(await EscenaDePrueba.LeerAsync(_base, ids[0]));

        Assert.Contains("abrigo gris", instruccion, StringComparison.Ordinal);
        Assert.DoesNotContain("chaqueta negra", instruccion, StringComparison.Ordinal);
    }
}
