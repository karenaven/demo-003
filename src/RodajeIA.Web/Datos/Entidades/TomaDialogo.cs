namespace RodajeIA.Web.Datos.Entidades;

public class TomaDialogo
{
    public int TomaId { get; set; }

    public Toma? Toma { get; set; }

    public int LineaDialogoId { get; set; }

    public LineaDialogo? LineaDialogo { get; set; }
}
