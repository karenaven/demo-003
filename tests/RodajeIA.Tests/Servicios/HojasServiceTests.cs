using Microsoft.EntityFrameworkCore;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Prompt;
using RodajeIA.Web.Servicios;

namespace RodajeIA.Tests.Servicios;

public sealed class HojasServiceTests : IDisposable
{
    private readonly BaseDatosEnMemoria _base = new();
    private readonly HojasService _servicio;
    private readonly int _serieId;

    public HojasServiceTests()
    {
        _servicio = new HojasService(_base);
        using var db = _base.CreateDbContext();
        var serie = new Serie { Nombre = "Demo" };
        db.Series.Add(serie);
        db.SaveChanges();
        _serieId = serie.Id;
    }

    public void Dispose() => _base.Dispose();

    private static DatosHoja Datos(string nombre = "Ana", string peinado = "trenza larga", string? heridas = null) =>
        new(nombre, "32 años", "Morena, ojos oscuros; voz cálida", peinado, "chaqueta negra", heridas, "Irónica", "Protagonista");

    [Fact]
    [Trait("AC", "AC-01e")]
    public async Task Crear_una_hoja_con_todos_los_campos_la_guarda_en_la_serie()
    {
        var resultado = await _servicio.CrearAsync(_serieId, Datos(heridas: "cicatriz"));

        Assert.True(resultado.Ok);
        var hoja = Assert.Single(await _servicio.ListarAsync(_serieId));
        Assert.Equal(("Ana", "ANA", "trenza larga", "cicatriz"), (hoja.Nombre, hoja.NombreClave, hoja.Peinado, hoja.HeridasMarcas));
    }

    [Fact]
    [Trait("AC", "AC-01f")]
    public async Task Listar_muestra_las_hojas_de_la_serie()
    {
        await _servicio.CrearAsync(_serieId, Datos("Ana"));
        await _servicio.CrearAsync(_serieId, Datos("José Daniel"));

        Assert.Equal(["Ana", "José Daniel"], (await _servicio.ListarAsync(_serieId)).Select(h => h.Nombre));
    }

    [Fact]
    [Trait("AC", "AC-01g")]
    public async Task Editar_el_vestuario_lo_guarda()
    {
        var id = (await _servicio.CrearAsync(_serieId, Datos())).Id!.Value;

        var resultado = await _servicio.EditarAsync(id, Datos() with { Vestuario = "camisa blanca" });

        Assert.True(resultado.Ok);
        Assert.Equal("camisa blanca", Assert.Single(await _servicio.ListarAsync(_serieId)).Vestuario);
    }

    [Fact]
    [Trait("AC", "AC-01i")]
    public async Task Crear_con_el_peinado_vacio_no_guarda_y_lo_indica()
    {
        var resultado = await _servicio.CrearAsync(_serieId, Datos(peinado: " "));

        Assert.Equal("Falta el peinado.", resultado.Motivo);
        Assert.Empty(await _servicio.ListarAsync(_serieId));
    }

    [Fact]
    [Trait("AC", "AC-01j")]
    public async Task Crear_sin_heridas_marcas_es_valido()
    {
        Assert.True((await _servicio.CrearAsync(_serieId, Datos(heridas: null))).Ok);
    }

    [Fact]
    [Trait("AC", "AC-01k")]
    public async Task Editar_dejando_el_peinado_vacio_no_guarda_y_conserva_el_anterior()
    {
        var id = (await _servicio.CrearAsync(_serieId, Datos())).Id!.Value;

        var resultado = await _servicio.EditarAsync(id, Datos(peinado: ""));

        Assert.Equal("Falta el peinado.", resultado.Motivo);
        Assert.Equal("trenza larga", Assert.Single(await _servicio.ListarAsync(_serieId)).Peinado);
    }

