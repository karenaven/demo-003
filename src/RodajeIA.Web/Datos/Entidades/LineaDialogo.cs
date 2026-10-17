using System.ComponentModel.DataAnnotations.Schema;

namespace RodajeIA.Web.Datos.Entidades;

public class LineaDialogo
{
    public int Id { get; set; }

    public int ClipId { get; set; }

    public Clip? Clip { get; set; }

    /// <summary>1..n dentro del clip.</summary>
    public int Posicion { get; set; }

    /// <summary>El marcador tal como está escrito (<c>ANA</c>, <c>MESERO</c>).</summary>
    public required string Personaje { get; set; }

    public string? Acotacion { get; set; }

    /// <summary>Sin las comillas.</summary>
    public required string Texto { get; set; }

    /// <summary>La línea completa del guion, sin espacios al inicio ni al final (FR-026a).</summary>
    public required string Original { get; set; }

    /// <summary>Identificador que recibe y devuelve la IA (FR-016a).</summary>
    [NotMapped]
    public string IdIa => IdIaDe(Clip?.Numero ?? throw new InvalidOperationException("Falta cargar el clip."), Posicion);

    public static string IdIaDe(int numeroClip, int posicion) => $"c{numeroClip}-d{posicion}";
}
