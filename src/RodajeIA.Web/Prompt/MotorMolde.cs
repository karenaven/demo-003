using System.Text;

namespace RodajeIA.Web.Prompt;

/// <summary>
/// Intérprete del molde (contracts/prompt.md): variables literales, opcionales <c>[ … ]</c> y la
/// regla de puntuación de FR-026. Los valores se insertan sin modificarse.
/// </summary>
public static class MotorMolde
{
    private static readonly char[] _puntuacionFinal = ['.', '!', '?', '…'];

    public static string Renderizar(PlantillaMolde plantilla, IReadOnlyDictionary<string, string?> variables)
    {
        var salida = new StringBuilder();
        string? ultimoValor = null;
        Escribir(plantilla.Piezas, variables, salida, ref ultimoValor);
        return salida.ToString();
    }

    /// <summary>Une las partes no vacías con el separador, nunca al principio ni al final.</summary>
    public static string Unir(string separador, IEnumerable<string> partes) =>
        string.Join(separador, partes.Where(p => p.Length > 0));

    private static void Escribir(
        IReadOnlyList<PlantillaMolde.Pieza> piezas,
        IReadOnlyDictionary<string, string?> variables,
        StringBuilder salida,
        ref string? ultimoValor)
    {
        foreach (var pieza in piezas)
        {
            switch (pieza)
            {
                case PlantillaMolde.Texto texto:
                    var valor = texto.Valor;
                    if (ultimoValor is not null && valor.StartsWith('.') && TerminaEnPuntuacion(ultimoValor))
                    {
                        valor = valor[1..];
                    }

                    salida.Append(valor);
                    ultimoValor = null;
                    break;

                case PlantillaMolde.Variable variable:
                    var contenido = Valor(variables, variable.Nombre) ?? "";
                    salida.Append(contenido);
                    ultimoValor = contenido;
                    break;

                case PlantillaMolde.Opcional opcional:
                    var incompleto = opcional.Piezas
                        .OfType<PlantillaMolde.Variable>()
                        .Any(v => string.IsNullOrEmpty(Valor(variables, v.Nombre)));
                    if (!incompleto)
                    {
                        Escribir(opcional.Piezas, variables, salida, ref ultimoValor);
                    }

                    break;
            }
        }
    }

    private static string? Valor(IReadOnlyDictionary<string, string?> variables, string nombre) =>
        variables.TryGetValue(nombre, out var valor) ? valor : null;

    private static bool TerminaEnPuntuacion(string valor) =>
        valor.Length > 0 && _puntuacionFinal.Contains(valor[^1]);
}