    [Fact]
    [Trait("AC", "AC-S08")]
    public async Task Un_nombre_repetido_sin_distinguir_mayusculas_se_rechaza_solo_en_la_misma_serie()
    {
        await _servicio.CrearAsync(_serieId, Datos("Ana"));
        var otraHoja = (await _servicio.CrearAsync(_serieId, Datos("Bea"))).Id!.Value;
        int otraSerie;
        await using (var db = _base.CreateDbContext())
        {
            var serie = new Serie { Nombre = "Otra" };
            db.Series.Add(serie);
            await db.SaveChangesAsync();
            otraSerie = serie.Id;
        }

        Assert.Equal("Ya existe un personaje llamado ANA en esta serie.", (await _servicio.CrearAsync(_serieId, Datos("ANA"))).Motivo);
        Assert.Equal("Ya existe un personaje llamado ana en esta serie.", (await _servicio.EditarAsync(otraHoja, Datos("ana"))).Motivo);
        Assert.True((await _servicio.CrearAsync(otraSerie, Datos("Ana"))).Ok);
        Assert.True((await _servicio.CrearAsync(_serieId, Datos("Aná"))).Ok);
    }

    [Theory]
    [Trait("AC", "AC-S09")]
    [InlineData("Audio")]
    [InlineData("texto en pantalla")]
    [InlineData("Escena")]
    [InlineData("Locación")]
    public async Task Un_nombre_reservado_del_guion_se_rechaza(string nombre)
    {
        var resultado = await _servicio.CrearAsync(_serieId, Datos(nombre));

        Assert.Equal($"«{nombre}» es un nombre reservado del guion; elegí otro nombre.", resultado.Motivo);
    }

    [Fact]
    [Trait("AC", "AC-01h")]
    public async Task Eliminar_una_hoja_borra_sus_variantes_aunque_la_escena_este_generada()
    {
        var (_, ana, _, episodio) = _base.CrearSerieDeEjemplo("Con escenas");
        var ids = await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.EscenaAnexoA);
        await using (var db = _base.CreateDbContext())
        {
            db.Variantes.Add(new VarianteEscena { EscenaId = ids[0], HojaId = ana.Id, Vestuario = "abrigo gris" });
            await db.SaveChangesAsync();
        }

        await EscenaDePrueba.MarcarGeneradaAsync(_base, ids[0]);

        var resultado = await _servicio.EliminarAsync(ana.Id);

        Assert.True(resultado.Ok);
        await using var lectura = _base.CreateDbContext();
        Assert.Empty(await lectura.Variantes.ToListAsync());
        Assert.False(await lectura.Hojas.AnyAsync(h => h.Id == ana.Id));
        Assert.False(await lectura.PersonajesEnEscena.AnyAsync(p => p.HojaId == ana.Id));
    }

    [Fact]
    [Trait("AC", "AC-S38")]
    public async Task Tras_eliminar_la_hoja_el_prompt_la_omite_y_el_dialogo_sigue_literal()
    {
        var (_, ana, _, episodio) = _base.CrearSerieDeEjemplo("Con escenas");
        var ids = await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.EscenaAnexoA);
        await EscenaDePrueba.MarcarGeneradaAsync(_base, ids[0]);
        var config = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto);

        await _servicio.EliminarAsync(ana.Id);
        var prompt = await new ArmadorPrompt(_base, config.Molde, config.Estilo).ArmarAsync(ids[0]);

        Assert.NotNull(prompt);
        Assert.DoesNotContain("Ana, 32 años.", prompt, StringComparison.Ordinal);
        Assert.Contains("José Daniel, 35 años.", prompt, StringComparison.Ordinal);
        Assert.Contains("ANA: \"Mentiroso.\"", prompt, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-S38")]
    public async Task Renombrar_una_hoja_usada_en_un_guion_cargado_avisa()
    {
        var (_, ana, _, episodio) = _base.CrearSerieDeEjemplo("Con escenas");
        await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.EscenaAnexoA);
        var sinUso = (await _servicio.CrearAsync(ana.SerieId, Datos("Bea"))).Id!.Value;

        var renombrada = await _servicio.EditarAsync(ana.Id, Datos("Anita"));
        var sinAviso = await _servicio.EditarAsync(sinUso, Datos("Beatriz"));
        var mismoNombre = await _servicio.EditarAsync(ana.Id, Datos("Anita") with { Vestuario = "otro" });

        Assert.Equal(HojasService.AvisoRenombrado, renombrada.Aviso);
        Assert.Null(sinAviso.Aviso);
        Assert.Null(mismoNombre.Aviso);
    }
}
