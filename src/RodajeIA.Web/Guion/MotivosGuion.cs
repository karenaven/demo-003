namespace RodajeIA.Web.Guion;

/// <summary>Los textos de los motivos de error, en una sola tabla (contracts/guion.md, FR-008a).</summary>
public static class MotivosGuion
{
    public static ErrorGuion E00() =>
        new(1, "E00", "El guion supera los 100.000 caracteres.");

    public static ErrorGuion E01() =>
        new(1, "E01", "El guion no tiene ninguna escena; debe empezar con un encabezado como \"ESCENA 1 — INT. LUGAR — NOCHE\".");

    public static ErrorGuion E02(int linea) =>
        new(linea, "E02", "Hay texto antes de la primera escena; el guion debe empezar con \"ESCENA 1 — …\".");

    public static ErrorGuion E03(int linea) =>
        new(linea, "E03", "El encabezado de escena no tiene el formato \"ESCENA N — INT. LUGAR — MOMENTO DEL DÍA\".");

    public static ErrorGuion E04(int linea, int escena, string valor) =>
        new(linea, "E04", $"En el encabezado de la escena {escena}, \"{valor}\" debe escribirse \"INT.\" o \"EXT.\", con punto.");

    public static ErrorGuion E05(int linea, int numero, int esperado) =>
        new(linea, "E05", $"La escena {numero} debería ser la escena {esperado}: los números empiezan en 1 y no se saltean ni repiten.");

    public static ErrorGuion E06(int linea, string campo, int escena, string problema) =>
        new(linea, "E06", $"El campo {campo} de la escena {escena} {problema}; el orden es LOCACIÓN, ILUMINACIÓN, PUESTA EN ESCENA, AUDIO, antes del primer CLIP.");

    public const string Repetido = "está repetido";
    public const string FueraDeOrden = "está fuera de orden";
    public const string DespuesDelPrimerClip = "está después del primer CLIP";

    public static ErrorGuion E07(int linea, string campo, int escena) =>
        new(linea, "E07", $"Falta el campo {campo} en la escena {escena}.");

    public static ErrorGuion E08(int linea, string campo, int escena) =>
        new(linea, "E08", $"El campo {campo} de la escena {escena} no tiene valor.");

    public static ErrorGuion E09(int linea, int escena) =>
        new(linea, "E09", $"Antes del primer CLIP de la escena {escena} solo pueden ir LOCACIÓN:, ILUMINACIÓN:, PUESTA EN ESCENA: y AUDIO:, cada uno en una línea.");

    public static ErrorGuion E10(int linea, string personaje) =>
        new(linea, "E10", $"El diálogo de {personaje} debe ir entre comillas: {personaje}: \"texto\".");

    public static ErrorGuion E11(int linea, int escena) =>
        new(linea, "E11", $"La escena {escena} no tiene ningún clip; agregá al menos un \"CLIP 1\".");

    public static ErrorGuion E12(int linea, int numero, int escena, int esperado) =>
        new(linea, "E12", $"El clip {numero} de la escena {escena} debería ser el clip {esperado}: los números empiezan en 1 y no se saltean ni repiten.");

    public static ErrorGuion E13(int linea, int clip, int escena) =>
        new(linea, "E13", $"El clip {clip} de la escena {escena} no tiene contenido.");

    public static ErrorGuion E14(int linea) =>
        new(linea, "E14", "El texto en pantalla debe ir entre comillas: TEXTO EN PANTALLA: \"texto\".");

    public static ErrorGuion E15(int linea, int clip, int escena) =>
        new(linea, "E15", $"El clip {clip} de la escena {escena} tiene más de un TEXTO EN PANTALLA; se permite uno solo.");

    public static ErrorGuion E16(int linea) =>
        new(linea, "E16", "El episodio supera el máximo de 10 escenas.");

    public static ErrorGuion E17(int linea, int escena) =>
        new(linea, "E17", $"La escena {escena} supera el máximo de 20 clips.");
}
