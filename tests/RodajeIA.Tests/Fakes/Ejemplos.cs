using System.Text;
using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Tests.Fakes;

/// <summary>Datos de ejemplo de los criterios de aceptación (PRD Anexo A y Anexo B).</summary>
public static class Ejemplos
{
    /// <summary>La escena válida del Anexo A, tal cual.</summary>
    public const string EscenaAnexoA = """
        ESCENA 1 — INT. RESTAURANTE — NOCHE
        LOCACIÓN: Restaurante pequeño y acogedor; pared de ladrillo visto, mesas de madera oscura, vela en portavelas de cristal.
        ILUMINACIÓN: Luz cálida ambiental (aprox. 2700 K), principal suave cenital de la lámpara sobre la mesa, reflejo de la vela en los rostros.
        PUESTA EN ESCENA: Sentados frente a frente en una mesa para dos. Ana siempre a la IZQUIERDA del encuadre, José Daniel siempre a la DERECHA.
        AUDIO: Ambiente tranquilo de restaurante, murmullo lejano y cubiertos; SIN música. Diálogo en español, acento latinoamericano neutro.
        CLIP 1
        Ana y José Daniel conversan en una mesa junto a la ventana. José Daniel sonríe pícaramente.
        ANA: "Mentiroso."
        JOSÉ DANIEL (sonriendo): "Bueno... pasó parecido."
        Ana niega con la cabeza, divertida, con una sonrisa amplia.
        CLIP 2
        Un mesero deja dos platos sobre la mesa. Ana y José Daniel agradecen con gestos breves.
        """;

    public static HojaPersonaje HojaAna(int serieId = 0) => new()
    {
        SerieId = serieId,
        Nombre = "Ana",
        NombreClave = "ANA",
        Edad = "32 años",
        DescripcionFisica = "Mujer de piel morena, ojos oscuros y rasgos finos; voz cálida y pausada",
        Peinado = "Pelo negro largo, suelto",
        Vestuario = "chaqueta negra",
        HeridasMarcas = null,
        Personalidad = "Irónica y observadora",
        Rol = "Protagonista",
    };

    public static HojaPersonaje HojaJoseDaniel(int serieId = 0) => new()
    {
        SerieId = serieId,
        Nombre = "José Daniel",
        NombreClave = "JOSÉ DANIEL",
        Edad = "35 años",
        DescripcionFisica = "Hombre alto, de barba corta y ojos claros; voz grave",
        Peinado = "Pelo castaño corto",
        Vestuario = "camisa blanca",
        HeridasMarcas = "cicatriz pequeña en el mentón",
        Personalidad = "Encantador y evasivo",
        Rol = "Coprotagonista",
    };

    public static HojaPersonaje Hoja(string nombre, int serieId = 0) => new()
    {
        SerieId = serieId,
        Nombre = nombre,
        NombreClave = HojaPersonaje.ClaveDe(nombre),
        Edad = "30 años",
        DescripcionFisica = "Descripción de " + nombre,
        Peinado = "corto",
        Vestuario = "jean",
        Personalidad = "tranquila",
        Rol = "secundario",
    };

    /// <summary>Un guion válido de <paramref name="escenas"/> escenas con <paramref name="clips"/> clips cada una.</summary>
    public static string Guion(int escenas, int clips, int dialogosPorClip = 1)
    {
        var texto = new StringBuilder();
        for (var e = 1; e <= escenas; e++)
        {
            texto.AppendLine($"ESCENA {e} — INT. RESTAURANTE — NOCHE");
            texto.AppendLine("LOCACIÓN: Restaurante pequeño.");
            texto.AppendLine("ILUMINACIÓN: Luz cálida.");
            texto.AppendLine("PUESTA EN ESCENA: Ana a la izquierda, José Daniel a la derecha.");
            texto.AppendLine("AUDIO: Murmullo lejano.");
            for (var c = 1; c <= clips; c++)
            {
                texto.AppendLine($"CLIP {c}");
                texto.AppendLine($"Ana mira a José Daniel en el clip {c}.");
                for (var d = 1; d <= dialogosPorClip; d++)
                {
                    texto.AppendLine(d % 2 == 1 ? $"ANA: \"Línea {d} del clip {c}.\"" : $"JOSÉ DANIEL (serio): \"Línea {d} del clip {c}.\"");
                }
            }
        }

        return texto.ToString();
    }
}
