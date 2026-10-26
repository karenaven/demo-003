---
description: "Lista de tareas para implementar RodajeIA v1"
---

# Tasks: RodajeIA v1 — del guion al prompt consistente por escena

**Input**: documentos de diseño en `specs/001-rodaje-ia-v1/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: obligatorios. La constitución V exige al menos un test automatizado por cada AC del
PRD y por cada AC-S01 a AC-S39, y que el test se escriba antes que la lógica determinística. En
cada historia, los tests van primero y MUST fallar antes de implementar.

**Convención de trazabilidad**: cada test lleva `[Trait("AC", "<id>")]` por cada criterio que
cubre (por ejemplo, `[Trait("AC", "AC-03a")]` o `[Trait("AC", "AC-S01")]`). Los tests con red
llevan además `[Trait("Categoria", "Gemini")]` y los de rendimiento,
`[Trait("Categoria", "Rendimiento")]`; ambos son `Explicit = true` (xUnit v3) y no corren con
`dotnet test` por defecto.

**Organization**: las tareas se agrupan por historia de usuario para implementarlas y probarlas
por separado.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes).
- **[Story]**: historia de la spec a la que pertenece (US1 a US6).
- Las rutas son relativas a la raíz del repositorio.

---

## Phase 1: Setup (inicialización)

**Purpose**: crear la solución, los proyectos y las herramientas.

- [X] T001 Crear la solución y los proyectos:
  - `dotnet new blazor --interactivity Server --all-interactive --framework net10.0 -o src/RodajeIA.Web`;
  - un proyecto de clases `tests/RodajeIA.Tests` (net10.0) con `OutputType=Exe`, como pide xUnit v3;
  - `RodajeIA.slnx` en la raíz, con los dos proyectos y la referencia de Tests a Web.
- [X] T002 Agregar a `src/RodajeIA.Web/RodajeIA.Web.csproj` los paquetes `Microsoft.EntityFrameworkCore.Sqlite` 10.0.13, `Microsoft.EntityFrameworkCore.Design` 10.0.13 (`PrivateAssets=all`) y `Google.GenAI` 1.26.1 (research R-01).
- [X] T003 Agregar a `tests/RodajeIA.Tests/RodajeIA.Tests.csproj` los paquetes `xunit.v3` 4.0.2, `xunit.runner.visualstudio` 4.0.1, `Microsoft.NET.Test.Sdk` 18.11.0 y `Microsoft.Extensions.TimeProvider.Testing` 10.11.0 (research R-01).
- [X] T004 [P] Crear `.config/dotnet-tools.json` con la herramienta local `dotnet-ef` 10.0.13.
- [X] T005 [P] Crear `.editorconfig` en la raíz con las reglas de C#: indentación de 4 espacios, `file_scoped` namespaces, `var` cuando el tipo es evidente, llaves obligatorias, nullable habilitado y severidad `warning` para estilo (lo verifica `dotnet format --verify-no-changes`).
- [X] T006 [P] Ampliar `.gitignore` en la raíz con `bin/`, `obj/`, `*.db`, `*.db-shm`, `*.db-wal` y `logs/`.
- [X] T007 Enlazar `config/**` como contenido copiado a la salida (`<Content Include="..\..\config\**" LinkBase="config" CopyToOutputDirectory="PreserveNewest" />`) en `src/RodajeIA.Web/RodajeIA.Web.csproj` y en `tests/RodajeIA.Tests/RodajeIA.Tests.csproj`, para que la app y los tests lean los archivos versionados reales.
- [X] T008 Habilitar `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>` y `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` en los dos `.csproj`. Comprobar que `dotnet build`, `dotnet test` y `dotnet format --verify-no-changes` pasan con la plantilla vacía.

---

## Phase 2: Foundational (bloquea a todas las historias)

**Purpose**: modelo de datos, configuración validada, motor del molde, hoja efectiva y
utilidades de test que usan varias historias.

**⚠️ CRITICAL**: ninguna historia puede empezar hasta terminar esta fase.

### Modelo de datos

- [X] T009 [P] Crear `src/RodajeIA.Web/Datos/Entidades/Serie.cs` (`Id`, `Nombre` "Obligatorio") y `src/RodajeIA.Web/Datos/Entidades/Episodio.cs` (Anexo B; RF-01a–d, RF-11a–d; FR-001, FR-004):
  - `Id`, `SerieId`;
  - `Numero`: "≥ 1. Índice único (SerieId, Numero). No se edita";
  - `Titulo`: "Obligatorio; editable";
  - `Guion`: "El texto tal como se pegó"; nulo hasta la primera carga válida.
- [X] T010 [P] Crear `src/RodajeIA.Web/Datos/Entidades/HojaPersonaje.cs` (Anexo B; RF-01e–h; FR-002, FR-002a, FR-002b):
  - `Id`, `SerieId`;
  - `Nombre`;
  - `NombreClave`: "`Nombre.ToUpperInvariant()`. Índice único (SerieId, NombreClave)";
  - `Edad`, `DescripcionFisica`, `Peinado`, `Vestuario`, `Personalidad` y `Rol`, obligatorios;
  - `HeridasMarcas`: opcional.
- [X] T011 [P] Crear `src/RodajeIA.Web/Datos/Entidades/Escena.cs` con los enums `IntExt` (`Int` | `Ext`) y `EstadoEscena` (`Pendiente` | `Generando` | `Generada` | `Error`) (Anexo B; RF-04d, RF-10d; FR-010, FR-019, FR-019a):
  - `Numero`: "1..10. Índice único (EpisodioId, Numero)";
  - `IntExt`, `Lugar`, `MomentoDia`, `Locacion`, `Iluminacion`, `PuestaEnEscena`, `Audio`, `Estado`;
  - `MotivoError`: "Solo en `Error`; se borra al salir de `Error`".
- [X] T012 [P] Crear `src/RodajeIA.Web/Datos/Entidades/Clip.cs` y `src/RodajeIA.Web/Datos/Entidades/LineaDialogo.cs` (Anexo B; RF-04b, RF-04c, RN-04; FR-010, FR-016a, FR-026a):
  - `Clip.Numero`: "1..20. Índice único (EscenaId, Numero)";
  - `Clip.Accion`: "Las líneas de acción del clip, unidas con `\n`";
  - `Clip.TextoEnPantalla` y `Clip.TextoEnPantallaOriginal`;
  - `LineaDialogo`: `Posicion`, `Personaje`, `Acotacion?`, `Texto` y `Original` ("La línea completa del guion, sin espacios al inicio ni al final");
  - la propiedad calculada, no mapeada, `IdIa => $"c{Clip.Numero}-d{Posicion}"`.
- [X] T013 [P] Crear `src/RodajeIA.Web/Datos/Entidades/PersonajeEnEscena.cs` (PK compuesta, `Orden` "Orden de primera aparición en la escena"), `PersonajeEnClip.cs` y `VarianteEscena.cs` (Anexo B; RF-05a, RF-05b, RF-09a, RN-05; FR-011, FR-025b, FR-029):
  - `Edad?`, `Peinado?`, `Vestuario?`: "Nulo = no reemplaza";
  - `ReemplazaHeridas` (bool) y `HeridasMarcas?`;
  - índice único (EscenaId, HojaId).

  Los tres van en `src/RodajeIA.Web/Datos/Entidades/`.
- [X] T014 [P] Crear `src/RodajeIA.Web/Datos/Entidades/Bloque.cs` ("Índice único (un bloque por clip)"), `Toma.cs` y `TomaDialogo.cs` (PK compuesta `TomaId` + `LineaDialogoId`), en `src/RodajeIA.Web/Datos/Entidades/` (Anexo B; RF-06a, RF-07b, RN-09; FR-015, FR-023).
  - `Toma`: `Orden`, `Plano`, `Optica`, `Iluminacion`, `Angulo?`, `Movimiento?` y `Accion` ("No vacía; sin espacios al inicio ni al final").
- [X] T015 Crear `src/RodajeIA.Web/Datos/RodajeDbContext.cs` (RF-01d, RF-01h, RF-02b, RF-11d; FR-001, FR-003, FR-004, FR-007):
  - los `DbSet` de todas las entidades;
  - los índices únicos de [data-model.md](data-model.md);
  - eliminación en cascada en todas las relaciones padre → hijo, incluidas Hoja → Variante, PersonajeEnEscena y PersonajeEnClip, y LineaDialogo → TomaDialogo;
  - los enums guardados como texto.
- [X] T016 Generar la migración inicial con `dotnet ef migrations add Inicial --project src/RodajeIA.Web --output-dir Datos/Migrations` y comprobar que `dotnet ef database update` crea `rodajeia.db` (constitución: restricciones técnicas, persistencia).
- [X] T017 Crear `tests/RodajeIA.Tests/Fakes/BaseDatosEnMemoria.cs`:
  - abre una `SqliteConnection("DataSource=:memory:")`, crea el esquema con `Database.Migrate()` (no con `EnsureCreated`, para que la suite pruebe las migraciones de `Datos/Migrations`) y expone un `IDbContextFactory<RodajeDbContext>` que reutiliza la conexión;
  - incluye ayudas para crear una serie, sus hojas "Ana" y "José Daniel" y un episodio.
- [X] T018 [P] Crear `tests/RodajeIA.Tests/Fakes/Ejemplos.cs` con:
  - la escena válida del Anexo A, como constante;
  - las hojas de ejemplo de "Ana" (con heridas/marcas vacías) y "José Daniel", con todos sus campos;
  - un generador de guiones de N escenas × M clips para los tests de límites y de rendimiento.

### Configuración (`config/`)

- [X] T019 [P] Escribir `tests/RodajeIA.Tests/Configuracion/CargadorConfiguracionTests.cs` con un caso por cada regla de [contracts/config.md](contracts/config.md):
  - para cada archivo, que falte, que tenga JSON inválido o que tenga el problema de esa regla (vocabulario, estilo, instrucción y molde);
  - que se informen varios archivos inválidos a la vez;
  - que los cuatro archivos reales del repositorio son válidos.

  Cubre `AC-S29`.
- [X] T020 [P] Crear los modelos inmutables en `src/RodajeIA.Web/Configuracion/` (RN-02, RN-06, Anexo D, Anexo E; FR-015, FR-025a):
  - `Vocabulario.cs`: categorías, valores, `obligatorio`, y `Contiene(categoria, valor)`;
  - `MoldePrompt.cs`: secciones, plantillas y separadores tal como en `config/molde-prompt.json`;
  - `EstiloGlobal.cs`;
  - `InstruccionGeneracion.cs`: el texto, con los marcadores `{{escena}}` y `{{personajes}}`.
- [X] T021 Implementar `src/RodajeIA.Web/Configuracion/CargadorConfiguracion.cs` y `ConfiguracionInvalidaException.cs`.
  - El cargador lee los cuatro archivos desde `{AppContext.BaseDirectory}/config` y los valida según [contracts/config.md](contracts/config.md) (FR-015a, FR-015c).
  - La excepción acumula todos los problemas en la forma "{archivo}: {problema}".
- [X] T022 Configurar `src/RodajeIA.Web/Program.cs` (FR-015d):
  - cargar la configuración antes de `app.Run()`; si falla, escribir `Configuración inválida: …` en la consola y salir con código 1;
  - registrar `Vocabulario`, `MoldePrompt`, `EstiloGlobal` e `InstruccionGeneracion` como singletons;
  - registrar `AddDbContextFactory<RodajeDbContext>` con SQLite (`Data Source=rodajeia.db`);
  - fijar `HubOptions.MaximumReceiveMessageSize = 1024 * 1024` (research R-09);
  - no aplicar migraciones al iniciar (se aplican con `dotnet ef database update`).

### Motor del molde y hoja efectiva (los usan US2, US3 y US6)

- [X] T023 [P] Escribir `tests/RodajeIA.Tests/Prompt/MotorMoldeTests.cs` según [contracts/prompt.md](contracts/prompt.md):
  - reemplazo literal de `{{x}}`;
  - `[ … ]` omitido completo si una variable está vacía o es nula;
  - los separadores nunca aparecen al principio ni al final;
  - regla de puntuación: se omite el "." después de un valor terminado en `.`, `!`, `?` o `…` (`AC-S34`).
- [X] T024 Implementar `src/RodajeIA.Web/Prompt/MotorMolde.cs`. Recibe una plantilla y un diccionario de variables y aplica las tres construcciones del contrato. No usa regex sobre el texto de los valores; los valores se insertan sin modificarse.
- [X] T025 [P] Escribir `tests/RodajeIA.Tests/Personajes/HojaEfectivaTests.cs`:
  - una variante que reemplaza edad, peinado y vestuario;
  - `ReemplazaHeridas` con `HeridasMarcas` vacío da una hoja sin heridas;
  - sin variante se obtiene la hoja base.

  Cubre `AC-09a` en su parte de cálculo.
- [X] T026 Implementar `src/RodajeIA.Web/Personajes/HojaEfectiva.cs`:
  - un record con los campos de la hoja;
  - `Calcular(HojaPersonaje, VarianteEscena?)`;
  - `Renderizar(MoldePrompt, MotorMolde)` con la plantilla `porPersonaje`, que se reutiliza en la instrucción y en el prompt para que el texto sea idéntico (constitución III).
- [X] T027 Crear el layout en español (RF-11e; contracts/ui.md):
  - `src/RodajeIA.Web/Components/Layout/MainLayout.razor`;
  - `NavMenu.razor`, con el enlace "Series" a `/`;
  - `<html lang="es">` en `Components/App.razor`;
  - quitar las páginas de ejemplo de la plantilla (Counter, Weather).

**Checkpoint**: modelo, configuración, motor del molde y hoja efectiva listos y testeados.

---

## Phase 3: User Story 1 — Cargar y validar el guion de un episodio (Priority: P1) 🎯 MVP

**Goal**: pegar un guion, validarlo sin IA, guardarlo dividido en escenas y clips y detectar a
los personajes con hoja, o bien rechazarlo completo con todos sus errores.

**Independent Test**: con una serie, un episodio y hojas cargadas en la base de test, procesar
la escena del Anexo A y cada error del Anexo A; comprobar escenas, clips, campos, diálogos,
personajes detectados, y la línea y el motivo de cada error ([contracts/guion.md](contracts/guion.md)).

### Tests for User Story 1 ⚠️

> Escribirlos primero y comprobar que fallan.

- [X] T028 [P] [US1] Escribir `tests/RodajeIA.Tests/Guion/ParserGuionValidoTests.cs`:
  - con la escena del Anexo A: encabezado y campos (`AC-04a`), 2 clips (`AC-04b`), acción y diálogos con y sin acotación (`AC-04c`);
  - texto en pantalla en un clip (`AC-04d`);
  - determinismo: el mismo guion parseado dos veces da lo mismo (`AC-04e`);
  - guion común en el encabezado (`AC-03o`) y líneas en blanco (`AC-03p`);
  - CRLF y BOM dan el mismo resultado y la misma numeración que LF (`AC-S03`);
  - comillas tipográficas e internas conservadas en `Original` (`AC-S07`, parte de parseo).
- [X] T029 [P] [US1] Escribir `tests/RodajeIA.Tests/Guion/ParserGuionErroresTests.cs`, un test por fila del catálogo E00 a E17, comprobando la línea y el motivo exacto del contrato. Cubre:
  - `AC-03a`, `AC-03b`, `AC-03c`, `AC-03d`, `AC-03e`, `AC-03g`;
  - `AC-03h`, `AC-03i`, `AC-03j`, `AC-03k`, `AC-03l`, `AC-03m`, `AC-03n`;
  - `AC-03q`, `AC-03r`, `AC-03s`, `AC-03t`, `AC-03u`;
  - `AC-S01` (`AUDIO:` después de `CLIP 1` da E06 y E07);
  - `AC-S02` (texto del motivo), `AC-S04` (campo en dos líneas), `AC-S05` (100.001 caracteres da un único E00) y `AC-S06` (`Locacion:`);
  - que la lista de errores está ordenada por línea y código;
  - `SC-007`: todo guion con errores se rechaza completo y cada error tiene línea y motivo.
- [X] T030 [P] [US1] Escribir `tests/RodajeIA.Tests/Personajes/DetectorPersonajesTests.cs`:
  - acción (`AC-05a`) y marcador de diálogo (`AC-05b`);
  - personajes sin hoja que no se detectan (`AC-05c`, `AC-05d`);
  - "Anabel" (`AC-05e`) y "Jose Daniel" sin tilde (`AC-05f`);
  - solo en `PUESTA EN ESCENA:` (`AC-05g`);
  - el nombre más largo en la acción y en la puesta en escena (`AC-S10`);
  - límites de palabra `¡Ana!`, `Ana,`, `Ana-María`, `Ana's` (`AC-S11`);
  - `JOSE DANIEL:` y "JOSÉ DANIEL entra." (`AC-S12`);
  - una acción en NFD que detecta a "José" (research R-04);
  - el orden de primera aparición, con desempate alfabético (`AC-S32`, parte de detección).
- [X] T031 [P] [US1] Escribir `tests/RodajeIA.Tests/Servicios/CargaGuionServiceTests.cs` con SQLite en memoria:
  - guion válido guardado en el episodio (`AC-02a`);
  - carga sin episodio rechazada (`AC-02b`);
  - el reemplazo elimina las escenas, clips, bloques y variantes anteriores (`AC-02c`);
  - un guion inválido conserva el anterior (`AC-02d`) o deja el episodio sin guion (`AC-03f`);
  - las escenas nuevas quedan en `Pendiente` y no se llamó a ningún `IClienteIa` (`AC-06j`);
  - se guardan `PersonajeEnEscena.Orden` y `PersonajeEnClip`.

### Implementation for User Story 1

- [X] T032 [P] [US1] Crear los records de resultado en `src/RodajeIA.Web/Guion/GuionParseado.cs` (`GuionParseado`, `EscenaParseada`, `ClipParseado`, `LineaDialogoParseada`, `TextoEnPantallaParseado`) y `src/RodajeIA.Web/Guion/ErrorGuion.cs` (`Linea`, `Codigo`, `Motivo`), junto con `ResultadoGuion` (`Valido(GuionParseado)` | `Invalido(IReadOnlyList<ErrorGuion>)`).
- [X] T033 [US1] Implementar `src/RodajeIA.Web/Guion/LectorLineas.cs` (FR-008b, FR-008d):
  - corta en el límite de 100.000 caracteres;
  - descarta el BOM;
  - separa por LF, CRLF o CR;
  - numera las líneas físicas desde 1;
  - entrega cada línea recortada y marca las vacías.
- [X] T034 [US1] Implementar `src/RodajeIA.Web/Guion/ParserGuion.cs` como máquina de estados, según la gramática y el catálogo de [contracts/guion.md](contracts/guion.md) (FR-008 a FR-010, FR-008a, FR-008c, FR-008e, FR-008f).
  - Acumula todos los errores y nunca devuelve un resultado parcial.
  - Los textos de los motivos salen de una sola tabla en `src/RodajeIA.Web/Guion/MotivosGuion.cs`.
- [X] T035 [US1] Implementar `src/RodajeIA.Web/Personajes/DetectorPersonajes.cs` (FR-011 a FR-012):
  - compara en NFC y por ordinal;
  - aplica el límite de palabra de FR-011b y el nombre más largo por posición;
  - compara el marcador con `NombreClave`;
  - ignora los nombres todo en mayúsculas;
  - calcula el orden de primera aparición por escena.
- [X] T036 [US1] Implementar `src/RodajeIA.Web/Servicios/CargaGuionService.cs`. Recibe `episodioId` y el texto, parsea, detecta con las hojas de la serie y, si es válido, en una transacción:
  - reemplaza `Episodio.Guion`;
  - elimina las escenas anteriores;
  - inserta escenas en `Pendiente`, clips, líneas y personajes detectados.

  Si es inválido, devuelve los errores sin tocar la base (FR-006, FR-007, FR-009, FR-013).
- [X] T037 [US1] Crear `src/RodajeIA.Web/Components/Pages/EpisodioPagina.razor` (ruta `/episodios/{episodioId:int}`), según [contracts/ui.md](contracts/ui.md):
  - título del episodio;
  - `<textarea>` enlazado con `onchange` y botón "Validar y guardar";
  - confirmación si ya hay guion;
  - lista "Línea {n}: {motivo}" si el guion es inválido;
  - lista de escenas en orden con su estado.

**Checkpoint**: US1 completa. Un guion se valida y se guarda; los tests de US1 pasan.

---

## Phase 4: User Story 2 — Generar los bloques de tomas de cada escena (Priority: P1)

**Goal**: con "Generar", pedir a Gemini, de a una escena y en toda la app, un bloque por clip con
valores del vocabulario; validar, reintentar, guardar de forma atómica y mostrar el avance en
vivo.

**Independent Test**: con un guion cargado y `IClienteIa` reemplazado por un doble con
respuestas controladas, más `FakeTimeProvider`, comprobar estados, intentos, esperas, motivos,
persistencia por escena y bloqueo global, sin red.

### Tests for User Story 2 ⚠️

- [X] T038 [P] [US2] Escribir `tests/RodajeIA.Tests/Generacion/SchemaSalidaTests.cs` (FR-015):
  - el schema generado desde `config/vocabulario.json` tiene un enum por categoría con los valores en el orden del archivo;
  - `required` incluye solo las categorías obligatorias, más `accion` y `dialogos`;
  - con un vocabulario de prueba distinto, el schema cambia sin tocar el código.
- [X] T039 [P] [US2] Escribir `tests/RodajeIA.Tests/Generacion/ArmadorInstruccionTests.cs` según [contracts/gemini.md](contracts/gemini.md):
  - el formato exacto de `{{escena}}` para la escena del Anexo A;
  - los ids `c1-d1` y `c1-d2`, y que no hay texto de otras escenas (`AC-S16`);
  - `(ninguno)` y `(ninguna)` cuando falta información;
  - `{{personajes}}` con la hoja efectiva renderizada por `porPersonaje`;
  - la variante "abrigo gris" presente y "chaqueta negra" ausente (`AC-09h`).
- [X] T040 [P] [US2] Escribir `tests/RodajeIA.Tests/Generacion/ValidadorRespuestaTests.cs`, una regla por test y en el orden de prioridad del contrato:
  - respuesta válida con 3 bloques y valores del vocabulario (`AC-06a`);
  - RN-09: falta una línea (`AC-06b`), línea repetida (`AC-06c`), id de otro clip (`AC-06d`);
  - RN-08: 2 bloques (`AC-06e`), clips 1, 1, 3 (`AC-06g`);
  - RN-10: bloque sin tomas (`AC-06h`), acción vacía (`AC-06i`);
  - JSON inválido o plano fuera del vocabulario (`AC-S17`);
  - diálogos fuera de orden (`AC-S18`);
  - opcionales `""` válidos y obligatorios `""` inválidos (`AC-S19`);
  - comillas en la acción (`AC-S20`);
  - con varias fallas, el motivo informa solo la primera (`AC-S21`);
  - `SC-003`: ningún campo técnico fuera del vocabulario llega a guardarse.
- [X] T041 [P] [US2] Escribir `tests/RodajeIA.Tests/Generacion/GeneradorEscenaTests.cs` con un `ClienteIaFalso` y `FakeTimeProvider`:
  - 3 errores dan exactamente 3 intentos y estado `Error` (`AC-10a`);
  - error y después éxito dan `Generada` (`AC-10c`);
  - timeout de 60 s en cada intento, esperas de 10 s y 30 s, notificaciones "intento i de 3" y motivo "La IA no respondió a tiempo" (`AC-S22`);
  - clave faltante: un solo intento, sin esperas y motivo de configuración (`AC-S23`);
  - falla al guardar: ningún bloque y motivo "error interno" (`AC-S24`);
  - el motivo se borra al reintentar con éxito y nunca contiene la clave (`AC-S25`);
  - la hoja efectiva se calcula al iniciar cada llamada (FR-003a).
- [X] T042 [P] [US2] Crear `tests/RodajeIA.Tests/Fakes/ClienteIaFalso.cs`, que implementa `IClienteIa` con una cola de resultados programados (JSON, excepción HTTP con código, demora) y registra cada instrucción recibida.
- [X] T043 [P] [US2] Escribir `tests/RodajeIA.Tests/Generacion/ServicioGeneracionTests.cs`:
  - dos escenas pendientes terminan en `Generada` (`AC-06f`);
  - solo se generan las pendientes (`AC-06k`);
  - una falla en la escena 3 conserva los bloques de la 1 y la 2 (`AC-10b`, `SC-009`);
  - reintentar una escena en `Error` la deja `Generada` (`AC-10d`);
  - una escena en curso se ve en `Generando` (`AC-10e`);
  - la escena 2 empieza solo después de la 1 y la 3 sigue `Pendiente` (`AC-S13`);
  - `TryIniciar` rechaza un segundo pedido en otro episodio y un reintento (`AC-S14`);
  - un error de configuración detiene la cola y deja pendientes las escenas que esperaban (`AC-S23`);
  - reintentar genera solo esa escena (`AC-S28`, primera parte);
  - un error interno en la escena 1 la deja en `Error` y la escena 2 se genera igual (FR-018a);
  - `EpisodioEnCurso` refleja las escenas en cola.
- [X] T044 [P] [US2] Escribir `tests/RodajeIA.Tests/Generacion/RecuperacionArranqueTests.cs`:
  - `Generando` pasa a `Error` con el motivo "Generación interrumpida por un cierre de la aplicación" (`AC-10f`);
  - las escenas `Pendiente` siguen así y no se llama a la IA (`AC-S28`, segunda parte).
- [X] T045 [P] [US2] Escribir `tests/RodajeIA.Tests/Generacion/RegistroIntentosTests.cs`:
  - un intento inválido escribe una línea JSONL con `escena`, `intento`, `duracionMs`, `resultado = "invalida"`, `motivo` y `respuestaCruda`;
  - un intento `ok` no incluye `respuestaCruda`;
  - el archivo nunca contiene el valor de `Gemini:ApiKey`.

  Cubre `AC-S27`.
- [X] T046 [P] [US2] Escribir `tests/RodajeIA.Tests/Generacion/NotificadorGeneracionTests.cs`: los suscriptores reciben cada cambio de estado e intento y el cambio de "generación en curso"; desuscribirse deja de notificar (`AC-S26`, parte de servicio).

### Implementation for User Story 2

- [X] T047 [P] [US2] Crear `src/RodajeIA.Web/Generacion/IClienteIa.cs`:
  - `Task<RespuestaIa> GenerarAsync(string instruccion, CancellationToken ct)`, donde `RespuestaIa(string Json)`;
  - `ErrorIa` (excepción con `CategoriaFalla`: `Timeout`, `ErrorIa`, `Configuracion`, `Interno`) y `CategoriaFalla.cs`, según la tabla de research R-06.
- [X] T048 [P] [US2] Implementar `src/RodajeIA.Web/Generacion/SchemaSalida.cs`, que construye una vez, desde `Vocabulario`, el schema de [contracts/gemini.md](contracts/gemini.md) como objeto serializable y como el tipo de schema del SDK `Google.GenAI`. Ningún valor del vocabulario aparece en el código (constitución II).
- [X] T049 [P] [US2] Implementar `src/RodajeIA.Web/Generacion/ArmadorInstruccion.cs`:
  - recibe la escena con clips, líneas, personajes presentes y variantes;
  - genera `{{escena}}` y `{{personajes}}` en el formato del contrato;
  - reemplaza los marcadores de `InstruccionGeneracion`.
- [X] T050 [P] [US2] Implementar `src/RodajeIA.Web/Generacion/ValidadorRespuesta.cs` sobre el JSON crudo con `System.Text.Json`:
  - aplica las reglas 1 a 6 del contrato, en orden;
  - devuelve `Valida(bloques)` o `Invalida(motivo)`, donde `motivo` es el detalle, por ejemplo "falta el bloque del clip 3".
- [X] T051 [US2] Implementar `src/RodajeIA.Web/Generacion/ClienteGemini.cs` con `Google.GenAI`:
  - `Gemini:Modelo` por defecto `gemini-3.1-flash-lite`; `Gemini:ApiKey` desde la configuración;
  - si falta la clave, lanzar `ErrorIa(Configuracion)` sin llamar a la red;
  - `ResponseMimeType = "application/json"` y el schema de `SchemaSalida`;
  - clasificar las fallas: 401, 403, 404 y 400 por clave dan `Configuracion`; 429, 5xx y errores de red dan `ErrorIa`.

  Verificar los nombres exactos de las propiedades del SDK al implementar (research R-05).
- [X] T052 [P] [US2] Implementar `src/RodajeIA.Web/Generacion/RegistroIntentos.cs`, que agrega líneas JSONL a `logs/generacion-AAAA-MM-DD.jsonl` (relativo al directorio de contenido) con el formato de [contracts/gemini.md](contracts/gemini.md), y además escribe un resumen con `ILogger` (FR-019c).
- [X] T053 [P] [US2] Implementar `src/RodajeIA.Web/Generacion/NotificadorGeneracion.cs` como singleton con los eventos `EstadoEscenaCambiado(escenaId, episodioId, estado, intento, motivo)` y `GeneracionEnCursoCambiada(bool, episodioId?)` (research R-08).
- [X] T054 [US2] Implementar `src/RodajeIA.Web/Generacion/GeneradorEscena.cs` (FR-016 a FR-019a):
  - marca la escena `Generando` y hace hasta 3 intentos;
  - en cada intento, recalcula la hoja efectiva y arma la instrucción;
  - usa un timeout de 60 s con `CancellationTokenSource(TimeSpan, TimeProvider)` y esperas de 10 s y 30 s con `Task.Delay(…, TimeProvider)`;
  - con `Configuracion`, no reintenta;
  - registra cada intento y notifica "intento i de 3";
  - si la respuesta es válida, guarda en una transacción los bloques, las tomas, `TomaDialogo` y `Estado = Generada`, y borra `MotivoError`;
  - si falla, guarda `Estado = Error` con el motivo "{categoría}: {detalle}";
  - devuelve el resultado, para que la cola sepa si debe detenerse.
- [X] T055 [US2] Implementar `src/RodajeIA.Web/Generacion/ServicioGeneracion.cs` como `BackgroundService` singleton (FR-014, FR-014a, FR-018a, FR-020):
  - un único hueco de trabajo;
  - `TryIniciarEpisodio(episodioId)`: encola las escenas `Pendiente` en orden de número;
  - `TryReintentar(escenaId)`: solo escenas en `Error`;
  - `bool EnCurso`, `int? EpisodioEnCurso` e `IReadOnlySet<int> EscenasEnCola`;
  - genera de a una con `GeneradorEscena`;
  - se detiene ante un error de configuración;
  - notifica el cambio de "en curso".
- [X] T056 [US2] Implementar `src/RodajeIA.Web/Datos/RecuperacionArranque.cs` (FR-021): pasa `Generando` a `Error` con el motivo de cierre. Invocarlo desde `src/RodajeIA.Web/Program.cs` antes de aceptar pedidos; la generación no se reanuda sola.
- [X] T057 [US2] Registrar en `src/RodajeIA.Web/Program.cs`:
  - `TimeProvider.System`;
  - `IClienteIa` → `ClienteGemini`;
  - `SchemaSalida`, `ArmadorInstruccion`, `ValidadorRespuesta`, `RegistroIntentos`, `NotificadorGeneracion` y `GeneradorEscena`;
  - `ServicioGeneracion` como singleton y hosted service.
- [X] T058 [US2] Bloquear en `src/RodajeIA.Web/Servicios/CargaGuionService.cs` la carga de un guion en un episodio con generación en curso, con el motivo correspondiente (FR-004a), y agregar el test en `tests/RodajeIA.Tests/Servicios/CargaGuionServiceTests.cs` (`AC-S15`, parte de recarga).
- [X] T059 [US2] Ampliar `src/RodajeIA.Web/Components/Pages/EpisodioPagina.razor` (FR-019b):
  - botón "Generar" y, por escena en `Error`, el motivo y "Reintentar";
  - mientras hay una generación en curso, los botones deshabilitados con "Hay una generación en curso" y la carga de guion deshabilitada;
  - estado "Generando · intento {i} de 3" en vivo;
  - suscripción a `NotificadorGeneracion` con `InvokeAsync(StateHasChanged)` e `IDisposable` para desuscribirse;
  - enlace a `/escenas/{id}` cuando la escena está `Generada`.
- [X] T060 [US2] Escribir `tests/RodajeIA.Tests/Gemini/SpikeGeminiTests.cs` con `[Fact(Explicit = true)]`, `[Trait("Categoria", "Gemini")]` y omisión si falta `Gemini:ApiKey`:
  - SC-006: 10 generaciones de una escena de 20 clips (generada con `Ejemplos`); informa el p95 de la llamada exitosa y lo compara con 30 s;
  - SC-002: 10 generaciones de la escena del Anexo A y 10 de la de 20 clips; el 100% termina `Generada` dentro de los 3 intentos.

**Checkpoint**: US1 y US2 completas. Un guion cargado se genera escena por escena; los tests de
US2 pasan sin red.

---

## Phase 5: User Story 3 — Armar y copiar el prompt final de cada escena (Priority: P1)

**Goal**: mostrar, para cada escena generada, el prompt final de seis secciones armado por
plantilla, con hojas, diálogos y textos en pantalla literales, y copiarlo con un botón.

**Independent Test**: con escenas, bloques y hojas guardados en la base de test, armar el
prompt y compararlo contra el texto esperado ([contracts/prompt.md](contracts/prompt.md)).

### Tests for User Story 3 ⚠️

- [X] T061 [P] [US3] Escribir `tests/RodajeIA.Tests/Prompt/ArmadorPromptTests.cs`:
  - las seis secciones en orden para la escena del Anexo A (`AC-08a`);
  - "Bloque 1:", "Bloque 2:" y "Bloque 3:" en orden (`AC-08b`);
  - sin "Heridas/marcas" cuando la hoja no las tiene (`AC-08c`);
  - hoja idéntica en dos escenas (`AC-08e`, SC-001);
  - diálogos literales (`AC-08f`, `SC-003`);
  - texto en pantalla literal sin "Sin subtítulos…" (`AC-08g`);
  - sin sección de personajes cuando no hay ninguno (`AC-08h`);
  - comillas tipográficas conservadas (`AC-S07`);
  - con un molde de prueba que cambia un título, el prompt usa el título nuevo y conserva las cadenas de contrato (`AC-S31`);
  - orden de personajes (`AC-S32`);
  - devuelve `null` para escenas en `Pendiente`, `Generando` y `Error` (`AC-S33`);
  - punto omitido tras un valor terminado en "." (`AC-S34`);
  - diálogos de una toma en orden del guion (`AC-S35`);
  - la toma sin ángulo ni movimiento coincide con el ejemplo esperado del contrato;
  - editar el vestuario de la hoja base de Ana después de generar la escena cambia el prompt de esa escena, sin regenerarla (`AC-S38`, parte de edición; FR-003a).
- [X] T062 [P] [US3] Escribir `tests/RodajeIA.Tests/Prompt/CopiaPromptTests.cs` con un `IJSRuntime` falso:
  - "Copiar" pasa a `rodajeia.copiar` exactamente el texto que devuelve `ArmadorPrompt` y, si el JS devuelve `true`, el resultado es "Copiado" (`AC-08d`);
  - si devuelve `false` o lanza, el resultado es "No se pudo copiar; seleccioná el texto y copialo a mano" (`AC-S36`).

### Implementation for User Story 3

- [X] T063 [US3] Implementar `src/RodajeIA.Web/Prompt/ArmadorPrompt.cs` (FR-025 a FR-027, FR-032):
  - `Task<string?> ArmarAsync(int escenaId)`: carga la escena, sus clips, bloques, tomas, líneas referenciadas, personajes presentes en orden y variantes;
  - devuelve `null` si la escena no está `Generada`;
  - arma cada sección con `MotorMolde` y `HojaEfectiva.Renderizar`;
  - inserta `linea.original` en orden de `Posicion` y `clip.textoEnPantallaOriginal`;
  - no guarda el resultado (FR-015d).
- [X] T064 [P] [US3] Crear `src/RodajeIA.Web/wwwroot/js/portapapeles.js` con `window.rodajeia.copiar(texto)`, que usa `navigator.clipboard.writeText` y devuelve `true` o `false` (nunca lanza). Referenciarlo en `src/RodajeIA.Web/Components/App.razor`.
- [X] T065 [US3] Implementar `src/RodajeIA.Web/Prompt/CopiaPrompt.cs` (FR-028, FR-028a): `Task<ResultadoCopia> CopiarAsync(string prompt)` llama a `rodajeia.copiar` por `IJSRuntime`, captura las excepciones de interop y devuelve `Copiado` o `NoSePudo`, con el texto a mostrar para cada uno.
- [X] T066 [US3] Crear `src/RodajeIA.Web/Components/Pages/EscenaPagina.razor` (ruta `/escenas/{escenaId:int}`), según [contracts/ui.md](contracts/ui.md):
  - encabezado y estado;
  - en `Generada`: `<textarea readonly>` con el prompt, la cantidad de caracteres y el botón "Copiar", que usa `CopiaPrompt` y muestra su texto ("Copiado" o "No se pudo copiar; seleccioná el texto y copialo a mano") (FR-028, FR-028a);
  - en los demás estados: solo el estado y el motivo (FR-025c).
- [X] T067 [US3] Registrar `MotorMolde`, `ArmadorPrompt` y `CopiaPrompt` en `src/RodajeIA.Web/Program.cs`.

**Checkpoint**: las tres historias P1 están completas. Del guion al prompt copiable, sin edición
manual.

---

## Phase 6: User Story 4 — Gestionar series, hojas de personaje y episodios (Priority: P2)

**Goal**: crear, listar, editar y eliminar series, hojas y episodios, con validaciones y
eliminaciones en cascada.

**Independent Test**: operaciones de servicio sobre SQLite en memoria; comprobar las
validaciones, la unicidad, las cascadas y los bloqueos durante una generación.

### Tests for User Story 4 ⚠️

- [X] T068 [P] [US4] Escribir `tests/RodajeIA.Tests/Servicios/SeriesServiceTests.cs`:
  - crear (`AC-01a`), listar (`AC-01b`) y renombrar (`AC-01c`);
  - eliminar en cascada hojas, episodios, escenas, clips, bloques y variantes (`AC-01d`);
  - eliminar una serie con una generación en curso queda rechazado con el motivo (`AC-S15`).
- [X] T069 [P] [US4] Escribir `tests/RodajeIA.Tests/Servicios/HojasServiceTests.cs`:
  - crear con todos los campos (`AC-01e`), listar (`AC-01f`) y editar vestuario (`AC-01g`);
  - eliminar con variantes en una escena `Generada` (`AC-01h`, FR-003a);
  - peinado vacío al crear (`AC-01i`) y al editar (`AC-01k`);
  - crear sin heridas/marcas (`AC-01j`);
  - "ANA" duplicado en la serie y "Ana" permitido en otra serie (`AC-S08`);
  - "Audio" reservado (`AC-S09`);
  - después de eliminar la hoja, el prompt la omite y el diálogo `ANA:` sigue literal; renombrar una hoja usada devuelve el aviso de FR-003b (`AC-S38`, partes de eliminación y renombrado).
- [X] T070 [P] [US4] Escribir `tests/RodajeIA.Tests/Servicios/EpisodiosServiceTests.cs`:
  - crear el episodio 1 "Piloto" (`AC-11a`), listar (`AC-11b`) y editar título (`AC-11c`);
  - eliminar en cascada (`AC-11d`);
  - ver escenas en orden con su estado (`AC-11e`);
  - número repetido (`AC-11f`) y número no editable (`AC-11g`);
  - eliminar un episodio con una generación en curso queda rechazado (`AC-S15`).

### Implementation for User Story 4

- [X] T071 [P] [US4] Implementar `src/RodajeIA.Web/Servicios/SeriesService.cs` (FR-001, FR-004a): crear, listar, renombrar y eliminar. Al eliminar, consulta `ServicioGeneracion.EpisodioEnCurso` y rechaza con "Hay una generación en curso en esta serie".
- [X] T072 [P] [US4] Implementar `src/RodajeIA.Web/Servicios/HojasService.cs` (FR-002 a FR-003b):
  - valida los campos obligatorios, recortados y no vacíos;
  - valida `NombreClave` único en la serie y fuera de los nombres reservados de FR-002b;
  - al renombrar una hoja con `PersonajeEnEscena`, devuelve el aviso "Los guiones cargados siguen usando el nombre anterior; recargalos para que coincidan.";
  - elimina en cascada aunque haya una generación en curso.
- [X] T073 [P] [US4] Implementar `src/RodajeIA.Web/Servicios/EpisodiosService.cs` (FR-004, FR-004a, FR-005): crear con número único, listar en orden, editar solo el título, eliminar (rechazado si `EpisodioEnCurso` es ese episodio) y obtener el detalle con las escenas en orden.
- [X] T074 [US4] Crear `src/RodajeIA.Web/Components/Pages/SeriesPagina.razor` (ruta `/`): lista, alta, renombrado y eliminación con confirmación que menciona la cascada.
- [X] T075 [US4] Crear `src/RodajeIA.Web/Components/Pages/SeriePagina.razor` (ruta `/series/{serieId:int}`), con pestañas:
  - "Personajes": formulario de hoja con los mensajes de validación y el aviso de renombrado;
  - "Episodios": alta con número y título, edición del título, eliminación y enlace a `/episodios/{id}`.
- [X] T076 [US4] Registrar `SeriesService`, `HojasService`, `EpisodiosService` y `CargaGuionService` en `src/RodajeIA.Web/Program.cs`, y hacer que `EpisodioPagina.razor` cargue su detalle con `EpisodiosService`.

**Checkpoint**: el recorrido completo de la demo (SC-008) se puede hacer desde la UI.

---

## Phase 7: User Story 5 — Revisar y editar las tomas generadas (Priority: P2)

**Goal**: ver los bloques de una escena generada y editar sus campos técnicos (solo valores del
vocabulario) y su acción; el prompt se vuelve a armar.

**Independent Test**: con una escena generada en la base de test, editar plano, ángulo y acción
con el servicio, recrear el contexto y comprobar el prompt.

### Tests for User Story 5 ⚠️

- [X] T077 [P] [US5] Escribir `tests/RodajeIA.Tests/Servicios/TomasServiceTests.cs`:
  - el detalle trae todos los bloques y tomas (`AC-07a`);
  - editar el plano a "primer plano" cambia el prompt (`AC-07b`);
  - editar la acción cambia el prompt (`AC-07c`);
  - un valor fuera del vocabulario queda rechazado y se conserva el anterior (`AC-07d`);
  - la edición persiste con un contexto nuevo (`AC-07e`);
  - vaciar el ángulo lo omite en el prompt (`AC-07f`);
  - un valor guardado que ya no existe se marca `FueraDeVocabulario` y el prompt lo muestra tal cual (`AC-S30`);
  - una acción vacía o solo con espacios queda rechazada y conserva el valor; se permiten comillas; no hay operación para editar referencias de diálogo; no se puede editar si la escena no está `Generada` (`AC-S37`).

### Implementation for User Story 5

- [X] T078 [US5] Implementar `src/RodajeIA.Web/Servicios/TomasService.cs` (FR-022 a FR-024, FR-015b, FR-023a):
  - `ObtenerBloquesAsync(escenaId)`: tomas con la marca `FueraDeVocabulario` por campo y las líneas referenciadas, de solo lectura;
  - `EditarCampoAsync(tomaId, categoria, valor?)`: solo valores vigentes; `null` permitido para ángulo y movimiento;
  - `EditarAccionAsync(tomaId, texto)`: recortada y no vacía;
  - rechaza si la escena no está `Generada`.
- [X] T079 [US5] Agregar a `src/RodajeIA.Web/Components/Pages/EscenaPagina.razor` la sección "Bloques" (solo en `Generada`):
  - por toma, desplegables con los valores de `Vocabulario` (más "(sin valor)" para ángulo y movimiento) y la marca "valor fuera del vocabulario";
  - campo de acción editable, que guarda al confirmarse;
  - líneas de diálogo de solo lectura;
  - después de cada cambio se vuelve a armar el prompt mostrado.
- [X] T080 [US5] Registrar `TomasService` en `src/RodajeIA.Web/Program.cs`.

**Checkpoint**: la revisión y la edición funcionan, y el prompt refleja cada cambio.

---

## Phase 8: User Story 6 — Definir variantes de personaje por escena (Priority: P3)

**Goal**: crear, listar, editar y eliminar variantes por escena antes de generar o al
reintentar; la IA y el prompt usan la hoja efectiva.

**Independent Test**: variante en la escena 2 de un personaje presente en las escenas 1 y 2;
comprobar la instrucción y los prompts, y las restricciones según el estado.

### Tests for User Story 6 ⚠️

- [X] T081 [P] [US6] Escribir `tests/RodajeIA.Tests/Servicios/VariantesServiceTests.cs`:
  - la escena 2 muestra la variante y la escena 1, la hoja base (`AC-09a`, a través de `ArmadorPrompt`);
  - listar (`AC-09b`) y editar (`AC-09c`);
  - eliminar deja intacta la hoja base (`AC-09d`, `AC-09m`);
  - una segunda variante del mismo personaje queda rechazada (`AC-09e`);
  - una variante sin cambios queda rechazada (`AC-09f`);
  - dejar vacío el único campo cambiado queda rechazado (`AC-09g`);
  - en `Generada`, crear (`AC-09i`), editar (`AC-09j`) o eliminar (`AC-09k`) queda rechazado;
  - en `Generando`, crear queda rechazado (`AC-09l`);
  - un personaje no detectado o un valor igual a la base quedan rechazados; "sin heridas" sobre una base con heridas se acepta; en `Error`, se permite y el reintento usa la hoja efectiva (`AC-S39`).

### Implementation for User Story 6

- [X] T082 [US6] Implementar `src/RodajeIA.Web/Servicios/VariantesService.cs` (FR-029 a FR-031, FR-029a):
  - listar, crear, editar y eliminar;
  - solo personajes en `PersonajeEnEscena`;
  - al menos un campo distinto de la base, comparado sin espacios al inicio ni al final;
  - edad, peinado y vestuario no pueden quedar vacíos si se reemplazan;
  - `ReemplazaHeridas` con un valor vacío significa "sin heridas";
  - solo en `Pendiente` o `Error`, con los motivos de rechazo de cada regla.
- [X] T083 [US6] Agregar a `src/RodajeIA.Web/Components/Pages/EscenaPagina.razor` la sección "Personajes y variantes":
  - personajes detectados con su variante;
  - formulario de edad, peinado, vestuario y heridas/marcas, con la opción "Sin heridas";
  - controles deshabilitados, con el motivo, en `Generando` y `Generada`.
- [X] T084 [US6] Registrar `VariantesService` en `src/RodajeIA.Web/Program.cs`.

**Checkpoint**: las seis historias están completas.

---

## Phase 9: Polish & Cross-Cutting Concerns

- [X] T085 [P] Escribir `tests/RodajeIA.Tests/Rendimiento/RendimientoTests.cs` (`Explicit = true`, `[Trait("Categoria", "Rendimiento")]`), según las condiciones de medición de la spec:
  - SC-004: p95 de 100 parseos y detecciones de un guion de 10 × 20 clips, sin la primera ejecución, menor que 1 s;
  - SC-005: p95 de 100 armados de prompt de una escena de 20 clips menor que 1 s.
- [X] T086 [P] Escribir `tests/RodajeIA.Tests/TrazabilidadTests.cs`, que recorre por reflexión los `[Trait("AC", …)]` de todos los tests y falla si falta alguno de los 99 AC del PRD (`AC-01a` a `AC-11g`) o de `AC-S01` a `AC-S39`. La lista esperada vive en el mismo archivo.
  - La lista marca `AC-S26` como cubierto por `NotificadorGeneracionTests`; la parte visual se valida en el paso 6 del quickstart.
- [X] T087 Actualizar [quickstart.md](quickstart.md) con los comandos verificados para correr los tests `Explicit` de las categorías `Rendimiento` y `Gemini` con xUnit v3.
- [ ] T088 Recorrer [quickstart.md](quickstart.md) completo a mano (pasos 1 a 9 y la falla de configuración) y anotar en el mismo archivo la fecha y el equipo; corregir lo que no coincida.
- [X] T089 Correr `dotnet format --verify-no-changes` y `dotnet test` en la raíz, y corregir lo que falle (AGENTS.md).

---

## Phase 10: Correcciones de la revisión de la demo

**Purpose**: resolver lo observado en la revisión de la demo. Un commit por tarea, con sus tests
en el mismo commit.

- [X] T090 [P] En `src/RodajeIA.Web/Components/Pages/EpisodioPagina.razor`, enlazar el `<textarea>` del guion con `@bind:event="oninput"`: con `onchange`, al pegar el guion e ir directo a "Validar y guardar" el botón sigue deshabilitado y el primer click se pierde.
- [X] T091 Generar el schema de salida por llamada (FR-015, FR-016a): `SchemaSalida.Para(clips)` agrega `clip` como enum de los números de clip y los ítems de `dialogos` como enum de los ids de la escena (sin líneas: `maxItems: 0`). `IClienteIa.GenerarAsync` recibe el schema; `GeneradorEscena` lo arma con los clips esperados. Actualizar `SchemaSalidaTests`, `ClienteIaFalso`, el spike y [contracts/gemini.md](contracts/gemini.md).
- [X] T092 Regla 7 de validación (FR-017a, RN-03, constitución IV): la acción de una toma no puede contener el valor completo de un campo de la hoja efectiva de un personaje presente (edad, descripción física, peinado, vestuario, heridas/marcas, personalidad, rol) cuando ese valor tiene 3 o más palabras, ni una etiqueta de campo del molde (`Peinado:`, `Rol:`, …). `ValidadorRespuesta.Validar` recibe las hojas efectivas; la respuesta es inválida y se reintenta. Tests en `ValidadorRespuestaTests` (AC-S40) y regla en la instrucción, la spec y [contracts/gemini.md](contracts/gemini.md).
- [ ] T093 Regla 8 de validación (RF-05a, FR-017a): un nombre de hoja escrito en la acción sin sus tildes o con otras mayúsculas (`Jose Daniel` por `José Daniel`) hace inválida la respuesta. Reforzar en `config/instruccion-generacion.txt` que la acción se escribe con ortografía completa (tildes y ñ) y con los nombres tal cual la hoja. Tests en `ValidadorRespuestaTests` (AC-S41) y regla en la spec y [contracts/gemini.md](contracts/gemini.md).
- [ ] T094 Volcar al `PRD.md` las respuestas de las sesiones de aclaración de la spec 001 que detallan o restringen al PRD, para que el PRD sea autosuficiente en el próximo módulo; la spec deja de declararse por encima del PRD.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup. Bloquea todas las historias.
- **US1 (Phase 3)**: depende de Foundational.
- **US2 (Phase 4)**: depende de US1 (necesita escenas cargadas y `EpisodioPagina.razor`).
- **US3 (Phase 5)**: depende de Foundational para el motor y la hoja efectiva. Sus tests usan escenas `Generada` creadas en la base de test, así que se puede hacer en paralelo con US2. En la app real necesita a US2 para tener escenas generadas.
- **US4 (Phase 6)**: depende de Foundational. Las validaciones de eliminación usan `ServicioGeneracion.EpisodioEnCurso` (T055); si US2 no está terminada, empezar con una interfaz `IEstadoGeneracion` y completarla después.
- **US5 (Phase 7)**: depende de US3 (`ArmadorPrompt` y `EscenaPagina.razor`).
- **US6 (Phase 8)**: depende de US3 (`EscenaPagina.razor`) y usa `GeneradorEscena` (US2) en `AC-S39`.
- **Polish (Phase 9)**: depende de todas las historias.

### Dentro de cada historia

- T017 depende de T016: la base de test se crea aplicando las migraciones.
- Los tests van primero y MUST fallar antes de implementar.
- Primero los records y los servicios, después los componentes Razor y al final el registro en `Program.cs`.
- `Program.cs` y `EscenaPagina.razor` se tocan en varias historias: esas tareas no son [P] entre sí.

### Parallel Opportunities

- Setup: T004, T005 y T006.
- Foundational: entidades T009 a T014; T018 a T020; T023 y T025.
- US1: tests T028 a T031; records T032.
- US2: tests T038 a T046; implementaciones T047 a T050, T052 y T053.
- US3 en paralelo con US2 (equipos distintos); US4 en paralelo con US3.
- US3: tests T061 y T062; T064 y T065.
- US4: tests T068 a T070; servicios T071 a T073.

---

## Parallel Example: User Story 2

```bash
# Tests de US2, todos en archivos distintos:
Task: "Escribir SchemaSalidaTests en tests/RodajeIA.Tests/Generacion/SchemaSalidaTests.cs"
Task: "Escribir ArmadorInstruccionTests en tests/RodajeIA.Tests/Generacion/ArmadorInstruccionTests.cs"
Task: "Escribir ValidadorRespuestaTests en tests/RodajeIA.Tests/Generacion/ValidadorRespuestaTests.cs"
Task: "Escribir GeneradorEscenaTests en tests/RodajeIA.Tests/Generacion/GeneradorEscenaTests.cs"

# Piezas independientes de US2:
Task: "Implementar SchemaSalida en src/RodajeIA.Web/Generacion/SchemaSalida.cs"
Task: "Implementar ArmadorInstruccion en src/RodajeIA.Web/Generacion/ArmadorInstruccion.cs"
Task: "Implementar ValidadorRespuesta en src/RodajeIA.Web/Generacion/ValidadorRespuesta.cs"
Task: "Implementar RegistroIntentos en src/RodajeIA.Web/Generacion/RegistroIntentos.cs"
```

## Parallel Example: User Story 1

```bash
Task: "Escribir ParserGuionValidoTests en tests/RodajeIA.Tests/Guion/ParserGuionValidoTests.cs"
Task: "Escribir ParserGuionErroresTests en tests/RodajeIA.Tests/Guion/ParserGuionErroresTests.cs"
Task: "Escribir DetectorPersonajesTests en tests/RodajeIA.Tests/Personajes/DetectorPersonajesTests.cs"
Task: "Escribir CargaGuionServiceTests en tests/RodajeIA.Tests/Servicios/CargaGuionServiceTests.cs"
```

---

## Implementation Strategy

### MVP (las tres historias P1)

1. Phase 1 (Setup) y Phase 2 (Foundational).
2. Phase 3 (US1): **STOP y validar.** El parser y la detección pasan sus tests; ya es un
   entregable que se puede demostrar (validar guiones para el/la guionista).
3. Phase 4 (US2) y Phase 5 (US3): **STOP y validar** el recorrido guion → generar → prompt con
   datos creados en la base.
4. Phase 6 (US4) para tener la UI de series, hojas y episodios: con eso se completa la demo
   (SC-008).

US4 es P2 en la spec, pero sin su UI no se pueden crear series ni hojas desde la app. Para la
demo, el MVP práctico es US1 + US2 + US3 + US4.

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 → guiones validados (demo parcial).
3. US2 → escenas generadas.
4. US3 → prompts copiables (objetivos 1 y 2 de la v1).
5. US4 → demo completa desde la UI.
6. US5 → edición de tomas.
7. US6 → variantes.
8. Polish → rendimiento, trazabilidad completa y quickstart verificado.

### Notes

- [P]: archivos distintos y sin dependencias pendientes.
- Hacer commit al terminar cada tarea o grupo lógico, con mensajes Conventional Commits.
- No agregar paquetes fuera de los de R-01 sin aprobación. No actualizar paquetes existentes sin
  preguntar (AGENTS.md).
- Toda tarea que toque el parser, el vocabulario, el schema, la validación de respuestas o el
  molde MUST revisar explícitamente los principios I a IV de la constitución.
