# Constitución de RodajeIA

## Principios fundamentales

### I. Guion determinístico, sin IA (NO NEGOCIABLE)

- La validación del guion, su división en escenas y clips, la extracción de los campos de
  escena, la separación de acción, diálogo y texto en pantalla, y la detección de personajes
  (PRD RF-03, RF-04, RF-05) MUST implementarse como código determinístico, sin llamar a ningún
  modelo de IA.
- El mismo guion procesado dos veces MUST producir resultados idénticos campo por campo.
- Un guion inválido MUST rechazarse completo, devolviendo cada error con su número de línea y
  su motivo (Anexo A). Nunca se normaliza, corrige ni guarda en parte: ni el guion, ni
  escenas, ni clips.

**Razón:** el guion es la fuente de verdad del episodio. Un parser determinístico es
predecible, rápido (RNF-03) y testeable sin red; delegarlo a la IA introduce errores
silenciosos imposibles de reproducir.

### II. Vocabulario cerrado como única fuente técnica

- Plano, óptica, iluminación, ángulo y movimiento (los dos últimos opcionales) MUST tomar
  valores exclusivamente de `config/vocabulario.json`, archivo versionado en el repositorio.
- El schema de salida estructurada de Gemini MUST generarse desde ese archivo, con cada campo
  técnico como enum. Está prohibido hardcodear o duplicar valores del vocabulario en el código.
- Las ediciones manuales de una toma (RF-07b) MUST rechazar cualquier valor fuera del
  vocabulario.
- No existen campos de cámara ni de profundidad de campo: la cámara es fija y forma parte del
  estilo global (`config/estilo.txt`).
- El único texto libre que produce la IA es la acción de cada toma.

**Razón:** la terminología inventada o genérica es lo que hace que un clip "se vea a IA"
(RN-02). Un único archivo versionado mantiene al productor/a como dueño/a del vocabulario.

### III. Continuidad literal por plantilla (NO NEGOCIABLE)

- Las hojas de personaje efectivas (hoja base + variante de la escena, RN-05), el diálogo con
  su marcador y acotación, y el texto en pantalla MUST insertarse literales en el prompt final
  mediante la plantilla de `config/molde-prompt.json` (RN-03, RN-04).
- La IA MUST recibir esos textos solo como contexto y MUST referenciar las líneas de diálogo
  por id; nunca los reescribe ni los parafrasea.
- La descripción de un personaje sin variantes MUST ser idéntica palabra por palabra en todos
  los prompts donde aparece (objetivo 1, AC-08e).
- El armado del prompt final MUST ser una función determinística de los datos guardados.

**Razón:** la consistencia visual de los personajes entre escenas y episodios es el problema
central que resuelve el producto.

### IV. Respuesta de IA validada y generación resiliente

- Toda respuesta de Gemini MUST validarse contra RN-08 (un bloque por clip, cada número de
  clip una sola vez), RN-09 (cada línea de diálogo referenciada exactamente una vez, solo ids
  del propio clip) y RN-10 (al menos una toma por bloque, acción no vacía). Una respuesta que
  no cumple se trata como fallida.
- Los reintentos MUST seguir RNF-07 (3 intentos en total, esperas de 10 s y 30 s) con un
  timeout por llamada de 60 s (RNF-06); agotados los intentos, la escena queda en "error".
- Los bloques de cada escena MUST persistirse en cuanto se generan, de modo que el fallo de
  otra escena no los borre (RF-10a). Al iniciar la app, las escenas en "generando" pasan a
  "error" (RF-10e).
- Las variantes de una escena en "generando" o "generada" MUST ser inmutables (RF-09e).

**Razón:** el modelo es una dependencia externa no determinística; el sistema debe detectar y
contener sus fallos en lugar de propagarlos al prompt final.

### V. Testeable sin el modelo

- Cada criterio de aceptación del PRD (AC-xx) MUST tener al menos un test automatizado en
  xUnit v3 que lo verifique.
- El parser, la detección de personajes, la validación de respuestas, la generación del
  schema y el armado del prompt MUST testearse sin llamar a Gemini.
- El acceso a Gemini MUST quedar detrás de una abstracción que permita sustituirlo por un
  doble de prueba (respuestas válidas, inválidas, errores y timeouts).
- Los tests de la suite por defecto (`dotnet test`) MUST NOT requerir red ni API key.
- Para la lógica determinística se SHOULD escribir el test antes que la implementación.

