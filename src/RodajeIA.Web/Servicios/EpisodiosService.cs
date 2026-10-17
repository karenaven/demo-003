using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Web.Servicios;

/// <summary>Episodios (FR-004, FR-004a, FR-005). El número no se edita.</summary>
public sealed class EpisodiosService(IDbContextFactory<RodajeDbContext> fabrica, IEstadoGeneracion estadoGeneracion)
{
    public const string SinTitulo = "Falta el título del episodio.";
    public const string GeneracionEnCurso = "Hay una generación en curso en este episodio; esperá a que termine para eliminarlo.";

    public async Task<IReadOnlyList<Episodio>> ListarAsync(int serieId, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        return await db.Episodios.AsNoTracking().Where(e => e.SerieId == serieId).OrderBy(e => e.Numero).ToListAsync(ct);
    }

    /// <summary>El episodio con su serie y sus escenas en orden de número (FR-005).</summary>
    public async Task<Episodio?> ObtenerDetalleAsync(int episodioId, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var episodio = await db.Episodios
            .Include(e => e.Serie)
            .Include(e => e.Escenas)
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == episodioId, ct);
        episodio?.Escenas.Sort((a, b) => a.Numero.CompareTo(b.Numero));
        return episodio;
    }

    public async Task<Resultado> CrearAsync(int serieId, int numero, string titulo, CancellationToken ct = default)
    {
        if (numero < 1)
        {
            return Resultado.Rechazo("El número del episodio debe ser 1 o mayor.");
        }

        if (string.IsNullOrWhiteSpace(titulo))
        {
            return Resultado.Rechazo(SinTitulo);
        }

        await using var db = await fabrica.CreateDbContextAsync(ct);
        if (await db.Episodios.AnyAsync(e => e.SerieId == serieId && e.Numero == numero, ct))
        {
            return Resultado.Rechazo($"Ya existe el episodio número {numero} en esta serie.");
        }

        var episodio = new Episodio { SerieId = serieId, Numero = numero, Titulo = titulo.Trim() };
        db.Episodios.Add(episodio);
        await db.SaveChangesAsync(ct);
        return Resultado.Exito(episodio.Id);
    }

    public async Task<Resultado> EditarTituloAsync(int episodioId, string titulo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(titulo))
        {
            return Resultado.Rechazo(SinTitulo);
        }

        await using var db = await fabrica.CreateDbContextAsync(ct);
        await db.Episodios.Where(e => e.Id == episodioId).ExecuteUpdateAsync(s => s.SetProperty(e => e.Titulo, titulo.Trim()), ct);
        return Resultado.Exito(episodioId);
    }

    public async Task<Resultado> EliminarAsync(int episodioId, CancellationToken ct = default)
    {
        if (estadoGeneracion.EpisodioEnCurso == episodioId)
        {
            return Resultado.Rechazo(GeneracionEnCurso);
        }

        await using var db = await fabrica.CreateDbContextAsync(ct);
        await db.Episodios.Where(e => e.Id == episodioId).ExecuteDeleteAsync(ct);
        return Resultado.Exito();
    }
}
