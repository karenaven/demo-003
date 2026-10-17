namespace RodajeIA.Web.Datos.Entidades;

public class PersonajeEnEscena
{
    public int EscenaId { get; set; }

    public Escena? Escena { get; set; }

    public int HojaId { get; set; }

    public HojaPersonaje? Hoja { get; set; }

    /// <summary>Orden de primera aparición en la escena (FR-025b).</summary>
    public int Orden { get; set; }
}
