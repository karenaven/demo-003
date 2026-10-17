namespace RodajeIA.Web.Configuracion;

public sealed record CategoriaVocabulario(string Nombre, bool Obligatorio, IReadOnlyList<string> Valores)
{
    public bool Contiene(string valor) => Valores.Contains(valor, StringComparer.Ordinal);
}

/// <summary>Vocabulario técnico cerrado de <c>config/vocabulario.json</c> (RN-02, Anexo E).</summary>
public sealed class Vocabulario(IReadOnlyList<CategoriaVocabulario> categorias)
{
    public const string Plano = "plano";
    public const string Optica = "optica";
    public const string Iluminacion = "iluminacion";
    public const string Angulo = "angulo";
    public const string Movimiento = "movimiento";

    public static readonly IReadOnlyList<string> NombresCategorias = [Plano, Optica, Iluminacion, Angulo, Movimiento];

    public static readonly IReadOnlySet<string> CategoriasObligatorias = new HashSet<string> { Plano, Optica, Iluminacion };

    /// <summary>Categorías en el orden de <see cref="NombresCategorias"/>.</summary>
    public IReadOnlyList<CategoriaVocabulario> Categorias { get; } = categorias;

    public CategoriaVocabulario Categoria(string nombre) =>
        Categorias.FirstOrDefault(c => c.Nombre == nombre) ?? throw new ArgumentException($"Categoría desconocida {nombre}.", nameof(nombre));

    public bool Contiene(string categoria, string valor) => Categoria(categoria).Contiene(valor);
}
