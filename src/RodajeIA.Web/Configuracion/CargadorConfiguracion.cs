using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using RodajeIA.Web.Prompt;

namespace RodajeIA.Web.Configuracion;

/// <summary>
/// Lee y valida los cuatro archivos de <c>config/</c> según contracts/config.md. Junta todos los
/// problemas y, si hay alguno, lanza <see cref="ConfiguracionInvalidaException"/>.
/// </summary>
public static partial class CargadorConfiguracion
{
    public const string ArchivoVocabulario = "vocabulario.json";
    public const string ArchivoEstilo = "estilo.txt";
    public const string ArchivoInstruccion = "instruccion-generacion.txt";
    public const string ArchivoMolde = "molde-prompt.json";

    public static string DirectorioPorDefecto => Path.Combine(AppContext.BaseDirectory, "config");

    public static ConfiguracionRodaje Cargar(string directorio)
    {
        var problemas = new List<string>();

        var vocabulario = CargarVocabulario(Path.Combine(directorio, ArchivoVocabulario), problemas);
        var estilo = CargarEstilo(Path.Combine(directorio, ArchivoEstilo), problemas);
        var instruccion = CargarInstruccion(Path.Combine(directorio, ArchivoInstruccion), problemas);
        var molde = CargarMolde(Path.Combine(directorio, ArchivoMolde), problemas);

        if (problemas.Count > 0)
        {
            throw new ConfiguracionInvalidaException(problemas);
        }

        return new ConfiguracionRodaje(vocabulario!, estilo!, instruccion!, molde!);
    }

    private static Vocabulario? CargarVocabulario(string ruta, List<string> problemas)
    {
        var raiz = LeerJson(ruta, ArchivoVocabulario, problemas);
        if (raiz is null)
        {
            return null;
        }

        void Problema(string texto) => problemas.Add($"{ArchivoVocabulario}: {texto}");

        if (raiz["categorias"] is not JsonObject categorias)
        {
            Problema("falta 'categorias'");
            return null;
        }

        var antes = problemas.Count;
        foreach (var (nombre, _) in categorias)
        {
            if (!Vocabulario.NombresCategorias.Contains(nombre))
            {
                Problema($"categoría desconocida {nombre}");
            }
        }

        var resultado = new List<CategoriaVocabulario>();
        foreach (var nombre in Vocabulario.NombresCategorias)
        {
            if (categorias[nombre] is not JsonObject categoria)
            {
                Problema($"falta la categoría {nombre}");
                continue;
            }

            if (categoria["obligatorio"] is not JsonValue obligatorioNodo || !obligatorioNodo.TryGetValue(out bool obligatorio))
            {
                Problema($"la categoría {nombre} no tiene 'obligatorio'");
                continue;
            }

            var debeSerObligatoria = Vocabulario.CategoriasObligatorias.Contains(nombre);
            if (obligatorio != debeSerObligatoria)
            {
                Problema(debeSerObligatoria ? $"{nombre} debe ser obligatoria" : $"{nombre} debe ser opcional");
            }

            if (categoria["valores"] is not JsonArray valoresNodo)
            {
                Problema($"la categoría {nombre} no tiene 'valores'");
                continue;
            }

            var valores = new List<string>();
            foreach (var valorNodo in valoresNodo)
            {
                if (valorNodo is not JsonValue v || !v.TryGetValue(out string? valor) || string.IsNullOrWhiteSpace(valor))
                {
                    Problema($"valor vacío en {nombre}");
                    continue;
                }

                if (valores.Contains(valor, StringComparer.Ordinal))
                {
                    Problema($"valor repetido '{valor}' en {nombre}");
                    continue;
                }

                valores.Add(valor);
            }

            if (valores.Count == 0 && valoresNodo.Count == 0)
            {
                Problema($"la categoría {nombre} no tiene valores");
            }

            resultado.Add(new CategoriaVocabulario(nombre, obligatorio, valores));
        }

        return problemas.Count == antes ? new Vocabulario(resultado) : null;
    }

    private static EstiloGlobal? CargarEstilo(string ruta, List<string> problemas)
    {
        var texto = LeerTexto(ruta, ArchivoEstilo, problemas);
        return texto is null ? null : new EstiloGlobal(texto);
    }

    private static InstruccionGeneracion? CargarInstruccion(string ruta, List<string> problemas)
    {
        var texto = LeerTexto(ruta, ArchivoInstruccion, problemas);
        if (texto is null)
        {
            return null;
        }

        var antes = problemas.Count;
        foreach (var marcador in new[] { InstruccionGeneracion.MarcadorEscena, InstruccionGeneracion.MarcadorPersonajes })
        {
            var veces = Regex.Count(texto, Regex.Escape(marcador));
            if (veces == 0)
            {
                problemas.Add($"{ArchivoInstruccion}: falta el marcador {marcador}");
            }
            else if (veces > 1)
            {
                problemas.Add($"{ArchivoInstruccion}: el marcador {marcador} aparece más de una vez");
            }
        }

        foreach (var marcador in MarcadorRegex().Matches(texto).Select(m => m.Value).Distinct())
        {
            if (marcador != InstruccionGeneracion.MarcadorEscena && marcador != InstruccionGeneracion.MarcadorPersonajes)
            {
                problemas.Add($"{ArchivoInstruccion}: marcador desconocido {marcador}");
            }
        }

        return problemas.Count == antes ? new InstruccionGeneracion(texto) : null;
    }

