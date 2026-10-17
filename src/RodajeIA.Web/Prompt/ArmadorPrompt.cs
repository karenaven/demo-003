using Microsoft.EntityFrameworkCore;
using RodajeIA.Web.Configuracion;
using RodajeIA.Web.Datos;
using RodajeIA.Web.Datos.Entidades;
using RodajeIA.Web.Personajes;

namespace RodajeIA.Web.Prompt;

/// <summary>
/// Arma el prompt final de una escena generada con el molde (FR-025 a FR-027, contracts/prompt.md).
/// Hojas efectivas, diálogos y texto en pantalla van literales (constitución III). El prompt no se
/// guarda: se arma cada vez desde los datos (FR-015d).
/// </summary>
public sealed class ArmadorPrompt(IDbContextFactory<RodajeDbContext> fabrica, MoldePrompt molde, EstiloGlobal estilo)
{
    /// <summary>El prompt de la escena, o nulo si no está en "generada" (FR-025c).</summary>
    public async Task<string?> ArmarAsync(int escenaId, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);
        var escena = await db.Escenas
            .Include(e => e.Clips).ThenInclude(c => c.Lineas)
            .Include(e => e.Clips).ThenInclude(c => c.Bloque).ThenInclude(b => b!.Tomas).ThenInclude(t => t.Dialogos)
            .Include(e => e.Personajes).ThenInclude(p => p.Hoja)
            .Include(e => e.Variantes)
            .AsSplitQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == escenaId, ct);

        return escena is { Estado: EstadoEscena.Generada } ? Armar(escena) : null;
    }

    public string Armar(Escena escena)
    {
        var variablesEscena = new Dictionary<string, string?>
        {
            ["estilo.texto"] = estilo.Texto,
            ["escena.locacion"] = escena.Locacion,
            ["escena.iluminacion"] = escena.Iluminacion,
            ["escena.puestaEnEscena"] = escena.PuestaEnEscena,
            ["escena.audio"] = escena.Audio,
        };

        var hojas = HojaEfectiva.DeEscena(escena);
        var secciones = new[]
        {
            MotorMolde.Renderizar(molde.Estilo, variablesEscena),
            string.Join('\n', molde.LocacionEIluminacionBase.Select(l => MotorMolde.Renderizar(l, variablesEscena))),
            hojas.Count == 0 ? "" : molde.TituloPersonajes + "\n" + string.Join('\n', hojas.Select(h => h.Renderizar(molde))),
            molde.TituloPuestaEnEscena + "\n" + MotorMolde.Renderizar(molde.PuestaEnEscena, variablesEscena),
            molde.TituloAudio + "\n" + MotorMolde.Renderizar(molde.Audio, variablesEscena),
            MotorMolde.Unir(molde.Bloques.SeparadorBloques, escena.Clips.OrderBy(c => c.Numero).Select(ArmarBloque)),
        };

        return MotorMolde.Unir(molde.SeparadorSecciones, secciones);
    }

    private string ArmarBloque(Clip clip)
    {
        var plantillas = molde.Bloques;
        var titulo = MotorMolde.Renderizar(plantillas.TituloBloque, new Dictionary<string, string?>
        {
            ["bloque.numero"] = clip.Numero.ToString(System.Globalization.CultureInfo.InvariantCulture),
        });

        var lineas = clip.Lineas.ToDictionary(l => l.Id);
        var tomas = (clip.Bloque?.Tomas ?? []).OrderBy(t => t.Orden).Select(toma =>
        {
            // Las líneas de una toma van en el orden del guion, no en el que las listó la IA (FR-026a).
            var dialogos = toma.Dialogos
                .Select(d => lineas[d.LineaDialogoId])
                .OrderBy(l => l.Posicion)
                .Select(l => MotorMolde.Renderizar(plantillas.LineaDialogo, new Dictionary<string, string?> { ["linea.original"] = l.Original }));

            return MotorMolde.Renderizar(plantillas.Toma, new Dictionary<string, string?>
            {
                ["toma.plano"] = toma.Plano,
                ["toma.optica"] = toma.Optica,
                ["toma.iluminacion"] = toma.Iluminacion,
                ["toma.angulo"] = toma.Angulo,
                ["toma.movimiento"] = toma.Movimiento,
                ["toma.accion"] = toma.Accion,
                ["toma.dialogos"] = MotorMolde.Unir(plantillas.SeparadorDialogos, dialogos),
            });
        });

        var cierre = clip.TextoEnPantallaOriginal is { } texto
            ? MotorMolde.Renderizar(plantillas.CierreConTextoEnPantalla, new Dictionary<string, string?> { ["clip.textoEnPantallaOriginal"] = texto })
            : MotorMolde.Renderizar(plantillas.CierreSinTextoEnPantalla, new Dictionary<string, string?>());

        return titulo + "\n" + MotorMolde.Unir(plantillas.SeparadorTomas, tomas) + " " + cierre;
    }
}
