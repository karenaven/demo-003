namespace RodajeIA.Web.Generacion;

/// <summary>Qué se está generando ahora; lo consultan las operaciones que no se permiten durante una generación (FR-004a).</summary>
public interface IEstadoGeneracion
{
    /// <summary>Si hay una generación en curso en toda la aplicación (FR-014a).</summary>
    bool EnCurso { get; }

    /// <summary>El episodio cuyas escenas se están generando o esperan su turno; nulo si no hay generación.</summary>
    int? EpisodioEnCurso { get; }
}
