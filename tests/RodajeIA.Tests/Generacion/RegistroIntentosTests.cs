using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using RodajeIA.Web.Generacion;

namespace RodajeIA.Tests.Generacion;

public sealed class RegistroIntentosTests : IDisposable
{
    private readonly string _directorio = Path.Combine(Path.GetTempPath(), "rodajeia-registro-" + Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider _reloj = new(new DateTimeOffset(2026, 10, 17, 14, 3, 22, TimeSpan.Zero));

    public void Dispose()
    {
        if (Directory.Exists(_directorio))
        {
            Directory.Delete(_directorio, recursive: true);
        }
    }

    private List<JsonNode> Lineas() =>
        File.ReadAllLines(Path.Combine(_directorio, "generacion-2026-10-17.jsonl")).Select(l => JsonNode.Parse(l)!).ToList();

    [Fact]
    [Trait("AC", "AC-S27")]
    public void Un_intento_invalido_registra_metadatos_y_la_respuesta_cruda()
    {
        var registro = new RegistroIntentos(_directorio, _reloj, NullLogger<RegistroIntentos>.Instance);

        registro.Registrar(new IntentoGeneracion(3, 2, 1, TimeSpan.FromMilliseconds(15123), ResultadoIntento.Invalida, "La respuesta no cumplió las reglas: falta el bloque del clip 3.", "{\"bloques\":[]}"));

        var linea = Assert.Single(Lineas());
        Assert.Equal("2026-10-17T14:03:22+00:00", (string?)linea["fecha"]);
        Assert.Equal(3, (int)linea["episodioId"]!);
        Assert.Equal(2, (int)linea["escena"]!);
        Assert.Equal(1, (int)linea["intento"]!);
        Assert.Equal(15123, (long)linea["duracionMs"]!);
        Assert.Equal("invalida", (string?)linea["resultado"]);
        Assert.Equal("La respuesta no cumplió las reglas: falta el bloque del clip 3.", (string?)linea["motivo"]);
        Assert.Equal("{\"bloques\":[]}", (string?)linea["respuestaCruda"]);
    }

    [Fact]
    [Trait("AC", "AC-S27")]
    public void Un_intento_ok_no_guarda_la_respuesta_cruda_y_nada_contiene_la_clave()
    {
        var registro = new RegistroIntentos(_directorio, _reloj, NullLogger<RegistroIntentos>.Instance);

        registro.Registrar(new IntentoGeneracion(3, 1, 1, TimeSpan.FromSeconds(1), ResultadoIntento.Ok, null, "{\"bloques\":[]}"));
        registro.Registrar(new IntentoGeneracion(3, 2, 1, TimeSpan.FromSeconds(1), ResultadoIntento.Configuracion, "Error de configuración de la IA: la clave fue rechazada (HTTP 401).", null));

        var lineas = Lineas();
        Assert.Equal(2, lineas.Count);
        Assert.Null(lineas[0]["respuestaCruda"]);
        Assert.Equal("configuracion", (string?)lineas[1]["resultado"]);
        Assert.DoesNotContain("AIza", File.ReadAllText(Path.Combine(_directorio, "generacion-2026-10-17.jsonl")), StringComparison.Ordinal);
    }
}
