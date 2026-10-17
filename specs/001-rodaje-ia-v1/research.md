# Research: RodajeIA v1

**Fecha**: 2026-10-17 · **Spec**: [spec.md](spec.md) · **Plan**: [plan.md](plan.md)

Cada decisión resuelve una incógnita del Technical Context o fija una práctica para una
dependencia. Las versiones de paquetes son las últimas estables publicadas en NuGet al
2026-10-17 (AGENTS.md: nunca preview/beta).

## R-01 · Paquetes y versiones

- **Decision**:

  | Paquete | Versión | Proyecto |
  | --- | --- | --- |
  | `Microsoft.EntityFrameworkCore.Sqlite` | 10.0.13 | Web |
  | `Microsoft.EntityFrameworkCore.Design` | 10.0.13 | Web (solo diseño) |
  | `dotnet-ef` (herramienta local, `.config/dotnet-tools.json`) | 10.0.13 | — |
  | `Google.GenAI` (SDK oficial de Gemini para .NET) | 1.26.1 | Web |
  | `xunit.v3` | 4.0.2 | Tests |
  | `xunit.runner.visualstudio` | 4.0.1 | Tests |
  | `Microsoft.NET.Test.Sdk` | 18.11.0 | Tests |
  | `Microsoft.Extensions.TimeProvider.Testing` | 10.11.0 | Tests |

- **Rationale**: es el stack fijado por la constitución. `Google.GenAI` es el SDK oficial y
  admite salida estructurada con schema (JSON + enums). `FakeTimeProvider` permite testear las
  esperas de 10 s/30 s y el timeout de 60 s sin esperar de verdad (principio V).
- **Alternatives considered**: llamar a la API REST de Gemini con `HttpClient` (más código
  propio de serialización y errores; se descarta salvo que el SDK no exponga algo necesario);
  Polly para reintentos (dependencia extra para un bucle de 3 intentos; se descarta por
  principio VI).

## R-02 · Estructura de la solución

- **Decision**: dos proyectos, `src/RodajeIA.Web` (Blazor Web App, Interactive Server) y
  `tests/RodajeIA.Tests` (xUnit v3), en `RodajeIA.slnx`. Dentro de Web, carpetas por
  responsabilidad (`Guion/`, `Personajes/`, `Generacion/`, `Prompt/`, `Configuracion/`,
  `Datos/`, `Servicios/`, `Components/`). La lógica determinística no depende de Blazor ni de
  EF Core.
- **Rationale**: es la estructura que fija la constitución; separar por carpetas mantiene
  testeable el núcleo sin crear proyectos extra.
- **Alternatives considered**: un proyecto `RodajeIA.Core` aparte (más aislamiento, pero un
  proyecto más sin necesidad real en v1).

## R-03 · Parser del guion

- **Decision**: parser propio, línea a línea, como máquina de estados (antes de la primera
  escena → encabezado → campos de escena → clip). Lee con `StringReader` (acepta LF, CRLF y CR),
  descarta un BOM inicial, recorta espacios y tabulaciones por línea para reconocer marcadores y
  numera líneas físicas (FR-008b). Acumula todos los errores en una lista (FR-008a) y devuelve
  un resultado `GuionValido | GuionInvalido(errores)`; nunca devuelve un resultado parcial. El
  tope de 100.000 caracteres se revisa antes de parsear (FR-008d). Contrato completo:
  [contracts/guion.md](contracts/guion.md).
- **Rationale**: la convención es por marcadores de línea completa; una máquina de estados
  reporta cada error en su línea exacta y es trivialmente determinística (principio I). Un
  guion máximo (10 × 20 clips) se procesa en milisegundos, muy por debajo de SC-004.
- **Alternatives considered**: una gramática con un generador de parsers (sobredimensionado
  para ~12 marcadores); regex sobre el texto completo (los números de línea y los errores
  múltiples se vuelven frágiles).

## R-04 · Detección de personajes

