using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Generacion;

public class NotificadorGeneracionTests
{
    [Fact]
    [Trait("AC", "AC-S26")]
    public void Los_suscriptores_reciben_cada_cambio_de_estado_e_intento()
    {
        var notificador = new NotificadorGeneracion();
        var recibidos = new List<CambioEstadoEscena>();
        void Manejador(CambioEstadoEscena c) => recibidos.Add(c);
        notificador.EstadoEscenaCambiado += Manejador;

        notificador.NotificarEscena(new CambioEstadoEscena(7, 1, EstadoEscena.Generando, 1, null));
        notificador.NotificarEscena(new CambioEstadoEscena(7, 1, EstadoEscena.Generando, 2, null));
        notificador.NotificarEscena(new CambioEstadoEscena(7, 1, EstadoEscena.Generada, null, null));

        Assert.Equal([(EstadoEscena.Generando, (int?)1), (EstadoEscena.Generando, 2), (EstadoEscena.Generada, null)], recibidos.Select(c => (c.Estado, c.Intento)));
    }

    [Fact]
    [Trait("AC", "AC-S26")]
    public void Recuerda_el_intento_en_curso_para_quien_abre_la_vista_despues()
    {
        var notificador = new NotificadorGeneracion();

        notificador.NotificarEscena(new CambioEstadoEscena(7, 1, EstadoEscena.Generando, 2, null));
        Assert.Equal(2, notificador.IntentoEnCurso(7));

        notificador.NotificarEscena(new CambioEstadoEscena(7, 1, EstadoEscena.Generada, null, null));
        Assert.Null(notificador.IntentoEnCurso(7));
    }

    [Fact]
    public void Avisa_los_cambios_de_generacion_en_curso_y_desuscribirse_corta_los_avisos()
    {
        var notificador = new NotificadorGeneracion();
        var avisos = new List<(bool, int?)>();
        void Manejador(bool enCurso, int? episodio) => avisos.Add((enCurso, episodio));
        notificador.GeneracionEnCursoCambiada += Manejador;

        notificador.NotificarGeneracionEnCurso(true, 3);
        notificador.GeneracionEnCursoCambiada -= Manejador;
        notificador.NotificarGeneracionEnCurso(false, null);

        Assert.Equal([(true, (int?)3)], avisos);
    }

    [Fact]
    public void Un_suscriptor_que_falla_no_corta_a_los_demas()
    {
        var notificador = new NotificadorGeneracion();
        var recibidos = 0;
        notificador.EstadoEscenaCambiado += _ => throw new InvalidOperationException("circuito cerrado");
        notificador.EstadoEscenaCambiado += _ => recibidos++;

        notificador.NotificarEscena(new CambioEstadoEscena(1, 1, EstadoEscena.Generando, 1, null));

        Assert.Equal(1, recibidos);
    }
}
