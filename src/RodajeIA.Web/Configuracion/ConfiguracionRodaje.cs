namespace RodajeIA.Web.Configuracion;

/// <summary>Los cuatro archivos de <c>config/</c>, ya validados. Se leen una sola vez, al iniciar (FR-015d).</summary>
public sealed record ConfiguracionRodaje(
    Vocabulario Vocabulario,
    EstiloGlobal Estilo,
    InstruccionGeneracion Instruccion,
    MoldePrompt Molde);
