using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Servicios;

namespace RodajeIA.Tests.Fakes;

/// <summary>Carga guiones de ejemplo en la base de test y arma respuestas de la IA para sus escenas.</summary>
public static class EscenaDePrueba
{
    /// <summary>Carga el guion en el episodio y devuelve los ids de las escenas en orden.</summary>
    public static async Task<IReadOnlyList<int>> CargarAsync(BaseDatosEnMemoria baseDatos, int episodioId, string guion)
    {
        var resultado = await new CargaGuionService(baseDatos, new EstadoGeneracionFalso()).CargarAsync(episodioId, guion);
        Assert.IsType<ResultadoCarga.Guardado>(resultado);
        await using var db = baseDatos.CreateDbContext();
        return await db.Escenas.Where(e => e.EpisodioId == episodioId).OrderBy(e => e.Numero).Select(e => e.Id).ToListAsync();
    }

    public static async Task<Escena> LeerAsync(BaseDatosEnMemoria baseDatos, int escenaId)
    {
        await using var db = baseDatos.CreateDbContext();
        return await db.Escenas
            .Include(e => e.Clips).ThenInclude(c => c.Lineas)
            .Include(e => e.Clips).ThenInclude(c => c.Bloque).ThenInclude(b => b!.Tomas).ThenInclude(t => t.Dialogos)
            .Include(e => e.Personajes).ThenInclude(p => p.Hoja)
            .Include(e => e.Variantes)
            .AsSplitQuery()
            .SingleAsync(e => e.Id == escenaId);
    }

    /// <summary>
    /// Marca la escena como generada con una toma por clip ("plano medio", "50mm", sin ángulo ni
    /// movimiento) que referencia todas sus líneas. <paramref name="ajustar"/> permite cambiar cada toma.
    /// </summary>
    public static async Task MarcarGeneradaAsync(BaseDatosEnMemoria baseDatos, int escenaId, Action<Clip, Toma>? ajustar = null)
    {
        await using var db = baseDatos.CreateDbContext();
        var escena = await db.Escenas.Include(e => e.Clips).ThenInclude(c => c.Lineas).SingleAsync(e => e.Id == escenaId);
        foreach (var clip in escena.Clips)
        {
            var toma = new Toma
            {
                Orden = 1,
                Plano = "plano medio",
                Optica = "50mm",
                Iluminacion = "continuidad con la luz base",
                Accion = $"Acción del clip {clip.Numero}.",
                Dialogos = clip.Lineas.OrderBy(l => l.Posicion).Select(l => new TomaDialogo { LineaDialogoId = l.Id }).ToList(),
            };
            ajustar?.Invoke(clip, toma);
            db.Bloques.Add(new Bloque { ClipId = clip.Id, Tomas = [toma] });
        }

        escena.Estado = EstadoEscena.Generada;
        await db.SaveChangesAsync();
    }

    /// <summary>Una respuesta válida: un bloque por clip con una sola toma que usa todas sus líneas.</summary>
    public static string RespuestaValida(Escena escena) =>
        Respuesta(escena.Clips.OrderBy(c => c.Numero).Select(c => (c.Numero, Ids(c))));

    public static string Respuesta(IEnumerable<(int Clip, IReadOnlyList<string> Dialogos)> bloques) =>
        new System.Text.Json.Nodes.JsonObject
        {
            ["bloques"] = new System.Text.Json.Nodes.JsonArray(bloques.Select(b => (System.Text.Json.Nodes.JsonNode)new System.Text.Json.Nodes.JsonObject
            {
                ["clip"] = b.Clip,
                ["tomas"] = new System.Text.Json.Nodes.JsonArray(Toma(b.Dialogos)),
            }).ToArray()),
        }.ToJsonString();

    public static System.Text.Json.Nodes.JsonObject Toma(IReadOnlyList<string> dialogos, string accion = "Ana mira a José Daniel.") => new()
    {
        ["plano"] = "plano medio",
        ["optica"] = "50mm",
        ["iluminacion"] = "continuidad con la luz base",
        ["accion"] = accion,
        ["dialogos"] = new System.Text.Json.Nodes.JsonArray(dialogos.Select(d => (System.Text.Json.Nodes.JsonNode)d).ToArray()),
    };

    public static IReadOnlyList<string> Ids(Clip clip) =>
        clip.Lineas.OrderBy(l => l.Posicion).Select(l => LineaDialogo.IdIaDe(clip.Numero, l.Posicion)).ToList();
}
