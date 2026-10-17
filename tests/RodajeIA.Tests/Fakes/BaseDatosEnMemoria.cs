using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Tests.Fakes;

/// <summary>
/// SQLite en memoria con el esquema creado por las migraciones reales (no con EnsureCreated), para
/// que la suite pruebe <c>Datos/Migrations</c>. La conexión vive mientras viva el objeto.
/// </summary>
public sealed class BaseDatosEnMemoria : IDisposable, IDbContextFactory<RodajeDbContext>
{
    private readonly SqliteConnection _conexion;
    private readonly DbContextOptions<RodajeDbContext> _opciones;

    public BaseDatosEnMemoria()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();
        _opciones = new DbContextOptionsBuilder<RodajeDbContext>().UseSqlite(_conexion).Options;
        using var db = CreateDbContext();
        db.Database.Migrate();
    }

    public RodajeDbContext CreateDbContext() => new(_opciones);

    public void Dispose() => _conexion.Dispose();

    /// <summary>Crea una serie con las hojas de Ana y José Daniel y el episodio 1.</summary>
    public (Serie Serie, HojaPersonaje Ana, HojaPersonaje JoseDaniel, Episodio Episodio) CrearSerieDeEjemplo(string nombre = "Demo")
    {
        using var db = CreateDbContext();
        var serie = new Serie { Nombre = nombre };
        db.Series.Add(serie);
        db.SaveChanges();

        var ana = Ejemplos.HojaAna(serie.Id);
        var jose = Ejemplos.HojaJoseDaniel(serie.Id);
        var episodio = new Episodio { SerieId = serie.Id, Numero = 1, Titulo = "Piloto" };
        db.Hojas.AddRange(ana, jose);
        db.Episodios.Add(episodio);
        db.SaveChanges();

        return (serie, ana, jose, episodio);
    }

    public HojaPersonaje AgregarHoja(int serieId, string nombre)
    {
        using var db = CreateDbContext();
        var hoja = Ejemplos.Hoja(nombre, serieId);
        db.Hojas.Add(hoja);
        db.SaveChanges();
        return hoja;
    }
}
