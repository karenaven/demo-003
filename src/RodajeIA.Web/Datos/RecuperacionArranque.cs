using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Web.Datos;

/// <summary>Al iniciar, toda escena en "generando" pasa a "error"; la generación no se reanuda sola (FR-021).</summary>
public static class RecuperacionArranque
{
    public const string BaseSinMigrar =
        "La base de datos no está creada o no está actualizada: ejecutá dotnet ef database update --project src/RodajeIA.Web.";

    public static async Task<int> RecuperarAsync(IDbContextFactory<RodajeDbContext> fabrica, CancellationToken ct)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        try
        {
            return await db.Escenas
                .Where(e => e.Estado == EstadoEscena.Generando)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(e => e.Estado, EstadoEscena.Error).SetProperty(e => e.MotivoError, MotivosGeneracion.Cierre),
                    ct);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            throw new InvalidOperationException(BaseSinMigrar, ex);
        }
    }
}
