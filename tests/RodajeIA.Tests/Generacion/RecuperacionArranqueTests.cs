using Microsoft.EntityFrameworkCore;
using RodajeIA.Tests.Fakes;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Tests.Generacion;

public sealed class RecuperacionArranqueTests : IDisposable
{
    private readonly BaseDatosEnMemoria _base = new();

    public void Dispose() => _base.Dispose();

    [Fact]
    [Trait("AC", "AC-10f")]
    [Trait("AC", "AC-S28")]
    public async Task Al_iniciar_las_escenas_en_generando_pasan_a_error_y_las_pendientes_siguen_igual()
    {
        var (_, _, _, episodio) = _base.CrearSerieDeEjemplo();
        var ids = await EscenaDePrueba.CargarAsync(_base, episodio.Id, Ejemplos.Guion(2, 1));
        await using (var db = _base.CreateDbContext())
        {
            await db.Escenas.Where(e => e.Id == ids[0]).ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, EstadoEscena.Generando));
        }

        var recuperadas = await RecuperacionArranque.RecuperarAsync(_base, CancellationToken.None);

        Assert.Equal(1, recuperadas);
        await using var lectura = _base.CreateDbContext();
        var escenas = await lectura.Escenas.OrderBy(e => e.Numero).ToListAsync();
        Assert.Equal(EstadoEscena.Error, escenas[0].Estado);
        Assert.Equal("Generación interrumpida por un cierre de la aplicación.", escenas[0].MotivoError);
        Assert.Equal(EstadoEscena.Pendiente, escenas[1].Estado);
    }

    [Fact]
    public async Task Una_base_sin_migrar_da_un_mensaje_claro()
    {
        using var conexion = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        conexion.Open();
        var opciones = new DbContextOptionsBuilder<RodajeDbContext>().UseSqlite(conexion).Options;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => RecuperacionArranque.RecuperarAsync(new Fabrica(opciones), CancellationToken.None));

        Assert.Contains("dotnet ef database update", ex.Message, StringComparison.Ordinal);
    }

    private sealed class Fabrica(DbContextOptions<RodajeDbContext> opciones) : IDbContextFactory<RodajeDbContext>
    {
        public RodajeDbContext CreateDbContext() => new(opciones);
    }
}
