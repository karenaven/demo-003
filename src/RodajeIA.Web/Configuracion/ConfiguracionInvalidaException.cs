namespace RodajeIA.Web.Configuracion;

/// <summary>Uno o más archivos de <c>config/</c> no son válidos; la aplicación no arranca (FR-015a, FR-015c).</summary>
public sealed class ConfiguracionInvalidaException(IReadOnlyList<string> problemas)
    : Exception("Configuración inválida: " + string.Join("; ", problemas))
{
    /// <summary>Cada problema en la forma "{archivo}: {problema}".</summary>
    public IReadOnlyList<string> Problemas { get; } = problemas;
}
