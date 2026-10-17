using RodajeIA.Web.Prompt;

namespace RodajeIA.Web.Configuracion;

/// <summary>Plantillas de la sección de bloques del molde.</summary>
public sealed record MoldeBloques(
    string SeparadorBloques,
    PlantillaMolde TituloBloque,
    PlantillaMolde Toma,
    string SeparadorTomas,
    PlantillaMolde LineaDialogo,
    string SeparadorDialogos,
    PlantillaMolde CierreConTextoEnPantalla,
    PlantillaMolde CierreSinTextoEnPantalla);

/// <summary>
/// Molde del prompt final de <c>config/molde-prompt.json</c>, fuente normativa de su redacción
/// (FR-025a). Las seis secciones van siempre en este orden.
/// </summary>
public sealed record MoldePrompt(
    string SeparadorSecciones,
    PlantillaMolde Estilo,
    IReadOnlyList<PlantillaMolde> LocacionEIluminacionBase,
    string TituloPersonajes,
    PlantillaMolde PorPersonaje,
    string TituloPuestaEnEscena,
    PlantillaMolde PuestaEnEscena,
    string TituloAudio,
    PlantillaMolde Audio,
    MoldeBloques Bloques)
{
    public static readonly IReadOnlyList<string> OrdenSecciones =
        ["estilo", "locacionEIluminacionBase", "personajes", "puestaEnEscena", "audio", "bloques"];

    /// <summary>Variables que puede usar el molde (contracts/prompt.md).</summary>
    public static readonly IReadOnlySet<string> VariablesPermitidas = new HashSet<string>(StringComparer.Ordinal)
    {
        "estilo.texto",
        "escena.locacion", "escena.iluminacion", "escena.puestaEnEscena", "escena.audio",
        "personaje.nombre", "personaje.edad", "personaje.descripcionFisica", "personaje.peinado",
        "personaje.vestuario", "personaje.heridas", "personaje.personalidad", "personaje.rol",
        "bloque.numero",
        "toma.plano", "toma.optica", "toma.iluminacion", "toma.angulo", "toma.movimiento",
        "toma.accion", "toma.dialogos",
        "linea.original",
        "clip.textoEnPantallaOriginal",
    };

    public const string ContratoTituloBloque = "Bloque {{bloque.numero}}:";
    public const string ContratoCorte = "Corte a";
    public const string ContratoSinTexto = "Sin subtítulos ni texto en pantalla.";
}
