using System.Text.RegularExpressions;
using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Web.Guion;

/// <summary>
/// Valida y divide el guion según contracts/guion.md, sin IA (constitución I). Recorre las líneas
/// una vez como máquina de estados, acumula todos los errores y nunca devuelve un resultado parcial.
/// </summary>
public static partial class ParserGuion
{
    public const int MaximoEscenas = 10;
    public const int MaximoClips = 20;

    private static readonly string[] _campos = ["LOCACIÓN:", "ILUMINACIÓN:", "PUESTA EN ESCENA:", "AUDIO:"];

    private const string MarcadorTextoEnPantalla = "TEXTO EN PANTALLA:";

    public static ResultadoGuion Parsear(string texto)
    {
        if (LectorLineas.SuperaElMaximo(texto))
        {
            return new ResultadoGuion.Invalido([MotivosGuion.E00()]);
        }

        var estado = new Estado();
        foreach (var linea in LectorLineas.Leer(texto))
        {
            if (!linea.EnBlanco)
            {
                Procesar(estado, linea);
            }
        }

        estado.CerrarEscena();

        if (estado.Escenas.Count == 0)
        {
            return new ResultadoGuion.Invalido([MotivosGuion.E01()]);
        }

        if (estado.Errores.Count > 0)
        {
            var ordenados = estado.Errores.OrderBy(e => e.Linea).ThenBy(e => e.Codigo, StringComparer.Ordinal).ToList();
            return new ResultadoGuion.Invalido(ordenados);
        }

        return new ResultadoGuion.Valido(new GuionParseado(estado.Escenas.Select(e => e.Construir()).ToList()));
    }

