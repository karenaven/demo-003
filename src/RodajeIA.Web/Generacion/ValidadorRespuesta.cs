using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Personajes;
using RodajeIA.Web.Prompt;

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
/// Valida el JSON crudo de la IA con las reglas 1 a 8 de contracts/gemini.md, en ese orden
/// (FR-017, FR-017a, FR-017b, RF-05a, RN-03, RN-08 a RN-10). Informa solo la primera regla que falla.
/// </summary>
public sealed partial class ValidadorRespuesta(Vocabulario vocabulario, MoldePrompt molde)
{
    private const string SinFormato = "la respuesta no tiene el formato pedido";

    /// <summary>Un fragmento de un campo de la hoja con menos palabras puede aparecer en una acción legítima.</summary>
    private const int MinimoPalabrasCopia = 3;

    /// <summary>Etiquetas de campo de la plantilla de personaje del molde ("Peinado:", "Rol:", …).</summary>
    private readonly IReadOnlyList<Regex> _etiquetas = Etiquetas(molde.PorPersonaje);

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

    /// <param name="hojas">Las hojas efectivas de los personajes presentes en la escena (reglas 7 y 8).</param>
    public ResultadoValidacion Validar(string json, IReadOnlyList<ClipEsperado> clips, IReadOnlyList<HojaEfectiva> hojas)
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

        // Regla 7 (RN-03): la acción no copia la hoja de un personaje; la hoja se inserta por plantilla.
        foreach (var bloque in ordenados)
        {
            foreach (var toma in bloque.Tomas)
            {
                if (CopiaDeHoja(toma.Accion, hojas) is { } copia)
                {
                    return Falla($"la acción de la toma {toma.Orden} del clip {bloque.Clip} copia {copia}");
                }
            }
        }

        // Regla 8 (RF-05a): los nombres de las hojas van en la acción tal cual la hoja, con sus tildes.
        foreach (var bloque in ordenados)
        {
            foreach (var toma in bloque.Tomas)
            {
                if (NombreMalEscrito(toma.Accion, hojas) is var (escrito, nombre))
                {
                    return Falla($"la acción de la toma {toma.Orden} del clip {bloque.Clip} escribe «{escrito}» en lugar de «{nombre}»");
                }
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

    /// <summary>
    /// Qué parte de una hoja aparece en la acción: una etiqueta de campo del molde, o una oración de
    /// un campo (sin el nombre), cortado en puntos, comas y puntos y coma, con al menos <see cref="MinimoPalabrasCopia"/> palabras. Nula si ninguna.
    /// </summary>
    private string? CopiaDeHoja(string accion, IReadOnlyList<HojaEfectiva> hojas)
    {
        var texto = accion.Normalize(NormalizationForm.FormC);
        if (_etiquetas.FirstOrDefault(e => e.IsMatch(texto)) is { } etiqueta)
        {
            return $"la etiqueta «{etiqueta.Match(texto).Value}» de la hoja";
        }

        foreach (var hoja in hojas)
        {
            foreach (var (campo, valor) in Campos(hoja))
            {
                var copiado = Fragmentos().Split(valor.Normalize(NormalizationForm.FormC))
                    .Select(f => f.Trim())
                    .Where(f => f.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= MinimoPalabrasCopia)
                    .Any(f => texto.Contains(f, StringComparison.OrdinalIgnoreCase));
                if (copiado)
                {
                    return $"{campo} de la hoja de {hoja.Nombre}";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// La primera palabra completa de la acción que coincide con un nombre de hoja si se ignoran las
    /// tildes, pero no está escrita igual (por ejemplo "Jose Daniel" por "José Daniel"). Las
    /// mayúsculas sí cuentan, para no confundir "la luz" con la hoja "Luz".
    /// </summary>
    private static (string Escrito, string Nombre)? NombreMalEscrito(string accion, IReadOnlyList<HojaEfectiva> hojas)
    {
        var texto = accion.Normalize(NormalizationForm.FormC);
        var textoSinTildes = SinTildes(texto);
        foreach (var hoja in hojas)
        {
            var nombre = hoja.Nombre.Normalize(NormalizationForm.FormC);
            var nombreSinTildes = SinTildes(nombre);
            for (var i = textoSinTildes.IndexOf(nombreSinTildes, StringComparison.Ordinal); i >= 0; i = textoSinTildes.IndexOf(nombreSinTildes, i + 1, StringComparison.Ordinal))
            {
                var fin = i + nombre.Length;
                var palabraCompleta = (i == 0 || !DetectorPersonajes.EsParteDePalabra(texto[i - 1]))
                    && (fin == texto.Length || !DetectorPersonajes.EsParteDePalabra(texto[fin]));
                var escrito = texto.Substring(i, nombre.Length);
                if (palabraCompleta && !string.Equals(escrito, nombre, StringComparison.Ordinal))
                {
                    return (escrito, hoja.Nombre);
                }
            }
        }

        return null;
    }

    /// <summary>Cada carácter NFC por su letra base; conserva el largo para mapear posiciones.</summary>
    private static string SinTildes(string nfc) =>
        string.Concat(nfc.Select(c => char.IsSurrogate(c) ? c : c.ToString().Normalize(NormalizationForm.FormD)[0]));

    private static IEnumerable<(string Campo, string Valor)> Campos(HojaEfectiva hoja)
    {
        yield return ("la edad", hoja.Edad);
        yield return ("la descripción física", hoja.DescripcionFisica);
        yield return ("el peinado", hoja.Peinado);
        yield return ("el vestuario", hoja.Vestuario);
        if (hoja.HeridasMarcas is { } heridas)
        {
            yield return ("las heridas/marcas", heridas);
        }

        yield return ("la personalidad", hoja.Personalidad);
        yield return ("el rol", hoja.Rol);
    }

    private static List<Regex> Etiquetas(PlantillaMolde porPersonaje) =>
        porPersonaje.Piezas
            .SelectMany(p => p is PlantillaMolde.Opcional o ? o.Piezas : [p])
            .OfType<PlantillaMolde.Texto>()
            .SelectMany(t => Etiqueta().Matches(t.Valor).Select(m => m.Groups[1].Value))
            .Distinct(StringComparer.Ordinal)
            .Select(e => new Regex($@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(e)}\s*:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .ToList();

    [GeneratedRegex(@"([\p{L}/]+):")]
    private static partial Regex Etiqueta();

    [GeneratedRegex(@"[.,;!?…]")]
    private static partial Regex Fragmentos();

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
