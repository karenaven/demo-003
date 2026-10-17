using Microsoft.EntityFrameworkCore;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Servicios;

namespace RodajeIA.Tests.Servicios;

public sealed class CargaGuionServiceTests : IDisposable
{
    private readonly BaseDatosEnMemoria _base = new();
    private readonly CargaGuionService _servicio;

    public CargaGuionServiceTests()
    {
        _servicio = new CargaGuionService(_base, new EstadoGeneracionFalso());
    }

    public void Dispose() => _base.Dispose();

    private static string GuionDeEscenas(int escenas) => Ejemplos.Guion(escenas, 2);

    [Fact]
    [Trait("AC", "AC-02a")]
    [Trait("AC", "AC-06j")]
    public async Task Un_guion_valido_queda_guardado_en_el_episodio_con_escenas_pendientes()
    {
        var (_, ana, jose, episodio) = _base.CrearSerieDeEjemplo();

        var resultado = await _servicio.CargarAsync(episodio.Id, Ejemplos.EscenaAnexoA);

        Assert.IsType<ResultadoCarga.Guardado>(resultado);
        await using var db = _base.CreateDbContext();
        var guardado = await db.Episodios.Include(e => e.Escenas).ThenInclude(e => e.Clips).ThenInclude(c => c.Lineas).SingleAsync();
        Assert.Equal(Ejemplos.EscenaAnexoA, guardado.Guion);
        var escena = Assert.Single(guardado.Escenas);
        Assert.Equal(EstadoEscena.Pendiente, escena.Estado);
        Assert.Equal(2, escena.Clips.Count);
        var clip1 = escena.Clips.Single(c => c.Numero == 1);
        Assert.Equal(["ANA: \"Mentiroso.\"", "JOSÉ DANIEL (sonriendo): \"Bueno... pasó parecido.\""], clip1.Lineas.OrderBy(l => l.Posicion).Select(l => l.Original));
        Assert.Empty(await db.Bloques.ToListAsync());
    }

    [Fact]
    public async Task Guarda_los_personajes_detectados_por_escena_con_su_orden_y_por_clip()
    {
        var (_, ana, jose, episodio) = _base.CrearSerieDeEjemplo();

        await _servicio.CargarAsync(episodio.Id, Ejemplos.EscenaAnexoA);

        await using var db = _base.CreateDbContext();
        var enEscena = await db.PersonajesEnEscena.OrderBy(p => p.Orden).Select(p => p.HojaId).ToListAsync();
        Assert.Equal([ana.Id, jose.Id], enEscena);
        var clip2 = await db.Clips.Include(c => c.Personajes).SingleAsync(c => c.Numero == 2);
        Assert.Equal([ana.Id, jose.Id], clip2.Personajes.Select(p => p.HojaId).Order());
    }

    [Fact]
    [Trait("AC", "AC-02b")]
    public async Task Sin_episodio_elegido_la_carga_no_se_permite()
    {
        _base.CrearSerieDeEjemplo();

        var resultado = await _servicio.CargarAsync(null, Ejemplos.EscenaAnexoA);
        var inexistente = await _servicio.CargarAsync(999, Ejemplos.EscenaAnexoA);

        Assert.IsType<ResultadoCarga.NoPermitido>(resultado);
        Assert.IsType<ResultadoCarga.NoPermitido>(inexistente);
        await using var db = _base.CreateDbContext();
        Assert.Empty(await db.Escenas.ToListAsync());
    }

    [Fact]
    [Trait("AC", "AC-02c")]
    public async Task Un_guion_nuevo_reemplaza_al_anterior_y_elimina_escenas_clips_bloques_y_variantes()
    {
        var (_, ana, _, episodio) = _base.CrearSerieDeEjemplo();
        await _servicio.CargarAsync(episodio.Id, GuionDeEscenas(3));
        await GenerarTodoConVariante(ana.Id);

        var resultado = await _servicio.CargarAsync(episodio.Id, GuionDeEscenas(2));

        Assert.IsType<ResultadoCarga.Guardado>(resultado);
        await using var db = _base.CreateDbContext();
        Assert.Equal(GuionDeEscenas(2), (await db.Episodios.SingleAsync()).Guion);
        Assert.Equal(2, await db.Escenas.CountAsync());
        Assert.All(await db.Escenas.ToListAsync(), e => Assert.Equal(EstadoEscena.Pendiente, e.Estado));
        Assert.Equal(4, await db.Clips.CountAsync());
        Assert.Empty(await db.Bloques.ToListAsync());
        Assert.Empty(await db.Tomas.ToListAsync());
        Assert.Empty(await db.Variantes.ToListAsync());
    }

