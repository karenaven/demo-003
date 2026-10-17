using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Fakes;

public sealed class EstadoGeneracionFalso : IEstadoGeneracion
{
    public int? EpisodioEnCurso { get; set; }

    public bool EnCurso => EpisodioEnCurso is not null;
}
