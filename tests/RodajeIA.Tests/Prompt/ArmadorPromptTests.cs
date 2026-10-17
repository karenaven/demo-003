using Microsoft.EntityFrameworkCore;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Prompt;

namespace RodajeIA.Tests.Prompt;

public sealed class ArmadorPromptTests : IDisposable
{
    private static readonly ConfiguracionRodaje _config = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto);
    private readonly BaseDatosEnMemoria _base = new();
    private readonly ArmadorPrompt _armador;
    private readonly int _episodioId;
    private readonly HojaPersonaje _ana;

    public ArmadorPromptTests()
    {
        _armador = new ArmadorPrompt(_base, _config.Molde, _config.Estilo);
        var (_, ana, _, episodio) = _base.CrearSerieDeEjemplo();
        (_ana, _episodioId) = (ana, episodio.Id);
    }

    public void Dispose() => _base.Dispose();

    private async Task<int> GenerarAsync(string guion, Action<Clip, Toma>? ajustar = null, int indice = 0)
    {
        var ids = await EscenaDePrueba.CargarAsync(_base, _episodioId, guion);
        foreach (var id in ids)
        {
            await EscenaDePrueba.MarcarGeneradaAsync(_base, id, ajustar);
        }

        return ids[indice];
    }

    private async Task<string> PromptAsync(int escenaId) =>
        await _armador.ArmarAsync(escenaId) ?? throw new InvalidOperationException("La escena no está generada.");

    private static string Linea(string prompt, string inicio) =>
        prompt.Split('\n').Single(l => l.StartsWith(inicio, StringComparison.Ordinal));

    [Fact]
    [Trait("AC", "AC-08a")]
    public async Task Tiene_las_seis_secciones_en_orden()
    {
        var prompt = await PromptAsync(await GenerarAsync(Ejemplos.EscenaAnexoA));

        var posiciones = new[] { _config.Estilo.Texto[..20], "Locación:", "Iluminación base:", "Personajes:", "Puesta en escena:", "Audio:", "Bloque 1:", "Bloque 2:" }
            .Select(t => prompt.IndexOf(t, StringComparison.Ordinal))
            .ToList();

        Assert.DoesNotContain(-1, posiciones);
        Assert.Equal(posiciones.Order(), posiciones);
        Assert.StartsWith(_config.Estilo.Texto, prompt, StringComparison.Ordinal);
        Assert.Contains("Ana, 32 años.", prompt, StringComparison.Ordinal);
        Assert.Contains("José Daniel, 35 años.", prompt, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-08b")]
    public async Task Contiene_un_bloque_numerado_por_clip_en_orden()
    {
        var prompt = await PromptAsync(await GenerarAsync(Ejemplos.Guion(1, 3)));

        var b1 = prompt.IndexOf("Bloque 1:", StringComparison.Ordinal);
        var b2 = prompt.IndexOf("Bloque 2:", StringComparison.Ordinal);
        var b3 = prompt.IndexOf("Bloque 3:", StringComparison.Ordinal);
        Assert.True(b1 >= 0 && b1 < b2 && b2 < b3);
        Assert.DoesNotContain("Bloque 4:", prompt, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-08c")]
    public async Task Sin_heridas_la_hoja_no_muestra_la_etiqueta()
    {
        var prompt = await PromptAsync(await GenerarAsync(Ejemplos.EscenaAnexoA));

        Assert.DoesNotContain("Heridas/marcas", Linea(prompt, "Ana,"), StringComparison.Ordinal);
        Assert.Contains("Heridas/marcas: cicatriz pequeña en el mentón.", Linea(prompt, "José Daniel,"), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-08e")]
    [Trait("AC", "SC-001")]
    public async Task La_hoja_de_un_personaje_sin_variantes_es_identica_en_dos_escenas()
    {
        var ids = await EscenaDePrueba.CargarAsync(_base, _episodioId, Ejemplos.Guion(2, 1));
        foreach (var id in ids)
        {
            await EscenaDePrueba.MarcarGeneradaAsync(_base, id);
        }

        var hojaEscena1 = Linea(await PromptAsync(ids[0]), "Ana,");
        var hojaEscena2 = Linea(await PromptAsync(ids[1]), "Ana,");

        Assert.Equal(hojaEscena1, hojaEscena2);
        Assert.Equal(_ana.Nombre + ", " + _ana.Edad + ".", hojaEscena1[..(_ana.Nombre.Length + _ana.Edad.Length + 3)]);
    }

    [Fact]
    [Trait("AC", "AC-08f")]
    [Trait("AC", "SC-003")]
    public async Task Los_dialogos_van_literales()
    {
        var prompt = await PromptAsync(await GenerarAsync(Ejemplos.EscenaAnexoA));

        Assert.Contains("ANA: \"Mentiroso.\"", prompt, StringComparison.Ordinal);
        Assert.Contains("JOSÉ DANIEL (sonriendo): \"Bueno... pasó parecido.\"", prompt, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-08g")]
    public async Task El_texto_en_pantalla_va_literal_y_sin_el_cierre_por_defecto()
    {
        var guion = Ejemplos.EscenaAnexoA + "\nTEXTO EN PANTALLA: \"Hermano, ¿puedes salir conmigo?\"";

        var bloques = (await PromptAsync(await GenerarAsync(guion))).Split("Bloque ");

        Assert.EndsWith("Único texto visible en pantalla: \"Hermano, ¿puedes salir conmigo?\"", bloques[2], StringComparison.Ordinal);
        Assert.DoesNotContain("Sin subtítulos", bloques[2], StringComparison.Ordinal);
        Assert.Contains("Sin subtítulos ni texto en pantalla.", bloques[1], StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-08h")]
    public async Task Sin_personajes_con_hoja_se_omite_la_seccion()
    {
        var guion = """
            ESCENA 1 — EXT. CALLE — DÍA
            LOCACIÓN: Calle.
            ILUMINACIÓN: Sol.
            PUESTA EN ESCENA: Nadie a la vista.
            AUDIO: Viento.
            CLIP 1
            Un perro cruza.
            """;

        var prompt = await PromptAsync(await GenerarAsync(guion));

        Assert.DoesNotContain("Personajes:", prompt, StringComparison.Ordinal);
        Assert.Contains("Puesta en escena:\nNadie a la vista.", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("\n\n\n", prompt, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-S07")]
    public async Task Conserva_las_comillas_tipograficas_e_internas()
    {
        var guion = Ejemplos.EscenaAnexoA.Replace("ANA: \"Mentiroso.\"", "ANA: “Dijo \"no\" y se fue.”", StringComparison.Ordinal);

        Assert.Contains("ANA: “Dijo \"no\" y se fue.”", await PromptAsync(await GenerarAsync(guion)), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-S31")]
    public async Task Un_cambio_en_el_molde_cambia_el_prompt_y_conserva_las_cadenas_de_contrato()
    {
        var directorio = Path.Combine(Path.GetTempPath(), "rodajeia-molde-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directorio);
        try
        {
            foreach (var archivo in Directory.GetFiles(CargadorConfiguracion.DirectorioPorDefecto))
            {
                File.Copy(archivo, Path.Combine(directorio, Path.GetFileName(archivo)));
            }

            var molde = Path.Combine(directorio, CargadorConfiguracion.ArchivoMolde);
            File.WriteAllText(molde, File.ReadAllText(molde).Replace("\"Personajes:\"", "\"Reparto:\"", StringComparison.Ordinal));
            var otra = CargadorConfiguracion.Cargar(directorio);
            var escenaId = await GenerarAsync(Ejemplos.EscenaAnexoA);

            var prompt = await new ArmadorPrompt(_base, otra.Molde, otra.Estilo).ArmarAsync(escenaId);

            Assert.NotNull(prompt);
            Assert.Contains("Reparto:\nAna,", prompt, StringComparison.Ordinal);
            Assert.DoesNotContain("Personajes:", prompt, StringComparison.Ordinal);
            Assert.Contains("Bloque 1:", prompt, StringComparison.Ordinal);
            Assert.Contains("Sin subtítulos ni texto en pantalla.", prompt, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directorio, recursive: true);
        }
    }

    [Fact]
    [Trait("AC", "AC-S32")]
    public async Task Las_hojas_van_en_orden_de_primera_aparicion()
    {
        var guion = Ejemplos.EscenaAnexoA.Replace(
            "PUESTA EN ESCENA: Sentados frente a frente en una mesa para dos. Ana siempre a la IZQUIERDA del encuadre, José Daniel siempre a la DERECHA.",
            "PUESTA EN ESCENA: José Daniel espera sentado.",
            StringComparison.Ordinal);

        var prompt = await PromptAsync(await GenerarAsync(guion));

        Assert.True(prompt.IndexOf("José Daniel, 35 años.", StringComparison.Ordinal) < prompt.IndexOf("Ana, 32 años.", StringComparison.Ordinal));
    }

    [Theory]
    [Trait("AC", "AC-S33")]
    [InlineData(EstadoEscena.Pendiente)]
    [InlineData(EstadoEscena.Generando)]
    [InlineData(EstadoEscena.Error)]
    public async Task No_hay_prompt_si_la_escena_no_esta_generada(EstadoEscena estado)
    {
        var escenaId = (await EscenaDePrueba.CargarAsync(_base, _episodioId, Ejemplos.EscenaAnexoA))[0];
        await using (var db = _base.CreateDbContext())
        {
            await db.Escenas.Where(e => e.Id == escenaId).ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, estado));
        }

        Assert.Null(await _armador.ArmarAsync(escenaId));
    }

    [Fact]
    [Trait("AC", "AC-S34")]
    public async Task No_duplica_el_punto_de_un_valor_que_ya_termina_en_punto()
    {
        await using (var db = _base.CreateDbContext())
        {
            var hoja = await db.Hojas.SingleAsync(h => h.Id == _ana.Id);
            hoja.Peinado = "Cola de caballo.";
            await db.SaveChangesAsync();
        }

        var prompt = await PromptAsync(await GenerarAsync(Ejemplos.EscenaAnexoA));

        Assert.Contains("Peinado: Cola de caballo. Vestuario:", prompt, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-S35")]
    public async Task Los_dialogos_de_una_toma_van_en_el_orden_del_guion()
    {
        var escenaId = await GenerarAsync(Ejemplos.EscenaAnexoA, (clip, toma) => toma.Dialogos.Reverse());

        var prompt = await PromptAsync(escenaId);

        Assert.True(prompt.IndexOf("ANA: \"Mentiroso.\"", StringComparison.Ordinal) < prompt.IndexOf("JOSÉ DANIEL (sonriendo)", StringComparison.Ordinal));
    }

    [Fact]
    public async Task La_toma_sin_angulo_ni_movimiento_coincide_con_el_ejemplo_del_contrato()
    {
        var escenaId = await GenerarAsync(Ejemplos.EscenaAnexoA, (clip, toma) =>
        {
            if (clip.Numero == 1)
            {
                toma.Accion = "Ana y José Daniel conversan; él sonríe pícaramente.";
            }
        });

        var prompt = await PromptAsync(escenaId);

        Assert.Contains(
            "Bloque 1:\nplano medio, óptica 50mm. Ana y José Daniel conversan; él sonríe pícaramente. ANA: \"Mentiroso.\" JOSÉ DANIEL (sonriendo): \"Bueno... pasó parecido.\" Iluminación: continuidad con la luz base. Sin subtítulos ni texto en pantalla.",
            prompt,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Varias_tomas_se_unen_con_corte_a()
    {
        var escenaId = (await EscenaDePrueba.CargarAsync(_base, _episodioId, Ejemplos.EscenaAnexoA))[0];
        await EscenaDePrueba.MarcarGeneradaAsync(_base, escenaId);
        await using (var db = _base.CreateDbContext())
        {
            var bloque = await db.Bloques.Include(b => b.Clip).SingleAsync(b => b.Clip!.EscenaId == escenaId && b.Clip.Numero == 2);
            db.Tomas.Add(new Toma { BloqueId = bloque.Id, Orden = 2, Plano = "primer plano", Optica = "85mm", Iluminacion = "luz suave de relleno", Angulo = "picado", Movimiento = "paneo", Accion = "Ana sonríe." });
            await db.SaveChangesAsync();
        }

        var prompt = await PromptAsync(escenaId);

        Assert.Contains("Acción del clip 2. Iluminación: continuidad con la luz base. Corte a primer plano, paneo, picado, óptica 85mm. Ana sonríe. Iluminación: luz suave de relleno. Sin subtítulos ni texto en pantalla.", prompt, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-S38")]
    public async Task Editar_la_hoja_base_cambia_el_prompt_de_una_escena_ya_generada()
    {
        var escenaId = await GenerarAsync(Ejemplos.EscenaAnexoA);
        Assert.Contains("Vestuario: chaqueta negra.", await PromptAsync(escenaId), StringComparison.Ordinal);

        await using (var db = _base.CreateDbContext())
        {
            var hoja = await db.Hojas.SingleAsync(h => h.Id == _ana.Id);
            hoja.Vestuario = "vestido verde";
            await db.SaveChangesAsync();
        }

        var prompt = await PromptAsync(escenaId);
        Assert.Contains("Vestuario: vestido verde.", prompt, StringComparison.Ordinal);
        Assert.DoesNotContain("chaqueta negra", prompt, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-09a")]
    public async Task La_variante_reemplaza_solo_en_su_escena()
    {
        var ids = await EscenaDePrueba.CargarAsync(_base, _episodioId, Ejemplos.Guion(2, 1));
        await using (var db = _base.CreateDbContext())
        {
            db.Variantes.Add(new VarianteEscena { EscenaId = ids[1], HojaId = _ana.Id, Edad = "40 años", Peinado = "rapado", Vestuario = "abrigo gris", ReemplazaHeridas = true, HeridasMarcas = "corte en la ceja" });
            await db.SaveChangesAsync();
        }

        foreach (var id in ids)
        {
            await EscenaDePrueba.MarcarGeneradaAsync(_base, id);
        }

        var escena1 = Linea(await PromptAsync(ids[0]), "Ana,");
        var escena2 = Linea(await PromptAsync(ids[1]), "Ana,");

        Assert.StartsWith("Ana, 32 años.", escena1, StringComparison.Ordinal);
        Assert.Contains("Vestuario: chaqueta negra.", escena1, StringComparison.Ordinal);
        Assert.StartsWith("Ana, 40 años.", escena2, StringComparison.Ordinal);
        Assert.Contains("Peinado: rapado. Vestuario: abrigo gris. Heridas/marcas: corte en la ceja.", escena2, StringComparison.Ordinal);
        Assert.Contains(_ana.DescripcionFisica, escena2, StringComparison.Ordinal);
    }
}
