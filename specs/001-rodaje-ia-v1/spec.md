# Feature Specification: RodajeIA v1 — del guion al prompt consistente por escena

**Feature Branch**: `001-rodaje-ia-v1`

**Created**: 2026-10-15

**Status**: Draft

**Input**: User description: "Generá el spec a partir del PRD" (fuente: `PRD.md`, PRD-003 v3)

Esta spec cubre la v1 completa descrita en `PRD.md`. Los identificadores RF, RN, RNF y AC del
PRD se conservan entre paréntesis para mantener la trazabilidad. Los anexos del PRD (A:
convención del guion, B: modelo de datos, C: reglas de negocio, D: estructura del prompt,
E: vocabulario) son parte normativa de esta spec y no se duplican aquí. Donde esta spec detalla
o restringe al PRD (sesiones de aclaración), prevalece esta spec.

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

### Session 2026-10-17

- Q: Si un error del guion depende de otro (por ejemplo, un `AUDIO:` después de `CLIP 1`),
  ¿se informan los dos? → A: Sí, se informan todos los errores en una sola pasada; ningún error
  oculta a otro.
- Q: ¿Con qué detalle se redacta el motivo de cada error? → A: Una frase en español que nombra
  el elemento, la regla incumplida y la forma esperada.
- Q: ¿Un campo de escena puede ocupar varias líneas? → A: No, cada campo va en una sola línea;
  una línea siguiente sin marcador antes del primer `CLIP` es el error "línea que no es un
  campo de escena" del Anexo A.
- Q: ¿Cómo se tratan CRLF, BOM y tabulaciones? → A: Se aceptan LF, CRLF y CR como fin de línea,
  se ignora un BOM inicial y se recortan espacios y tabulaciones al inicio y al final de cada
  línea para reconocer marcadores; los números de línea cuentan líneas físicas.
- Q: ¿Hay un límite de tamaño del texto del guion? → A: Sí, 100.000 caracteres; si se supera,
  se informa un único error en la línea 1 y no se sigue validando.
- Q: ¿Qué comillas valen en diálogos y textos en pantalla? → A: Rectas (`"`) y tipográficas
  (`“ ”`); se permiten comillas internas y el texto se inserta tal como está escrito.
- Q: ¿Los marcadores admiten variantes de escritura? → A: No, se reconocen solo en su forma
  exacta del Anexo A (mayúsculas, tildes, dos puntos pegados).
- Q: ¿Puede una hoja llamarse como un marcador reservado? → A: No, se rechaza al crearla o
  renombrarla, indicando el motivo.
- Q: ¿Qué cuenta como "palabra completa"? → A: Letras (con tildes y ñ), dígitos, guion y
  apóstrofo forman parte de la palabra; cualquier otro carácter la corta.
- Q: ¿Cómo se compara un marcador de diálogo con la hoja? → A: El marcador debe ser el nombre
  de la hoja en mayúsculas, con sus tildes; `JOSE DANIEL:` no detecta a "José Daniel".
- Q: ¿Es inconsistente la unicidad sin mayúsculas con la detección sensible a mayúsculas? →
  A: No; la unicidad garantiza que cada marcador apunte a una sola hoja. Las tildes sí
  distinguen nombres.
- Q: ¿La regla del nombre más largo aplica a `PUESTA EN ESCENA:`? → A: Sí, igual que en la
  acción.
- Q: ¿Un nombre todo en mayúsculas dentro de la acción cuenta como mención? → A: No; las
  mayúsculas solo valen como marcador de diálogo.
- Q: ¿Qué formato tienen los ids de línea de diálogo? → A: Posicional por escena,
  `c{clip}-d{n}` (por ejemplo `c1-d2`), derivado del guion.
- Q: ¿Una respuesta con valores fuera del vocabulario o con estructura incorrecta es fallida?
  → A: Sí, se trata como fallida y se reintenta.
- Q: ¿El límite de 60 s es por llamada o por escena? → A: Por llamada; las esperas no cuentan y
  no hay tope adicional por escena (peor caso 220 s).
- Q: ¿Cómo se mide el p95 de SC-006? → A: Solo la duración de la llamada exitosa, sin intentos
  fallidos ni esperas.
- Q: ¿Las categorías de motivo cubren todos los modos de falla? → A: Se agrega "error interno"
  como categoría de cierre.
- Q: ¿La IA recibe contexto además de la escena actual? → A: No, solo la escena actual y las
  hojas efectivas de sus personajes presentes.
- Q: ¿El límite de una generación a la vez es por episodio o global? → A: Global: una sola
  generación en curso en toda la aplicación.
- Q: ¿Cómo se ve el avance de la generación? → A: En vivo, sin recargar: cada cambio de estado
  de una escena se muestra en cuanto ocurre.
- Q: ¿Se puede eliminar una serie o un episodio mientras se generan sus escenas? → A: No, se
  impide indicando el motivo.
- Q: ¿Se puede editar una hoja base mientras una escena que la usa está en "generando"? → A: Sí;
  la IA usa la hoja tal como estaba al iniciar cada llamada y el prompt muestra siempre la
  hoja actual.
- Q: Si se elimina una hoja con variantes en escenas "generadas", ¿qué muestra el prompt? → A:
  El personaje se omite de la sección de hojas y sus diálogos siguen literales.
- Q: ¿Qué pasa si el vocabulario falta o es inválido? → A: La aplicación no arranca y muestra el
  archivo y el problema.
- Q: ¿Qué pasa con tomas guardadas cuyos valores ya no están en el vocabulario? → A: Se
  conservan y el prompt las muestra tal cual, pero la revisión las marca y la edición solo
  ofrece valores vigentes.
- Q: ¿Quién define los textos exactos del prompt? → A: `config/molde-prompt.json` es la fuente
  normativa de la redacción; la spec fija la estructura y tres cadenas de contrato ("Bloque N:",
  "Corte a" y "Sin subtítulos ni texto en pantalla.").
- Q: ¿En qué orden aparecen los personajes en la sección de hojas? → A: Por primera aparición en
  la escena (`PUESTA EN ESCENA:` y luego los clips en orden); empate, alfabético.
- Q: ¿Cómo se inserta una línea de diálogo en el prompt? → A: La línea original del guion, sin
  espacios al inicio y al final, sin rearmarla; lo mismo para el texto en pantalla.
