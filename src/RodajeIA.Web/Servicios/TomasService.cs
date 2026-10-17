using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Web.Servicios;

/// <param name="Dialogos">Las líneas referenciadas, literales y en el orden del guion; de solo lectura (FR-023a).</param>
/// <param name="FueraDeVocabulario">Categorías cuyo valor guardado ya no está en el vocabulario (FR-015b).</param>
public sealed record TomaRevision(
    int Id,
    int Orden,
    string Plano,
    string Optica,
    string Iluminacion,
    string? Angulo,
    string? Movimiento,
    string Accion,
    IReadOnlyList<string> Dialogos,
    IReadOnlyList<string> FueraDeVocabulario);

public sealed record BloqueRevision(int Clip, IReadOnlyList<TomaRevision> Tomas);

/// <summary>Revisión y edición de las tomas de una escena generada (FR-022 a FR-024, FR-015b, FR-023a).</summary>
public sealed class TomasService(IDbContextFactory<RodajeDbContext> fabrica, Vocabulario vocabulario)
{
    public const string SoloGenerada = "Solo se pueden editar las tomas de una escena generada.";
    public const string AccionVacia = "La acción no puede quedar vacía.";

    private static readonly IReadOnlyDictionary<string, (string Nombre, string Obligatorio)> _textos =
        new Dictionary<string, (string, string)>
        {
            [Vocabulario.Plano] = ("plano", "El plano es obligatorio."),
            [Vocabulario.Optica] = ("óptica", "La óptica es obligatoria."),
            [Vocabulario.Iluminacion] = ("iluminación", "La iluminación es obligatoria."),
            [Vocabulario.Angulo] = ("ángulo", ""),
            [Vocabulario.Movimiento] = ("movimiento", ""),
        };

    /// <summary>Los valores vigentes que se ofrecen al editar una categoría.</summary>
    public IReadOnlyList<string> Opciones(string categoria) => vocabulario.Categoria(categoria).Valores;

    public bool EsObligatoria(string categoria) => vocabulario.Categoria(categoria).Obligatorio;

    public async Task<IReadOnlyList<BloqueRevision>> ObtenerBloquesAsync(int escenaId, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var clips = await db.Clips
            .Where(c => c.EscenaId == escenaId)
            .Include(c => c.Lineas)
            .Include(c => c.Bloque).ThenInclude(b => b!.Tomas).ThenInclude(t => t.Dialogos)
            .AsSplitQuery()
            .AsNoTracking()
            .OrderBy(c => c.Numero)
            .ToListAsync(ct);

        return clips.Where(c => c.Bloque is not null).Select(c =>
        {
            var lineas = c.Lineas.ToDictionary(l => l.Id);
            return new BloqueRevision(c.Numero, c.Bloque!.Tomas.OrderBy(t => t.Orden).Select(t => new TomaRevision(
                t.Id,
                t.Orden,
                t.Plano,
                t.Optica,
                t.Iluminacion,
                t.Angulo,
                t.Movimiento,
                t.Accion,
                t.Dialogos.Select(d => lineas[d.LineaDialogoId]).OrderBy(l => l.Posicion).Select(l => l.Original).ToList(),
                FueraDeVocabulario(t))).ToList());
        }).ToList();
    }

    public async Task<Resultado> EditarCampoAsync(int tomaId, string categoria, string? valor, CancellationToken ct = default)
    {
        var definicion = vocabulario.Categoria(categoria);
        valor = string.IsNullOrWhiteSpace(valor) ? null : valor;
        if (valor is null && definicion.Obligatorio)
        {
            return Resultado.Rechazo(_textos[categoria].Obligatorio);
        }

        if (valor is not null && !definicion.Contiene(valor))
        {
            return Resultado.Rechazo($"«{valor}» no está en el vocabulario de {_textos[categoria].Nombre}.");
        }

        return await ModificarAsync(tomaId, toma =>
        {
            switch (categoria)
            {
                case Vocabulario.Plano:
                    toma.Plano = valor!;
                    break;
                case Vocabulario.Optica:
                    toma.Optica = valor!;
                    break;
                case Vocabulario.Iluminacion:
                    toma.Iluminacion = valor!;
                    break;
                case Vocabulario.Angulo:
                    toma.Angulo = valor;
                    break;
                case Vocabulario.Movimiento:
                    toma.Movimiento = valor;
                    break;
            }
        }, ct);
    }

    public async Task<Resultado> EditarAccionAsync(int tomaId, string texto, CancellationToken ct = default)
    {
        var recortado = texto.Trim();
        if (recortado.Length == 0)
        {
            return Resultado.Rechazo(AccionVacia);
        }

        return await ModificarAsync(tomaId, toma => toma.Accion = recortado, ct);
    }

    private async Task<Resultado> ModificarAsync(int tomaId, Action<Toma> cambio, CancellationToken ct)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var toma = await db.Tomas.Include(t => t.Bloque).ThenInclude(b => b!.Clip).ThenInclude(c => c!.Escena)
            .SingleOrDefaultAsync(t => t.Id == tomaId, ct);
        if (toma?.Bloque?.Clip?.Escena is not { Estado: EstadoEscena.Generada })
        {
            return Resultado.Rechazo(SoloGenerada);
        }

        cambio(toma);
        await db.SaveChangesAsync(ct);
        return Resultado.Exito(tomaId);
    }

    private List<string> FueraDeVocabulario(Toma toma)
    {
        var valores = new (string Categoria, string? Valor)[]
        {
            (Vocabulario.Plano, toma.Plano),
            (Vocabulario.Optica, toma.Optica),
            (Vocabulario.Iluminacion, toma.Iluminacion),
            (Vocabulario.Angulo, toma.Angulo),
            (Vocabulario.Movimiento, toma.Movimiento),
        };
        return valores.Where(v => v.Valor is not null && !vocabulario.Contiene(v.Categoria, v.Valor)).Select(v => v.Categoria).ToList();
    }
}
