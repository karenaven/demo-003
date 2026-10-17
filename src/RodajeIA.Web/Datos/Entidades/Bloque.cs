namespace RodajeIA.Web.Datos.Entidades;

public class Bloque
{
    public int Id { get; set; }

    /// <summary>Un bloque por clip.</summary>
    public int ClipId { get; set; }

    public Clip? Clip { get; set; }

    public List<Toma> Tomas { get; set; } = [];
}