- **Decision**: para cada hoja de la serie, buscar sus ocurrencias como palabra completa
  (FR-011b: el carácter vecino no es letra, dígito, `-`, `'` ni `’`), comparando por ordinal
  sobre el texto en forma NFC. Se resuelven solapamientos eligiendo, en cada posición, el nombre
  más largo (FR-011a). Los marcadores de diálogo se comparan con `nombre.ToUpperInvariant()`
  exacto (FR-011c). La salida incluye, por escena, el orden de primera aparición (FR-025b).
- **Rationale**: la comparación en NFC solo afecta a la detección: dos textos Unicode
  equivalentes ("é" precompuesta frente a "e" + acento combinante, común al copiar desde
  ciertos editores) son el mismo nombre. El guion guardado no se modifica, así que no hay
  normalización del guion.
- **Alternatives considered**: comparar sin NFC (un guion pegado en NFD no detectaría a
  "José" y fallaría en silencio); `Regex` con `\b` (`\b` de .NET trata `-` y `'` como límite y
  no cumple FR-011b).

## R-05 · Integración con Gemini

- **Decision**: interfaz propia `IClienteIa` con un método
  `GenerarAsync(string instruccion, CancellationToken ct) → RespuestaIa(string json)`. La
  implementación `ClienteGemini` usa `Google.GenAI` con `ResponseMimeType = "application/json"`
  y un `ResponseSchema` construido en tiempo de arranque desde `config/vocabulario.json`
  (`SchemaSalida`), con cada campo técnico como enum (FR-015). El modelo y la clave se leen de
  configuración (`Gemini:Modelo`, por defecto `gemini-3.1-flash-lite`; `Gemini:ApiKey` desde
  user-secrets). La validación de la respuesta (FR-017, FR-017a, FR-017b) vive fuera del
  cliente, en `ValidadorRespuesta`, y trabaja sobre el JSON crudo. Contrato:
  [contracts/gemini.md](contracts/gemini.md).
- **Rationale**: la interfaz permite usar un doble de prueba con respuestas válidas,
  inválidas, errores y timeouts (principio V) y deja el SDK detrás de un solo archivo. Validar el
  JSON crudo (en vez de confiar en la deserialización del SDK) cubre FR-017: JSON inválido,
  campos faltantes o de otro tipo y valores fuera del vocabulario.
- **Alternatives considered**: `Microsoft.Extensions.AI` `IChatClient` (abstracción genérica;
  la salida estructurada con enums queda menos explícita y no aporta en v1).
- **A verificar en implementación**: nombres exactos de las propiedades de schema del SDK y los
  tipos de excepción que expone para errores HTTP. La clasificación de errores se define por
  código HTTP (R-06) para no depender de nombres de clases.

## R-06 · Reintentos, timeout y clasificación de errores

- **Decision**: bucle propio de 3 intentos en `GeneradorEscena`. Cada llamada usa un
  `CancellationTokenSource` con `CancelAfter(60 s)` sobre el `TimeProvider` inyectado; las
  esperas son `Task.Delay(10 s / 30 s, timeProvider)`. Clasificación del fallo (FR-019a,
  FR-018a):

  | Situación | Categoría de motivo | ¿Se reintenta? |
  | --- | --- | --- |
  | Se cumplió el timeout de 60 s | La IA no respondió a tiempo | Sí |
  | HTTP 429, 5xx o error de red | La IA devolvió un error o no está disponible | Sí |
  | Respuesta que no pasa la validación | La respuesta no cumplió las reglas (detalle) | Sí |
  | Falta la clave; HTTP 401/403; HTTP 400 por clave inválida; HTTP 404 de modelo | Error de configuración de la IA | No; se detiene la cola |
  | Excepción no prevista, falla al guardar | Error interno | No |
  | Escena en "generando" al iniciar | Generación interrumpida por un cierre | — |

- **Rationale**: con `TimeProvider` los tests de AC-S22 corren en milisegundos. La tabla hace
  explícito que solo los errores transitorios y las respuestas inválidas consumen intentos.
