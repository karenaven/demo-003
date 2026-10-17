namespace RodajeIA.Web.Configuracion;

/// <summary>Plantilla de <c>config/instruccion-generacion.txt</c> con <c>{{escena}}</c> y <c>{{personajes}}</c>.</summary>
public sealed record InstruccionGeneracion(string Plantilla)
{
    public const string MarcadorEscena = "{{escena}}";
    public const string MarcadorPersonajes = "{{personajes}}";

    public string Armar(string escena, string personajes) =>
        Plantilla.Replace(MarcadorEscena, escena, StringComparison.Ordinal)
                 .Replace(MarcadorPersonajes, personajes, StringComparison.Ordinal);
}
