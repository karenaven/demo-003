# Feature Specification: RodajeIA v1 — del guion al prompt consistente por escena

**Feature Branch**: `001-rodaje-ia-v1`

**Created**: 2026-10-15

**Status**: Draft

**Input**: User description: "Generá el spec a partir del PRD" (fuente: `PRD.md`, PRD-003 v3)

Esta spec cubre la v1 completa descrita en `PRD.md`. Los identificadores RF, RN, RNF y AC del
PRD se conservan entre paréntesis para mantener la trazabilidad. Los anexos del PRD (A:
convención del guion, B: modelo de datos, C: reglas de negocio, D: estructura del prompt,
E: vocabulario) son parte normativa de esta spec y no se duplican aquí.

## Clarifications

### Session 2026-10-16

- Q: ¿Los nombres de las hojas de personaje deben ser únicos dentro de una serie? → A: Sí,
  únicos en la serie sin distinguir mayúsculas; no se puede crear ni renombrar una hoja con un
  nombre ya existente en la serie, y se indica el motivo.
- Q: Si existen las hojas "José" y "José Daniel" y la acción dice "José Daniel", ¿se detecta
  solo a José Daniel o a ambos? → A: Gana el nombre más largo; ese tramo cuenta solo para José
  Daniel y "José" se detecta solo si aparece en otro lugar por sí mismo.
- Q: Al pulsar "Generar" en un episodio con varias escenas pendientes, ¿se generan de a una o
  varias a la vez? → A: De a una, en orden de número de escena; la siguiente empieza cuando la
  anterior queda "generada" o "error".
- Q: Cuando una escena queda en "error", ¿se muestra el motivo de la falla? → A: Sí, un motivo
  corto en lenguaje simple, solo del último intento fallido.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cargar y validar el guion de un episodio (Priority: P1)

El/la productor/a elige una serie y un episodio existentes, pega el guion en texto plano y lo
confirma. Si el guion respeta la convención de formato, queda guardado y dividido en escenas y
clips, con la acción, el diálogo y el texto en pantalla separados y los personajes con hoja
detectados en cada clip y escena. Si el guion es inválido, ve la lista completa de errores, cada
uno con su número de línea y motivo, para devolverlo al/la guionista; no se guarda nada.

**Why this priority**: todo lo demás (generación, revisión, prompt final) depende de tener el
guion estructurado. Además es la parte de mayor riesgo y se puede verificar entera sin IA.

**Independent Test**: con una serie, un episodio y hojas de personaje cargadas, pegar el guion de
ejemplo del Anexo A y comprobar escenas, clips, campos, diálogos y personajes detectados; pegar
guiones con cada error del Anexo A y comprobar la línea y el motivo reportados.

**Acceptance Scenarios**:

1. **Given** una serie y un episodio elegidos y un guion válido, **When** se pega y se confirma
   la carga, **Then** el guion queda guardado en ese episodio (AC-02a).
2. **Given** un guion al que le falta `AUDIO:` en la escena 1 y que no tiene ningún `CLIP` en la
   escena 2, **When** se intenta cargar, **Then** se muestran los dos errores, cada uno en la
   línea del encabezado de su escena (AC-03a).
3. **Given** un episodio con un guion válido guardado y un guion nuevo inválido, **When** se
   intenta cargar el nuevo, **Then** el episodio conserva el guion anterior completo (AC-02d).
4. **Given** la escena de ejemplo del Anexo A, **When** se procesa, **Then** tiene 2 clips y el
   clip 1 tiene dos líneas de acción y dos diálogos: ANA sin acotación ("Mentiroso.") y
   JOSÉ DANIEL con acotación "sonriendo" ("Bueno... pasó parecido.") (AC-04b, AC-04c).
5. **Given** la hoja "Ana" y un clip cuya acción dice "Anabel" sin diálogo de `ANA:`, **When**
   se procesa, **Then** Ana no aparece como presente en el clip (AC-05e).
6. **Given** un clip con `MESERO: "¿Algo más?"` y una serie sin hoja "Mesero", **When** se carga,
   **Then** el guion es válido, la línea queda como diálogo y "Mesero" no se detecta como
   personaje (AC-05d).
