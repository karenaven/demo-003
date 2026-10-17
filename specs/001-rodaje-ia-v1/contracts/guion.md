# Contrato: guion de un episodio

**Fuente**: PRD Anexo A; spec FR-008 a FR-012. Este contrato fija la gramática que acepta el
parser y el texto de cada error. Todo es determinístico y sin IA (constitución I).

## Lectura del texto (FR-008b, FR-008d)

1. Si el texto tiene más de 100.000 caracteres → un único error `E00` en la línea 1 y no se
   sigue validando.
2. Se descarta un BOM inicial (U+FEFF).
3. Las líneas se separan con LF, CRLF o CR. La línea N es la N-ésima línea física, contando las
   líneas en blanco.
4. Antes de reconocer marcadores, cada línea se recorta de espacios y tabulaciones al inicio y al
   final. Una línea que queda vacía es una línea en blanco: se ignora, pero cuenta para la
   numeración.

## Gramática

`␠` es uno o más espacios. Los marcadores se reconocen solo en su forma exacta (FR-008e).

```text
guion            := escena+                         (texto antes de la 1.ª escena → E02)
escena           := encabezado campos clip+
encabezado       := "ESCENA" ␠ N ␠ SEP ␠ INTEXT ␠ LUGAR ␠ SEP ␠ MOMENTO
SEP              := "—" | "-"
INTEXT           := "INT." | "EXT."
campos           := "LOCACIÓN:" valor  "ILUMINACIÓN:" valor
                    "PUESTA EN ESCENA:" valor  "AUDIO:" valor      (en este orden, una línea c/u)
clip             := "CLIP" ␠ N  contenido+
contenido        := dialogo | texto_pantalla | accion
dialogo          := PERSONAJE [ ␠? "(" acotacion ")" ] ":" ␠? CITA
texto_pantalla   := "TEXTO EN PANTALLA:" ␠? CITA
CITA             := ABRE texto CIERRA              (ABRE ∈ { " “ }, CIERRA ∈ { " ” } al final de la línea)
accion           := cualquier otra línea no vacía
PERSONAJE        := letras mayúsculas (con tildes y Ñ), espacios, "-" o "'"; empieza con letra
```

- `valor` es el resto de la línea después de los dos puntos, recortado; no puede estar vacío.
- `N` en `ESCENA` y `CLIP` es un entero decimal sin signo.
- Una línea es candidata a diálogo si, antes del primer `:`, coincide con
  `PERSONAJE [(acotación)]`. Si coincide, pero lo que sigue no es una `CITA`, se informa `E10`.
- `TEXTO EN PANTALLA:` tiene prioridad sobre la regla de diálogo.
- Dentro de un clip, una línea que empieza con un marcador de campo de escena (`LOCACIÓN:`,
  etc.) se informa como `E06` ("ubicado después del primer CLIP").

## Catálogo de errores (FR-008, FR-008a)

Cada motivo es una frase en español con el elemento, la regla y la forma esperada. `{…}` se
reemplaza por el dato concreto. "Línea" indica qué línea se reporta.

