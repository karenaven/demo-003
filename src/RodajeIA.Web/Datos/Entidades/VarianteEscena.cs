namespace RodajeIA.Web.Datos.Entidades;

/// <summary>Variante de personaje: reemplazos de la hoja base para una escena.</summary>
public class VarianteEscena
{
    public int Id { get; set; }

    public int EscenaId { get; set; }

    public Escena? Escena { get; set; }

    public int HojaId { get; set; }

    public HojaPersonaje? Hoja { get; set; }

    /// <summary>Nulo = no reemplaza.</summary>
    public string? Edad { get; set; }

    /// <summary>Nulo = no reemplaza.</summary>
    public string? Peinado { get; set; }

    /// <summary>Nulo = no reemplaza.</summary>
    public string? Vestuario { get; set; }

    /// <summary>Si es verdadero, reemplaza heridas/marcas con <see cref="HeridasMarcas"/>.</summary>
    public bool ReemplazaHeridas { get; set; }

    /// <summary>Con <see cref="ReemplazaHeridas"/>, nulo o vacío significa "sin heridas".</summary>
    public string? HeridasMarcas { get; set; }
}