    private static MoldePrompt? CargarMolde(string ruta, List<string> problemas)
    {
        var raiz = LeerJson(ruta, ArchivoMolde, problemas);
        if (raiz is null)
        {
            return null;
        }

        var antes = problemas.Count;
        void Problema(string texto) => problemas.Add($"{ArchivoMolde}: {texto}");

        if (raiz["secciones"] is not JsonArray seccionesNodo)
        {
            Problema("falta 'secciones'");
            return null;
        }

        var secciones = seccionesNodo.OfType<JsonObject>().ToList();
        var ids = secciones.Select(s => (string?)s["id"]).ToList();
        foreach (var id in MoldePrompt.OrdenSecciones)
        {
            if (!ids.Contains(id))
            {
                Problema($"falta la sección {id}");
            }
        }

        foreach (var id in ids.Where(id => id is not null && !MoldePrompt.OrdenSecciones.Contains(id)))
        {
            Problema($"sección desconocida {id}");
        }

        if (problemas.Count > antes)
        {
            return null;
        }

        if (!ids.SequenceEqual(MoldePrompt.OrdenSecciones))
        {
            Problema("sección fuera de orden");
            return null;
        }

        JsonObject Seccion(string id) => secciones.First(s => (string?)s["id"] == id);

        string Cadena(JsonObject seccion, string campo)
        {
            if (seccion[campo] is JsonValue v && v.TryGetValue(out string? texto))
            {
                return texto;
            }

            Problema($"falta '{campo}' en la sección {(string?)seccion["id"]}");
            return "";
        }

        PlantillaMolde Plantilla(JsonObject seccion, string campo)
        {
            var plantilla = PlantillaMolde.Analizar(Cadena(seccion, campo), out var error);
            if (error is not null)
            {
                Problema($"{error} en '{campo}'");
            }

            foreach (var variable in plantilla.Variables.Distinct())
            {
                if (!MoldePrompt.VariablesPermitidas.Contains(variable))
                {
                    Problema($"variable desconocida {{{{{variable}}}}}");
                }
            }

            return plantilla;
        }

        var locacion = Seccion("locacionEIluminacionBase");
        var lineas = new List<PlantillaMolde>();
        if (locacion["lineas"] is JsonArray lineasNodo)
        {
            for (var i = 0; i < lineasNodo.Count; i++)
            {
                var plantilla = PlantillaMolde.Analizar((string?)lineasNodo[i] ?? "", out var error);
                if (error is not null)
                {
                    Problema($"{error} en 'lineas'");
                }

                lineas.Add(plantilla);
            }
        }
        else
        {
            Problema("falta 'lineas' en la sección locacionEIluminacionBase");
        }

        var personajes = Seccion("personajes");
        var puesta = Seccion("puestaEnEscena");
        var audio = Seccion("audio");
        var bloques = Seccion("bloques");

        var moldeBloques = new MoldeBloques(
            Cadena(bloques, "separadorBloques"),
            Plantilla(bloques, "tituloBloque"),
            Plantilla(bloques, "toma"),
            Cadena(bloques, "separadorTomas"),
            Plantilla(bloques, "lineaDialogo"),
            Cadena(bloques, "separadorDialogos"),
            Plantilla(bloques, "cierreConTextoEnPantalla"),
            Plantilla(bloques, "cierreSinTextoEnPantalla"));

        var molde = new MoldePrompt(
            (string?)raiz["separadorSecciones"] ?? "\n\n",
            Plantilla(Seccion("estilo"), "contenido"),
            lineas,
            Cadena(personajes, "titulo"),
            Plantilla(personajes, "porPersonaje"),
            Cadena(puesta, "titulo"),
            Plantilla(puesta, "contenido"),
            Cadena(audio, "titulo"),
            Plantilla(audio, "contenido"),
            moldeBloques);

        if (!moldeBloques.TituloBloque.Fuente.Contains(MoldePrompt.ContratoTituloBloque, StringComparison.Ordinal))
        {
            Problema("falta la cadena de contrato 'Bloque N:'");
        }

        if (!moldeBloques.SeparadorTomas.Contains(MoldePrompt.ContratoCorte, StringComparison.Ordinal))
        {
            Problema("falta la cadena de contrato 'Corte a'");
        }

        if (moldeBloques.CierreSinTextoEnPantalla.Fuente != MoldePrompt.ContratoSinTexto)
        {
            Problema("falta la cadena de contrato 'Sin subtítulos ni texto en pantalla.'");
        }

        return problemas.Count == antes ? molde : null;
    }

    private static string? LeerTexto(string ruta, string archivo, List<string> problemas)
    {
        if (!File.Exists(ruta))
        {
            problemas.Add($"{archivo}: no existe");
            return null;
        }

        var texto = File.ReadAllText(ruta).Trim();
        if (texto.Length == 0)
        {
            problemas.Add($"{archivo}: está vacío");
            return null;
        }

        return texto;
    }

    private static JsonNode? LeerJson(string ruta, string archivo, List<string> problemas)
    {
        if (!File.Exists(ruta))
        {
            problemas.Add($"{archivo}: no existe");
            return null;
        }

        try
        {
            return JsonNode.Parse(File.ReadAllText(ruta)) ?? throw new JsonException("vacío");
        }
        catch (JsonException ex)
        {
            var linea = ex.LineNumber is long l ? $" (línea {l + 1})" : "";
            problemas.Add($"{archivo}: no es JSON válido{linea}");
            return null;
        }
    }

    [GeneratedRegex(@"\{\{[^{}]*\}\}")]
    private static partial Regex MarcadorRegex();
}
