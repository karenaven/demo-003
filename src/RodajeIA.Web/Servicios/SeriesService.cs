using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Web.Servicios;

/// <summary>Series (FR-001, FR-004a).</summary>
public sealed class SeriesService(IDbContextFactory<RodajeDbContext> fabrica, IEstadoGeneracion estadoGeneracion)
{
    public const string SinNombre = "Falta el nombre de la serie.";
    public const string GeneracionEnCurso = "Hay una generación en curso en esta serie; esperá a que termine para eliminarla.";

    public async Task<IReadOnlyList<Serie>> ListarAsync(CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        return await db.Series.AsNoTracking().OrderBy(s => s.Nombre).ToListAsync(ct);
    }

    public async Task<Serie?> ObtenerAsync(int serieId, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        return await db.Series.AsNoTracking().SingleOrDefaultAsync(s => s.Id == serieId, ct);
    }

    public async Task<Resultado> CrearAsync(string nombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Resultado.Rechazo(SinNombre);
        }

        await using var db = await fabrica.CreateDbContextAsync(ct);
        var serie = new Serie { Nombre = nombre.Trim() };
        db.Series.Add(serie);
        await db.SaveChangesAsync(ct);
        return Resultado.Exito(serie.Id);
    }

    public async Task<Resultado> RenombrarAsync(int serieId, string nombre, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Resultado.Rechazo(SinNombre);
        }

        await using var db = await fabrica.CreateDbContextAsync(ct);
        await db.Series.Where(s => s.Id == serieId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Nombre, nombre.Trim()), ct);
        return Resultado.Exito(serieId);
    }

    public async Task<Resultado> EliminarAsync(int serieId, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        if (estadoGeneracion.EpisodioEnCurso is { } enCurso && await db.Episodios.AnyAsync(e => e.Id == enCurso && e.SerieId == serieId, ct))
        {
            return Resultado.Rechazo(GeneracionEnCurso);
        }

        await db.Series.Where(s => s.Id == serieId).ExecuteDeleteAsync(ct);
        return Resultado.Exito();
    }
}
