using System.Text;

namespace RodajeIA.Web.Prompt;

/// <summary>
/// Plantilla del molde ya analizada: texto literal, variables <c>{{x.y}}</c> y opcionales
/// <c>[ … ]</c> sin anidar (contracts/prompt.md).
/// </summary>
public sealed class PlantillaMolde
{
    public abstract record Pieza;

    public sealed record Texto(string Valor) : Pieza;

    public sealed record Variable(string Nombre) : Pieza;

    public sealed record Opcional(IReadOnlyList<Pieza> Piezas) : Pieza;

    private PlantillaMolde(string fuente, IReadOnlyList<Pieza> piezas)
    {
        Fuente = fuente;
        Piezas = piezas;
    }

    public string Fuente { get; }

    public IReadOnlyList<Pieza> Piezas { get; }

    public IEnumerable<string> Variables =>
        Piezas.SelectMany(p => p is Opcional o ? o.Piezas : [p]).OfType<Variable>().Select(v => v.Nombre);

    /// <summary>Analiza la plantilla; devuelve un error legible si los corchetes o llaves no cierran.</summary>
    public static PlantillaMolde Analizar(string fuente, out string? error)
    {
        error = null;
        var raiz = new List<Pieza>();
        List<Pieza>? opcional = null;
        var texto = new StringBuilder();

        void CerrarTexto()
        {
            if (texto.Length > 0)
            {
                (opcional ?? raiz).Add(new Texto(texto.ToString()));
                texto.Clear();
            }
        }

        for (var i = 0; i < fuente.Length; i++)
        {
            if (fuente[i] == '{' && i + 1 < fuente.Length && fuente[i + 1] == '{')
            {
                var fin = fuente.IndexOf("}}", i + 2, StringComparison.Ordinal);
                if (fin < 0)
                {
                    error = "llave sin cerrar";
                    break;
                }

                CerrarTexto();
                (opcional ?? raiz).Add(new Variable(fuente[(i + 2)..fin].Trim()));
                i = fin + 1;
            }
            else if (fuente[i] == '[')
            {
                if (opcional is not null)
                {
                    error = "corchete anidado";
                    break;
                }

                CerrarTexto();
                opcional = [];
            }
            else if (fuente[i] == ']')
            {
                if (opcional is null)
                {
                    error = "corchete sin abrir";
                    break;
                }

                CerrarTexto();
                raiz.Add(new Opcional(opcional));
                opcional = null;
            }
            else
            {
                texto.Append(fuente[i]);
            }
        }

        if (error is null && opcional is not null)
        {
            error = "corchete sin cerrar";
        }

        CerrarTexto();
        return new PlantillaMolde(fuente, raiz);
    }
}