7. **Given** un guion válido, **When** se procesa dos veces, **Then** ambos resultados son
   iguales campo por campo (AC-04e).

Cubre además AC-02b, AC-02c, AC-03b–AC-03u, AC-04a, AC-04d, AC-05a–AC-05c, AC-05f y AC-05g.

---

### User Story 2 - Generar los bloques de tomas de cada escena (Priority: P1)

Con el guion cargado (todas las escenas en "pendiente"), el/la productor/a pulsa "Generar". El
sistema pide a la IA, escena por escena, un bloque por clip con una o más tomas, cuyos campos
técnicos (plano, óptica, iluminación y, opcionalmente, ángulo y movimiento) salen solo del
vocabulario cerrado. Cada respuesta se valida; si no cumple las reglas se reintenta, y si agota
los intentos la escena queda en "error" sin afectar a las demás. El/la productor/a ve el estado
de cada escena y puede reintentar las que quedaron en "error".

**Why this priority**: es el objetivo 2 del producto (la especificación técnica deja de ser
manual) y la mitad del valor de la v1.

**Independent Test**: con un guion cargado y la IA reemplazada por respuestas controladas
(válidas, inválidas, errores), pulsar "Generar" y comprobar estados, cantidad de bloques,
valores del vocabulario, intentos y persistencia por escena.

**Acceptance Scenarios**:

1. **Given** un guion válido recién cargado de 2 escenas, **When** termina la carga, **Then**
   ambas escenas están en "pendiente" y no se llamó a la IA (AC-06j).
2. **Given** 2 escenas en "pendiente" y la IA respondiendo bien, **When** se pulsa "Generar",
   **Then** ambas terminan en "generada" (AC-06f).
3. **Given** una escena de 3 clips y una respuesta con 2 bloques, o con bloques para los clips
   1, 1 y 3, **When** se valida, **Then** se trata como fallida y se reintenta (AC-06e, AC-06g).
4. **Given** un clip con dos líneas de diálogo y una respuesta que referencia solo una, **When**
   se valida, **Then** se trata como fallida y se reintenta (AC-06b).
5. **Given** que la IA falla en los 3 intentos de la escena 3 y las escenas 1 y 2 se generaron
   bien, **When** termina la generación, **Then** se hicieron exactamente 3 intentos para la
   escena 3, que queda en "error", y las escenas 1 y 2 conservan sus bloques (AC-10a, AC-10b).
6. **Given** una escena en "error", **When** se reintenta y la IA responde bien, **Then** queda
   con sus bloques y deja de estar en "error" (AC-10d).
7. **Given** una escena guardada en "generando", **When** se inicia la aplicación, **Then** queda
   en "error" (AC-10f).

Cubre además AC-06a, AC-06c, AC-06d, AC-06h, AC-06i, AC-06k, AC-10c y AC-10e.

---

### User Story 3 - Armar y copiar el prompt final de cada escena (Priority: P1)

Para cada escena generada, el/la productor/a ve el prompt final armado por plantilla, con seis
secciones en orden fijo (estilo, locación e iluminación base, hojas de los personajes
presentes, puesta en escena, audio y un bloque numerado por clip), y lo copia completo con un
solo botón para pegarlo en la IA de video. Las hojas de personaje, el diálogo y el texto en
pantalla aparecen literales, idénticos en todas las escenas.

**Why this priority**: es el objetivo 1 del producto (consistencia de personajes) y la salida
que el/la productor/a usa realmente.

**Independent Test**: con escenas y bloques guardados, armar el prompt y compararlo contra el
esperado: secciones, orden de bloques, hojas idénticas entre escenas, diálogos literales.

**Acceptance Scenarios**:

1. **Given** dos escenas distintas con el mismo personaje y sin variantes, **When** se arman
   ambos prompts, **Then** la descripción del personaje es idéntica palabra por palabra
   (AC-08e).
2. **Given** el clip 1 de la escena de ejemplo, **When** se arma el prompt, **Then** contiene
   `ANA: "Mentiroso."` y `JOSÉ DANIEL (sonriendo): "Bueno... pasó parecido."` idénticos al guion
   (AC-08f).
3. **Given** una escena de 3 clips generada, **When** se arma el prompt, **Then** contiene
   "Bloque 1:", "Bloque 2:" y "Bloque 3:" en ese orden (AC-08b).
