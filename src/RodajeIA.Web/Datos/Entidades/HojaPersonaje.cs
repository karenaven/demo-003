namespace RodajeIA.Web.Datos.Entidades;

public class HojaPersonaje
{
    public int Id { get; set; }

    public int SerieId { get; set; }

    public Serie? Serie { get; set; }

    /// <summary>Tal como se escribe en la acción: mayúscula inicial y tildes.</summary>
    public required string Nombre { get; set; }

    /// <summary><c>Nombre.ToUpperInvariant()</c>; único dentro de la serie (FR-002a).</summary>
    public required string NombreClave { get; set; }

    public required string Edad { get; set; }

    /// <summary>Incluye la voz.</summary>
    public required string DescripcionFisica { get; set; }

    public required string Peinado { get; set; }

    public required string Vestuario { get; set; }

    public string? HeridasMarcas { get; set; }

    public required string Personalidad { get; set; }

    public required string Rol { get; set; }

    public static string ClaveDe(string nombre) => nombre.Trim().ToUpperInvariant();
}
