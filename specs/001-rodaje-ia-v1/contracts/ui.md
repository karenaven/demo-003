# Contrato: interfaz (Blazor, Interactive Server)

**Fuente**: spec, User Stories 1 a 6. La v1 es monousuario y local, sin login (constitución VI).
Los textos de la UI están en español. Cada acción rechazada muestra su motivo junto al control
que la originó.

## Rutas

| Ruta | Página | Contenido |
| --- | --- | --- |
| `/` | Series | Lista de series; crear, editar o eliminar una serie (FR-001). |
| `/series/{serieId}` | Serie | Pestañas "Personajes" (hojas: crear, editar, eliminar; FR-002 a FR-003b) y "Episodios" (crear con número y título, editar título, eliminar; FR-004, FR-004a). |
| `/episodios/{episodioId}` | Episodio | Carga del guion, lista de escenas con estado y botón "Generar" (FR-005 a FR-021). |
| `/escenas/{escenaId}` | Escena | Variantes, bloques y tomas (revisión y edición) y prompt final con "Copiar" (FR-022 a FR-032). |

## Episodio (`/episodios/{episodioId}`)

- **Cargar guion:** un `<textarea>` y el botón "Validar y guardar".
  - Si el guion es inválido, se muestra la lista de errores ("Línea {n}: {motivo}") y no se
    guarda nada (FR-008, FR-009).
  - Si el episodio ya tiene guion, se pide confirmación antes de reemplazarlo (FR-007).
  - Mientras el episodio tiene una generación en curso, la carga está deshabilitada y muestra el
    motivo (FR-004a).
- **Escenas:** en orden de número, cada una con su encabezado y su estado:
  - "Pendiente".
  - "Generando · intento {i} de 3".
  - "Generada", con un enlace a la escena.
  - "Error: {motivo}", con el botón "Reintentar" (FR-005, FR-018, FR-019a, FR-020).
- **Generar:** se habilita si hay escenas en "pendiente" y no hay ninguna generación en curso en
  la app. Si hay una, se deshabilita con el texto "Hay una generación en curso" (FR-014,
  FR-014a). "Reintentar" sigue la misma regla.
- **En vivo:** los estados y la disponibilidad de los botones se actualizan sin recargar, en
  todas las pestañas abiertas (FR-019b).

## Escena (`/escenas/{escenaId}`)

- **Encabezado:** número, INT/EXT, lugar, momento, estado y motivo si está en "error".
- **Personajes presentes y variantes:** se listan los personajes detectados y su variante, si
  tiene.
  - Se puede crear, editar o eliminar una variante solo en "pendiente" o "error" (FR-029 a
    FR-031). En los demás estados, los controles se deshabilitan y muestran el motivo.
  - El formulario ofrece edad, peinado, vestuario y heridas/marcas, con la opción "Sin heridas"
    (FR-029a).
- **Bloques** (solo en "generada"): un bloque por clip, con sus tomas.
  - Cada toma tiene listas desplegables con los valores vigentes del vocabulario; ángulo y
    movimiento ofrecen además "(sin valor)".
  - El texto de la acción es editable.
  - Las líneas de diálogo referenciadas se muestran como texto de solo lectura (FR-022, FR-023,
    FR-023a).
  - Una toma con un valor fuera del vocabulario muestra la marca "valor fuera del vocabulario"
    (FR-015b).
  - Cada cambio se guarda al confirmarse el campo y el prompt se vuelve a armar (FR-024).
- **Prompt** (solo en "generada"): un `<textarea readonly>` con el prompt completo, la cantidad
  de caracteres y el botón "Copiar".
  - Si se copió, se muestra "Copiado".
  - Si falla, se muestra "No se pudo copiar; seleccioná el texto y copialo a mano" (FR-025,
    FR-028, FR-028a).
  - En los demás estados no hay prompt ni "Copiar" (FR-025c).

## Serie (`/series/{serieId}`)

- **Formulario de hoja:**
  - Todos los campos son obligatorios salvo heridas/marcas.
  - Se rechaza un nombre repetido sin distinguir mayúsculas o un nombre reservado (FR-002,
    FR-002a, FR-002b).
  - Al renombrar una hoja usada en un guion cargado, se muestra el aviso de FR-003b.
- **Eliminar:**
  - Una hoja, un episodio o una serie: se pide confirmación y se avisa de lo que se elimina en
    cascada.
  - Un episodio o una serie con una generación en curso: no se permite y se muestra el motivo
    (FR-004a).