- **Alternatives considered**: Polly (ver R-01). Un error interno se podría reintentar, pero
  sin causa conocida un reintento repite la falla y alarga la espera.

## R-07 · Ejecución de la generación en segundo plano

- **Decision**: singleton `ServicioGeneracion` (un `BackgroundService`) con un único hueco de
  trabajo. `TryIniciar(pedido)` devuelve `false` si ya hay una generación en curso (FR-014a,
  global para la app y para todas las pestañas). El pedido es la lista ordenada de escenas
  (todas las "pendiente" de un episodio, o una escena en "error" para reintentar). Expone
  `EpisodioEnCurso` para FR-004a (impedir eliminar o recargar). Cada escena usa su propio
  `DbContext`, creado con `IDbContextFactory`.
- **Rationale**: la generación dura minutos y no debe depender del circuito de Blazor de quien
  pulsó "Generar"; un hosted service vive mientras viva la app. Un único hueco implementa el
  bloqueo global de forma directa.
- **Alternatives considered**: `Task.Run` desde el componente (muere o queda huérfano si se
  cierra la pestaña); una cola persistente (fuera de alcance: no se encola, FR-014a).

## R-08 · Avance en vivo en la UI

- **Decision**: singleton `NotificadorGeneracion` con un evento
  `EstadoEscenaCambiado(escenaId, estado, intento, motivo)`. Los componentes del episodio se
  suscriben al montarse, llaman a `InvokeAsync(StateHasChanged)` y se desuscriben en `Dispose`.
  El evento `GeneracionEnCursoCambiada` habilita o deshabilita "Generar" y "Reintentar" en todas
  las pestañas (FR-014a, FR-019b).
- **Rationale**: con Interactive Server ya hay una conexión SignalR por pestaña; un evento en
  memoria alcanza para un solo usuario y un solo proceso.
- **Alternatives considered**: sondeo periódico (latencia y consultas innecesarias); un hub de
  SignalR propio (duplica lo que Blazor ya da).

## R-09 · Tamaño del guion en Blazor Server

- **Decision**: subir `HubOptions.MaximumReceiveMessageSize` a 1 MB y leer el guion con un
  `<textarea>` sin `@bind:event="oninput"` (solo `onchange`), así se envía una vez al confirmar.
- **Rationale**: el límite por defecto de SignalR es 32 KB y un guion de 100.000 caracteres
  puede ocupar hasta ~400 KB en UTF-8. Sin este ajuste, pegar un guion grande corta el
  circuito antes de que FR-008d pueda responder.
- **Alternatives considered**: subir el guion como archivo (cambia el flujo de "pegar texto",
  FR-006).

## R-10 · Persistencia

- **Decision**: SQLite con EF Core, migraciones en `src/RodajeIA.Web/Datos/Migrations`,
  archivo `rodajeia.db` en el directorio de contenido (excluido de git). Eliminaciones en
  cascada configuradas en el modelo (FR-001, FR-003, FR-004, FR-007). Índices únicos:
  (serie, nombre en mayúsculas) para hojas, (serie, número) para episodios, (escena, hoja) para
  variantes. El guardado de los bloques de una escena y su paso a "generada" se hacen en una sola
  transacción (FR-019). Recargar un guion reemplaza el anterior en una transacción (FR-007,
  FR-009). Al iniciar, antes de aceptar pedidos, un `UPDATE` pasa "generando" a "error" con el
  motivo de cierre (FR-021).
- **Rationale**: es el stack fijado. La unicidad sin distinguir mayúsculas pero con tildes
  (FR-002a) se obtiene guardando `NombreClave = Nombre.ToUpperInvariant()`, que conserva las
  tildes. `COLLATE NOCASE` de SQLite solo maneja ASCII y no sirve.
- **Alternatives considered**: guardar el guion parseado como JSON en una columna (complica
  editar tomas y eliminar en cascada).

