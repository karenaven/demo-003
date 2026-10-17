using System.Text.Json;
using System.Text.Json.Nodes;
using RodajeIA.Web.Configuracion;

namespace RodajeIA.Web.Generacion;

/// <summary>Un clip de la escena con los ids de sus líneas en el orden del guion.</summary>
public sealed record ClipEsperado(int Numero, IReadOnlyList<string> Lineas);

public sealed record TomaValidada(
    string Plano,
    string Optica,
    string Iluminacion,
    string? Angulo,
    string? Movimiento,
    string Accion,
    IReadOnlyList<string> Dialogos);

public sealed record BloqueValidado(int Clip, IReadOnlyList<TomaValidada> Tomas);

public abstract record ResultadoValidacion
{
    /// <summary>Bloques en orden de número de clip.</summary>
    public sealed record Valida(IReadOnlyList<BloqueValidado> Bloques) : ResultadoValidacion;

    /// <summary>El detalle de la primera regla incumplida (FR-017b).</summary>
    public sealed record Invalida(string Motivo) : ResultadoValidacion;
}

/// <summary>
/// Valida el JSON crudo de la IA con las reglas 1 a 6 de contracts/gemini.md, en ese orden
/// (FR-017, FR-017a, FR-017b, RN-08 a RN-10). Informa solo la primera regla que falla.
/// </summary>
public sealed class ValidadorRespuesta(Vocabulario vocabulario)
{
    private const string SinFormato = "la respuesta no tiene el formato pedido";

    private static readonly IReadOnlyDictionary<string, string> _nombres = new Dictionary<string, string>
    {
        [Vocabulario.Plano] = "plano",
        [Vocabulario.Optica] = "óptica",
        [Vocabulario.Iluminacion] = "iluminación",
        [Vocabulario.Angulo] = "ángulo",
        [Vocabulario.Movimiento] = "movimiento",
    };

    private sealed record TomaCruda(int Orden, IReadOnlyDictionary<string, string?> Campos, string Accion, IReadOnlyList<string> Dialogos);

    private sealed record BloqueCrudo(int Clip, IReadOnlyList<TomaCruda> Tomas);

    public ResultadoValidacion Validar(string json, IReadOnlyList<ClipEsperado> clips)
    {
        // Regla 1: formato.
        if (LeerEstructura(json) is not { } bloques)
        {
            return new ResultadoValidacion.Invalida(SinFormato);
        }

        // Regla 1b: campos técnicos obligatorios y dentro del vocabulario.
        foreach (var bloque in bloques)
        {
            foreach (var toma in bloque.Tomas)
            {
                foreach (var categoria in vocabulario.Categorias)
                {
                    var valor = toma.Campos[categoria.Nombre];
                    if (string.IsNullOrEmpty(valor))
                    {
                        if (categoria.Obligatorio)
                        {
                            return Falla($"la toma {toma.Orden} del clip {bloque.Clip} no tiene {_nombres[categoria.Nombre]}");
                        }
                    }
                    else if (!categoria.Contiene(valor))
                    {
                        return Falla($"el {_nombres[categoria.Nombre]} «{valor}» de la toma {toma.Orden} del clip {bloque.Clip} no está en el vocabulario");
                    }
                }
            }
        }

        // Regla 2 (RN-08): un bloque por clip, cada número una vez.
        var vistos = new HashSet<int>();
        foreach (var bloque in bloques)
        {
            if (!clips.Any(c => c.Numero == bloque.Clip))
            {
                return Falla($"el clip {bloque.Clip} no existe en la escena");
            }

            if (!vistos.Add(bloque.Clip))
            {
                return Falla($"el clip {bloque.Clip} aparece dos veces");
            }
        }

        foreach (var clip in clips.OrderBy(c => c.Numero))
        {
            if (!vistos.Contains(clip.Numero))
            {
                return Falla($"falta el bloque del clip {clip.Numero}");
            }
        }

        var ordenados = bloques.OrderBy(b => b.Clip).ToList();

        // Regla 3 (RN-10): al menos una toma por bloque y acción no vacía.
        foreach (var bloque in ordenados)
        {
            if (bloque.Tomas.Count == 0)
            {
                return Falla($"el bloque del clip {bloque.Clip} no tiene tomas");
            }

            foreach (var toma in bloque.Tomas.Where(t => t.Accion.Length == 0))
            {
                return Falla($"la toma {toma.Orden} del clip {bloque.Clip} no tiene acción");
            }
        }

        // Regla 4 (RN-09): cada línea del clip exactamente una vez, sin ids ajenos.
        foreach (var bloque in ordenados)
        {
            var lineas = clips.Single(c => c.Numero == bloque.Clip).Lineas;
            var referenciados = bloque.Tomas.SelectMany(t => t.Dialogos).ToList();
            if (referenciados.FirstOrDefault(id => !lineas.Contains(id)) is { } ajeno)
            {
                return Falla($"el id {ajeno} no es del clip {bloque.Clip}");
            }

            if (referenciados.GroupBy(id => id).FirstOrDefault(g => g.Count() > 1) is { } repetido)
            {
                return Falla($"el diálogo {repetido.Key} se usa dos veces");
            }

            if (lineas.FirstOrDefault(id => !referenciados.Contains(id)) is { } faltante)
            {
                return Falla($"falta el diálogo {faltante} en el clip {bloque.Clip}");
            }
        }

        // Regla 5 (FR-017a): recorriendo las tomas, el orden de las líneas no retrocede.
        foreach (var bloque in ordenados)
        {
            var lineas = clips.Single(c => c.Numero == bloque.Clip).Lineas.ToList();
            var maximoAnterior = -1;
            foreach (var toma in bloque.Tomas)
            {
                if (toma.Dialogos.Count == 0)
                {
                    continue;
                }

                var posiciones = toma.Dialogos.Select(id => lineas.IndexOf(id)).ToList();
                if (posiciones.Min() < maximoAnterior)
                {
                    return Falla($"diálogos fuera de orden en el clip {bloque.Clip}");
                }

                maximoAnterior = posiciones.Max();
            }
        }

        // Regla 6 (FR-017a): la acción no contiene comillas.
        foreach (var bloque in ordenados)
        {
            foreach (var toma in bloque.Tomas.Where(t => t.Accion.IndexOfAny(['"', '“', '”']) >= 0))
            {
                return Falla($"la acción de la toma {toma.Orden} del clip {bloque.Clip} tiene comillas");
            }
        }

        return new ResultadoValidacion.Valida(ordenados.Select(b => new BloqueValidado(
            b.Clip,
            b.Tomas.Select(t => new TomaValidada(
                t.Campos[Vocabulario.Plano]!,
                t.Campos[Vocabulario.Optica]!,
                t.Campos[Vocabulario.Iluminacion]!,
                NullSiVacio(t.Campos[Vocabulario.Angulo]),
                NullSiVacio(t.Campos[Vocabulario.Movimiento]),
                t.Accion,
                t.Dialogos)).ToList())).ToList());
    }