**Razón:** la mayoría de las reglas de negocio son determinísticas y deben poder verificarse
rápido y sin costo; la suite es la red de seguridad de los principios I–IV.

### VI. Simplicidad y alcance v1

- La v1 es monousuario y local: sin login, roles ni separación de datos entre usuarios.
- Lo listado en "Fuera de Alcance" del PRD MUST NOT implementarse sin enmendar antes el PRD.
- Se MUST preferir la solución más simple que cumpla el RF; toda complejidad adicional
  (capas, patrones, servicios) MUST justificarse en el "Complexity Tracking" del plan.
- El vocabulario, el estilo global, la instrucción de generación y el molde del prompt se
  editan como archivos versionados en `config/`; no hay pantallas de administración.

**Razón:** el objetivo de la v1 es validar el flujo guion → prompt consistente; el alcance
acotado mantiene el producto entregable.

## Restricciones técnicas

- **Stack:** .NET 10, Blazor Web App (Interactive Server), SQLite con EF Core, Gemini como
  LLM, xUnit v3 para tests, `dotnet format` con las reglas de `.editorconfig` como linter.
- **Estructura:** solución `RodajeIA.slnx`; app en `src/RodajeIA.Web`; tests en
  `tests/RodajeIA.Tests`.
- **Persistencia:** los cambios de esquema MUST hacerse con migraciones de EF Core. Las
  eliminaciones en cascada MUST respetar las reglas del PRD (RF-01d, RF-01h, RF-02b, RF-11d).
- **Secretos:** la API key de Gemini MUST guardarse con `dotnet user-secrets` (o variables de
  entorno); nunca en el repositorio, que es público.
- **Dependencias:** un paquete NuGet nuevo MUST usar la última versión estable (nunca
  preview/beta). Los paquetes existentes no se actualizan sin aprobación explícita.
- **Rendimiento (p95, equipo local):** validación y parseo de un guion máximo < 1 s (RNF-03);
  armado del prompt por escena < 1 s (RNF-04); generación de una escena de 20 clips < 30 s
  (RNF-05).
- **Idioma:** la UI, los mensajes de error del guion y los prompts están en español.

## Flujo de desarrollo y puertas de calidad

- **Trazabilidad:** cada spec, plan y tarea MUST referenciar los RF, RN, RNF y AC del PRD que
  cubre. El PRD (`PRD.md`) es la especificación de producto de referencia.
- **Flujo Spec Kit:** las funcionalidades avanzan por `/speckit-specify` → `/speckit-clarify`
  (si hace falta) → `/speckit-plan` → `/speckit-tasks` → `/speckit-implement`. El "Constitution
  Check" del plan MUST evaluar los principios I–VI antes y después del diseño.
- **Puerta de calidad:** antes de dar por terminado un cambio MUST pasar
  `dotnet format --verify-no-changes` y `dotnet test`; lo que falle se corrige antes de
  continuar.
- **Commits:** mensajes en formato Conventional Commits (`tipo(scope): descripción`), según la
  skill `conventional-commit`.
- **Revisión:** todo cambio que toque el parser, el vocabulario, el schema, la validación de
  respuestas o el molde del prompt MUST verificar explícitamente el cumplimiento de los
  principios I–IV.

## Gobernanza

- Esta constitución prevalece sobre cualquier otra práctica del proyecto. `AGENTS.md` es la
  guía operativa para agentes y MUST mantenerse consistente con ella; ante un conflicto, manda
  la constitución y se corrige `AGENTS.md`.
- **Enmiendas:** se proponen con un cambio a `.specify/memory/constitution.md` (vía
  `/speckit-constitution`) que explique el motivo y el impacto en plantillas, specs y planes
  existentes, y se aprueban por la persona dueña del producto antes del merge.
- **Versionado (SemVer):** MAJOR para eliminar o redefinir un principio de forma incompatible;
  MINOR para agregar un principio o sección, o ampliar materialmente una regla; PATCH para
  aclaraciones y redacción sin cambio semántico.
- **Cumplimiento:** cada plan y cada revisión de código MUST verificar el cumplimiento de esta
  constitución; toda desviación MUST justificarse por escrito en el "Complexity Tracking" del
  plan o, si no se justifica, corregirse.

**Version**: 1.0.0 | **Ratified**: 2026-10-14 | **Last Amended**: 2026-10-14
