# Data Model: RodajeIA v1

**Fecha**: 2026-10-17 · **Spec**: [spec.md](spec.md) (Key Entities, PRD Anexo B)

Persistencia en SQLite con EF Core (research R-10). Todas las claves primarias son `int`
autoincrementales. "Texto" es `TEXT` sin límite salvo que se indique. Los campos de texto
obligatorios se guardan sin espacios al inicio ni al final y no pueden quedar vacíos.

## Diagrama

```text
Serie 1─* HojaPersonaje
Serie 1─* Episodio 1─* Escena 1─* Clip 1─* LineaDialogo
                          │         └─0..1 Bloque 1─* Toma *─* LineaDialogo (TomaDialogo)
                          ├─* PersonajeEnEscena *─1 HojaPersonaje
                          ├─* VarianteEscena    *─1 HojaPersonaje
Clip ─* PersonajeEnClip *─1 HojaPersonaje
```

Todas las relaciones padre → hijo eliminan en cascada.

## Entidades

### Serie

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | int | PK |
| Nombre | texto | Obligatorio |

Eliminarla elimina sus hojas y episodios, y todo lo que depende de ellos (FR-001). No se puede
eliminar si uno de sus episodios tiene una generación en curso (FR-004a).

### HojaPersonaje

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | int | PK |
| SerieId | int | FK → Serie, cascada |
| Nombre | texto | Obligatorio. Tal como se escribe en la acción (con mayúscula inicial y tildes) |
| NombreClave | texto | `Nombre.ToUpperInvariant()`. Índice único (SerieId, NombreClave) (FR-002a) |
| Edad | texto | Obligatorio |
| DescripcionFisica | texto | Obligatorio; incluye la voz |
| Peinado | texto | Obligatorio |
| Vestuario | texto | Obligatorio |
| HeridasMarcas | texto? | Opcional |
| Personalidad | texto | Obligatorio |
| Rol | texto | Obligatorio |

- `NombreClave` no puede ser `ESCENA`, `CLIP`, `LOCACIÓN`, `ILUMINACIÓN`, `PUESTA EN ESCENA`,
  `AUDIO` ni `TEXTO EN PANTALLA` (FR-002b).
- Renombrar una hoja que figura en `PersonajeEnEscena` se permite, mostrando el aviso de FR-003b.
- Eliminarla elimina sus `VarianteEscena`, `PersonajeEnEscena` y `PersonajeEnClip`, aunque la
  escena esté en "generando" o "generada" (FR-003, FR-003a).

### Episodio

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | int | PK |
| SerieId | int | FK → Serie, cascada |
| Numero | int | ≥ 1. Índice único (SerieId, Numero). No se edita (FR-004) |
| Titulo | texto | Obligatorio; editable |
| Guion | texto? | El texto tal como se pegó (FR-008b). Nulo hasta la primera carga válida |

Cargar un guion válido reemplaza, en una transacción, `Guion` y todas las escenas (con clips,
bloques y variantes) del anterior (FR-007). Un guion inválido no toca nada (FR-009). No se puede
cargar un guion nuevo ni eliminar el episodio mientras tiene una generación en curso (FR-004a).

### Escena

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | int | PK |
| EpisodioId | int | FK → Episodio, cascada |
| Numero | int | 1..10. Índice único (EpisodioId, Numero) |
| IntExt | enum `Int` \| `Ext` | Del encabezado |
| Lugar | texto | Del encabezado |
| MomentoDia | texto | Del encabezado |
| Locacion | texto | `LOCACIÓN:` |
| Iluminacion | texto | `ILUMINACIÓN:` (iluminación base) |
| PuestaEnEscena | texto | `PUESTA EN ESCENA:` |
| Audio | texto | `AUDIO:` |
| Estado | enum `Pendiente` \| `Generando` \| `Generada` \| `Error` | Ver transiciones |
| MotivoError | texto? | Solo en `Error`; se borra al salir de `Error` (FR-019a) |

El número de intento en curso ("intento 2 de 3") no se guarda: lo publica
`NotificadorGeneracion` mientras dura la generación (research R-08).

### PersonajeEnEscena

| Campo | Tipo | Reglas |
| --- | --- | --- |
| EscenaId | int | PK compuesta; FK → Escena, cascada |
| HojaId | int | PK compuesta; FK → HojaPersonaje, cascada |
| Orden | int | Orden de primera aparición en la escena (FR-025b) |

Se calcula al cargar el guion y no se vuelve a calcular (FR-003b).

### Clip

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | int | PK |
| EscenaId | int | FK → Escena, cascada |
| Numero | int | 1..20. Índice único (EscenaId, Numero) |
| Accion | texto | Las líneas de acción del clip, unidas con `\n`, en orden |
| TextoEnPantalla | texto? | Solo el texto entre comillas |
| TextoEnPantallaOriginal | texto? | El texto con sus comillas originales (FR-026a) |

Un clip tiene contenido si tiene al menos una línea de acción, de diálogo o de texto en pantalla
(Anexo A).

### PersonajeEnClip

| Campo | Tipo | Reglas |
| --- | --- | --- |
| ClipId | int | PK compuesta; FK → Clip, cascada |
| HojaId | int | PK compuesta; FK → HojaPersonaje, cascada |

