using Microsoft.EntityFrameworkCore;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Prompt;
using RodajeIA.Web.Servicios;

namespace RodajeIA.Tests.Servicios;

public sealed class VariantesServiceTests : IDisposable
{
    private static readonly ConfiguracionRodaje _config = CargadorConfiguracion.Cargar(CargadorConfiguracion.DirectorioPorDefecto);
    private readonly EntornoGeneracion _entorno = new();
    private readonly VariantesService _servicio;
    private readonly HojaPersonaje _ana;
    private readonly HojaPersonaje _jose;
    private readonly HojaPersonaje _bea;
    private readonly IReadOnlyList<int> _escenas;

    public VariantesServiceTests()
    {
        _servicio = new VariantesService(_entorno.Base);
        var (serie, ana, jose, episodio) = _entorno.Base.CrearSerieDeEjemplo();
        (_ana, _jose) = (ana, jose);
        _bea = _entorno.Base.AgregarHoja(serie.Id, "Bea");
        _escenas = EscenaDePrueba.CargarAsync(_entorno.Base, episodio.Id, Ejemplos.Guion(2, 1)).GetAwaiter().GetResult();
    }

    public void Dispose() => _entorno.Dispose();

    private static DatosVariante Vestuario(string vestuario) => new(null, null, vestuario, false, null);