    private static ResultadoValidacion.Invalida Falla(string motivo) => new(motivo);

    private static string? NullSiVacio(string? valor) => string.IsNullOrEmpty(valor) ? null : valor;

    /// <summary>Regla 1: tipos correctos, sin campos desconocidos y con los requeridos presentes.</summary>
    private List<BloqueCrudo>? LeerEstructura(string json)
    {
        JsonNode? raiz;
        try
        {
            raiz = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (raiz is not JsonObject objeto || !SoloCampos(objeto, ["bloques"]) || objeto["bloques"] is not JsonArray bloquesNodo)
        {
            return null;
        }

        var camposToma = vocabulario.Categorias.Select(c => c.Nombre).Append("accion").Append("dialogos").ToList();
        var bloques = new List<BloqueCrudo>();
        foreach (var bloqueNodo in bloquesNodo)
        {
            if (bloqueNodo is not JsonObject bloque
                || !SoloCampos(bloque, ["clip", "tomas"])
                || bloque["clip"] is not JsonValue clipNodo
                || !clipNodo.TryGetValue(out int clip)
                || bloque["tomas"] is not JsonArray tomasNodo)
            {
                return null;
            }

            var tomas = new List<TomaCruda>();
            foreach (var tomaNodo in tomasNodo)
            {
                if (tomaNodo is not JsonObject toma
                    || !SoloCampos(toma, camposToma)
                    || toma["accion"] is not JsonValue accionNodo
                    || !accionNodo.TryGetValue(out string? accion)
                    || toma["dialogos"] is not JsonArray dialogosNodo)
                {
                    return null;
                }

                var campos = new Dictionary<string, string?>();
                foreach (var categoria in vocabulario.Categorias)
                {
                    switch (toma[categoria.Nombre])
                    {
                        case null:
                            campos[categoria.Nombre] = null;
                            break;
                        case JsonValue v when v.TryGetValue(out string? texto):
                            campos[categoria.Nombre] = texto;
                            break;
                        default:
                            return null;
                    }
                }

                var dialogos = new List<string>();
                foreach (var d in dialogosNodo)
                {
                    if (d is not JsonValue dv || !dv.TryGetValue(out string? id))
                    {
                        return null;
                    }

                    dialogos.Add(id);
                }

                tomas.Add(new TomaCruda(tomas.Count + 1, campos, accion.Trim(), dialogos));
            }

            bloques.Add(new BloqueCrudo(clip, tomas));
        }

        return bloques;
    }

    private static bool SoloCampos(JsonObject objeto, IReadOnlyCollection<string> permitidos) =>
        objeto.All(p => permitidos.Contains(p.Key));
}