    private static void Procesar(Estado estado, LineaGuion linea)
    {
        if (EsEncabezado(linea.Texto))
        {
            estado.AbrirEscena(linea);
            return;
        }

        var escena = estado.EscenaActual;
        if (escena is null)
        {
            estado.TextoAntesDeLaPrimeraEscena.Add(linea.Numero);
            return;
        }

        var campo = Array.FindIndex(_campos, c => linea.Texto.StartsWith(c, StringComparison.Ordinal));

        if (escena.ClipActual is null)
        {
            if (campo >= 0)
            {
                escena.AgregarCampo(estado, campo, linea);
            }
            else if (ClipRegex().Match(linea.Texto) is { Success: true } clip)
            {
                escena.AbrirClip(estado, linea, int.Parse(clip.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                estado.Errores.Add(MotivosGuion.E09(linea.Numero, escena.Numero));
            }

            return;
        }

        if (campo >= 0)
        {
            estado.Errores.Add(MotivosGuion.E06(linea.Numero, _campos[campo], escena.Numero, MotivosGuion.DespuesDelPrimerClip));
            escena.ClipActual.TieneContenido = true;
        }
        else if (ClipRegex().Match(linea.Texto) is { Success: true } clip)
        {
            escena.AbrirClip(estado, linea, int.Parse(clip.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (linea.Texto.StartsWith(MarcadorTextoEnPantalla, StringComparison.Ordinal))
        {
            escena.ClipActual.AgregarTextoEnPantalla(estado, escena.Numero, linea, linea.Texto[MarcadorTextoEnPantalla.Length..].Trim());
        }
        else if (DialogoRegex().Match(linea.Texto) is { Success: true } dialogo)
        {
            escena.ClipActual.AgregarDialogo(estado, linea, dialogo);
        }
        else
        {
            escena.ClipActual.Acciones.Add(new LineaAccionParseada(linea.Numero, linea.Texto));
            escena.ClipActual.TieneContenido = true;
        }
    }

    private static bool EsEncabezado(string texto) =>
        texto == "ESCENA" || texto.StartsWith("ESCENA ", StringComparison.Ordinal);

    /// <summary>Texto entre una comilla de apertura (" o “) y una de cierre (" o ”) al final de la línea.</summary>
    private static string? Cita(string resto)
    {
        if (resto.Length >= 2 && resto[0] is '"' or '“' && resto[^1] is '"' or '”')
        {
            return resto[1..^1];
        }

        return null;
    }

    private sealed class Estado
    {
        public List<ErrorGuion> Errores { get; } = [];

        public List<int> TextoAntesDeLaPrimeraEscena { get; } = [];

        public List<EscenaEnCurso> Escenas { get; } = [];

        public EscenaEnCurso? EscenaActual { get; private set; }

        public void AbrirEscena(LineaGuion linea)
        {
            CerrarEscena();
            if (Escenas.Count == 0)
            {
                Errores.AddRange(TextoAntesDeLaPrimeraEscena.Select(MotivosGuion.E02));
            }

            var esperado = Escenas.Count + 1;
            var escena = new EscenaEnCurso(linea.Numero, esperado);
            EscenaActual = escena;
            Escenas.Add(escena);

            if (esperado == MaximoEscenas + 1)
            {
                Errores.Add(MotivosGuion.E16(linea.Numero));
            }

            var encabezado = EncabezadoRegex().Match(linea.Texto);
            if (!encabezado.Success)
            {
                Errores.Add(MotivosGuion.E03(linea.Numero));
                return;
            }

            var numero = int.Parse(encabezado.Groups["n"].Value, System.Globalization.CultureInfo.InvariantCulture);
            escena.Numero = numero;
            if (numero != esperado)
            {
                Errores.Add(MotivosGuion.E05(linea.Numero, numero, esperado));
            }

            var medio = encabezado.Groups["medio"].Value;
            var intExt = IntExtRegex().Match(medio);
            if (!intExt.Success)
            {
                var valor = medio.Split(' ', 2)[0];
                Errores.Add(MotivosGuion.E04(linea.Numero, numero, valor));
                return;
            }

            escena.IntExt = intExt.Groups["ie"].Value == "INT." ? IntExt.Int : IntExt.Ext;
            escena.Lugar = intExt.Groups["lugar"].Value.Trim();
            escena.MomentoDia = encabezado.Groups["momento"].Value.Trim();
        }

        public void CerrarEscena()
        {
            EscenaActual?.Cerrar(this);
            EscenaActual = null;
        }
    }

    private sealed class EscenaEnCurso(int linea, int numero)
    {
        private readonly (int Linea, string Valor)?[] _campos = new (int, string)?[4];

        public int Linea { get; } = linea;

        public int Numero { get; set; } = numero;

        public IntExt IntExt { get; set; }

        public string Lugar { get; set; } = "";

        public string MomentoDia { get; set; } = "";

        public List<ClipEnCurso> Clips { get; } = [];

        public ClipEnCurso? ClipActual { get; private set; }

        public void AgregarCampo(Estado estado, int indice, LineaGuion linea)
        {
            var nombre = ParserGuion._campos[indice];
            if (_campos[indice] is not null)
            {
                estado.Errores.Add(MotivosGuion.E06(linea.Numero, nombre, Numero, MotivosGuion.Repetido));
                return;
            }

            var valor = linea.Texto[nombre.Length..].Trim();
            _campos[indice] = (linea.Numero, valor);
            if (valor.Length == 0)
            {
                estado.Errores.Add(MotivosGuion.E08(linea.Numero, nombre, Numero));
            }
        }

        public void AbrirClip(Estado estado, LineaGuion linea, int numero)
        {
            CerrarClip(estado);
            var esperado = Clips.Count + 1;
            if (numero != esperado)
            {
                estado.Errores.Add(MotivosGuion.E12(linea.Numero, numero, Numero, esperado));
            }

            if (esperado == MaximoClips + 1)
            {
                estado.Errores.Add(MotivosGuion.E17(linea.Numero, Numero));
            }

            ClipActual = new ClipEnCurso(linea.Numero, esperado);
            Clips.Add(ClipActual);
        }

        public void Cerrar(Estado estado)
        {
            CerrarClip(estado);

            for (var i = 0; i < _campos.Length; i++)
            {
                if (_campos[i] is null)
                {
                    estado.Errores.Add(MotivosGuion.E07(Linea, ParserGuion._campos[i], Numero));
                }
            }

            // Un campo está fuera de orden si un campo que debería ir antes aparece después (AC-03r).
            for (var i = 0; i < _campos.Length; i++)
            {
                if (_campos[i] is { } campo && _campos.Take(i).Any(anterior => anterior?.Linea > campo.Linea))
                {
                    estado.Errores.Add(MotivosGuion.E06(campo.Linea, ParserGuion._campos[i], Numero, MotivosGuion.FueraDeOrden));
                }
            }

            if (Clips.Count == 0)
            {
                estado.Errores.Add(MotivosGuion.E11(Linea, Numero));
            }
        }

        public EscenaParseada Construir() => new(
            Linea,
            Numero,
            IntExt,
            Lugar,
            MomentoDia,
            _campos[0]!.Value.Valor,
            _campos[1]!.Value.Valor,
            _campos[2]!.Value.Valor,
            _campos[3]!.Value.Valor,
            Clips.Select(c => c.Construir()).ToList());

        private void CerrarClip(Estado estado)
        {
            if (ClipActual is { TieneContenido: false })
            {
                estado.Errores.Add(MotivosGuion.E13(ClipActual.Linea, ClipActual.Numero, Numero));
            }

            ClipActual = null;
        }
    }

    private sealed class ClipEnCurso(int linea, int numero)
    {
        public int Linea { get; } = linea;

        public int Numero { get; } = numero;

        public bool TieneContenido { get; set; }

        public List<LineaAccionParseada> Acciones { get; } = [];

        public List<LineaDialogoParseada> Dialogos { get; } = [];

        public TextoEnPantallaParseado? TextoEnPantalla { get; private set; }

        public void AgregarTextoEnPantalla(Estado estado, int escena, LineaGuion linea, string resto)
        {
            TieneContenido = true;
            if (TextoEnPantalla is not null)
            {
                estado.Errores.Add(MotivosGuion.E15(linea.Numero, Numero, escena));
                return;
            }

            if (Cita(resto) is not { } texto)
            {
                estado.Errores.Add(MotivosGuion.E14(linea.Numero));
                return;
            }

            TextoEnPantalla = new TextoEnPantallaParseado(linea.Numero, texto, resto);
        }

        public void AgregarDialogo(Estado estado, LineaGuion linea, Match dialogo)
        {
            TieneContenido = true;
            var personaje = dialogo.Groups["personaje"].Value.TrimEnd();
            if (Cita(dialogo.Groups["resto"].Value.Trim()) is not { } texto)
            {
                estado.Errores.Add(MotivosGuion.E10(linea.Numero, personaje));
                return;
            }

            var acotacion = dialogo.Groups["acotacion"].Success ? dialogo.Groups["acotacion"].Value.Trim() : null;
            Dialogos.Add(new LineaDialogoParseada(linea.Numero, Dialogos.Count + 1, personaje, acotacion, texto, linea.Texto));
        }

        public ClipParseado Construir() => new(Linea, Numero, Acciones, Dialogos, TextoEnPantalla);
    }

    [GeneratedRegex(@"^ESCENA\s+(?<n>\d+)\s+[—-]\s+(?<medio>.+)\s+[—-]\s+(?<momento>.+?)$")]
    private static partial Regex EncabezadoRegex();

    [GeneratedRegex(@"^(?<ie>INT\.|EXT\.)\s+(?<lugar>.+)$")]
    private static partial Regex IntExtRegex();

    [GeneratedRegex(@"^CLIP\s+(\d+)$")]
    private static partial Regex ClipRegex();

    [GeneratedRegex(@"^(?<personaje>\p{Lu}[\p{Lu} '\-]*?)\s*(?:\((?<acotacion>[^)]*)\))?\s*:(?<resto>.*)$")]
    private static partial Regex DialogoRegex();
}
