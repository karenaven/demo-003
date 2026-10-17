using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos.Entidades;

namespace RodajeIA.Web.Datos;

public class RodajeDbContext(DbContextOptions<RodajeDbContext> options) : DbContext(options)
{
    public DbSet<Serie> Series => Set<Serie>();

    public DbSet<HojaPersonaje> Hojas => Set<HojaPersonaje>();

    public DbSet<Episodio> Episodios => Set<Episodio>();

    public DbSet<Escena> Escenas => Set<Escena>();

    public DbSet<Clip> Clips => Set<Clip>();

    public DbSet<LineaDialogo> LineasDialogo => Set<LineaDialogo>();

    public DbSet<PersonajeEnEscena> PersonajesEnEscena => Set<PersonajeEnEscena>();

    public DbSet<PersonajeEnClip> PersonajesEnClip => Set<PersonajeEnClip>();

    public DbSet<VarianteEscena> Variantes => Set<VarianteEscena>();

    public DbSet<Bloque> Bloques => Set<Bloque>();

    public DbSet<Toma> Tomas => Set<Toma>();

    public DbSet<TomaDialogo> TomasDialogo => Set<TomaDialogo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Serie>(e =>
        {
            e.HasMany(s => s.Hojas).WithOne(h => h.Serie).HasForeignKey(h => h.SerieId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(s => s.Episodios).WithOne(ep => ep.Serie).HasForeignKey(ep => ep.SerieId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HojaPersonaje>(e =>
        {
            e.HasIndex(h => new { h.SerieId, h.NombreClave }).IsUnique();
        });

        modelBuilder.Entity<Episodio>(e =>
        {
            e.HasIndex(ep => new { ep.SerieId, ep.Numero }).IsUnique();
            e.HasMany(ep => ep.Escenas).WithOne(es => es.Episodio).HasForeignKey(es => es.EpisodioId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Escena>(e =>
        {
            e.HasIndex(es => new { es.EpisodioId, es.Numero }).IsUnique();
            e.Property(es => es.IntExt).HasConversion<string>();
            e.Property(es => es.Estado).HasConversion<string>();
            e.HasMany(es => es.Clips).WithOne(c => c.Escena).HasForeignKey(c => c.EscenaId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(es => es.Personajes).WithOne(p => p.Escena).HasForeignKey(p => p.EscenaId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(es => es.Variantes).WithOne(v => v.Escena).HasForeignKey(v => v.EscenaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonajeEnEscena>(e =>
        {
            e.HasKey(p => new { p.EscenaId, p.HojaId });
            e.HasOne(p => p.Hoja).WithMany().HasForeignKey(p => p.HojaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Clip>(e =>
        {
            e.HasIndex(c => new { c.EscenaId, c.Numero }).IsUnique();
            e.HasMany(c => c.Lineas).WithOne(l => l.Clip).HasForeignKey(l => l.ClipId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Personajes).WithOne(p => p.Clip).HasForeignKey(p => p.ClipId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Bloque).WithOne(b => b.Clip).HasForeignKey<Bloque>(b => b.ClipId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LineaDialogo>(e =>
        {
            e.HasIndex(l => new { l.ClipId, l.Posicion }).IsUnique();
        });

        modelBuilder.Entity<PersonajeEnClip>(e =>
        {
            e.HasKey(p => new { p.ClipId, p.HojaId });
            e.HasOne(p => p.Hoja).WithMany().HasForeignKey(p => p.HojaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VarianteEscena>(e =>
        {
            e.HasIndex(v => new { v.EscenaId, v.HojaId }).IsUnique();
            e.HasOne(v => v.Hoja).WithMany().HasForeignKey(v => v.HojaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Bloque>(e =>
        {
            e.HasIndex(b => b.ClipId).IsUnique();
            e.HasMany(b => b.Tomas).WithOne(t => t.Bloque).HasForeignKey(t => t.BloqueId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Toma>(e =>
        {
            e.HasIndex(t => new { t.BloqueId, t.Orden }).IsUnique();
            e.HasMany(t => t.Dialogos).WithOne(d => d.Toma).HasForeignKey(d => d.TomaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TomaDialogo>(e =>
        {
            e.HasKey(d => new { d.TomaId, d.LineaDialogoId });
            e.HasOne(d => d.LineaDialogo).WithMany().HasForeignKey(d => d.LineaDialogoId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
