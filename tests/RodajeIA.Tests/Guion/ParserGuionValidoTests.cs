using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Guion;

namespace RodajeIA.Tests.Guion;

public class ParserGuionValidoTests
{
    internal static GuionParseado Valido(string texto)
    {
        var resultado = ParserGuion.Parsear(texto);
        var invalido = resultado as ResultadoGuion.Invalido;
        Assert.True(invalido is null, invalido is null ? "" : string.Join("\n", invalido.Errores));
        return ((ResultadoGuion.Valido)resultado).Guion;
    }

    [Fact]
    [Trait("AC", "AC-04a")]
    public void Extrae_el_encabezado_y_los_cuatro_campos()
    {
        var escena = Assert.Single(Valido(Ejemplos.EscenaAnexoA).Escenas);

        Assert.Equal(1, escena.Numero);
        Assert.Equal(IntExt.Int, escena.IntExt);
        Assert.Equal("RESTAURANTE", escena.Lugar);
        Assert.Equal("NOCHE", escena.MomentoDia);
        Assert.Equal("Restaurante pequeño y acogedor; pared de ladrillo visto, mesas de madera oscura, vela en portavelas de cristal.", escena.Locacion);
        Assert.Equal("Luz cálida ambiental (aprox. 2700 K), principal suave cenital de la lámpara sobre la mesa, reflejo de la vela en los rostros.", escena.Iluminacion);
        Assert.Equal("Sentados frente a frente en una mesa para dos. Ana siempre a la IZQUIERDA del encuadre, José Daniel siempre a la DERECHA.", escena.PuestaEnEscena);
        Assert.Equal("Ambiente tranquilo de restaurante, murmullo lejano y cubiertos; SIN música. Diálogo en español, acento latinoamericano neutro.", escena.Audio);
    }

    [Fact]
    [Trait("AC", "AC-04b")]
    public void La_escena_de_ejemplo_tiene_dos_clips()
    {
        var escena = Assert.Single(Valido(Ejemplos.EscenaAnexoA).Escenas);

        Assert.Equal([1, 2], escena.Clips.Select(c => c.Numero));
    }

    [Fact]
    [Trait("AC", "AC-04c")]
    public void Separa_accion_y_dialogos_con_y_sin_acotacion()
    {
        var clip = Valido(Ejemplos.EscenaAnexoA).Escenas[0].Clips[0];

        Assert.Equal(
            "Ana y José Daniel conversan en una mesa junto a la ventana. José Daniel sonríe pícaramente.\nAna niega con la cabeza, divertida, con una sonrisa amplia.",
            clip.Accion);
        Assert.Collection(
            clip.Dialogos,
            ana =>
            {
                Assert.Equal(1, ana.Posicion);
                Assert.Equal("ANA", ana.Personaje);
                Assert.Null(ana.Acotacion);
                Assert.Equal("Mentiroso.", ana.Texto);
                Assert.Equal("ANA: \"Mentiroso.\"", ana.Original);
            },
            jose =>
            {
                Assert.Equal(2, jose.Posicion);
                Assert.Equal("JOSÉ DANIEL", jose.Personaje);
                Assert.Equal("sonriendo", jose.Acotacion);
                Assert.Equal("Bueno... pasó parecido.", jose.Texto);
                Assert.Equal("JOSÉ DANIEL (sonriendo): \"Bueno... pasó parecido.\"", jose.Original);
            });
        Assert.Null(clip.TextoEnPantalla);
    }

    [Fact]
    [Trait("AC", "AC-04d")]
    public void El_texto_en_pantalla_no_va_a_la_accion_ni_al_dialogo()
    {
        var texto = Ejemplos.EscenaAnexoA + "\nTEXTO EN PANTALLA: \"Hermano, ¿puedes salir conmigo?\"";

        var clip = Valido(texto).Escenas[0].Clips[1];

        Assert.NotNull(clip.TextoEnPantalla);
        Assert.Equal("Hermano, ¿puedes salir conmigo?", clip.TextoEnPantalla.Texto);
        Assert.Equal("\"Hermano, ¿puedes salir conmigo?\"", clip.TextoEnPantalla.Original);
        Assert.DoesNotContain("Hermano", clip.Accion, StringComparison.Ordinal);
        Assert.Empty(clip.Dialogos);
    }

