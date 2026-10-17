namespace RodajeIA.Web.Datos.Entidades;

public class PersonajeEnClip
{
    public int ClipId { get; set; }

    public Clip? Clip { get; set; }

    public int HojaId { get; set; }

    public HojaPersonaje? Hoja { get; set; }
}
