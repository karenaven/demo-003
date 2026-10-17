namespace RodajeIA.Web.Guion;

/// <summary>Un error de validación del guion, con su línea física y motivo (contracts/guion.md).</summary>
public sealed record ErrorGuion(int Linea, string Codigo, string Motivo)
{
    public override string ToString() => $"Línea {Linea}: {Motivo}";
}
