using System.Reflection;

namespace RodajeIA.Tests;

/// <summary>
/// Cada criterio de aceptación del PRD y de la spec tiene al menos un test (constitución V). AC-S26
/// se cubre con NotificadorGeneracionTests; su parte visual se valida en el paso 6 del quickstart.
/// </summary>
public class TrazabilidadTests
{
    /// <summary>Los 99 AC del PRD (AC-01a a AC-11g).</summary>
    private static readonly string[] _acPrd =
    [
        "AC-01a", "AC-01b", "AC-01c", "AC-01d", "AC-01e", "AC-01f", "AC-01g", "AC-01h", "AC-01i", "AC-01j",
        "AC-01k", "AC-02a", "AC-02b", "AC-02c", "AC-02d", "AC-03a", "AC-03b", "AC-03c", "AC-03d", "AC-03e",
        "AC-03f", "AC-03g", "AC-03h", "AC-03i", "AC-03j", "AC-03k", "AC-03l", "AC-03m", "AC-03n", "AC-03o",
        "AC-03p", "AC-03q", "AC-03r", "AC-03s", "AC-03t", "AC-03u", "AC-04a", "AC-04b", "AC-04c", "AC-04d",
        "AC-04e", "AC-05a", "AC-05b", "AC-05c", "AC-05d", "AC-05e", "AC-05f", "AC-05g", "AC-06a", "AC-06b",
        "AC-06c", "AC-06d", "AC-06e", "AC-06f", "AC-06g", "AC-06h", "AC-06i", "AC-06j", "AC-06k", "AC-07a",
        "AC-07b", "AC-07c", "AC-07d", "AC-07e", "AC-07f", "AC-08a", "AC-08b", "AC-08c", "AC-08d", "AC-08e",
        "AC-08f", "AC-08g", "AC-08h", "AC-09a", "AC-09b", "AC-09c", "AC-09d", "AC-09e", "AC-09f", "AC-09g",
        "AC-09h", "AC-09i", "AC-09j", "AC-09k", "AC-09l", "AC-09m", "AC-10a", "AC-10b", "AC-10c", "AC-10d",
        "AC-10e", "AC-10f", "AC-11a", "AC-11b", "AC-11c", "AC-11d", "AC-11e", "AC-11f", "AC-11g",
    ];

    /// <summary>Los escenarios de aceptación de las aclaraciones de la spec.</summary>
    private static readonly string[] _acSpec =
    [
        "AC-S01", "AC-S02", "AC-S03", "AC-S04", "AC-S05", "AC-S06", "AC-S07", "AC-S08", "AC-S09", "AC-S10",
        "AC-S11", "AC-S12", "AC-S13", "AC-S14", "AC-S15", "AC-S16", "AC-S17", "AC-S18", "AC-S19", "AC-S20",
        "AC-S21", "AC-S22", "AC-S23", "AC-S24", "AC-S25", "AC-S26", "AC-S27", "AC-S28", "AC-S29", "AC-S30",
        "AC-S31", "AC-S32", "AC-S33", "AC-S34", "AC-S35", "AC-S36", "AC-S37", "AC-S38", "AC-S39",
        "AC-S40",
    ];

    private static IReadOnlySet<string> Cubiertos() =>
        typeof(TrazabilidadTests).Assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .SelectMany(m => m.CustomAttributes)
            .Where(a => a.AttributeType == typeof(TraitAttribute)
                && a.ConstructorArguments.Count == 2
                && (string?)a.ConstructorArguments[0].Value == "AC")
            .Select(a => (string)a.ConstructorArguments[1].Value!)
            .ToHashSet();

    [Fact]
    public void Las_listas_tienen_los_99_ac_del_prd_y_los_40_de_la_spec()
    {
        Assert.Equal(99, _acPrd.Distinct().Count());
        Assert.Equal(40, _acSpec.Distinct().Count());
    }

    [Fact]
    public void Todo_criterio_de_aceptacion_tiene_al_menos_un_test()
    {
        var cubiertos = Cubiertos();

        var faltantes = _acPrd.Concat(_acSpec).Where(ac => !cubiertos.Contains(ac)).ToList();

        Assert.True(faltantes.Count == 0, "AC sin test: " + string.Join(", ", faltantes));
    }
}