- Q: ¿Qué se muestra en lugar del prompt si la escena no está "generada"? → A: El estado y la
  acción posible ("Reintentar" con el motivo, si está en "error"); sin prompt ni botón de copiar.
- Q: ¿La regla de puntuación del molde pasa a la spec? → A: Sí.
- Q: ¿Qué pasa si copiar al portapapeles falla? → A: Se avisa y el prompt queda seleccionable
  para copiarlo a mano.
- Q: ¿Hay un largo máximo del prompt? → A: No en v1; se registra como supuesto y se muestra la
  cantidad de caracteres.
- Q: ¿En qué condiciones se miden SC-004 a SC-006? → A: Las del PRD y el spike: 100 ejecuciones
  (sin la primera) para SC-004 y SC-005, al menos 10 generaciones para SC-006, en el equipo
  local de desarrollo, que se anota junto al resultado.
- Q: ¿Cuándo se vuelve a medir el spike? → A: Cuando cambia el modelo de IA, la instrucción de
  generación o el vocabulario.
- Q: ¿Los separadores y cierres del bloque se definen en la spec? → A: No, en el molde
  (FR-025a); los opcionales vacíos no dejan separadores sueltos.
- Q: ¿En qué orden se insertan los diálogos de una toma? → A: En el orden del guion, no en el
  que los devuelve la IA.
- Q: ¿Es válida una respuesta que reparte las líneas de un clip fuera de orden entre tomas? →
  A: No, se trata como fallida y se reintenta.
- Q: ¿Cómo se tratan los campos técnicos vacíos? → A: Un ángulo o movimiento vacío equivale a
  omitido; un plano, óptica o iluminación vacío hace fallida la respuesta.
- Q: ¿Una acción con solo espacios cuenta como vacía? → A: Sí.
- Q: ¿Hay máximo de tomas por bloque o de largo de la acción? → A: No en v1.
- Q: ¿Qué pasa si la acción de la IA repite un diálogo o agrega texto en pantalla? → A: Una
  acción con comillas hace fallida la respuesta.
- Q: Si una respuesta incumple varias reglas, ¿qué motivo se muestra? → A: La primera según un
  orden de prioridad fijo.
- Q: ¿Un error de configuración de la IA se reintenta? → A: No; la escena pasa a "error" de
  inmediato y la cola se detiene, dejando las siguientes en "pendiente".
- Q: ¿Qué se muestra durante las esperas entre intentos? → A: La escena sigue en "generando"
  y se muestra el intento en curso ("intento 2 de 3").
- Q: ¿Qué pasa con "Generar" y "Reintentar" durante una generación? → A: Quedan deshabilitados
  con el motivo; un pedido que llegue igual se rechaza. No hay cola de pedidos.
- Q: ¿Reintentar genera solo esa escena? → A: Sí.
- Q: Tras un cierre de la app, ¿la generación se reanuda sola? → A: No; las escenas pendientes
  esperan a que se pulse "Generar".
- Q: ¿El bloqueo aplica entre pestañas? → A: Sí, es único en el servidor y todas las pestañas
  ven la misma generación.
- Q: ¿Cómo se guardan los bloques de una escena? → A: De forma atómica, junto con el paso a
  "generada"; si falla, la escena queda en "error" (error interno) sin bloques sueltos.
- Q: ¿Se puede recargar el guion de un episodio mientras se generan sus escenas? → A: No en ese
  episodio; sí en otros.
- Q: ¿Las variantes de una escena en "error" o "pendiente" se usan al generar? → A: Sí, se
  pueden modificar y la generación usa la hoja efectiva vigente al empezar.
- Q: ¿Qué reglas aplican al editar la acción a mano? → A: No puede quedar vacía; se permiten
  comillas.
- Q: ¿Se pueden editar las referencias de diálogo de una toma? → A: No, quedan como las dejó la
  IA.
- Q: ¿En qué estados se editan las tomas? → A: Solo en "generada"; ningún reintento puede pisar
  ediciones manuales.
- Q: ¿Qué pasa si se renombra una hoja después de cargar un guion? → A: Se permite, con un aviso
  de que los guiones cargados siguen usando el nombre anterior.
- Q: ¿Cómo se guardan los personajes detectados? → A: Como referencia a la hoja, no como copia
  del nombre.
- Q: ¿Se puede crear una variante de un personaje no detectado en la escena? → A: No.
- Q: ¿Qué cuenta como "cambiar al menos un campo"? → A: Un valor distinto del de la hoja base;
  heridas/marcas se puede vaciar a propósito.
- Q: ¿La IA recibe a los personajes sin hoja? → A: Sí, a través de las líneas de diálogo con su
  marcador; no hay lista aparte.
- Q: ¿Qué pasa si faltan o son inválidos el estilo, la instrucción o el molde? → A: La
  aplicación no arranca, igual que con el vocabulario.
- Q: ¿Cuándo se leen los archivos de `config/`? → A: Al iniciar; el prompt no se guarda y se
  arma siempre con el molde vigente, así que un cambio (tras reiniciar) afecta a todos los
  prompts.
- Q: ¿La spec recoge que el schema se genera desde el vocabulario? → A: Sí, en FR-015.
- Q: ¿Qué se dice sobre los datos enviados a la IA? → A: Se documenta como supuesto, sin aviso
  en pantalla.
- Q: ¿Hay registro de las llamadas a la IA? → A: Sí: metadatos por intento y la respuesta cruda
  si es inválida; nunca la clave.
- Q: ¿Cómo se mide SC-002? → A: Con dos escenas de referencia y 10 generaciones de cada una.
- Q: ¿Cómo se cubren con criterios de aceptación los FR agregados en las aclaraciones? → A: Con
  escenarios propios en la spec (AC-S01…).

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

### Escenarios de aceptación de las aclaraciones

Estos escenarios cubren los requisitos agregados en las sesiones de aclaración, que no tienen
AC propio en el PRD. Cada AC-Sxx MUST tener al menos un test automatizado, igual que los AC del
PRD (constitución, principio V).

**Guion y detección (User Story 1)**

