using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Web.Servicios;

public sealed record DatosHoja(
    string Nombre,
    string Edad,
    string DescripcionFisica,
    string Peinado,
    string Vestuario,
    string? HeridasMarcas,
    string Personalidad,
    string Rol);

/// <summary>Hojas de personaje (FR-002 a FR-003b).</summary>
public sealed class HojasService(IDbContextFactory<RodajeDbContext> fabrica)
{
    public const string AvisoRenombrado = "Los guiones cargados siguen usando el nombre anterior; recargalos para que coincidan.";

    /// <summary>Marcadores del guion que no pueden ser nombre de hoja (FR-002b).</summary>
    public static readonly IReadOnlySet<string> NombresReservados = new HashSet<string>(StringComparer.Ordinal)
    {
        "ESCENA", "CLIP", "LOCACIÓN", "ILUMINACIÓN", "PUESTA EN ESCENA", "AUDIO", "TEXTO EN PANTALLA",
    };

    public async Task<IReadOnlyList<HojaPersonaje>> ListarAsync(int serieId, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        return await db.Hojas.AsNoTracking().Where(h => h.SerieId == serieId).OrderBy(h => h.Nombre).ToListAsync(ct);
    }

    public async Task<Resultado> CrearAsync(int serieId, DatosHoja datos, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        if (await ValidarAsync(db, serieId, null, datos, ct) is { } motivo)
        {
            return Resultado.Rechazo(motivo);
        }

        var hoja = new HojaPersonaje
        {
            SerieId = serieId,
            Nombre = "",
            NombreClave = "",
            Edad = "",
            DescripcionFisica = "",
            Peinado = "",
            Vestuario = "",
            Personalidad = "",
            Rol = "",
        };
        Aplicar(hoja, datos);
        db.Hojas.Add(hoja);
        await db.SaveChangesAsync(ct);
        return Resultado.Exito(hoja.Id);
    }

    public async Task<Resultado> EditarAsync(int hojaId, DatosHoja datos, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var hoja = await db.Hojas.SingleOrDefaultAsync(h => h.Id == hojaId, ct);
        if (hoja is null)
        {
            return Resultado.Rechazo("No existe el personaje.");
        }

        if (await ValidarAsync(db, hoja.SerieId, hojaId, datos, ct) is { } motivo)
        {
            return Resultado.Rechazo(motivo);
        }

        var renombrada = hoja.Nombre != datos.Nombre.Trim();
        var usada = await db.PersonajesEnEscena.AnyAsync(p => p.HojaId == hojaId, ct);
        Aplicar(hoja, datos);
        await db.SaveChangesAsync(ct);
        return Resultado.Exito(hojaId, renombrada && usada ? AvisoRenombrado : null);
    }

    /// <summary>Elimina la hoja con sus variantes, aunque sus escenas estén en "generando" o "generada" (FR-003, FR-003a).</summary>
    public async Task<Resultado> EliminarAsync(int hojaId, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        await db.Hojas.Where(h => h.Id == hojaId).ExecuteDeleteAsync(ct);
        return Resultado.Exito();
    }

    private static async Task<string?> ValidarAsync(RodajeDbContext db, int serieId, int? hojaId, DatosHoja datos, CancellationToken ct)
    {
        var obligatorios = new (string Valor, string Falta)[]
        {
            (datos.Nombre, "Falta el nombre."),
            (datos.Edad, "Falta la edad."),
            (datos.DescripcionFisica, "Falta la descripción física."),
            (datos.Peinado, "Falta el peinado."),
            (datos.Vestuario, "Falta el vestuario."),
            (datos.Personalidad, "Falta la personalidad."),
            (datos.Rol, "Falta el rol."),
        };
        if (obligatorios.FirstOrDefault(o => string.IsNullOrWhiteSpace(o.Valor)).Falta is { } falta)
        {
            return falta;
        }

        var nombre = datos.Nombre.Trim();
        var clave = HojaPersonaje.ClaveDe(nombre);
        if (NombresReservados.Contains(clave))
        {
            return $"«{nombre}» es un nombre reservado del guion; elegí otro nombre.";
        }

        if (await db.Hojas.AnyAsync(h => h.SerieId == serieId && h.NombreClave == clave && h.Id != hojaId, ct))
        {
            return $"Ya existe un personaje llamado {nombre} en esta serie.";
        }

        return null;
    }

    private static void Aplicar(HojaPersonaje hoja, DatosHoja datos)
    {
        hoja.Nombre = datos.Nombre.Trim();
        hoja.NombreClave = HojaPersonaje.ClaveDe(datos.Nombre);
        hoja.Edad = datos.Edad.Trim();
        hoja.DescripcionFisica = datos.DescripcionFisica.Trim();
        hoja.Peinado = datos.Peinado.Trim();
        hoja.Vestuario = datos.Vestuario.Trim();
        hoja.HeridasMarcas = string.IsNullOrWhiteSpace(datos.HeridasMarcas) ? null : datos.HeridasMarcas.Trim();
        hoja.Personalidad = datos.Personalidad.Trim();
        hoja.Rol = datos.Rol.Trim();
    }
}
