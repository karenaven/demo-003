namespace RodajeIA.Web.Datos.Entidades;

public class Clip
{
    public int Id { get; set; }

    public int EscenaId { get; set; }

    public Escena? Escena { get; set; }

    /// <summary>1..20; único dentro de la escena.</summary>
    public int Numero { get; set; }

    /// <summary>Las líneas de acción del clip, unidas con <c>\n</c>, en orden.</summary>
    public string Accion { get; set; } = "";

    /// <summary>Solo el texto entre comillas.</summary>
    public string? TextoEnPantalla { get; set; }

    /// <summary>El texto con sus comillas originales (FR-026a).</summary>
    public string? TextoEnPantallaOriginal { get; set; }

    public List<LineaDialogo> Lineas { get; set; } = [];

    public List<PersonajeEnClip> Personajes { get; set; } = [];

    public Bloque? Bloque { get; set; }
}
