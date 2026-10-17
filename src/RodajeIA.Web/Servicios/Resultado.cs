namespace RodajeIA.Web.Servicios;

/// <summary>Resultado de una operación de la UI: o se hizo (con un aviso opcional) o se rechazó con un motivo.</summary>
public sealed record Resultado(bool Ok, string? Motivo = null, string? Aviso = null, int? Id = null)
{
    public static Resultado Exito(int? id = null, string? aviso = null) => new(true, null, aviso, id);

    public static Resultado Rechazo(string motivo) => new(false, motivo);
}
