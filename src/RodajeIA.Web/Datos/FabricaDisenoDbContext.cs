using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RodajeIA.Web.Datos;

/// <summary>Usada solo por <c>dotnet ef</c> para crear migraciones sin arrancar la aplicación.</summary>
public sealed class FabricaDisenoDbContext : IDesignTimeDbContextFactory<RodajeDbContext>
{
    public RodajeDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<RodajeDbContext>().UseSqlite("Data Source=rodajeia.db").Options);
}
