using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Web.Servicios;

/// <summary>
/// Los reemplazos pedidos para una variante. Un campo nulo o vacío no reemplaza; con
/// <see cref="ReemplazaHeridas"/>, <see cref="HeridasMarcas"/> vacío significa "sin heridas".
/// </summary>
public sealed record DatosVariante(string? Edad, string? Peinado, string? Vestuario, bool ReemplazaHeridas, string? HeridasMarcas);

/// <summary>Variantes de personaje por escena (FR-029 a FR-031, FR-029a).</summary>
public sealed class VariantesService(IDbContextFactory<RodajeDbContext> fabrica)
{
    public const string SinCambios = "La variante debe cambiar al menos un campo respecto de la hoja base.";
    public const string EscenaBloqueada = "No se pueden cambiar las variantes de una escena que se está generando o ya está generada.";

    public async Task<IReadOnlyList<VarianteEscena>> ListarAsync(int escenaId, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        return await db.Variantes.AsNoTracking().Include(v => v.Hoja).Where(v => v.EscenaId == escenaId).ToListAsync(ct);
    }

    public async Task<Resultado> CrearAsync(int escenaId, int hojaId, DatosVariante datos, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var escena = await db.Escenas.Include(e => e.Personajes).SingleOrDefaultAsync(e => e.Id == escenaId, ct);
        var hoja = await db.Hojas.SingleOrDefaultAsync(h => h.Id == hojaId, ct);
        if (escena is null || hoja is null)
        {
            return Resultado.Rechazo("No existe la escena o el personaje.");
        }

        if (Bloqueada(escena))
        {
            return Resultado.Rechazo(EscenaBloqueada);
        }

        if (escena.Personajes.All(p => p.HojaId != hojaId))
        {
            return Resultado.Rechazo($"{hoja.Nombre} no aparece en esta escena; solo se pueden crear variantes de los personajes detectados.");
        }

        if (await db.Variantes.AnyAsync(v => v.EscenaId == escenaId && v.HojaId == hojaId, ct))
        {
            return Resultado.Rechazo($"{hoja.Nombre} ya tiene una variante en esta escena.");
        }

        var variante = new VarianteEscena { EscenaId = escenaId, HojaId = hojaId };
        if (!Aplicar(variante, hoja, datos))
        {
            return Resultado.Rechazo(SinCambios);
        }

        db.Variantes.Add(variante);
        await db.SaveChangesAsync(ct);
        return Resultado.Exito(variante.Id);
    }

    public async Task<Resultado> EditarAsync(int varianteId, DatosVariante datos, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var variante = await db.Variantes.Include(v => v.Escena).Include(v => v.Hoja).SingleOrDefaultAsync(v => v.Id == varianteId, ct);
        if (variante is null)
        {
            return Resultado.Rechazo("No existe la variante.");
        }

        if (Bloqueada(variante.Escena!))
        {
            return Resultado.Rechazo(EscenaBloqueada);
        }

        if (!Aplicar(variante, variante.Hoja!, datos))
        {
            return Resultado.Rechazo(SinCambios);
        }

        await db.SaveChangesAsync(ct);
        return Resultado.Exito(varianteId);
    }

    /// <summary>Elimina la variante; la hoja base no cambia (FR-030).</summary>
    public async Task<Resultado> EliminarAsync(int varianteId, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var variante = await db.Variantes.Include(v => v.Escena).SingleOrDefaultAsync(v => v.Id == varianteId, ct);
        if (variante is null)
        {
            return Resultado.Exito();
        }

        if (Bloqueada(variante.Escena!))
        {
            return Resultado.Rechazo(EscenaBloqueada);
        }

        db.Variantes.Remove(variante);
        await db.SaveChangesAsync(ct);
        return Resultado.Exito();
    }

    /// <summary>Las variantes solo se cambian en "pendiente" o "error" (FR-031).</summary>
    private static bool Bloqueada(Escena escena) => escena.Estado is EstadoEscena.Generando or EstadoEscena.Generada;

    /// <summary>Copia los reemplazos a la variante; devuelve si al menos uno difiere de la hoja base (FR-029a).</summary>
    private static bool Aplicar(VarianteEscena variante, HojaPersonaje hoja, DatosVariante datos)
    {
        var edad = Limpio(datos.Edad);
        var peinado = Limpio(datos.Peinado);
        var vestuario = Limpio(datos.Vestuario);
        var heridas = Limpio(datos.HeridasMarcas);

        var cambia = (edad is not null && edad != hoja.Edad)
            || (peinado is not null && peinado != hoja.Peinado)
            || (vestuario is not null && vestuario != hoja.Vestuario)
            || (datos.ReemplazaHeridas && heridas != Limpio(hoja.HeridasMarcas));
        if (!cambia)
        {
            return false;
        }

        variante.Edad = edad;
        variante.Peinado = peinado;
        variante.Vestuario = vestuario;
        variante.ReemplazaHeridas = datos.ReemplazaHeridas;
        variante.HeridasMarcas = datos.ReemplazaHeridas ? heridas : null;
        return true;
    }

    private static string? Limpio(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
