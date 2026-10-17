using Microsoft.EntityFrameworkCore;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Servicios;

namespace RodajeIA.Tests.Servicios;

public sealed class EpisodiosServiceTests : IDisposable
{
    private readonly BaseDatosEnMemoria _base = new();
    private readonly EstadoGeneracionFalso _estado = new();
    private readonly EpisodiosService _servicio;
    private readonly int _serieId;

    public EpisodiosServiceTests()
    {
        _servicio = new EpisodiosService(_base, _estado);
        using var db = _base.CreateDbContext();
        var serie = new Serie { Nombre = "Demo" };
        db.Series.Add(serie);
        db.SaveChanges();
        _serieId = serie.Id;
    }

    public void Dispose() => _base.Dispose();

    [Fact]
    [Trait("AC", "AC-11a")]
    public async Task Crear_el_episodio_1_lo_guarda_en_la_serie()
    {
        var resultado = await _servicio.CrearAsync(_serieId, 1, "Piloto");

        Assert.True(resultado.Ok);
        var episodio = Assert.Single(await _servicio.ListarAsync(_serieId));
        Assert.Equal((1, "Piloto"), (episodio.Numero, episodio.Titulo));
    }

    [Theory]
    [InlineData(0, "Piloto", "El número del episodio debe ser 1 o mayor.")]
    [InlineData(1, " ", "Falta el título del episodio.")]
    public async Task Numero_o_titulo_invalidos_no_se_guardan(int numero, string titulo, string motivo)
    {
        Assert.Equal(motivo, (await _servicio.CrearAsync(_serieId, numero, titulo)).Motivo);
    }

    [Fact]
    [Trait("AC", "AC-11b")]
    public async Task Listar_muestra_los_episodios_en_orden()
    {
        await _servicio.CrearAsync(_serieId, 2, "La cena");
        await _servicio.CrearAsync(_serieId, 1, "Piloto");

        Assert.Equal([1, 2], (await _servicio.ListarAsync(_serieId)).Select(e => e.Numero));
    }

    [Fact]
    [Trait("AC", "AC-11c")]
    [Trait("AC", "AC-11g")]
    public async Task Editar_cambia_solo_el_titulo()
    {
        var id = (await _servicio.CrearAsync(_serieId, 1, "Piloto")).Id!.Value;

        await _servicio.EditarTituloAsync(id, "La cena");

        var episodio = Assert.Single(await _servicio.ListarAsync(_serieId));
        Assert.Equal((1, "La cena"), (episodio.Numero, episodio.Titulo));
        Assert.DoesNotContain(typeof(EpisodiosService).GetMethods(), m => m.Name.Contains("Numero", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("AC", "AC-11d")]
    public async Task Eliminar_borra_guion_escenas_clips_bloques_y_variantes()
    {
        var (_, ana, _, episodio) = _base.CrearSerieDeEjemplo("Con escenas");
        var ids = await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.EscenaAnexoA);
        await using (var db = _base.CreateDbContext())
        {
            db.Variantes.Add(new VarianteEscena { EscenaId = ids[0], HojaId = ana.Id, Vestuario = "abrigo gris" });
            await db.SaveChangesAsync();
        }

        await EscenaDePrueba.MarcarGeneradaAsync(_base, ids[0]);

        Assert.True((await _servicio.EliminarAsync(episodio.Id)).Ok);

        await using var lectura = _base.CreateDbContext();
        Assert.False(await lectura.Episodios.AnyAsync(e => e.Id == episodio.Id));
        Assert.Empty(await lectura.Escenas.ToListAsync());
        Assert.Empty(await lectura.Clips.ToListAsync());
        Assert.Empty(await lectura.Bloques.ToListAsync());
        Assert.Empty(await lectura.Variantes.ToListAsync());
        Assert.Equal(2, await lectura.Hojas.CountAsync(h => h.SerieId == ana.SerieId));
    }

    [Fact]
    [Trait("AC", "AC-11e")]
    public async Task El_detalle_muestra_las_escenas_en_orden_con_su_estado()
    {
        var (_, _, _, episodio) = _base.CrearSerieDeEjemplo("Con escenas");
        var ids = await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.Guion(3, 1));
        await EscenaDePrueba.MarcarGeneradaAsync(_base, ids[0]);
        await EscenaDePrueba.MarcarGeneradaAsync(_base, ids[1]);
        await using (var db = _base.CreateDbContext())
        {
            await db.Escenas.Where(e => e.Id == ids[2]).ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, EstadoEscena.Error));
        }

        var detalle = await _servicio.ObtenerDetalleAsync(episodio.Id);

        Assert.NotNull(detalle);
        Assert.Equal([(1, EstadoEscena.Generada), (2, EstadoEscena.Generada), (3, EstadoEscena.Error)], detalle.Escenas.Select(e => (e.Numero, e.Estado)));
    }

    [Fact]
    [Trait("AC", "AC-11f")]
    public async Task Un_numero_repetido_en_la_serie_no_se_guarda()
    {
        await _servicio.CrearAsync(_serieId, 1, "Piloto");

        var resultado = await _servicio.CrearAsync(_serieId, 1, "Otro");

        Assert.Equal("Ya existe el episodio número 1 en esta serie.", resultado.Motivo);
        Assert.Single(await _servicio.ListarAsync(_serieId));
    }

    [Fact]
    [Trait("AC", "AC-S15")]
    public async Task No_se_puede_eliminar_un_episodio_con_una_generacion_en_curso()
    {
        var id = (await _servicio.CrearAsync(_serieId, 1, "Piloto")).Id!.Value;
        _estado.EpisodioEnCurso = id;

        var resultado = await _servicio.EliminarAsync(id);

        Assert.Equal("Hay una generación en curso en este episodio; esperá a que termine para eliminarlo.", resultado.Motivo);
        Assert.Single(await _servicio.ListarAsync(_serieId));
    }
}
