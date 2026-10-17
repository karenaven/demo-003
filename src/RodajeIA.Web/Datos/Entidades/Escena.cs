namespace RodajeIA.Web.Datos.Entidades;

public enum IntExt
{
    Int,
    Ext,
}

public enum EstadoEscena
{
    Pendiente,
    Generando,
    Generada,
    Error,
}

public class Escena
{
    public int Id { get; set; }

    public int EpisodioId { get; set; }

    public Episodio? Episodio { get; set; }

    /// <summary>1..10; único dentro del episodio.</summary>
    public int Numero { get; set; }

    public IntExt IntExt { get; set; }

    public required string Lugar { get; set; }

    public required string MomentoDia { get; set; }

    public required string Locacion { get; set; }

    /// <summary>Iluminación base de la escena.</summary>
    public required string Iluminacion { get; set; }

    public required string PuestaEnEscena { get; set; }

    public required string Audio { get; set; }

    public EstadoEscena Estado { get; set; } = EstadoEscena.Pendiente;

    /// <summary>Solo en <see cref="EstadoEscena.Error"/>; se borra al salir de ese estado (FR-019a).</summary>
    public string? MotivoError { get; set; }

    public List<Clip> Clips { get; set; } = [];

    public List<PersonajeEnEscena> Personajes { get; set; } = [];

    public List<VarianteEscena> Variantes { get; set; } = [];
}