4. **Given** una escena sin personajes con hoja, **When** se arma el prompt, **Then** se omite la
   sección de personajes y se incluye el resto (AC-08h).
5. **Given** un prompt armado, **When** se pulsa copiar, **Then** el portapapeles contiene el
   prompt completo, idéntico al mostrado (AC-08d).

Cubre además AC-08a, AC-08c y AC-08g.

---

### User Story 4 - Gestionar series, hojas de personaje y episodios (Priority: P2)

El/la productor/a crea, lista, edita y elimina series; dentro de cada serie, hojas de personaje
(con todos los campos obligatorios salvo heridas/marcas) y episodios (con número único y título).
Eliminar una serie o un episodio borra todo lo que depende de ellos. Al abrir un episodio ve sus
escenas en orden, con su estado y, si están generadas, su prompt final.

**Why this priority**: es el soporte de datos necesario para las historias P1, pero de por sí
no entrega el valor central; se prioriza solo lo mínimo para la demo.

**Independent Test**: crear, listar, editar y eliminar series, hojas y episodios, y comprobar
validaciones y eliminaciones en cascada.

**Acceptance Scenarios**:

1. **Given** una serie existente, **When** se crea una hoja de personaje con el peinado vacío,
   **Then** no se guarda y se indica que falta el peinado (AC-01i).
2. **Given** una serie con hojas y episodios con guion, **When** se elimina la serie, **Then** ya
   no existen la serie, sus hojas, sus episodios ni sus escenas, clips, bloques y variantes
   (AC-01d).
3. **Given** una serie con el episodio número 1, **When** se intenta crear otro episodio número 1,
   **Then** no se guarda y se indica que el número ya existe (AC-11f).
4. **Given** un episodio con escenas 1 y 2 generadas y la 3 en "error", **When** se abre, **Then**
   se muestran en orden 1, 2, 3, las dos primeras con su prompt y la 3 con su estado (AC-11e).

Cubre además AC-01a–AC-01c, AC-01e–AC-01h, AC-01j, AC-01k, AC-11a–AC-11d y AC-11g.

---

### User Story 5 - Revisar y editar las tomas generadas (Priority: P2)

El/la productor/a revisa los bloques de cada escena y edita los campos técnicos de cualquier toma
(solo con valores del vocabulario; ángulo y movimiento se pueden vaciar) o su texto de acción.
Cada edición se guarda y el prompt final se vuelve a armar automáticamente.

**Why this priority**: la generación busca no requerir intervención manual; la edición es la
red de seguridad del/la productor/a, no el flujo principal.

**Independent Test**: con una escena generada, editar plano, ángulo y acción, reiniciar la
aplicación y comprobar que el prompt refleja los cambios.

**Acceptance Scenarios**:

1. **Given** una toma con plano "plano medio", **When** se edita a "primer plano" y se arma el
   prompt, **Then** dice "primer plano" y no "plano medio" (AC-07b).
2. **Given** una toma con plano "plano medio", **When** se intenta asignar un valor fuera del
   vocabulario, **Then** no se acepta y la toma conserva "plano medio" (AC-07d).
3. **Given** una toma editada, **When** se reinicia la aplicación, **Then** la edición sigue
   guardada (AC-07e).

Cubre además AC-07a, AC-07c y AC-07f.

---

### User Story 6 - Definir variantes de personaje por escena (Priority: P3)

Antes de generar, el/la productor/a define para una escena una variante de un personaje que
cambia edad, peinado, vestuario o heridas/marcas (al menos uno). La IA y el prompt de esa escena
usan la hoja efectiva (base + variante); las demás escenas siguen con la hoja base. Una vez que
la escena está "generando" o "generada", sus variantes no se pueden modificar.

**Why this priority**: resuelve casos reales (cambio de ropa, heridas) pero la v1 entrega valor
sin ellas.

**Independent Test**: crear una variante en la escena 2 de un personaje presente en las escenas
1 y 2, generar y comprobar ambos prompts; intentar modificar variantes de escenas generadas.

**Acceptance Scenarios**:

1. **Given** un personaje en las escenas 1 y 2 con variante solo en la 2 que cambia los cuatro
   campos, **When** se arman ambos prompts, **Then** la escena 2 muestra los valores de la
   variante y el resto de la hoja base, y la escena 1 muestra la hoja base (AC-09a).
2. **Given** una variante de vestuario "abrigo gris" sobre base "chaqueta negra", **When** se
   genera la escena, **Then** la instrucción enviada a la IA contiene "abrigo gris" y no
   "chaqueta negra" (AC-09h).
3. **Given** una escena en "generada", **When** se intenta crear, editar o eliminar una variante,
   **Then** no se permite y se indica el motivo (AC-09i, AC-09j, AC-09k).
4. **Given** un personaje que ya tiene variante en la escena 2, **When** se intenta crear otra,
   **Then** no se guarda y se indica que ya existe una (AC-09e).

Cubre además AC-09b–AC-09d, AC-09f, AC-09g, AC-09l y AC-09m.

---

### Edge Cases

- Guion vacío o sin ningún encabezado `ESCENA`: error en la línea 1 (AC-03b, AC-03u).
- Más de 10 escenas o más de 20 clips en una escena: error en la línea del encabezado o `CLIP`
  que excede el límite (AC-03c, AC-03d).
- Encabezado con guion común (`-`) en lugar de raya (`—`) y líneas en blanco en cualquier lugar:
  se aceptan y dan el mismo resultado (AC-03o, AC-03p); las líneas en blanco cuentan para la
  numeración reportada.
- Personaje mencionado solo en `PUESTA EN ESCENA:`: presente en la escena pero en ningún clip
  (AC-05g).
- Nombre sin tilde ("Jose Daniel") o como prefijo de otra palabra ("Anabel"): no se detecta
  (AC-05e, AC-05f).
- Hojas "José" y "José Daniel" con una acción que dice solo "José Daniel": se detecta José
  Daniel y no José; si la acción dice además "José" por separado, se detectan los dos (FR-011a).
- Hoja con un nombre ya usado en la serie, aunque cambien mayúsculas ("ana" frente a "Ana"): no
  se guarda y se indica que el nombre ya existe (FR-002a).
- Hoja sin heridas/marcas: el prompt no muestra la etiqueta "Heridas/marcas" (AC-08c).
- Clip con texto en pantalla: el bloque lo incluye literal y no agrega "Sin subtítulos ni texto
  en pantalla." (AC-08g).
- La IA no responde a tiempo, devuelve error o devuelve una respuesta inválida: se reintenta
  según RNF-07 y, agotados los intentos, la escena queda en "error" con el motivo del último
  intento (AC-10a, AC-10c, FR-019a).
- Se pulsa "Generar" o "Reintentar" mientras el episodio ya está generando: no se inicia una
  segunda generación en paralelo (FR-014a).
- La aplicación se cierra en medio de una generación: al reiniciar, las escenas en "generando"
  pasan a "error" (AC-10f).
- Recarga de guion sobre un episodio ya generado: reemplaza todo el guion anterior (AC-02c).
- Edición de una hoja base usada en escenas ya generadas: el prompt toma la hoja nueva; la
  acción escrita por la IA puede quedar contradictoria y se corrige a mano (riesgo aceptado).
- Creación o renombrado de una hoja después de cargar un guion: no vuelve a detectar
  personajes; hay que recargar el guion.

## Requirements *(mandatory)*

### Functional Requirements

**Series, personajes y episodios**

- **FR-001**: El sistema MUST permitir crear, listar, editar y eliminar series (RF-01a–d).
  Eliminar una serie MUST eliminar sus hojas de personaje, sus episodios y todo lo que depende
  de ellos.
- **FR-002**: El sistema MUST permitir crear, listar, editar y eliminar hojas de personaje dentro
  de una serie, con los campos del Anexo B; todos son obligatorios salvo heridas/marcas, y una
  edición no puede dejar vacío un campo obligatorio (RF-01e–g).
- **FR-002a**: El nombre de una hoja de personaje MUST ser único dentro de su serie, comparado
  sin distinguir mayúsculas de minúsculas ("Ana" y "ANA" son el mismo nombre). Crear o renombrar
  una hoja con un nombre ya existente en la serie MUST rechazarse indicando que el nombre ya
  existe. Series distintas pueden tener hojas con el mismo nombre.
