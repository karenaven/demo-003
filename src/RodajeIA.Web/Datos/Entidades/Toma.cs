namespace RodajeIA.Web.Datos.Entidades;

public class Toma
{
    public int Id { get; set; }

    public int BloqueId { get; set; }

    public Bloque? Bloque { get; set; }

    /// <summary>1..n dentro del bloque, en el orden de la respuesta.</summary>
    public int Orden { get; set; }

    public required string Plano { get; set; }

    public required string Optica { get; set; }

    public required string Iluminacion { get; set; }

    public string? Angulo { get; set; }

    public string? Movimiento { get; set; }

    /// <summary>No vacía; sin espacios al inicio ni al final.</summary>
    public required string Accion { get; set; }

    public List<TomaDialogo> Dialogos { get; set; } = [];
}
