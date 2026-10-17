# Implementation Plan: RodajeIA v1 — del guion al prompt consistente por escena

**Branch**: `001-rodaje-ia-v1` | **Date**: 2026-10-17 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-rodaje-ia-v1/spec.md`

## Summary

App web local y monousuario para convertir el guion de un episodio en un prompt final por
escena, listo para copiar en la IA de video. Tiene cuatro etapas:

1. Un parser determinístico valida y divide el guion y detecta los personajes con hoja
   (constitución I).
2. Un servicio en segundo plano pide a Gemini, escena por escena y de a una en toda la app, los
   bloques de tomas. Usa salida estructurada, con un schema generado desde
   `config/vocabulario.json` (constitución II).
3. La respuesta se valida y se reintenta según reglas fijas, y se guarda de forma atómica
   (constitución IV).
4. El prompt se arma con un intérprete de `config/molde-prompt.json` que inserta literales las
   hojas efectivas, los diálogos y los textos en pantalla (constitución III).

La UI es Blazor Interactive Server, con estados en vivo.

## Technical Context

**Language/Version**: C# 14 sobre .NET 10 (SDK 10.0.401 instalado)

**Primary Dependencies**: ASP.NET Core Blazor Web App (Interactive Server); EF Core 10.0.13 con
SQLite; `Google.GenAI` 1.26.1 (research R-01, R-05)

**Storage**: SQLite (`rodajeia.db`, local, fuera de git), con migraciones de EF Core; archivos
versionados en `config/`; log JSONL en `logs/` (R-10, R-13)

**Testing**: xUnit v3 4.0.2, `Microsoft.Extensions.TimeProvider.Testing` 10.11.0 y SQLite en
memoria; categorías `Rendimiento` y `Gemini` fuera de la suite por defecto (R-15)

**Target Platform**: equipo local del/la productor/a (Windows, macOS o Linux), navegador
moderno en `localhost`

**Project Type**: web app (Blazor Server) en un solo proyecto, más un proyecto de tests

**Performance Goals**: p95 < 1 s para validar y parsear un guion máximo (SC-004) y para armar un
prompt (SC-005); p95 < 30 s para generar una escena de 20 clips (SC-006, validado por el spike)

**Constraints**:
- Sin IA en el guion y la detección.
- Sin valores técnicos fuera del vocabulario.
- Timeout de 60 s por llamada a Gemini y 3 intentos.
- Una sola generación en curso en toda la app.
- Guion de hasta 100.000 caracteres (límite de SignalR ajustado, R-09).
- Suite por defecto sin red.

**Scale/Scope**: un usuario; hasta 10 escenas × 20 clips por episodio; 4 páginas
([contracts/ui.md](contracts/ui.md)); 99 AC del PRD más AC-S01 a AC-S39, cada uno con al menos un
test

No quedan NEEDS CLARIFICATION: todas las incógnitas se resolvieron en
[research.md](research.md).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Antes del diseño | Después del diseño (Phase 1) |
| --- | --- | --- |
| **I. Guion determinístico, sin IA** | ✅ La spec exige parser y detección sin IA (FR-010, FR-011) | ✅ Parser propio por máquina de estados y detector por ocurrencias ([contracts/guion.md](contracts/guion.md), R-03, R-04). Un guion inválido no toca la base: se guarda en una transacción solo si no hay errores (R-10) |
| **II. Vocabulario cerrado** | ✅ FR-015 y FR-023 | ✅ El schema se genera al iniciar desde el JSON ([contracts/gemini.md](contracts/gemini.md)). La validación revisa el vocabulario aunque haya schema (FR-017). La UI solo ofrece valores vigentes. Ningún valor en el código. Cámara y profundidad de campo no existen como campos |
| **III. Continuidad literal por plantilla** | ✅ FR-026, FR-026a, FR-032 | ✅ El motor del molde inserta `linea.original`, `clip.textoEnPantallaOriginal` y las hojas efectivas tal cual ([contracts/prompt.md](contracts/prompt.md)). La IA recibe las líneas solo con su id y las referencia. El prompt es una función de los datos guardados (no se guarda, FR-015d) |
| **IV. Respuesta validada, generación resiliente** | ✅ FR-017 a FR-021 | ✅ Validador con orden fijo (RN-08 a RN-10 y FR-017a); bucle de 3 intentos con `TimeProvider`; guardado atómico; "generando" pasa a "error" al iniciar; variantes inmutables en "generando" y "generada" (R-06, R-07, R-10) |
| **V. Testeable sin el modelo** | ✅ La spec exige tests por AC | ✅ `IClienteIa` con doble de prueba, `FakeTimeProvider` y SQLite en memoria. Los tests con red solo corren en la categoría `Gemini`, aparte (R-15) |
| **VI. Simplicidad y alcance v1** | ✅ Monousuario y sin pantallas de administración | ✅ Dos proyectos; sin repositorios ni capas extra; sin Polly, Serilog, bUnit ni motores de plantillas; un hosted service y un evento en memoria como única infraestructura de concurrencia |
| **Restricciones técnicas** | ✅ | ✅ Stack fijado; secretos en user-secrets; paquetes nuevos en su última versión estable (R-01); cascadas según el PRD; UI y mensajes en español |

**Resultado**: pasa, sin violaciones; no se necesita Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-rodaje-ia-v1/
├── plan.md              # Este archivo
├── research.md          # Phase 0: decisiones R-01 a R-15
├── data-model.md        # Phase 1: entidades, reglas y estados
├── quickstart.md        # Phase 1: guía de validación
├── contracts/
│   ├── guion.md         # Gramática, lectura y catálogo de errores; detección
│   ├── gemini.md        # Instrucción, schema, validación, intentos y log
│   ├── prompt.md        # Interpretación del molde y armado
│   ├── config.md        # Validación de config/ al iniciar
│   └── ui.md            # Rutas y comportamiento de pantallas
├── checklists/
│   ├── requirements.md
│   └── pipeline.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
RodajeIA.slnx
.editorconfig                     # reglas de dotnet format
.config/dotnet-tools.json         # dotnet-ef 10.0.13
config/                           # vocabulario, estilo, instrucción y molde (ya existen)

src/RodajeIA.Web/
├── Program.cs                    # DI, validación de config, migración de estados, HubOptions
├── Components/
│   ├── App.razor, Routes.razor, Layout/
│   └── Pages/                    # Series, Serie, Episodio, Escena (contracts/ui.md)
├── Configuracion/                # CargadorConfiguracion, Vocabulario, MoldePrompt, EstiloGlobal, InstruccionGeneracion
├── Guion/                        # ParserGuion, ErrorGuion, GuionParseado
├── Personajes/                   # DetectorPersonajes, HojaEfectiva
├── Generacion/                   # IClienteIa, ClienteGemini, SchemaSalida, ArmadorInstruccion,
│                                 # ValidadorRespuesta, GeneradorEscena, ServicioGeneracion,
│                                 # NotificadorGeneracion, RegistroIntentos
├── Prompt/                       # MotorMolde, ArmadorPrompt
├── Servicios/                    # Reglas de CRUD: Series, Hojas, Episodios, CargaGuion, Variantes, Tomas
├── Datos/                        # RodajeDbContext, entidades EF, Migrations/
└── wwwroot/js/portapapeles.js

tests/RodajeIA.Tests/
├── Guion/                        # parser y errores (AC-02, AC-03, AC-04, AC-S01 a AC-S07)
├── Personajes/                   # detección (AC-05, AC-S10 a AC-S12)
├── Generacion/                   # schema, validador, generador e intentos (AC-06, AC-10, AC-S13 a AC-S28)
├── Prompt/                       # motor y armado (AC-08, AC-S31 a AC-S36)
├── Configuracion/                # validación de config/ (AC-S29)
├── Servicios/                    # SQLite en memoria: cascadas, unicidad, variantes y tomas (AC-01, AC-07, AC-09, AC-11)
├── Rendimiento/                  # SC-004, SC-005 [Categoria=Rendimiento]
├── Gemini/                       # spike SC-006 y SC-002 [Categoria=Gemini]
└── Fakes/                        # ClienteIaFalso, datos de ejemplo del Anexo A
```

**Structure Decision**: un proyecto web y un proyecto de tests, como fija la constitución. La
lógica determinística (`Guion/`, `Personajes/`, `Prompt/`, `Generacion/ValidadorRespuesta`,
`Configuracion/`) no depende de Blazor ni de EF Core, así que se testea con objetos en memoria.
Los componentes Razor delegan en `Servicios/` y no contienen reglas de negocio.

## Complexity Tracking

Sin violaciones de la constitución. Hay tres piezas de infraestructura, cada una justificada en
[research.md](research.md) como la opción más simple para su requisito:
- `ServicioGeneracion` (R-07);
- `NotificadorGeneracion` (R-08);
- `RegistroIntentos` (R-13).