    [Fact]
    [Trait("AC", "AC-04e")]
    public void Procesar_dos_veces_da_el_mismo_resultado()
    {
        var guion = Ejemplos.Guion(3, 4, dialogosPorClip: 2);

        var primero = Valido(guion);
        var segundo = Valido(guion);

        Assert.Equal(Describir(primero), Describir(segundo));
    }

    [Fact]
    [Trait("AC", "AC-03o")]
    public void Acepta_guion_comun_en_el_encabezado()
    {
        var conGuion = Ejemplos.EscenaAnexoA.Replace("ESCENA 1 — INT. RESTAURANTE — NOCHE", "ESCENA 1 - INT. RESTAURANTE - NOCHE", StringComparison.Ordinal);

        Assert.Equal(Describir(Valido(Ejemplos.EscenaAnexoA)), Describir(Valido(conGuion)));
    }

    [Fact]
    [Trait("AC", "AC-03p")]
    public void Las_lineas_en_blanco_no_cambian_el_resultado()
    {
        var conBlancos = Ejemplos.EscenaAnexoA
            .Replace("\nILUMINACIÓN:", "\n\n  \nILUMINACIÓN:", StringComparison.Ordinal)
            .Replace("\nCLIP 2", "\n\n\nCLIP 2", StringComparison.Ordinal);

        Assert.Equal(Describir(Valido(Ejemplos.EscenaAnexoA)), Describir(Valido(conBlancos)));
    }

    [Fact]
    [Trait("AC", "AC-S03")]
    public void Crlf_bom_y_tabulaciones_dan_el_mismo_resultado_y_numeracion()
    {
        var conCrlf = "﻿" + Ejemplos.EscenaAnexoA.Replace("\n", "\r\n", StringComparison.Ordinal).Replace("CLIP 2", "\tCLIP 2  ", StringComparison.Ordinal);

        Assert.Equal(Describir(Valido(Ejemplos.EscenaAnexoA)), Describir(Valido(conCrlf)));

        var invalidoLf = Ejemplos.EscenaAnexoA.Replace("ANA: \"Mentiroso.\"", "ANA: Mentiroso.", StringComparison.Ordinal);
        var invalidoCrlf = "﻿" + invalidoLf.Replace("\n", "\r\n", StringComparison.Ordinal);
        var invalidoCr = invalidoLf.Replace("\n", "\r", StringComparison.Ordinal);
        foreach (var texto in new[] { invalidoLf, invalidoCrlf, invalidoCr })
        {
            var error = Assert.Single(((ResultadoGuion.Invalido)ParserGuion.Parsear(texto)).Errores);
            Assert.Equal(8, error.Linea);
        }
    }

    [Fact]
    [Trait("AC", "AC-S07")]
    public void Conserva_comillas_tipograficas_e_internas_en_el_original()
    {
        var texto = Ejemplos.EscenaAnexoA.Replace("ANA: \"Mentiroso.\"", "ANA: “Dijo \"no\" y se fue.”", StringComparison.Ordinal)
            + "\nTEXTO EN PANTALLA: “Hola”";

        var escena = Valido(texto).Escenas[0];
        var linea = escena.Clips[0].Dialogos[0];

        Assert.Equal("Dijo \"no\" y se fue.", linea.Texto);
        Assert.Equal("ANA: “Dijo \"no\" y se fue.”", linea.Original);
        Assert.Equal("“Hola”", escena.Clips[1].TextoEnPantalla!.Original);
    }

    [Fact]
    public void Los_personajes_sin_hoja_tienen_dialogo_valido()
    {
        var texto = Ejemplos.EscenaAnexoA + "\nMESERO: \"¿Algo más?\"";

        var linea = Assert.Single(Valido(texto).Escenas[0].Clips[1].Dialogos);

        Assert.Equal("MESERO", linea.Personaje);
    }

    private static string Describir(GuionParseado guion) =>
        string.Join("\n", guion.Escenas.Select(e =>
            $"{e.Numero}|{e.IntExt}|{e.Lugar}|{e.MomentoDia}|{e.Locacion}|{e.Iluminacion}|{e.PuestaEnEscena}|{e.Audio}\n" +
            string.Join("\n", e.Clips.Select(c =>
                $"  {c.Numero}|{c.Accion}|{c.TextoEnPantalla?.Original}|" +
                string.Join("|", c.Dialogos.Select(d => $"{d.Posicion}:{d.Personaje}:{d.Acotacion}:{d.Texto}:{d.Original}"))))));
}