    private async Task CambiarEstadoAsync(int escenaId, EstadoEscena estado)
    {
        await using var db = _entorno.Base.CreateDbContext();
        await db.Escenas.Where(e => e.Id == escenaId).ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, estado));
    }

    private async Task<string> PromptAsync(int escenaId)
    {
        await EscenaDePrueba.MarcarGeneradaAsync(_entorno.Base, escenaId);
        return (await new ArmadorPrompt(_entorno.Base, _config.Molde, _config.Estilo).ArmarAsync(escenaId))!;
    }

    [Fact]
    [Trait("AC", "AC-09a")]
    public async Task La_variante_creada_se_usa_solo_en_su_escena()
    {
        var resultado = await _servicio.CrearAsync(_escenas[1], _ana.Id, new DatosVariante("40 años", "rapado", "abrigo gris", true, "corte en la ceja"));

        Assert.True(resultado.Ok);
        Assert.Contains("Ana, 40 años.", await PromptAsync(_escenas[1]), StringComparison.Ordinal);
        Assert.Contains("Ana, 32 años.", await PromptAsync(_escenas[0]), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-09b")]
    public async Task Listar_muestra_las_variantes_de_la_escena()
    {
        await _servicio.CrearAsync(_escenas[0], _ana.Id, Vestuario("abrigo gris"));
        await _servicio.CrearAsync(_escenas[0], _jose.Id, Vestuario("traje azul"));

        Assert.Equal(["Ana", "José Daniel"], (await _servicio.ListarAsync(_escenas[0])).Select(v => v.Hoja!.Nombre).Order());
    }

    [Fact]
    [Trait("AC", "AC-09c")]
    public async Task Editar_cambia_la_variante()
    {
        var id = (await _servicio.CrearAsync(_escenas[0], _ana.Id, Vestuario("vestido rojo"))).Id!.Value;

        Assert.True((await _servicio.EditarAsync(id, Vestuario("abrigo gris"))).Ok);

        Assert.Equal("abrigo gris", Assert.Single(await _servicio.ListarAsync(_escenas[0])).Vestuario);
    }

    [Fact]
    [Trait("AC", "AC-09d")]
    [Trait("AC", "AC-09m")]
    public async Task Eliminar_la_variante_deja_intacta_la_hoja_base()
    {
        var id = (await _servicio.CrearAsync(_escenas[1], _ana.Id, Vestuario("abrigo gris"))).Id!.Value;

        Assert.True((await _servicio.EliminarAsync(id)).Ok);

        Assert.Empty(await _servicio.ListarAsync(_escenas[1]));
        await using var db = _entorno.Base.CreateDbContext();
        Assert.Equal("chaqueta negra", (await db.Hojas.SingleAsync(h => h.Id == _ana.Id)).Vestuario);
        Assert.Contains("Vestuario: chaqueta negra.", await PromptAsync(_escenas[1]), StringComparison.Ordinal);
    }

    [Fact]
    [Trait("AC", "AC-09e")]
    public async Task Una_segunda_variante_del_mismo_personaje_se_rechaza()
    {
        await _servicio.CrearAsync(_escenas[1], _ana.Id, Vestuario("abrigo gris"));

        var resultado = await _servicio.CrearAsync(_escenas[1], _ana.Id, Vestuario("vestido rojo"));

        Assert.Equal("Ana ya tiene una variante en esta escena.", resultado.Motivo);
    }

    [Fact]
    [Trait("AC", "AC-09f")]
    public async Task Una_variante_sin_cambios_se_rechaza()
    {
        var resultado = await _servicio.CrearAsync(_escenas[1], _ana.Id, new DatosVariante(null, " ", null, false, null));

        Assert.Equal(VariantesService.SinCambios, resultado.Motivo);
    }

    [Fact]
    [Trait("AC", "AC-09g")]
    public async Task Vaciar_el_unico_campo_cambiado_se_rechaza()
    {
        var id = (await _servicio.CrearAsync(_escenas[1], _ana.Id, Vestuario("abrigo gris"))).Id!.Value;

        var resultado = await _servicio.EditarAsync(id, Vestuario(""));

        Assert.Equal(VariantesService.SinCambios, resultado.Motivo);
        Assert.Equal("abrigo gris", Assert.Single(await _servicio.ListarAsync(_escenas[1])).Vestuario);
    }

    [Fact]
    [Trait("AC", "AC-09i")]
    [Trait("AC", "AC-09j")]
    [Trait("AC", "AC-09k")]
    public async Task En_una_escena_generada_no_se_puede_crear_editar_ni_eliminar()
    {
        var id = (await _servicio.CrearAsync(_escenas[1], _ana.Id, Vestuario("abrigo gris"))).Id!.Value;
        await CambiarEstadoAsync(_escenas[1], EstadoEscena.Generada);

        Assert.Equal(VariantesService.EscenaBloqueada, (await _servicio.CrearAsync(_escenas[1], _jose.Id, Vestuario("traje"))).Motivo);
        Assert.Equal(VariantesService.EscenaBloqueada, (await _servicio.EditarAsync(id, Vestuario("vestido rojo"))).Motivo);
        Assert.Equal(VariantesService.EscenaBloqueada, (await _servicio.EliminarAsync(id)).Motivo);
        Assert.Equal("abrigo gris", Assert.Single(await _servicio.ListarAsync(_escenas[1])).Vestuario);
    }

    [Fact]
    [Trait("AC", "AC-09l")]
    public async Task En_una_escena_generando_no_se_puede_crear()
    {
        await CambiarEstadoAsync(_escenas[1], EstadoEscena.Generando);

        Assert.Equal(VariantesService.EscenaBloqueada, (await _servicio.CrearAsync(_escenas[1], _ana.Id, Vestuario("abrigo gris"))).Motivo);
    }

    [Fact]
    [Trait("AC", "AC-S39")]
    public async Task Reglas_de_FR_029a_y_reintento_con_la_hoja_efectiva()
    {
        var escenaId = _escenas[0];
        await CambiarEstadoAsync(escenaId, EstadoEscena.Error);

        var noDetectado = await _servicio.CrearAsync(escenaId, _bea.Id, Vestuario("vestido"));
        var igualALaBase = await _servicio.CrearAsync(escenaId, _ana.Id, Vestuario(" chaqueta negra "));
        var sinHeridas = await _servicio.CrearAsync(escenaId, _jose.Id, new DatosVariante(null, null, null, true, ""));

        Assert.Equal("Bea no aparece en esta escena; solo se pueden crear variantes de los personajes detectados.", noDetectado.Motivo);
        Assert.Equal(VariantesService.SinCambios, igualALaBase.Motivo);
        Assert.True(sinHeridas.Ok);

        _entorno.Ia.Responde(EscenaDePrueba.Respuesta([(1, ["c1-d1"])]));
        await _entorno.ConRelojAsync(_entorno.Generador.GenerarAsync(escenaId, CancellationToken.None));

        Assert.DoesNotContain("cicatriz", _entorno.Ia.Instrucciones.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sin_heridas_sobre_una_base_sin_heridas_no_es_un_cambio()
    {
        var resultado = await _servicio.CrearAsync(_escenas[0], _ana.Id, new DatosVariante(null, null, null, true, null));

        Assert.Equal(VariantesService.SinCambios, resultado.Motivo);
    }
}
