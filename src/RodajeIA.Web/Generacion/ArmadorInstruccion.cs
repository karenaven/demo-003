using System.Text;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Personajes;

namespace RodajeIA.Web.Generacion;

/// <summary>
/// Arma la instrucción de una escena reemplazando <c>{{escena}}</c> y <c>{{personajes}}</c> de
/// <c>config/instruccion-generacion.txt</c> (contracts/gemini.md). Solo incluye esa escena (FR-016b).
/// </summary>
public sealed class ArmadorInstruccion(InstruccionGeneracion instruccion, MoldePrompt molde)
{
    /// <summary>Requiere la escena con clips, líneas, personajes con su hoja y variantes cargados.</summary>
    public string Armar(Escena escena) => instruccion.Armar(ArmarEscena(escena), ArmarPersonajes(escena));

    public static string ArmarEscena(Escena escena)
    {
        var texto = new StringBuilder();
        texto.Append($"ESCENA {escena.Numero} — {(escena.IntExt == IntExt.Int ? "INT." : "EXT.")} {escena.Lugar} — {escena.MomentoDia}\n");
        texto.Append($"Locación: {escena.Locacion}\n");
        texto.Append($"Iluminación base: {escena.Iluminacion}\n");
        texto.Append($"Puesta en escena: {escena.PuestaEnEscena}\n");
        texto.Append($"Audio: {escena.Audio}\n");

        foreach (var clip in escena.Clips.OrderBy(c => c.Numero))
        {
            texto.Append($"\nCLIP {clip.Numero}\n");
            texto.Append(clip.Accion.Length == 0 ? "Acción: (ninguna)\n" : $"Acción:\n{clip.Accion}\n");

            var lineas = clip.Lineas.OrderBy(l => l.Posicion).ToList();
            if (lineas.Count == 0)
            {
                texto.Append("Diálogos: (ninguno)\n");
            }
            else
            {
                texto.Append("Diálogos:\n");
                foreach (var linea in lineas)
                {
                    texto.Append($"[{LineaDialogo.IdIaDe(clip.Numero, linea.Posicion)}] {linea.Original}\n");
                }
            }

            texto.Append($"Texto en pantalla: {clip.TextoEnPantallaOriginal ?? "(ninguno)"}\n");
        }

        return texto.ToString().TrimEnd('\n');
    }

    public string ArmarPersonajes(Escena escena)
    {
        var hojas = HojaEfectiva.DeEscena(escena);
        return hojas.Count == 0 ? "(ninguno)" : string.Join('\n', hojas.Select(h => h.Renderizar(molde)));
    }
}
