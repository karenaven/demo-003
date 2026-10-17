using RodajeIA.Web.Prompt;

namespace RodajeIA.Tests.Prompt;

public class MotorMoldeTests
{
    private static string Renderizar(string plantilla, params (string Nombre, string? Valor)[] variables)
    {
        var analizada = PlantillaMolde.Analizar(plantilla, out var error);
        Assert.Null(error);
        return MotorMolde.Renderizar(analizada, variables.ToDictionary(v => v.Nombre, v => v.Valor));
    }

    [Fact]
    public void Reemplaza_las_variables_literalmente()
    {
        var texto = Renderizar("Locación: {{escena.locacion}}", ("escena.locacion", "Bar \"El {{Gato}}\" [viejo]"));

        Assert.Equal("Locación: Bar \"El {{Gato}}\" [viejo]", texto);
    }

    [Fact]
    public void Omite_un_opcional_completo_si_una_variable_esta_vacia()
    {
        const string plantilla = "{{toma.plano}}[, {{toma.movimiento}}][, {{toma.angulo}}], óptica {{toma.optica}}.";

        var sinOpcionales = Renderizar(plantilla, ("toma.plano", "plano medio"), ("toma.movimiento", ""), ("toma.angulo", null), ("toma.optica", "50mm"));
        var conUno = Renderizar(plantilla, ("toma.plano", "plano medio"), ("toma.movimiento", "paneo"), ("toma.angulo", null), ("toma.optica", "50mm"));

        Assert.Equal("plano medio, óptica 50mm.", sinOpcionales);
        Assert.Equal("plano medio, paneo, óptica 50mm.", conUno);
    }

    [Fact]
    public void Una_variable_sin_valor_conocido_cuenta_como_vacia_dentro_de_un_opcional()
    {
        Assert.Equal("Rol: villano.", Renderizar("Rol: {{personaje.rol}}.[ Heridas/marcas: {{personaje.heridas}}.]", ("personaje.rol", "villano")));
    }

    [Fact]
    [Trait("AC", "AC-S34")]
    public void Omite_el_punto_despues_de_un_valor_que_ya_termina_en_puntuacion()
    {
        const string plantilla = "Peinado: {{personaje.peinado}}. Vestuario: {{personaje.vestuario}}.";

        Assert.Equal("Peinado: Cola de caballo. Vestuario: jean.", Renderizar(plantilla, ("personaje.peinado", "Cola de caballo."), ("personaje.vestuario", "jean")));
        Assert.Equal("Peinado: ¡Rapado! Vestuario: traje…", Renderizar(plantilla, ("personaje.peinado", "¡Rapado!"), ("personaje.vestuario", "traje…")));
        Assert.Equal("Peinado: ¿Peluca? Vestuario: x.", Renderizar(plantilla, ("personaje.peinado", "¿Peluca?"), ("personaje.vestuario", "x")));
    }

    [Fact]
    [Trait("AC", "AC-S34")]
    public void La_regla_de_puntuacion_aplica_dentro_de_un_opcional()
    {
        Assert.Equal(" Heridas/marcas: cicatriz.", Renderizar("[ Heridas/marcas: {{personaje.heridas}}.]", ("personaje.heridas", "cicatriz.")));
    }

    [Fact]
    public void Unir_no_pone_separadores_al_principio_ni_al_final_ni_entre_partes_vacias()
    {
        Assert.Equal("a Corte a b", MotorMolde.Unir(" Corte a ", ["", "a", "", "b", ""]));
        Assert.Equal("", MotorMolde.Unir("\n\n", ["", ""]));
    }
}