### LineaDialogo

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | int | PK |
| ClipId | int | FK → Clip, cascada |
| Posicion | int | 1..n dentro del clip |
| Personaje | texto | El marcador tal como está escrito (`ANA`, `MESERO`) |
| Acotacion | texto? | Sin paréntesis |
| Texto | texto | Sin las comillas |
| Original | texto | La línea completa del guion, sin espacios al inicio ni al final (FR-026a) |

`IdIa` = `c{Clip.Numero}-d{Posicion}`; se calcula, no se guarda (FR-016a).

### VarianteEscena

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | int | PK |
| EscenaId | int | FK → Escena, cascada |
| HojaId | int | FK → HojaPersonaje, cascada. Índice único (EscenaId, HojaId) |
| Edad | texto? | Nulo = no reemplaza |
| Peinado | texto? | Nulo = no reemplaza |
| Vestuario | texto? | Nulo = no reemplaza |
| ReemplazaHeridas | bool | `true` = reemplaza heridas/marcas con `HeridasMarcas` |
| HeridasMarcas | texto? | Con `ReemplazaHeridas`, nulo o vacío significa "sin heridas" |

**Reglas (FR-029, FR-029a, FR-031):**
- La hoja tiene que estar en `PersonajeEnEscena` de esa escena.
- Al menos un campo reemplazado tiene que ser distinto del de la hoja base (comparado sin
  espacios al inicio ni al final).
- Edad, peinado y vestuario, si se reemplazan, no pueden quedar vacíos.
- Se puede crear, editar o eliminar solo si la escena está en `Pendiente` o `Error`.

**Hoja efectiva** = la hoja base con los campos reemplazados por la variante. Se calcula al
iniciar cada llamada a la IA y al armar cada prompt (FR-003a, FR-032).

### Bloque

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | int | PK |
| ClipId | int | FK → Clip, cascada. Índice único (un bloque por clip) |

### Toma

| Campo | Tipo | Reglas |
| --- | --- | --- |
| Id | int | PK |
| BloqueId | int | FK → Bloque, cascada |
| Orden | int | 1..n dentro del bloque, en el orden de la respuesta |
| Plano | texto | Valor del vocabulario al guardarse |
| Optica | texto | Valor del vocabulario al guardarse |
| Iluminacion | texto | Valor del vocabulario al guardarse |
| Angulo | texto? | Opcional |
| Movimiento | texto? | Opcional |
| Accion | texto | No vacía; sin espacios al inicio ni al final |

- Al editar, los campos técnicos solo aceptan valores del vocabulario vigente; ángulo y
  movimiento se pueden vaciar (FR-023).
- Un valor guardado que ya no está en el vocabulario se conserva y se marca en la revisión
  (FR-015b).
- Solo se edita si la escena está en `Generada` (FR-023a).

### TomaDialogo

| Campo | Tipo | Reglas |
| --- | --- | --- |
| TomaId | int | PK compuesta; FK → Toma, cascada |
| LineaDialogoId | int | PK compuesta; FK → LineaDialogo, cascada |

Cada línea de un clip está en exactamente una toma de su bloque (RN-09), en un orden de tomas no
decreciente respecto del guion (FR-017a). No se edita (FR-023a).

## Transiciones de estado de la escena

```text
            (carga de guion)
                  │
                  ▼
             Pendiente ──── le toca su turno ────► Generando
                                                   │   │   │
            respuesta válida y guardado atómico ───┘   │   └── se agotan los intentos,
                  ▼                                     │       error de configuración,
               Generada                                 │       error interno
                                                        ▼
  Error ◄──────────── al iniciar la app, si estaba en Generando (FR-021)
    │
    └── "Reintentar" ──► Generando
```

- `Pendiente → Generando`: cuando la escena llega a su turno en la generación en curso
  (FR-014a). Las que esperan siguen en `Pendiente`.
- `Generando → Generada`: los bloques y el cambio de estado se guardan en una sola transacción
  (FR-019).
- `Generando → Error`: con `MotivoError` (FR-019a). Si el motivo es de configuración, las
  escenas que esperaban siguen en `Pendiente` (FR-018a).
- `Error → Generando`: solo con "Reintentar" y si no hay otra generación en curso (FR-020,
  FR-014a). `MotivoError` se borra.
- `Generada` es final mientras no se recargue el guion; no se regenera (fuera de alcance).
- Recargar el guion elimina todas las escenas, y las nuevas empiezan en `Pendiente` (FR-007,
  FR-013).

## Objetos en memoria (no persistidos)

| Objeto | Origen | Uso |
| --- | --- | --- |
| `Vocabulario` | `config/vocabulario.json` | Schema de salida, validación de respuestas y de ediciones |
| `MoldePrompt` | `config/molde-prompt.json` | Armado del prompt (FR-025a) |
| `EstiloGlobal` | `config/estilo.txt` | Sección 1 del prompt |
| `InstruccionGeneracion` | `config/instruccion-generacion.txt` | Plantilla de la instrucción a la IA |
| `GuionParseado` | Parser | Resultado de la validación; se guarda solo si no tiene errores |
| `RespuestaIa` | Cliente de IA | JSON crudo; se valida antes de convertirse en `Bloque` y `Toma` |
