namespace RodajeIA.Web.Datos.Entidades;

public class Serie
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public List<HojaPersonaje> Hojas { get; set; } = [];

    public List<Episodio> Episodios { get; set; } = [];
}
