using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Generacion;
using RodajeIA.Web.Guion;
using RodajeIA.Web.Personajes;

namespace RodajeIA.Web.Servicios;

/// <summary>
/// Carga el guion de un episodio (FR-006 a FR-013): valida y divide sin IA y, solo si es válido,
/// reemplaza en una transacción el guion y todas las escenas del anterior.
/// </summary>
public sealed class CargaGuionService(IDbContextFactory<RodajeDbContext> fabrica, IEstadoGeneracion estadoGeneracion)
{
    public const string SinEpisodio = "Elegí una serie y un episodio antes de cargar el guion.";
    public const string GeneracionEnCurso = "Hay una generación en curso en este episodio; esperá a que termine para cargar otro guion.";

    public async Task<ResultadoCarga> CargarAsync(int? episodioId, string texto, CancellationToken ct = default)
    {
        if (episodioId is null)
        {
            return new ResultadoCarga.NoPermitido(SinEpisodio);
        }

        await using var db = await fabrica.CreateDbContextAsync(ct);
        var episodio = await db.Episodios.SingleOrDefaultAsync(e => e.Id == episodioId, ct);
        if (episodio is null)
        {
            return new ResultadoCarga.NoPermitido(SinEpisodio);
        }

        if (estadoGeneracion.EpisodioEnCurso == episodio.Id)
        {
            return new ResultadoCarga.NoPermitido(GeneracionEnCurso);
        }

        var resultado = ParserGuion.Parsear(texto);
        if (resultado is ResultadoGuion.Invalido invalido)
        {
            return new ResultadoCarga.Rechazado(invalido.Errores);
        }

        var guion = ((ResultadoGuion.Valido)resultado).Guion;
        var hojas = await db.Hojas.Where(h => h.SerieId == episodio.SerieId).ToListAsync(ct);

        await using var transaccion = await db.Database.BeginTransactionAsync(ct);
        await db.Escenas.Where(e => e.EpisodioId == episodio.Id).ExecuteDeleteAsync(ct);

        episodio.Guion = texto;
        foreach (var escenaParseada in guion.Escenas)
        {
            db.Escenas.Add(CrearEscena(episodio.Id, escenaParseada, DetectorPersonajes.Detectar(escenaParseada, hojas)));
        }

        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);
        return new ResultadoCarga.Guardado(guion.Escenas.Count);
    }

    private static Escena CrearEscena(int episodioId, EscenaParseada parseada, DeteccionEscena deteccion) => new()
    {
        EpisodioId = episodioId,
        Numero = parseada.Numero,
        IntExt = parseada.IntExt,
        Lugar = parseada.Lugar,
        MomentoDia = parseada.MomentoDia,
        Locacion = parseada.Locacion,
        Iluminacion = parseada.Iluminacion,
        PuestaEnEscena = parseada.PuestaEnEscena,
        Audio = parseada.Audio,
        Estado = EstadoEscena.Pendiente,
        Personajes = deteccion.PresentesEnOrden.Select((hojaId, i) => new PersonajeEnEscena { HojaId = hojaId, Orden = i + 1 }).ToList(),
        Clips = parseada.Clips.Select(c => new Clip
        {
            Numero = c.Numero,
            Accion = c.Accion,
            TextoEnPantalla = c.TextoEnPantalla?.Texto,
            TextoEnPantallaOriginal = c.TextoEnPantalla?.Original,
            Lineas = c.Dialogos.Select(d => new LineaDialogo
            {
                Posicion = d.Posicion,
                Personaje = d.Personaje,
                Acotacion = d.Acotacion,
                Texto = d.Texto,
                Original = d.Original,
            }).ToList(),
            Personajes = deteccion.PorClip[c.Numero].Select(hojaId => new PersonajeEnClip { HojaId = hojaId }).ToList(),
        }).ToList(),
    };
}
