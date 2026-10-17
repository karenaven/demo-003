namespace RodajeIA.Web.Guion;

/// <summary>Una línea física del guion: su número (desde 1) y su texto recortado.</summary>
public readonly record struct LineaGuion(int Numero, string Texto)
{
    public bool EnBlanco => Texto.Length == 0;
}

/// <summary>
/// Lee el texto del guion según FR-008b: descarta un BOM inicial, separa por LF, CRLF o CR,
/// numera líneas físicas (las en blanco también cuentan) y recorta espacios y tabulaciones.
/// </summary>
public static class LectorLineas
{
    public const int MaximoCaracteres = 100_000;

    private static readonly char[] _espacios = [' ', '\t'];

    public static bool SuperaElMaximo(string texto) => texto.Length > MaximoCaracteres;

    public static IEnumerable<LineaGuion> Leer(string texto)
    {
        if (texto.Length > 0 && texto[0] == '﻿')
        {
            texto = texto[1..];
        }

        using var lector = new StringReader(texto);
        var numero = 0;
        while (lector.ReadLine() is { } linea)
        {
            numero++;
            yield return new LineaGuion(numero, linea.Trim(_espacios));
        }
    }
}