    [Fact]
    [Trait("AC", "AC-02d")]
    public async Task Un_guion_invalido_conserva_el_anterior_completo()
    {
        var (_, ana, _, episodio) = _base.CrearSerieDeEjemplo();
        await _servicio.CargarAsync(episodio.Id, GuionDeEscenas(3));
        await GenerarTodoConVariante(ana.Id);

        var resultado = await _servicio.CargarAsync(episodio.Id, "ESCENA 1 — INT RESTAURANTE — NOCHE");

        Assert.NotEmpty(Assert.IsType<ResultadoCarga.Rechazado>(resultado).Errores);
        await using var db = _base.CreateDbContext();
        Assert.Equal(GuionDeEscenas(3), (await db.Episodios.SingleAsync()).Guion);
        Assert.Equal(3, await db.Escenas.CountAsync());
        Assert.Equal(6, await db.Bloques.CountAsync());
        Assert.Equal(3, await db.Variantes.CountAsync());
    }

    [Fact]
    [Trait("AC", "AC-03f")]
    public async Task Un_guion_invalido_en_un_episodio_sin_guion_no_guarda_nada()
    {
        var (_, _, _, episodio) = _base.CrearSerieDeEjemplo();

        var resultado = await _servicio.CargarAsync(episodio.Id, Ejemplos.EscenaAnexoA.Replace("ANA: \"Mentiroso.\"", "ANA: Mentiroso.", StringComparison.Ordinal));

        Assert.IsType<ResultadoCarga.Rechazado>(resultado);
        await using var db = _base.CreateDbContext();
        Assert.Null((await db.Episodios.SingleAsync()).Guion);
        Assert.Empty(await db.Escenas.ToListAsync());
        Assert.Empty(await db.Clips.ToListAsync());
        Assert.Empty(await db.Bloques.ToListAsync());
    }

    [Fact]
    [Trait("AC", "AC-S15")]
    public async Task No_se_puede_cargar_un_guion_en_un_episodio_con_generacion_en_curso()
    {
        var (serie, _, _, episodio) = _base.CrearSerieDeEjemplo();
        await using (var db = _base.CreateDbContext())
        {
            db.Episodios.Add(new Episodio { SerieId = serie.Id, Numero = 2, Titulo = "Otro" });
            await db.SaveChangesAsync();
        }

        var estado = new EstadoGeneracionFalso { EpisodioEnCurso = episodio.Id };
        var servicio = new CargaGuionService(_base, estado);

        var bloqueado = await servicio.CargarAsync(episodio.Id, Ejemplos.EscenaAnexoA);
        var otro = await servicio.CargarAsync(episodio.Id + 1, Ejemplos.EscenaAnexoA);

        Assert.Equal("Hay una generación en curso en este episodio; esperá a que termine para cargar otro guion.", Assert.IsType<ResultadoCarga.NoPermitido>(bloqueado).Motivo);
        Assert.IsType<ResultadoCarga.Guardado>(otro);
    }

    /// <summary>Simula una generación terminada: bloques y tomas en cada clip y una variante por escena.</summary>
    private async Task GenerarTodoConVariante(int hojaId)
    {
        await using var db = _base.CreateDbContext();
        foreach (var escena in await db.Escenas.Include(e => e.Clips).ToListAsync())
        {
            escena.Estado = EstadoEscena.Generada;
            db.Variantes.Add(new VarianteEscena { EscenaId = escena.Id, HojaId = hojaId, Vestuario = "abrigo gris" });
            foreach (var clip in escena.Clips)
            {
                db.Bloques.Add(new Bloque
                {
                    ClipId = clip.Id,
                    Tomas = [new Toma { Orden = 1, Plano = "plano medio", Optica = "50mm", Iluminacion = "continuidad con la luz base", Accion = "Ana mira." }],
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
