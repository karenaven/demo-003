namespace RodajeIA.Web.Generacion;

/// <summary>Motivos cortos, en lenguaje simple, que se guardan con una escena en "error" (FR-019a).</summary>
public static class MotivosGeneracion
{
    public const string Cierre = "Generación interrumpida por un cierre de la aplicación.";

    public static string Para(CategoriaFalla categoria, string? detalle) => categoria switch
    {
        CategoriaFalla.Timeout => "La IA no respondió a tiempo.",
        CategoriaFalla.ErrorIa => Con("La IA devolvió un error o no está disponible", detalle),
        CategoriaFalla.RespuestaInvalida => Con("La respuesta no cumplió las reglas", detalle),
        CategoriaFalla.Configuracion => Con("Error de configuración de la IA", detalle),
        _ => "Error interno.",
    };

    private static string Con(string categoria, string? detalle) =>
        string.IsNullOrWhiteSpace(detalle) ? categoria + "." : $"{categoria}: {detalle.TrimEnd('.')}.";
}