- **FR-003**: Eliminar una hoja de personaje MUST eliminar también sus variantes, incluidas las
  de escenas en "generando" o "generada" (RF-01h).
- **FR-004**: El sistema MUST permitir crear episodios con número y título, con número único
  dentro de la serie; listarlos; editar solo su título; y eliminarlos junto con su guion,
  escenas, clips, bloques y variantes (RF-11a–d).
- **FR-005**: El sistema MUST mostrar, dentro de cada episodio, sus escenas en orden, con su
  estado de generación y, si está generada, su prompt final (RF-11e).

**Carga, validación y división del guion**

- **FR-006**: El sistema MUST permitir cargar el guion de un episodio como texto plano, asociado
  a una serie y un episodio elegidos antes de la carga (RF-02a).
- **FR-007**: Cargar un guion válido en un episodio que ya tiene uno MUST reemplazarlo y eliminar
  las escenas, clips, bloques y variantes del anterior (RF-02b).
- **FR-008**: El sistema MUST validar el guion contra la convención, los límites (10 escenas,
  20 clips por escena) y los errores del Anexo A, y mostrar todos los errores con su número de
  línea y motivo según las reglas de "Línea que se reporta" (RF-03a, RF-03b).
- **FR-009**: Un guion inválido MUST rechazarse completo, sin guardar ninguna parte y sin
  modificar el guion anterior del episodio (RF-03c).
- **FR-010**: La validación, la división en escenas y clips, la extracción del encabezado y los
  cuatro campos de escena, y la separación de acción, diálogo (personaje, acotación opcional,
  texto) y texto en pantalla MUST ser determinísticas y sin IA (RF-04a–d).
- **FR-011**: El sistema MUST detectar, sin IA, los personajes con hoja presentes en cada clip
  (nombre exacto como palabra completa en la acción, con mayúscula inicial y tildes, o marcador
  de diálogo en mayúsculas) y en cada escena (presentes en algún clip o mencionados en
  `PUESTA EN ESCENA:`) (RF-05a, RF-05b).
- **FR-011a**: Cuando el nombre de una hoja está contenido en el nombre de otra de la misma
  serie (por ejemplo "José" y "José Daniel"), un mismo tramo de texto MUST contar solo para el
  nombre más largo que coincide en esa posición. El nombre más corto se detecta solo si aparece
  como palabra completa en otro tramo que no forma parte de una coincidencia más larga. Los
  marcadores de diálogo se comparan con el nombre completo antes de los dos puntos, por lo que
  `JOSÉ DANIEL:` cuenta solo para José Daniel.
- **FR-012**: Los personajes sin hoja MUST poder tener diálogo, que se acepta y se inserta
  literal, pero no se detectan como personajes (RN-07).

**Generación**

- **FR-013**: Las escenas de un guion recién cargado MUST quedar en "pendiente" sin iniciar la
  generación (RF-06d).
- **FR-014**: El sistema MUST ofrecer un botón "Generar" que genera todas las escenas en
  "pendiente" de un episodio, y solo esas (RF-06c).
- **FR-014a**: Las escenas MUST generarse de a una, en orden ascendente de número de escena: la
  siguiente escena empieza solo cuando la anterior quedó en "generada" o "error". Las escenas
  que esperan su turno siguen en "pendiente". Mientras el episodio tiene una generación en
  curso, pulsar "Generar" o reintentar una escena de ese episodio MUST NOT iniciar otra
  generación en paralelo.
- **FR-015**: Para cada escena, el sistema MUST pedir a la IA un bloque por clip, cada uno con
  una o más tomas, cuyos campos técnicos solo pueden tomar valores del vocabulario cerrado del
  Anexo E (RF-06a, RN-02). El único texto libre de la IA es la acción de cada toma.
- **FR-016**: La IA MUST recibir como contexto la escena completa (campos, clips e identificadores
  de cada línea de diálogo) y las hojas efectivas de los personajes presentes; MUST referenciar
  las líneas de diálogo por identificador y no las reescribe (RN-03, RN-04, RN-05).
