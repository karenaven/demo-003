using RodajeIA.Web.Guion;

namespace RodajeIA.Web.Servicios;

public abstract record ResultadoCarga
{
    public sealed record Guardado(int Escenas) : ResultadoCarga;

    public sealed record Rechazado(IReadOnlyList<ErrorGuion> Errores) : ResultadoCarga;

    public sealed record NoPermitido(string Motivo) : ResultadoCarga;
}
