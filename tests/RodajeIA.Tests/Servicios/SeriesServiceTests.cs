using Microsoft.EntityFrameworkCore;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Servicios;

namespace RodajeIA.Tests.Servicios;

public sealed class SeriesServiceTests : IDisposable
{
    private readonly BaseDatosEnMemoria _base = new();
    private readonly EstadoGeneracionFalso _estado = new();
    private readonly SeriesService _servicio;

    public SeriesServiceTests() => _servicio = new SeriesService(_base, _estado);

    public void Dispose() => _base.Dispose();

    [Fact]
    [Trait("AC", "AC-01a")]
    public async Task Crear_una_serie_la_guarda()
    {
        var resultado = await _servicio.CrearAsync("Serie A");

        Assert.True(resultado.Ok);
        Assert.Equal("Serie A", Assert.Single(await _servicio.ListarAsync()).Nombre);
    }

    [Fact]
    public async Task Una_serie_sin_nombre_no_se_guarda()
    {
        var resultado = await _servicio.CrearAsync("   ");

        Assert.Equal("Falta el nombre de la serie.", resultado.Motivo);
        Assert.Empty(await _servicio.ListarAsync());
    }

    [Fact]
    [Trait("AC", "AC-01b")]
    public async Task Listar_muestra_todas_las_series()
    {
        await _servicio.CrearAsync("Serie A");
        await _servicio.CrearAsync("Serie B");

        Assert.Equal(["Serie A", "Serie B"], (await _servicio.ListarAsync()).Select(s => s.Nombre));
    }

    [Fact]
    [Trait("AC", "AC-01c")]
    public async Task Renombrar_cambia_el_nombre()
    {
        var id = (await _servicio.CrearAsync("Serie A")).Id!.Value;

        await _servicio.RenombrarAsync(id, "Serie B");

        Assert.Equal(["Serie B"], (await _servicio.ListarAsync()).Select(s => s.Nombre));
    }

    [Fact]
    [Trait("AC", "AC-01d")]
    public async Task Eliminar_borra_en_cascada_hojas_episodios_escenas_clips_bloques_y_variantes()
    {
        var (serie, ana, _, episodio) = _base.CrearSerieDeEjemplo();
        var ids = await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.EscenaAnexoA);
        await using (var db = _base.CreateDbContext())
        {
            db.Variantes.Add(new Web.Datos.Entidades.VarianteEscena { EscenaId = ids[0], HojaId = ana.Id, Vestuario = "abrigo gris" });
            await db.SaveChangesAsync();
        }

        await EscenaDePrueba.MarcarGeneradaAsync(_base, ids[0]);

        var resultado = await _servicio.EliminarAsync(serie.Id);

        Assert.True(resultado.Ok);
        await using var lectura = _base.CreateDbContext();
        Assert.Empty(await lectura.Series.ToListAsync());
        Assert.Empty(await lectura.Hojas.ToListAsync());
        Assert.Empty(await lectura.Episodios.ToListAsync());
        Assert.Empty(await lectura.Escenas.ToListAsync());
        Assert.Empty(await lectura.Clips.ToListAsync());
        Assert.Empty(await lectura.LineasDialogo.ToListAsync());
        Assert.Empty(await lectura.Bloques.ToListAsync());
        Assert.Empty(await lectura.Tomas.ToListAsync());
        Assert.Empty(await lectura.Variantes.ToListAsync());
    }

    [Fact]
    [Trait("AC", "AC-S15")]
    public async Task No_se_puede_eliminar_una_serie_con_una_generacion_en_curso()
    {
        var (serie, _, _, episodio) = _base.CrearSerieDeEjemplo();
        _estado.EpisodioEnCurso = episodio.Id;

        var resultado = await _servicio.EliminarAsync(serie.Id);

        Assert.Equal("Hay una generación en curso en esta serie; esperá a que termine para eliminarla.", resultado.Motivo);
        Assert.Single(await _servicio.ListarAsync());
    }
}
