# Contrato: generación con la IA (Gemini)

**Fuente**: spec FR-014 a FR-021, PRD Anexo C (RN-02, RN-08 a RN-10) y Anexo D. Implementación
detrás de `IClienteIa` (research R-05).

## Instrucción enviada (una por escena)

Se arma reemplazando los dos marcadores de `config/instruccion-generacion.txt`:

### `{{escena}}`

Formato de texto, determinístico (el mismo dato da el mismo texto):

```text
ESCENA {numero} — {INT.|EXT.} {lugar} — {momento}
Locación: {locacion}
Iluminación base: {iluminacion}
Puesta en escena: {puestaEnEscena}
Audio: {audio}

CLIP {n}
Acción:
{línea de acción 1}
{línea de acción 2}
Diálogos:
[c{n}-d1] {Original de la línea 1}
[c{n}-d2] {Original de la línea 2}
Texto en pantalla: {TextoEnPantallaOriginal}

CLIP {n+1}
…
```

- Si un clip no tiene diálogos, la sección `Diálogos:` dice `(ninguno)`; si no tiene texto en
  pantalla, `Texto en pantalla: (ninguno)`. Si no tiene acción, `Acción: (ninguna)`.
- Las líneas de diálogo van con su id (FR-016a) y su línea original, incluidas las de personajes
  sin hoja (FR-016b).
- No se incluye ninguna otra escena (FR-016b).

### `{{personajes}}`

Las hojas efectivas de los personajes presentes en la escena, en el orden de FR-025b, cada una
armada con la plantilla `porPersonaje` de `config/molde-prompt.json` (el mismo texto que irá en
el prompt), una por línea. Si no hay ninguno: `(ninguno)`.

La hoja efectiva se calcula al iniciar cada llamada, no al empezar la escena (FR-003a).

## Schema de salida estructurada

Se genera al iniciar desde `config/vocabulario.json` (FR-015). `{valores de X}` es la lista
`categorias.X.valores` en el orden del archivo:

```json
{
  "type": "object",
  "properties": {
    "bloques": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "clip": { "type": "integer" },
          "tomas": {
            "type": "array",
            "items": {
              "type": "object",
              "properties": {
                "plano":       { "type": "string", "enum": ["{valores de plano}"] },
                "optica":      { "type": "string", "enum": ["{valores de optica}"] },
                "iluminacion": { "type": "string", "enum": ["{valores de iluminacion}"] },
                "angulo":      { "type": "string", "enum": ["{valores de angulo}"] },
                "movimiento":  { "type": "string", "enum": ["{valores de movimiento}"] },
                "accion":      { "type": "string" },
                "dialogos":    { "type": "array", "items": { "type": "string" } }
              },
              "required": ["plano", "optica", "iluminacion", "accion", "dialogos"]
            }
          }
        },
        "required": ["clip", "tomas"]
      }
    }
  },
  "required": ["bloques"]
}
```

Una categoría con `"obligatorio": true` va en `required`; con `false`, no (ángulo,
movimiento). Ningún valor se escribe en el código (constitución II).

## Validación de la respuesta (FR-017, FR-017a, FR-017b)

Se valida el JSON crudo. La primera regla que falla, en este orden, da el motivo:

| # | Regla | Motivo (ejemplos) |
| --- | --- | --- |
| 1 | JSON válido con la estructura del schema: tipos correctos, sin campos desconocidos, `required` presentes | "la respuesta no tiene el formato pedido" |
| 1b | Campos técnicos dentro del vocabulario vigente. Ángulo y movimiento ausentes, nulos o `""` = sin valor. Plano, óptica o iluminación ausentes, nulos o `""` = falla (RN-01) | "el plano «xyz» de la toma 2 del clip 1 no está en el vocabulario" |
| 2 | RN-08: un bloque por clip, cada número de 1 a N exactamente una vez | "falta el bloque del clip 3" · "el clip 1 aparece dos veces" |
| 3 | RN-10: al menos una toma por bloque; acción no vacía después de recortarla | "el bloque del clip 2 no tiene tomas" · "la toma 1 del clip 2 no tiene acción" |
| 4 | RN-09: cada línea del clip referenciada exactamente una vez; sin ids inexistentes ni de otro clip | "falta el diálogo c2-d1" · "el diálogo c1-d1 se usa dos veces" · "el id c3-d1 no es del clip 2" |
| 5 | Orden: recorriendo las tomas del bloque, el orden de las líneas no retrocede | "diálogos fuera de orden en el clip 2" |
| 6 | La acción no contiene `"`, `“` ni `”` | "la acción de la toma 1 del clip 4 tiene comillas" |

Una respuesta válida se convierte en `Bloque` y `Toma` (con `TomaDialogo`) y se guarda junto con
el paso a "generada", en una transacción (FR-019).

## Intentos y motivo final

Ver la tabla de clasificación en [research.md](../research.md) (R-06). Resumen:

- Hasta 3 intentos; espera de 10 s antes del 2.º y de 30 s antes del 3.º; timeout de 60 s por
  llamada (FR-018).
- El error de configuración no se reintenta y detiene la cola (FR-018a).
- El motivo guardado es el del último intento fallido, en la forma
  "{categoría}: {detalle}". Ejemplo: "La respuesta no cumplió las reglas: falta el bloque del
  clip 3." Nunca incluye la clave ni cabeceras (FR-019a).

## Registro (FR-019c)

Una línea JSON por intento en `logs/generacion-AAAA-MM-DD.jsonl`:

```json
{ "fecha": "2026-10-17T14:03:22Z", "episodioId": 3, "escena": 2, "intento": 1,
  "duracionMs": 15123, "resultado": "invalida",
  "motivo": "La respuesta no cumplió las reglas: falta el bloque del clip 3.",
  "respuestaCruda": "{…}" }
```

- `resultado` es uno de: `ok`, `timeout`, `error_ia`, `invalida`, `configuracion` o
  `error_interno`.
- `respuestaCruda` aparece solo cuando `resultado = invalida`.