- **FR-017**: El sistema MUST tratar como fallida una respuesta que no cumple RN-08 (un bloque
  por clip, cada número de clip una vez), RN-09 (cada línea de diálogo del clip referenciada
  exactamente una vez, sin ids ajenos o inexistentes) o RN-10 (al menos una toma por bloque,
  acción no vacía) (RF-06b).
- **FR-018**: Ante error, falta de respuesta en 60 s o respuesta inválida, el sistema MUST
  reintentar hasta 2 veces (3 intentos en total), esperando 10 s antes del primer reintento y
  30 s antes del segundo (RNF-06, RNF-07).
- **FR-019**: El sistema MUST mantener por escena un estado: "pendiente", "generando",
  "generada" o "error"; MUST guardar los bloques de una escena en cuanto se generan, de forma
  que el fallo de otra escena no los borre; y MUST marcar "error" cuando se agotan los intentos
  (RF-10a, RF-10b, RF-10d).
- **FR-019a**: Cuando una escena queda en "error", el sistema MUST guardar y mostrar junto a su
  estado un motivo corto, en lenguaje simple, del último intento fallido, que distinga al menos:
  la IA no respondió a tiempo, la IA devolvió un error o no está disponible, la respuesta no
  cumplió las reglas (indicando cuál, por ejemplo "falta el bloque del clip 3"), error de
  configuración de la IA, y generación interrumpida por un cierre de la aplicación (FR-021). El
  motivo MUST NOT incluir datos sensibles como la clave de la IA, y se borra cuando la escena
  sale de "error".
- **FR-020**: El sistema MUST permitir reintentar la generación de una escena en "error"
  (RF-10c).
- **FR-021**: Al iniciar la aplicación, toda escena en "generando" MUST pasar a "error" (RF-10e).

**Revisión y edición**

- **FR-022**: El sistema MUST mostrar los bloques generados de cada escena con todas sus tomas
  (RF-07a).
- **FR-023**: El sistema MUST permitir editar los campos técnicos de cada toma, aceptando solo
  valores del vocabulario y permitiendo vaciar ángulo y movimiento, y editar su texto de acción
  (RF-07b, RF-07c).
- **FR-024**: Cada edición MUST guardarse y el texto del bloque MUST volver a armarse
  automáticamente (RF-07d, RF-07e).

**Prompt final**

- **FR-025**: El sistema MUST armar, por plantilla determinística (Anexo D), un prompt final por
  escena con seis secciones en orden fijo: estilo global, locación e iluminación base, hojas
  efectivas de los personajes presentes (se omite si no hay ninguno con hoja), puesta en
  escena, audio y un bloque numerado por clip en el orden de los clips (RF-08a).
- **FR-026**: Las hojas de personaje, el diálogo (con marcador y acotación) y el texto en
  pantalla MUST insertarse literales; ningún campo obligatorio del prompt puede quedar vacío y
  los campos opcionales vacíos se omiten (RN-01, RN-03, RN-04).
- **FR-027**: Cada bloque MUST unir sus tomas con "Corte a" y cerrar con el texto en pantalla del
  clip, si existe, o con "Sin subtítulos ni texto en pantalla." (Anexo D).
- **FR-028**: El sistema MUST permitir copiar el prompt final completo de una escena con un solo
  botón (RF-08b).

**Variantes de personaje**

- **FR-029**: El sistema MUST permitir crear, listar, editar y eliminar, por escena, una variante
  de un personaje que reemplace edad, peinado, vestuario o heridas/marcas; cada variante cambia
  al menos un campo y hay como máximo una por personaje y escena (RF-09a–d).
- **FR-030**: Eliminar una variante MUST dejar intacta la hoja base (RF-09d).
- **FR-031**: El sistema MUST impedir crear, editar o eliminar variantes de una escena en
  "generando" o "generada", indicando el motivo (RF-09e).
- **FR-032**: La hoja efectiva (hoja base con los campos de la variante reemplazados) MUST ser
  la que recibe la IA y la que se inserta en el prompt de esa escena (RN-05).

### Key Entities *(include if feature involves data)*

- **Serie**: agrupa hojas de personaje y episodios; tiene nombre.
- **Hoja de personaje**: descripción base de un personaje dentro de una serie (nombre, edad,
  descripción física con voz, peinado, vestuario, heridas/marcas opcionales, personalidad, rol).
