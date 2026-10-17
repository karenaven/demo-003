using System.Text.Json.Nodes;
using RodajeIA.Web.Configuracion;

namespace RodajeIA.Web.Generacion;

/// <summary>
/// Schema de salida estructurada generado desde el vocabulario, con cada campo técnico como enum
/// (FR-015, constitución II). Ningún valor del vocabulario está escrito en el código.
/// </summary>
public sealed class SchemaSalida(Vocabulario vocabulario)
{
    private readonly JsonObject _schema = Construir(vocabulario);

    /// <summary>Una copia del schema; quien la reciba puede modificarla.</summary>
    public JsonObject Json => _schema.DeepClone().AsObject();

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