- **AC-S01** (FR-008a): **Given** un guion con `AUDIO:` después de `CLIP 1` en la escena 1,
  **When** se valida, **Then** se informan dos errores: uno en la línea de ese `AUDIO:` y otro
  en la línea del encabezado de la escena 1 por faltar `AUDIO:` antes del primer clip.
- **AC-S02** (FR-008a): **Given** un guion sin `AUDIO:` en la escena 1, **When** se valida,
  **Then** el motivo nombra el campo, la escena y la regla ("Falta el campo AUDIO: en la
  escena 1.").
- **AC-S03** (FR-008b): **Given** el guion de ejemplo del Anexo A con fines de línea CRLF y un
  BOM inicial, **When** se procesa, **Then** el resultado es igual campo por campo al del mismo
  guion con LF y sin BOM, y un error en la línea 7 se reporta en la línea 7 en ambos.
- **AC-S04** (FR-008c): **Given** un `LOCACIÓN:` seguido de una línea sin marcador antes del
  primer `CLIP`, **When** se valida, **Then** se informa "línea que no es un campo de escena" en
  esa segunda línea.
- **AC-S05** (FR-008d): **Given** un guion de 100.001 caracteres, **When** se valida, **Then** se
  informa un único error, en la línea 1, y no se informan otros.
- **AC-S06** (FR-008e): **Given** una escena con `Locacion:` en lugar de `LOCACIÓN:`, **When** se
  valida, **Then** se informan la línea que no es un campo y la falta de `LOCACIÓN:`.
- **AC-S07** (FR-008f, FR-026a): **Given** el diálogo `ANA: “Dijo "no" y se fue.”`, **When** se
  carga y se arma el prompt, **Then** el guion es válido y el prompt contiene esa línea idéntica,
  con sus comillas tipográficas e internas.
- **AC-S08** (FR-002a): **Given** una serie con la hoja "Ana", **When** se intenta crear o
  renombrar otra hoja a "ANA", **Then** no se guarda y se indica que el nombre ya existe; en otra
  serie, "Ana" se puede crear.
- **AC-S09** (FR-002b): **Given** una serie, **When** se intenta crear una hoja llamada "Audio",
  **Then** no se guarda y se indica que es un nombre reservado.
- **AC-S10** (FR-011a): **Given** las hojas "José" y "José Daniel" y una acción que dice solo
  "José Daniel sonríe.", **When** se procesa, **Then** se detecta a José Daniel y no a José; si
  la acción dice además "José entra.", se detectan los dos. Lo mismo vale en
  `PUESTA EN ESCENA:`.
- **AC-S11** (FR-011b): **Given** la hoja "Ana", **When** la acción dice "¡Ana!" o "Ana,",
  **Then** se detecta a Ana; **When** dice "Ana-María" o "Ana's", **Then** no se detecta.
- **AC-S12** (FR-011c, FR-011d): **Given** la hoja "José Daniel", **When** un clip tiene
  `JOSE DANIEL: "Hola."` o la acción dice "JOSÉ DANIEL entra.", **Then** José Daniel no se
  detecta y el diálogo se acepta como de un personaje sin hoja.

**Generación (User Story 2)**

- **AC-S13** (FR-014a): **Given** un episodio con escenas 1 a 3 en "pendiente", **When** se
  pulsa "Generar", **Then** la escena 2 empieza solo después de que la 1 queda en "generada" o
  "error", y la 3 sigue en "pendiente" mientras tanto.
- **AC-S14** (FR-014a): **Given** una generación en curso en el episodio 1, **When** se pide
  "Generar" en el episodio 2 o "Reintentar" en cualquier escena, desde esta u otra pestaña,
  **Then** se rechaza con "Hay una generación en curso" y no se llama a la IA.
- **AC-S15** (FR-004a): **Given** una generación en curso en un episodio, **When** se intenta
  eliminar ese episodio o su serie, o cargar un guion nuevo en él, **Then** no se permite y se
  indica el motivo; en otro episodio sí se puede cargar un guion.
- **AC-S16** (FR-016a, FR-016b): **Given** la escena de ejemplo del Anexo A, **When** se arma la
  instrucción, **Then** las líneas del clip 1 tienen los ids `c1-d1` y `c1-d2`, y la instrucción
  no contiene texto de otras escenas.
- **AC-S17** (FR-017): **Given** una respuesta que no es JSON válido, o con un plano fuera del
  vocabulario, **When** se valida, **Then** se trata como fallida y se reintenta.
- **AC-S18** (FR-017a): **Given** un clip con las líneas `c1-d1` y `c1-d2`, **When** la
  respuesta pone `c1-d2` en la toma 1 y `c1-d1` en la toma 2, **Then** se trata como fallida.
- **AC-S19** (FR-017a): **Given** una toma sin ángulo y con movimiento `""`, **When** se valida,
  **Then** es válida y el prompt omite los dos; **Given** una toma con óptica `""` o con una
  acción de solo espacios, **Then** es fallida.
- **AC-S20** (FR-017a): **Given** una toma cuya acción contiene comillas, **When** se valida,
  **Then** se trata como fallida.
- **AC-S21** (FR-017b): **Given** una respuesta con un bloque de menos y una acción vacía,
  **When** la escena queda en "error", **Then** el motivo informa solo el bloque faltante.
- **AC-S22** (FR-018): **Given** una IA que tarda más de 60 s en cada llamada, **When** se genera
  una escena, **Then** se hacen 3 llamadas con esperas de 10 s y 30 s; durante todo ese tiempo la
  escena está en "generando" con el intento en curso, y termina en "error" con el motivo "la IA
  no respondió a tiempo".
- **AC-S23** (FR-018a): **Given** que falta la clave de la IA y hay 3 escenas pendientes,
  **When** se pulsa "Generar", **Then** se hace una sola llamada, la escena 1 queda en "error"
  con motivo de configuración y las escenas 2 y 3 siguen en "pendiente".
- **AC-S24** (FR-019, FR-019a): **Given** una respuesta válida y una falla al guardar los
  bloques, **When** termina la escena, **Then** no queda ningún bloque guardado y la escena está
  en "error" con el motivo "error interno".
- **AC-S25** (FR-019a): **Given** una escena en "error" con motivo, **When** se reintenta con
  éxito, **Then** el motivo se borra; ningún motivo contiene la clave de la IA.
- **AC-S26** (FR-019b): **Given** una vista de episodio abierta, **When** una escena pasa de
  "pendiente" a "generando" y a "generada", **Then** la vista muestra cada estado sin recargar.
- **AC-S27** (FR-019c): **Given** un intento con respuesta inválida, **When** termina, **Then** el
  log tiene la escena, el intento, la duración, el motivo y la respuesta cruda, y no contiene la
  clave.
- **AC-S28** (FR-020, FR-021): **Given** escenas 1 en "error" y 2 en "pendiente", **When** se
  reintenta la 1, **Then** solo se genera la 1. **Given** un cierre de la app con la escena 1 en
  "generando" y la 2 en "pendiente", **When** se inicia, **Then** la 1 queda en "error" con el
  motivo de cierre, la 2 sigue en "pendiente" y no se llama a la IA.
- **AC-S29** (FR-015a, FR-015c): **Given** un `config/vocabulario.json` con JSON inválido, o un
  `config/molde-prompt.json` sin "Corte a", **When** se inicia la aplicación, **Then** no
  arranca e informa el archivo y el problema.
- **AC-S30** (FR-015b): **Given** una toma guardada con un plano que ya no está en el
  vocabulario, **When** se arma el prompt y se revisa la toma, **Then** el prompt muestra el valor
  tal cual, la toma se marca "valor fuera del vocabulario" y la edición ofrece solo valores
  vigentes.

**Prompt, edición y variantes (User Stories 3, 5 y 6)**

- **AC-S31** (FR-025a, FR-015d): **Given** una escena generada y un cambio en el título de una
  sección del molde, **When** se reinicia y se arma el prompt, **Then** el prompt usa el título
  nuevo y conserva "Bloque N:", "Corte a" y "Sin subtítulos ni texto en pantalla.".
- **AC-S32** (FR-025b): **Given** una escena cuya `PUESTA EN ESCENA:` nombra a José Daniel y
  cuyo clip 1 nombra primero a Ana, **When** se arma el prompt, **Then** la hoja de José Daniel
  aparece antes que la de Ana.
- **AC-S33** (FR-025c): **Given** escenas en "pendiente", "generando" y "error", **When** se abre
  el episodio, **Then** ninguna muestra prompt ni botón de copiar, y la de "error" muestra el
  motivo y "Reintentar".
- **AC-S34** (FR-026): **Given** una hoja con peinado "Cola de caballo.", **When** se arma el
  prompt, **Then** el texto dice "Peinado: Cola de caballo." sin un segundo punto.
- **AC-S35** (FR-026a): **Given** una toma que referencia `c1-d2` y `c1-d1` en ese orden, en un
  bloque de una sola toma, **When** se arma el prompt, **Then** la línea de ANA aparece antes que
  la de JOSÉ DANIEL.
- **AC-S36** (FR-028a): **Given** un portapapeles no disponible, **When** se pulsa copiar,
  **Then** se muestra el aviso y el prompt queda seleccionable en un área de solo lectura.
- **AC-S37** (FR-023, FR-023a): **Given** una toma de una escena generada, **When** se intenta
  dejar su acción vacía, **Then** no se guarda y conserva su valor; las referencias de diálogo
  no se ofrecen para editar.
- **AC-S38** (FR-003a, FR-003b): **Given** una escena generada con Ana, **When** se edita su
  vestuario, **Then** el prompt muestra el vestuario nuevo; **When** se elimina su hoja,
  **Then** el prompt ya no tiene su hoja y el diálogo `ANA:` sigue literal; **When** se renombra,
  **Then** se muestra el aviso de guiones cargados.
- **AC-S39** (FR-029a, FR-031): **Given** una escena en "error" donde Ana está presente y José
  Daniel no, **When** se crea una variante para José Daniel, **Then** se rechaza; **When** se
  crea para Ana con un vestuario igual al de su hoja, **Then** se rechaza; **When** se crea con
  heridas/marcas vacío sobre una base que tiene heridas, **Then** se guarda y el reintento usa la
  hoja efectiva.
- **AC-S40** (FR-017a, RN-03): **Given** la hoja de Ana en la escena, **When** la acción de una
  toma trae la hoja renderizada ("Ana, 32 años. Mujer de piel morena… Rol: Protagonista espera
  bajo el toldo"), un fragmento de 3 o más palabras de un campo ("mujer de piel morena") o una
  etiqueta de campo del molde ("Rol:"), **Then** la respuesta es fallida y se reintenta; **When**
  menciona un rasgo suelto ("se ajusta la chaqueta negra"), **Then** es válida.

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
- Se pulsa "Generar" o "Reintentar" mientras hay una generación en curso, en ese u otro
  episodio: no se inicia una segunda generación en paralelo (FR-014a).
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
  existe. Series distintas pueden tener hojas con el mismo nombre. Las tildes sí distinguen
  nombres ("Jose" y "José" son hojas distintas). La comparación sin mayúsculas garantiza que
  cada marcador de diálogo en mayúsculas corresponda a una sola hoja; no contradice la
  detección en la acción, que exige la escritura exacta (FR-011).
- **FR-002b**: Crear o renombrar una hoja MUST rechazarse, indicando el motivo, si su nombre
  en mayúsculas coincide con un marcador reservado del guion: `ESCENA`, `CLIP`, `LOCACIÓN`,
  `ILUMINACIÓN`, `PUESTA EN ESCENA`, `AUDIO` o `TEXTO EN PANTALLA`.
- **FR-003**: Eliminar una hoja de personaje MUST eliminar también sus variantes, incluidas las
  de escenas en "generando" o "generada" (RF-01h).
- **FR-003a**: Editar o eliminar una hoja MUST permitirse aunque haya una generación en curso
  que la use. Cada llamada a la IA usa la hoja efectiva tal como estaba al iniciar esa llamada;
  el prompt final se arma siempre con las hojas actuales. Si se elimina una hoja, sus escenas
  dejan de tenerla entre los personajes presentes: el prompt la omite de la sección de hojas
  (que se omite entera si queda vacía) y sus diálogos siguen insertándose literales, como los
  de un personaje sin hoja (FR-012).
- **FR-003b**: Los personajes detectados de cada clip y escena MUST guardarse como referencia a
  su hoja, no como copia del nombre: el prompt refleja las ediciones y la eliminación de la hoja,
  pero la presencia no se recalcula sin recargar el guion. Renombrar una hoja que figura entre
  los personajes detectados de algún guion cargado MUST permitirse, avisando: "Los guiones
  cargados siguen usando el nombre anterior; recargalos para que coincidan." Los diálogos se
  siguen insertando literales con su marcador original (riesgo aceptado, como editar una hoja
  usada en escenas generadas).
- **FR-004**: El sistema MUST permitir crear episodios con número y título, con número único
  dentro de la serie; listarlos; editar solo su título; y eliminarlos junto con su guion,
  escenas, clips, bloques y variantes (RF-11a–d).
- **FR-004a**: El sistema MUST impedir eliminar una serie o un episodio mientras alguna de sus
  escenas está en "generando" o esperando su turno en la generación en curso (FR-014a),
  indicando el motivo. Con la misma condición, MUST impedir cargar un guion nuevo en ese
  episodio; en los demás episodios la carga sigue disponible.
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
- **FR-008a**: La validación MUST informar todos los errores del guion en una sola pasada; un
  error no suprime a otros, aunque sean consecuencia de él (por ejemplo, un `AUDIO:` después de
  `CLIP 1` se informa en su línea y, además, el `AUDIO:` faltante en el encabezado de la
  escena). Cada motivo MUST ser una frase en español que nombra el elemento afectado, la regla
  incumplida y la forma esperada (por ejemplo: "Falta el campo AUDIO: en la escena 1." o
  "El diálogo de ANA debe ir entre comillas: ANA: \"texto\".").
- **FR-008b**: Para leer el guion, el sistema MUST aceptar LF, CRLF y CR como fin de línea,
  ignorar un BOM inicial y recortar espacios y tabulaciones al inicio y al final de cada línea
  antes de reconocer marcadores. El número de línea reportado cuenta líneas físicas, incluidas
  las líneas en blanco. Esto no modifica el guion guardado, que se conserva tal como se pegó.
- **FR-008c**: Cada campo de escena (`LOCACIÓN:`, `ILUMINACIÓN:`, `PUESTA EN ESCENA:`,
  `AUDIO:`) MUST ocupar una sola línea. Una línea sin marcador entre el encabezado y el primer
  `CLIP` es el error "línea que no es un campo de escena" del Anexo A.
- **FR-008d**: Un guion de más de 100.000 caracteres MUST rechazarse con un único error en la
  línea 1 ("El guion supera los 100.000 caracteres."), sin seguir validando; es la única
  excepción a FR-008a.
- **FR-008e**: Los marcadores MUST reconocerse solo en su forma exacta del Anexo A:
  mayúsculas, con tildes y con los dos puntos pegados al marcador ("Locacion:" o
  "LOCACIÓN :" no son marcadores). En el encabezado de escena siguen valiendo la raya o el
  guion común como separadores.
- **FR-008f**: El texto de un diálogo o de un `TEXTO EN PANTALLA` MUST ir entre una comilla de
  apertura (`"` o `“`) inmediatamente después de los dos puntos (con espacios opcionales) y una
  comilla de cierre (`"` o `”`) al final de la línea. Se permiten comillas dentro del texto, y
  la línea se inserta en el prompt tal como está escrita, sin cambiar el tipo de comillas.
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
  `JOSÉ DANIEL:` cuenta solo para José Daniel. Esta regla aplica igual en la acción y en
  `PUESTA EN ESCENA:`.
- **FR-011b**: Un nombre es "palabra completa" cuando el carácter anterior y el siguiente no
  son letras (incluidas las acentuadas y la ñ), dígitos, guion (`-`) ni apóstrofo (`'` o `’`);
  el inicio y el fin de línea también cortan. Por ejemplo, "Ana," y "¡Ana!" detectan a Ana;
  "Ana-María" y "Ana's" no.
- **FR-011c**: Un marcador de diálogo detecta a un personaje solo si el texto antes de los dos
  puntos (o de la acotación) es exactamente el nombre de su hoja en mayúsculas, con sus tildes.
  `JOSE DANIEL:` no detecta a la hoja "José Daniel": se acepta como diálogo de un personaje sin
  hoja (FR-012).
- **FR-011d**: En la acción y en `PUESTA EN ESCENA:`, un nombre escrito todo en mayúsculas
  ("ANA entra") MUST NOT contar como mención; las mayúsculas solo detectan a un personaje como
  marcador de diálogo (FR-011c).
- **FR-012**: Los personajes sin hoja MUST poder tener diálogo, que se acepta y se inserta
  literal, pero no se detectan como personajes (RN-07).

**Generación**

- **FR-013**: Las escenas de un guion recién cargado MUST quedar en "pendiente" sin iniciar la
  generación (RF-06d).
- **FR-014**: El sistema MUST ofrecer un botón "Generar" que genera todas las escenas en
  "pendiente" de un episodio, y solo esas (RF-06c).
- **FR-014a**: Las escenas MUST generarse de a una, en orden ascendente de número de escena: la
  siguiente escena empieza solo cuando la anterior quedó en "generada" o "error". Las escenas
  que esperan su turno siguen en "pendiente". La aplicación MUST tener como máximo una
  generación en curso en total: mientras haya una, pulsar "Generar" o reintentar una escena de
  ese u otro episodio MUST NOT iniciar otra generación en paralelo. Mientras tanto, los botones
  "Generar" y "Reintentar" de todos los episodios MUST mostrarse deshabilitados con el motivo
  ("Hay una generación en curso"), y un pedido que llegue igual MUST rechazarse con ese
  mensaje; no se encola. El bloqueo es único en el servidor: rige para todas las pestañas y
  ventanas abiertas, que ven la misma generación en curso.
- **FR-015**: Para cada escena, el sistema MUST pedir a la IA un bloque por clip, cada uno con
  una o más tomas, cuyos campos técnicos solo pueden tomar valores del vocabulario cerrado del
  Anexo E (RF-06a, RN-02). El único texto libre de la IA es la acción de cada toma. El schema
  de salida estructurada MUST generarse desde `config/vocabulario.json`, con plano, óptica,
  iluminación, ángulo y movimiento como enums; ningún valor del vocabulario se escribe en el
  código. En cada llamada, el schema MUST además acotar la cantidad de bloques y el número de
  clip a los clips de la escena, y restringir los diálogos a un enum con los identificadores de
  sus líneas (FR-016a).
- **FR-015a**: Al iniciar, el sistema MUST validar `config/vocabulario.json` (que exista, que
  sea JSON válido, que las categorías obligatorias tengan al menos un valor y que no haya
  valores repetidos dentro de una categoría). Si no es válido, la aplicación MUST NOT arrancar y
  MUST informar el archivo y el problema.
- **FR-015b**: Si el vocabulario cambia y una toma guardada tiene un valor que ya no existe, el
  valor MUST conservarse y el prompt lo muestra tal cual. La revisión MUST marcar esa toma como
  "valor fuera del vocabulario", y al editarla solo se ofrecen valores vigentes.
- **FR-015c**: Al iniciar, el sistema MUST validar también los demás archivos de `config/`:
  `estilo.txt` existe y no está vacío; `instruccion-generacion.txt` existe y contiene los
  marcadores donde se insertan la escena y las hojas efectivas; `molde-prompt.json` es JSON
  válido, tiene las seis secciones de FR-025 y conserva las cadenas de contrato de FR-025a. Si
  alguno falla, la aplicación MUST NOT arrancar y MUST informar el archivo y el problema.
- **FR-015d**: Los archivos de `config/` MUST leerse una sola vez, al iniciar; un cambio exige
  reiniciar la aplicación. El prompt final MUST NOT guardarse: se arma cada vez a partir de los
  datos guardados y del molde y el estilo vigentes, así que tras reiniciar un cambio se ve en
  todos los prompts, incluidos los de escenas ya generadas.
- **FR-016**: La IA MUST recibir como contexto la escena completa (campos, clips e identificadores
  de cada línea de diálogo) y las hojas efectivas de los personajes presentes; MUST referenciar
  las líneas de diálogo por identificador y no las reescribe (RN-03, RN-04, RN-05).
- **FR-016a**: El identificador de cada línea de diálogo MUST ser `c{clip}-d{n}`, donde `{clip}`
  es el número de clip y `{n}` la posición de la línea dentro del clip, empezando en 1 (por
  ejemplo `c1-d2`). Es único dentro de la escena y se deriva del guion, por lo que el mismo
  guion produce siempre los mismos identificadores.
- **FR-016b**: La IA MUST recibir solo la escena que se genera y las hojas efectivas de sus
  personajes presentes; no recibe otras escenas del episodio ni de otros episodios. La
  continuidad entre escenas la garantizan las plantillas (FR-026), no el contexto de la IA.
  Los personajes sin hoja llegan a la IA solo a través de sus líneas de diálogo (con marcador e
  identificador); no se envía una lista aparte, y la IA los describe genéricamente (RN-07).
- **FR-017**: El sistema MUST tratar como fallida una respuesta que no cumple RN-08 (un bloque
  por clip, cada número de clip una vez), RN-09 (cada línea de diálogo del clip referenciada
  exactamente una vez, sin ids ajenos o inexistentes) o RN-10 (al menos una toma por bloque,
  acción no vacía) (RF-06b). También MUST tratar como fallida una respuesta que no es JSON
  válido, que no respeta la estructura pedida (campos faltantes, sobrantes o de otro tipo) o
  que trae en plano, óptica, iluminación, ángulo o movimiento un valor fuera del vocabulario,
  aunque el schema de salida debería impedirlo.
- **FR-017a**: Además, al validar una respuesta el sistema MUST aplicar estas reglas:
  - Recorriendo las tomas de un bloque en orden, las líneas de diálogo referenciadas MUST
    respetar el orden del guion; si una toma referencia una línea anterior a otra ya
    referenciada en una toma previa, la respuesta es fallida (motivo, por ejemplo: "diálogos
    fuera de orden en el clip 2").
  - Un ángulo o movimiento ausente, nulo o con cadena vacía equivale a "sin valor"; un plano,
    una óptica o una iluminación ausente, nula o vacía hace fallida la respuesta (RN-01).
  - Una acción que, sin espacios ni saltos de línea al inicio y al final, queda vacía cuenta
    como vacía (RN-10). La acción válida se guarda recortada.
  - No hay máximo de tomas por bloque ni de largo de la acción: la IA decide cuántas tomas
    tiene cada bloque (Anexo D).
  - Una acción que contiene comillas (`"`, `“` o `”`) hace fallida la respuesta: el diálogo y
    el texto en pantalla solo los inserta el sistema (RN-04).
  - Una acción que copia la hoja de un personaje presente hace fallida la respuesta: la hoja la
    inserta el sistema (RN-03). Cuenta como copia una etiqueta de campo de la plantilla de
    personaje del molde (por ejemplo `Rol:`) o un fragmento de un campo de la hoja efectiva
    (todos menos el nombre; cortado en puntos, comas y puntos y coma) con 3 o más palabras,
    sin distinguir mayúsculas. La instrucción sola no alcanzó: en la demo la acción repetía la
    hoja completa.
- **FR-017b**: Si una respuesta incumple varias reglas, el motivo (FR-019a) MUST informar solo
  la primera según este orden: estructura o vocabulario (FR-017), RN-08, RN-10, RN-09, orden de
  los diálogos, comillas en la acción y copia de la hoja en la acción (FR-017a).
- **FR-018**: Ante error, falta de respuesta en 60 s o respuesta inválida, el sistema MUST
  reintentar hasta 2 veces (3 intentos en total), esperando 10 s antes del primer reintento y
  30 s antes del segundo (RNF-06, RNF-07). El límite de 60 s aplica a cada llamada; las esperas
  entre intentos no cuentan dentro de él y no hay otro tope por escena, de modo que una escena
  tarda como máximo 3 × 60 s + 10 s + 30 s = 220 s antes de quedar en "error". Durante los
  intentos y las esperas la escena sigue en "generando", y la vista muestra el intento en curso
  (por ejemplo "intento 2 de 3").
- **FR-018a**: Hay dos excepciones a los reintentos de FR-018:
  - Un error de configuración de la IA (falta la clave, clave rechazada, modelo inexistente)
    MUST NOT reintentarse: la escena pasa a "error" de inmediato con ese motivo, y la
    generación en curso se detiene, dejando en "pendiente" las escenas que esperaban su turno.
    Es la única excepción al avance de la cola de FR-014a.
  - Un error interno (cualquier falla que no sea de la IA, por ejemplo al guardar los bloques)
    MUST NOT reintentarse: la escena pasa a "error" con el motivo "error interno" y la cola
    sigue con la escena siguiente.
- **FR-019**: El sistema MUST mantener por escena un estado: "pendiente", "generando",
  "generada" o "error"; MUST guardar los bloques de una escena en cuanto se generan, de forma
  que el fallo de otra escena no los borre; y MUST marcar "error" cuando se agotan los intentos
  (RF-10a, RF-10b, RF-10d). Los bloques de una escena MUST guardarse de forma atómica, en la
  misma operación que la pasa a "generada": si el guardado falla, no queda ningún bloque de esa
  escena y pasa a "error" con el motivo "error interno" (FR-019a).
- **FR-019a**: Cuando una escena queda en "error", el sistema MUST guardar y mostrar junto a su
  estado un motivo corto, en lenguaje simple, del último intento fallido, que distinga al menos:
  la IA no respondió a tiempo, la IA devolvió un error o no está disponible, la respuesta no
  cumplió las reglas (indicando cuál, por ejemplo "falta el bloque del clip 3"), error de
  configuración de la IA, generación interrumpida por un cierre de la aplicación (FR-021), y
  error interno para cualquier otra falla (por ejemplo, al guardar los bloques). El
  motivo MUST NOT incluir datos sensibles como la clave de la IA, y se borra cuando la escena
  sale de "error".
- **FR-019b**: Mientras hay una generación en curso, la vista del episodio MUST mostrar cada
  cambio de estado de sus escenas en cuanto ocurre, sin que el/la productor/a recargue la
  página; los bloques y el prompt de una escena aparecen cuando queda "generada".
- **FR-019c**: El sistema MUST registrar en un log local cada intento de generación: escena,
  número de intento, duración, resultado y motivo (FR-019a). Si la respuesta es inválida, MUST
  registrar además la respuesta cruda. El log MUST NOT incluir la clave de la IA ni las
  cabeceras de autenticación.
- **FR-020**: El sistema MUST permitir reintentar la generación de una escena en "error"
  (RF-10c). Reintentar genera solo esa escena, con los intentos de FR-018; las escenas en
  "pendiente" siguen esperando a "Generar".
- **FR-021**: Al iniciar la aplicación, toda escena en "generando" MUST pasar a "error" (RF-10e).
  La generación interrumpida MUST NOT reanudarse sola: las escenas que quedaron en "pendiente"
  siguen así hasta que se pulse "Generar".

**Revisión y edición**

- **FR-022**: El sistema MUST mostrar los bloques generados de cada escena con todas sus tomas
  (RF-07a).
- **FR-023**: El sistema MUST permitir editar los campos técnicos de cada toma, aceptando solo
  valores del vocabulario y permitiendo vaciar ángulo y movimiento, y editar su texto de acción
  (RF-07b, RF-07c). La acción editada MUST NOT quedar vacía ni solo con espacios: se rechaza
  indicando el motivo y la toma conserva su valor anterior. Se guarda sin espacios al inicio ni
  al final y puede contener comillas (la regla de comillas de FR-017a aplica solo a la IA).
- **FR-023a**: Las tomas solo se pueden editar en escenas "generadas", que son las únicas con
  bloques (FR-019); como una escena generada no se regenera, ningún reintento puede pisar una
  edición manual. Las referencias de diálogo de una toma MUST NOT editarse: quedan como las
  devolvió la IA.
- **FR-024**: Cada edición MUST guardarse y el texto del bloque MUST volver a armarse
  automáticamente (RF-07d, RF-07e).

**Prompt final**

- **FR-025**: El sistema MUST armar, por plantilla determinística (Anexo D), un prompt final por
  escena con seis secciones en orden fijo: estilo global, locación e iluminación base, hojas
  efectivas de los personajes presentes (se omite si no hay ninguno con hoja), puesta en
  escena, audio y un bloque numerado por clip en el orden de los clips (RF-08a).
- **FR-025a**: La redacción del prompt (títulos de sección, etiquetas y orden de los campos de la
  hoja, plantilla de toma, separadores) MUST salir de `config/molde-prompt.json`, que es su
  fuente normativa; cambiarla no requiere enmendar esta spec. Tres cadenas son de contrato y el
  molde MUST conservarlas: "Bloque N:" como título de cada bloque, "Corte a" entre tomas y
  "Sin subtítulos ni texto en pantalla." como cierre de un clip sin texto en pantalla. Un
  opcional vacío del molde se omite junto con sus separadores, sin dejar comas ni espacios
  sueltos (por ejemplo, una toma sin movimiento ni ángulo queda
  "plano medio, óptica 50mm. …"). SC-001 se
  verifica comparando, para una hoja dada, el texto que produce el molde en cada prompt.
- **FR-025b**: Los personajes de la sección de hojas MUST aparecer en el orden de su primera
  aparición en la escena, leyendo primero `PUESTA EN ESCENA:` y después los clips en orden. Si
  dos empiezan en la misma posición, se ordenan alfabéticamente por nombre de hoja.
- **FR-025c**: Para una escena que no está en "generada", el sistema MUST NOT armar ni mostrar
  un prompt ni ofrecer el botón de copiar. Muestra su estado y, si está en "error", el motivo
  (FR-019a) y el botón "Reintentar".
- **FR-026**: Las hojas de personaje, el diálogo (con marcador y acotación) y el texto en
  pantalla MUST insertarse literales; ningún campo obligatorio del prompt puede quedar vacío y
  los campos opcionales vacíos se omiten (RN-01, RN-03, RN-04). Cuando la plantilla pone un
  "." justo después de un valor que ya termina en `.`, `!`, `?` o `…`, ese punto MUST omitirse;
  el valor se inserta igual.
- **FR-026a**: Cada línea de diálogo MUST insertarse como la línea original del guion (sin los
  espacios y tabulaciones del inicio y del final), con su marcador, su acotación y sus comillas
  tal como se escribieron, sin rearmarla a partir de sus partes. El texto en pantalla MUST
  insertarse igual: el texto con sus comillas originales. Las líneas que referencia una toma
  MUST insertarse en el orden en que aparecen en el clip, sin importar el orden en que la IA
  las lista.
- **FR-027**: Cada bloque MUST unir sus tomas con "Corte a" y cerrar con el texto en pantalla del
  clip, si existe, o con "Sin subtítulos ni texto en pantalla." (Anexo D).
- **FR-028**: El sistema MUST permitir copiar el prompt final completo de una escena con un solo
  botón (RF-08b).
- **FR-028a**: Al copiar con éxito, el sistema MUST confirmarlo ("Copiado"). Si el portapapeles
  falla o no está disponible, MUST avisar ("No se pudo copiar; seleccioná el texto y copialo a
  mano") y el prompt MUST quedar en un área de texto de solo lectura que se puede seleccionar
  completa. Junto al botón se muestra la cantidad de caracteres del prompt.

**Variantes de personaje**

- **FR-029**: El sistema MUST permitir crear, listar, editar y eliminar, por escena, una variante
  de un personaje que reemplace edad, peinado, vestuario o heridas/marcas; cada variante cambia
  al menos un campo y hay como máximo una por personaje y escena (RF-09a–d).
- **FR-029a**: Solo se MUST permitir crear variantes de personajes detectados en la escena
  (FR-011); cualquier otra se rechaza indicando el motivo. Un campo de la variante cuenta como
  cambiado solo si su valor, sin espacios al inicio ni al final, es distinto del de la hoja
  base. Edad, peinado y vestuario no pueden quedar vacíos en la variante; heridas/marcas sí se
  puede vaciar a propósito, y eso cuenta como cambio si la hoja base tiene un valor.
- **FR-030**: Eliminar una variante MUST dejar intacta la hoja base (RF-09d).
- **FR-031**: El sistema MUST impedir crear, editar o eliminar variantes de una escena en
  "generando" o "generada", indicando el motivo (RF-09e). La única excepción es eliminar la
  hoja base, que borra también sus variantes (FR-003, FR-003a). Las variantes de una escena en
  "pendiente" (aunque espere su turno en la cola) o en "error" se pueden modificar; su
  generación o reintento usa la hoja efectiva vigente al empezar cada llamada.
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
  (definición de éxito de v1, objetivo 2). Se mide con dos escenas de referencia (la del
  Anexo A y la de 20 clips del spike), 10 generaciones de cada una: se cumple si el 100% termina
  en "generada" dentro de los 3 intentos, con un bloque por clip y sin incumplir RN-01 a RN-10.
  La calidad estética de las tomas es juicio del/la productor/a y queda fuera de la medición.
- **SC-003**: El 100% de los campos técnicos de las tomas pertenece al vocabulario cerrado vigente
  al momento de generarlas o editarlas (FR-015b), y el 100% de los diálogos y textos en
  pantalla aparece en el prompt idéntico al guion.
- **SC-004**: Validar y dividir un guion de tamaño máximo (10 escenas × 20 clips) tarda menos de
  1 segundo en el 95% de los casos (RNF-03).
- **SC-005**: Armar el prompt final de una escena tarda menos de 1 segundo en el 95% de los casos
  (RNF-04).
- **SC-006**: Generar una escena de 20 clips tarda menos de 30 segundos en el 95% de los casos,
  sin contar reintentos (RNF-05): se mide solo la duración de la llamada exitosa, sin los
  intentos fallidos ni las esperas entre intentos.
- **SC-007**: Un guion inválido se rechaza siempre completo, y el 100% de sus errores se informa
  con número de línea y motivo, de modo que el/la guionista puede corregirlo sin preguntar.
- **SC-008**: El recorrido crear serie → crear personaje → crear episodio → pegar guion →
  "Generar" → ver bloques y prompt final se completa en menos de 2 minutos con una escena de 4
  clips (criterio de demo).
- **SC-009**: Una falla de generación en una escena nunca hace perder los bloques ya generados de
  otras escenas.

**Condiciones de medición (SC-004 a SC-006):** se miden en el equipo local de desarrollo, que
se anota (CPU, RAM, sistema operativo) junto a cada resultado. SC-004 y SC-005 se calculan como
p95 de 100 ejecuciones, descartando la primera (arranque en frío). SC-006 se calcula como p95 de
al menos 10 generaciones de una escena de 20 clips, con el modelo configurado y la instrucción
y el vocabulario vigentes, como en el spike del 2026-10-04.

## Assumptions

- La v1 es monousuario y corre en el equipo local: sin login, roles ni separación de datos entre
  usuarios; los tiempos se miden en ese equipo.
- Al servicio externo de IA se envían, por cada escena que se genera, solo sus campos, clips y
  líneas de diálogo y las hojas efectivas de sus personajes presentes (FR-016b). El/la
  productor/a configura la clave y acepta las condiciones del servicio; la v1 no muestra un
  aviso en pantalla.
- Se usa un servicio externo de IA de texto con salida estructurada que admite listas cerradas de
  valores; el spike del 2026-10-04 confirmó que cumple SC-006 (p95 de 22,2 s). La medición MUST
  repetirse cuando cambia el modelo de IA, `config/instruccion-generacion.txt` o el vocabulario
  (que cambia el schema de salida). Excepción: el cambio del 2026-10-17 a la instrucción (no
  usar comillas en la acción y repartir los diálogos en el orden del clip, FR-017a) no amplía el
  contexto ni cambia el formato de la respuesta, así que el spike del 2026-10-04 sigue vigente.
- El vocabulario (`config/vocabulario.json`), el estilo global (`config/estilo.txt`), la
  instrucción de generación (`config/instruccion-generacion.txt`) y el molde del prompt
  (`config/molde-prompt.json`) ya existen como archivos versionados y se editan sin pantalla de
  administración. La redacción del molde es un borrador que se ajustará comparándolo con el
  prompt manual actual, respetando las cadenas de contrato de FR-025a.
- La IA de video acepta el prompt completo de una escena de hasta 20 clips. El límite de largo
  de Google Flow no está verificado; en v1 el prompt no se limita ni se recorta, y se muestra su
  cantidad de caracteres (FR-028a).
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
