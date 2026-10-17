using System.Globalization;
using System.Text;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Guion;

namespace RodajeIA.Web.Personajes;

/// <summary>Personajes con hoja presentes en una escena y en cada uno de sus clips.</summary>
/// <param name="PresentesEnOrden">Ids de hoja en orden de primera aparición (FR-025b).</param>
/// <param name="PorClip">Ids de hoja presentes por número de clip.</param>
public sealed record DeteccionEscena(IReadOnlyList<int> PresentesEnOrden, IReadOnlyDictionary<int, IReadOnlySet<int>> PorClip);

/// <summary>
/// Detección determinística, sin IA (FR-011 a FR-012, contracts/guion.md). Los textos se comparan
/// en forma NFC solo para detectar; el guion guardado no se modifica (research R-04).
/// </summary>
public static class DetectorPersonajes
{
    public static DeteccionEscena Detectar(EscenaParseada escena, IReadOnlyList<HojaPersonaje> hojas)
    {
        var nombres = hojas
            .Select(h => (Hoja: h, Nombre: h.Nombre.Normalize(NormalizationForm.FormC), Clave: h.NombreClave.Normalize(NormalizationForm.FormC)))
            .ToList();

        // Cada aparición con su posición: (orden del tramo de texto, columna).
        var apariciones = new List<(int Tramo, int Columna, HojaPersonaje Hoja)>();
        var porClip = new Dictionary<int, IReadOnlySet<int>>();

        foreach (var (columna, hoja) in BuscarEnTexto(escena.PuestaEnEscena, nombres))
        {
            apariciones.Add((0, columna, hoja));
        }

        foreach (var clip in escena.Clips)
        {
            var presentes = new HashSet<int>();
            var lineas = clip.Acciones.Select(a => (a.Linea, Accion: (string?)a.Texto, Marcador: (string?)null))
                .Concat(clip.Dialogos.Select(d => (d.Linea, Accion: (string?)null, Marcador: (string?)d.Personaje)))
                .OrderBy(l => l.Linea);

            foreach (var (linea, accion, marcador) in lineas)
            {
                if (accion is not null)
                {
                    foreach (var (columna, hoja) in BuscarEnTexto(accion, nombres))
                    {
                        presentes.Add(hoja.Id);
                        apariciones.Add((linea, columna, hoja));
                    }
                }
                else
                {
                    var marcadorNfc = marcador!.Normalize(NormalizationForm.FormC);
                    var hoja = nombres.FirstOrDefault(n => string.Equals(n.Clave, marcadorNfc, StringComparison.Ordinal)).Hoja;
                    if (hoja is not null)
                    {
                        presentes.Add(hoja.Id);
                        apariciones.Add((linea, 0, hoja));
                    }
                }
            }

            porClip[clip.Numero] = presentes;
        }

        var orden = apariciones
            .OrderBy(a => a.Tramo)
            .ThenBy(a => a.Columna)
            .ThenBy(a => a.Hoja.Nombre, StringComparer.Ordinal)
            .Select(a => a.Hoja.Id)
            .Distinct()
            .ToList();

        return new DeteccionEscena(orden, porClip);
    }

    /// <summary>
    /// Ocurrencias de los nombres como palabra completa (FR-011b). Si dos se solapan, gana la más
    /// larga (FR-011a). Devuelve la columna de inicio de cada ocurrencia aceptada.
    /// </summary>
    private static IEnumerable<(int Columna, HojaPersonaje Hoja)> BuscarEnTexto(
        string texto,
        IReadOnlyList<(HojaPersonaje Hoja, string Nombre, string Clave)> nombres)
    {
        var nfc = texto.Normalize(NormalizationForm.FormC);
        var candidatos = new List<(int Inicio, int Largo, HojaPersonaje Hoja)>();

        foreach (var (hoja, nombre, _) in nombres)
        {
            for (var i = nfc.IndexOf(nombre, StringComparison.Ordinal); i >= 0; i = nfc.IndexOf(nombre, i + 1, StringComparison.Ordinal))
            {
                var fin = i + nombre.Length;
                if ((i == 0 || !EsParteDePalabra(nfc[i - 1])) && (fin == nfc.Length || !EsParteDePalabra(nfc[fin])))
                {
                    candidatos.Add((i, nombre.Length, hoja));
                }
            }
        }

        var ocupadoHasta = -1;
        foreach (var candidato in candidatos.OrderBy(c => c.Inicio).ThenByDescending(c => c.Largo))
        {
            if (candidato.Inicio >= ocupadoHasta)
            {
                ocupadoHasta = candidato.Inicio + candidato.Largo;
                yield return (candidato.Inicio, candidato.Hoja);
            }
        }
    }

    private static bool EsParteDePalabra(char c) =>
        char.IsLetterOrDigit(c)
        || c is '-' or '\'' or '’'
        || CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark;
}
