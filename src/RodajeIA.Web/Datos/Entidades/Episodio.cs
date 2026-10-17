namespace RodajeIA.Web.Datos.Entidades;

public class Episodio
{
    public int Id { get; set; }

    public int SerieId { get; set; }

    public Serie? Serie { get; set; }

    /// <summary>Número único dentro de la serie (≥ 1). No se edita.</summary>
    public int Numero { get; set; }

    public required string Titulo { get; set; }

    /// <summary>El guion tal como se pegó; nulo hasta la primera carga válida.</summary>
    public string? Guion { get; set; }

    public List<Escena> Escenas { get; set; } = [];
}