## R-11 · Motor del molde del prompt

- **Decision**: intérprete propio y pequeño de `config/molde-prompt.json`: reemplaza
  `{{variable}}`, omite `[...]` si alguna variable interna está vacía, aplica la regla de
  puntuación (FR-026) y une secciones, bloques, tomas y diálogos con los separadores del molde.
  El prompt no se guarda: se arma a pedido desde la base y el molde cargado al iniciar
  (FR-015d). Contrato: [contracts/prompt.md](contracts/prompt.md).
- **Rationale**: son tres construcciones (variable, opcional, regla de puntuación); un motor de
  plantillas genérico (Scriban, Handlebars) no las expresa igual y suma una dependencia.
- **Alternatives considered**: plantillas Razor (mezclan presentación y contrato y no se editan
  como archivo de configuración).

## R-12 · Validación de `config/` al iniciar

- **Decision**: `CargadorConfiguracion` lee y valida los cuatro archivos antes de `app.Run()`
  (FR-015a, FR-015c). Si algo falla, lanza `ConfiguracionInvalidaException` con el archivo y el
  problema; `Program.cs` lo escribe en la consola y termina con código distinto de 0. Los objetos
  validados se registran como singletons inmutables. Reglas: [contracts/config.md](contracts/config.md).
- **Rationale**: es la forma más directa de cumplir "la aplicación no arranca" y deja el error
  visible en la terminal que usa el/la productor/a.

## R-13 · Registro de intentos

- **Decision**: `RegistroIntentos` agrega una línea JSON por intento a
  `logs/generacion-AAAA-MM-DD.jsonl` (escena, episodio, intento, duración, resultado, motivo y,
  si la respuesta es inválida, `respuestaCruda`). No registra la instrucción completa ni nada de
  la configuración de `Gemini`. Además, `ILogger` escribe un resumen en la consola. `logs/` va en
  `.gitignore`.
- **Rationale**: FR-019c pide un log local con la respuesta cruda; un archivo JSONL se lee con
  cualquier editor y se escribe sin paquetes extra.
- **Alternatives considered**: Serilog con sink de archivo (dependencia extra para una sola
  necesidad).

## R-14 · Portapapeles

- **Decision**: interop JS mínimo (`wwwroot/js/portapapeles.js`) con
  `navigator.clipboard.writeText`; devuelve `true` o `false`. Si falla, el componente muestra el
  aviso de FR-028a y el prompt ya está en un `<textarea readonly>` que se puede seleccionar.
- **Rationale**: no hay API de portapapeles en Blazor Server; `localhost` es contexto seguro, así
  que la API funciona en el uso normal.

## R-15 · Tests y medición

- **Decision**:
  - Unitarios: parser, detector, schema, validador, motor del molde, armador del prompt y
    generador de escena (con `IClienteIa` falso y `FakeTimeProvider`).
  - Integración: servicios con SQLite en memoria (`DataSource=:memory:`, conexión abierta
    durante el test) para cascadas, unicidad, atomicidad y el paso de "generando" a "error" al
    iniciar.
  - Componentes: no se agrega bUnit en v1. Los componentes delegan en servicios testeados, y la
    UI se valida con [quickstart.md](quickstart.md).
  - Rendimiento (SC-004, SC-005): tests marcados `[Trait("Categoria", "Rendimiento")]`, fuera de
    la suite por defecto mediante un filtro en `xunit.runner.json` o en la invocación.
  - Spike (SC-006) y SC-002: test marcado `[Trait("Categoria", "Gemini")]` que se omite si no hay
    `Gemini:ApiKey`; nunca corre en `dotnet test` por defecto (constitución V).
- **Rationale**: cada AC del PRD y cada AC-S tiene al menos un test que no necesita red. Las
  mediciones con red se pueden repetir, pero se corren a mano.
- **Alternatives considered**: bUnit para los componentes (útil, pero suma un paquete; se puede
  agregar si aparece lógica no trivial en la UI).
