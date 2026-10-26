using System.Text.Json.Nodes;
using RodajeIA.Web.Configuracion;

namespace RodajeIA.Web.Generacion;

/// <summary>
/// Schema de salida estructurada generado desde el vocabulario, con cada campo técnico como enum
/// (FR-015, constitución II). Ningún valor del vocabulario está escrito en el código. Se arma por
/// llamada: el número de clip queda acotado a los clips de la escena y los diálogos son un enum
/// con los ids de sus líneas (FR-016a), para que la IA no pueda devolver el texto en vez del id.
/// </summary>
public sealed class SchemaSalida(Vocabulario vocabulario)
{
    private readonly JsonObject _base = Construir(vocabulario);

    /// <summary>Un schema nuevo para la escena con estos clips; quien lo reciba puede modificarlo.</summary>
    public JsonObject Para(IReadOnlyList<ClipEsperado> clips)
    {
        var schema = _base.DeepClone().AsObject();
        // Sin minItems/maxItems en "bloques": Gemini responde HTTP 400 con escenas de 20 clips.
        // La cantidad de bloques la controla la validación (RN-08).
        var bloque = schema["properties"]!["bloques"]!["items"]!["properties"]!.AsObject();
        bloque["clip"] = new JsonObject
        {
            ["type"] = "integer",
            ["minimum"] = clips.Min(c => c.Numero),
            ["maximum"] = clips.Max(c => c.Numero),
        };

        var ids = clips.SelectMany(c => c.Lineas).Select(id => (JsonNode)id).ToArray();
        var item = new JsonObject { ["type"] = "string" };
        var dialogos = new JsonObject { ["type"] = "array", ["items"] = item };
        if (ids.Length == 0)
        {
            dialogos["maxItems"] = 0;
        }
        else
        {
            item["enum"] = new JsonArray(ids);
        }

        bloque["tomas"]!["items"]!["properties"]!["dialogos"] = dialogos;
        return schema;
    }

    private static JsonObject Construir(Vocabulario vocabulario)
    {
        var propiedades = new JsonObject();
        var requeridos = new JsonArray();
        foreach (var categoria in vocabulario.Categorias)
        {
            propiedades[categoria.Nombre] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray(categoria.Valores.Select(v => (JsonNode)v).ToArray()),
            };
            if (categoria.Obligatorio)
            {
                requeridos.Add(categoria.Nombre);
            }
        }

        propiedades["accion"] = new JsonObject { ["type"] = "string" };
        propiedades["dialogos"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "string" } };
        requeridos.Add("accion");
        requeridos.Add("dialogos");

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["bloques"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["clip"] = new JsonObject { ["type"] = "integer" },
                            ["tomas"] = new JsonObject
                            {
                                ["type"] = "array",
                                ["minItems"] = 1,
                                ["items"] = new JsonObject
                                {
                                    ["type"] = "object",
                                    ["properties"] = propiedades,
                                    ["required"] = requeridos,
                                },
                            },
                        },
                        ["required"] = new JsonArray("clip", "tomas"),
                    },
                },
            },
            ["required"] = new JsonArray("bloques"),
        };
    }
}
