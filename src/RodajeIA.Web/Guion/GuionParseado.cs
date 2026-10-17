using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Web.Guion;

public sealed record LineaAccionParseada(int Linea, string Texto);

public sealed record LineaDialogoParseada(int Linea, int Posicion, string Personaje, string? Acotacion, string Texto, string Original);

public sealed record TextoEnPantallaParseado(int Linea, string Texto, string Original);

public sealed record ClipParseado(
    int Linea,
    int Numero,
    IReadOnlyList<LineaAccionParseada> Acciones,
    IReadOnlyList<LineaDialogoParseada> Dialogos,
    TextoEnPantallaParseado? TextoEnPantalla)
{
    /// <summary>Las líneas de acción unidas con <c>\n</c>, en orden.</summary>
    public string Accion => string.Join('\n', Acciones.Select(a => a.Texto));
}

public sealed record EscenaParseada(
    int Linea,
    int Numero,
    IntExt IntExt,
    string Lugar,
    string MomentoDia,
    string Locacion,
    string Iluminacion,
    string PuestaEnEscena,
    string Audio,
    IReadOnlyList<ClipParseado> Clips);

public sealed record GuionParseado(IReadOnlyList<EscenaParseada> Escenas);

/// <summary>El resultado del parser: un guion completo o la lista de todos sus errores, nunca un resultado parcial.</summary>
public abstract record ResultadoGuion
{
    public sealed record Valido(GuionParseado Guion) : ResultadoGuion;

    public sealed record Invalido(IReadOnlyList<ErrorGuion> Errores) : ResultadoGuion;
}