- **Episodio**: pertenece a una serie; número único en la serie, título y guion en texto plano.
- **Escena**: pertenece a un episodio; número, INT/EXT, lugar, momento del día, locación,
  iluminación base, puesta en escena, audio, personajes detectados, estado de generación y,
  si está en "error", el motivo del último intento fallido.
- **Variante de personaje**: reemplazo de edad, peinado, vestuario o heridas/marcas de una hoja
  para una escena concreta; como máximo una por personaje y escena.
- **Clip**: pertenece a una escena; número, acción, líneas de diálogo (personaje, acotación
  opcional, texto, identificador) y texto en pantalla opcional.
- **Bloque**: la lista ordenada de tomas generadas para un clip.
- **Toma**: plano, óptica, iluminación, ángulo (opcional), movimiento (opcional), texto de acción
  y referencias a las líneas de diálogo del clip.
- **Vocabulario técnico**: categorías y valores permitidos, global y versionado junto al
  producto; lo mantiene el/la productor/a.
- **Estilo global**: texto fijo de estilo cinematográfico, único para todas las series.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En el 100% de los prompts donde aparece un personaje sin variante, su descripción
  es idéntica palabra por palabra a la de su hoja (objetivo 1).
- **SC-002**: Un guion válido produce, para cada escena, un prompt final con un bloque por clip
  sin que el/la productor/a tenga que escribir ni corregir nada, más allá de pulsar "Generar"
  (definición de éxito de v1, objetivo 2).
- **SC-003**: El 100% de los campos técnicos de las tomas pertenece al vocabulario cerrado, y el
  100% de los diálogos y textos en pantalla aparece en el prompt idéntico al guion.
- **SC-004**: Validar y dividir un guion de tamaño máximo (10 escenas × 20 clips) tarda menos de
  1 segundo en el 95% de los casos (RNF-03).
- **SC-005**: Armar el prompt final de una escena tarda menos de 1 segundo en el 95% de los casos
  (RNF-04).
- **SC-006**: Generar una escena de 20 clips tarda menos de 30 segundos en el 95% de los casos,
  sin contar reintentos (RNF-05).
- **SC-007**: Un guion inválido se rechaza siempre completo, y el 100% de sus errores se informa
  con número de línea y motivo, de modo que el/la guionista puede corregirlo sin preguntar.
- **SC-008**: El recorrido crear serie → crear personaje → crear episodio → pegar guion →
  "Generar" → ver bloques y prompt final se completa en menos de 2 minutos con una escena de 4
  clips (criterio de demo).
- **SC-009**: Una falla de generación en una escena nunca hace perder los bloques ya generados de
  otras escenas.

## Assumptions

- La v1 es monousuario y corre en el equipo local: sin login, roles ni separación de datos entre
  usuarios; los tiempos se miden en ese equipo.
- Se usa un servicio externo de IA de texto con salida estructurada que admite listas cerradas de
  valores; el spike del 2026-10-04 confirmó que cumple SC-006 (p95 de 22,2 s).
- El vocabulario (`config/vocabulario.json`), el estilo global (`config/estilo.txt`), la
  instrucción de generación (`config/instruccion-generacion.txt`) y el molde del prompt
  (`config/molde-prompt.json`) ya existen como archivos versionados y se editan sin pantalla de
  administración. La redacción del molde es un borrador que se ajustará comparándolo con el
  prompt manual actual.
- El/la guionista respeta la convención de formato; si no, el guion se le devuelve con los
  errores. Que cada clip entre en unos 8 segundos es criterio suyo y no se valida.
- Las variantes se definen antes de pulsar "Generar".
- Quedan fuera de alcance, según el PRD: integración directa con la IA de video; generar o editar
  imagen o video; multiusuario; analítica; storyboard visual; normalizar guiones mal
  formateados; pantallas de administración de vocabulario o estilo; validar la duración de los
  clips; agregar o quitar tomas a mano; hojas para personajes secundarios; recargar un guion
  mientras se genera el anterior; regenerar escenas ya generadas (solo se reintentan las que
  están en "error"); cancelar una generación en curso; volver a detectar personajes sin recargar
  el guion.