| Código | Caso (Anexo A) | Línea | Motivo |
| --- | --- | --- | --- |
| E00 | Más de 100.000 caracteres | 1 | El guion supera los 100.000 caracteres. |
| E01 | Guion vacío o sin ningún `ESCENA` | 1 | El guion no tiene ninguna escena; debe empezar con un encabezado como "ESCENA 1 — INT. LUGAR — NOCHE". |
| E02 | Texto antes del primer `ESCENA` | esa línea | Hay texto antes de la primera escena; el guion debe empezar con "ESCENA 1 — …". |
| E03 | Encabezado mal formado | esa línea | El encabezado de escena no tiene el formato "ESCENA N — INT. LUGAR — MOMENTO DEL DÍA". |
| E04 | INT/EXT escrito de otra forma | esa línea | En el encabezado de la escena {n}, "{valor}" debe escribirse "INT." o "EXT.", con punto. |
| E05 | Número de escena que no sigue la secuencia | esa línea | La escena {n} debería ser la escena {esperado}: los números empiezan en 1 y no se saltean ni repiten. |
| E06 | Campo de escena fuera de lugar | esa línea | El campo {campo} de la escena {n} {problema}; el orden es LOCACIÓN, ILUMINACIÓN, PUESTA EN ESCENA, AUDIO, antes del primer CLIP. |
| E07 | Campo de escena ausente | encabezado | Falta el campo {campo} en la escena {n}. |
| E08 | Campo de escena sin valor | esa línea | El campo {campo} de la escena {n} no tiene valor. |
| E09 | Línea que no es un campo, entre el encabezado y el primer `CLIP` | esa línea | Antes del primer CLIP de la escena {n} solo pueden ir LOCACIÓN:, ILUMINACIÓN:, PUESTA EN ESCENA: y AUDIO:, cada uno en una línea. |
| E11 | Escena sin ningún `CLIP` | encabezado | La escena {n} no tiene ningún clip; agregá al menos un "CLIP 1". |
| E12 | Número de clip que no sigue la secuencia | esa línea | El clip {n} de la escena {e} debería ser el clip {esperado}: los números empiezan en 1 y no se saltean ni repiten. |
| E13 | Clip sin contenido | línea del `CLIP` | El clip {n} de la escena {e} no tiene contenido. |
| E10 | Diálogo sin comillas | esa línea | El diálogo de {PERSONAJE} debe ir entre comillas: {PERSONAJE}: "texto". |
| E14 | `TEXTO EN PANTALLA` sin comillas | esa línea | El texto en pantalla debe ir entre comillas: TEXTO EN PANTALLA: "texto". |
| E15 | Más de un `TEXTO EN PANTALLA` en un clip | la 2.ª y siguientes | El clip {n} de la escena {e} tiene más de un TEXTO EN PANTALLA; se permite uno solo. |
| E16 | Más de 10 escenas | encabezado de la 11.ª | El episodio supera el máximo de 10 escenas. |
| E17 | Más de 20 clips en una escena | `CLIP` n.º 21 | La escena {e} supera el máximo de 20 clips. |

`{problema}` de E06 es uno de: "está repetido", "está fuera de orden", "está después del primer
CLIP".

**Orden de la lista de errores:** por número de línea ascendente. Si hay dos errores en la misma
línea, se ordenan por código.

**Errores dependientes (FR-008a):** se informan todos. Ejemplo (AC-S01): un `AUDIO:` después de
`CLIP 1` da E06 en su línea y E07 ("Falta el campo AUDIO: en la escena 1.") en el encabezado.

## Resultado de un guion válido

Por escena: número, INT/EXT, lugar, momento del día, los cuatro campos y sus clips. Por clip:
número, líneas de acción en orden, líneas de diálogo (`Posicion`, `Personaje`, `Acotacion`,
`Texto`, `Original`) y texto en pantalla (`Texto`, `Original`). Ver
[data-model.md](../data-model.md).

## Detección de personajes (FR-011 a FR-011d, FR-012)

Entrada: el resultado anterior y las hojas de la serie (`Nombre`, `NombreClave`).

1. **Acción y `PUESTA EN ESCENA:`**: se buscan los nombres de las hojas en el texto (comparados
   en NFC y por ordinal, research R-04). Una ocurrencia cuenta si es palabra completa (FR-011b).
   Si varias ocurrencias se solapan, gana la más larga (FR-011a). Un nombre todo en mayúsculas
   no cuenta (FR-011d), salvo que coincida con la escritura exacta de la hoja.
2. **Diálogo**: `Personaje` de la línea == `NombreClave` de una hoja → presente (FR-011c). Si
   no coincide con ninguna hoja, es un personaje sin hoja: no es error y no se detecta (FR-012).
3. **Clip**: presente si aparece en su acción o es marcador de alguno de sus diálogos.
4. **Escena**: presente si está en algún clip o en `PUESTA EN ESCENA:`. El orden de primera
   aparición (FR-025b) se toma recorriendo `PUESTA EN ESCENA:` y después los clips en orden,
   cada uno de arriba abajo (acción y diálogo según su línea). Si dos personajes empiezan en la
   misma posición, se ordenan alfabéticamente por `Nombre`.
