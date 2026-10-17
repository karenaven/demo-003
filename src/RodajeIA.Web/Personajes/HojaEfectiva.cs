using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Prompt;

namespace RodajeIA.Web.Personajes;

/// <summary>
/// Hoja base con los campos de la variante de la escena reemplazados (RN-05, FR-032). El mismo
/// texto renderizado va a la instrucción de la IA y al prompt final (constitución III).
/// </summary>
public sealed record HojaEfectiva(
    int HojaId,
    string Nombre,
    string Edad,
    string DescripcionFisica,
    string Peinado,
    string Vestuario,
    string? HeridasMarcas,
    string Personalidad,
    string Rol)
{
    public static HojaEfectiva Calcular(HojaPersonaje hoja, VarianteEscena? variante)
    {
        var heridas = variante is { ReemplazaHeridas: true } ? variante.HeridasMarcas : hoja.HeridasMarcas;

        return new HojaEfectiva(
            hoja.Id,
            hoja.Nombre,
            variante?.Edad ?? hoja.Edad,
            hoja.DescripcionFisica,
            variante?.Peinado ?? hoja.Peinado,
            variante?.Vestuario ?? hoja.Vestuario,
            string.IsNullOrWhiteSpace(heridas) ? null : heridas,
            hoja.Personalidad,
            hoja.Rol);
    }

    /// <summary>
    /// Las hojas efectivas de los personajes presentes en la escena, en orden de primera aparición
    /// (FR-025b). Requiere <c>Personajes</c> con su hoja y <c>Variantes</c> cargados.
    /// </summary>
    public static IReadOnlyList<HojaEfectiva> DeEscena(Escena escena) =>
        escena.Personajes
            .OrderBy(p => p.Orden)
            .Select(p => Calcular(
                p.Hoja ?? throw new InvalidOperationException("Falta cargar la hoja del personaje."),
                escena.Variantes.SingleOrDefault(v => v.HojaId == p.HojaId)))
            .ToList();

    public string Renderizar(MoldePrompt molde) =>
        MotorMolde.Renderizar(molde.PorPersonaje, new Dictionary<string, string?>
        {
            ["personaje.nombre"] = Nombre,
            ["personaje.edad"] = Edad,
            ["personaje.descripcionFisica"] = DescripcionFisica,
            ["personaje.peinado"] = Peinado,
            ["personaje.vestuario"] = Vestuario,
            ["personaje.heridas"] = HeridasMarcas,
            ["personaje.personalidad"] = Personalidad,
            ["personaje.rol"] = Rol,
        });
}
