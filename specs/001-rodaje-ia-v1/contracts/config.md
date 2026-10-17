# Contrato: archivos de `config/`

**Fuente**: spec FR-015a, FR-015c, FR-015d; constitución II y VI. Se leen y validan una sola
vez, al iniciar. Si algún archivo falla, la aplicación no arranca: escribe en la consola
`Configuración inválida: {archivo}: {problema}` y termina con código distinto de 0 (research
R-12). Si fallan varios, se informan todos.

## `config/vocabulario.json`

| Regla | Problema informado (ejemplo) |
| --- | --- |
| Existe y es JSON válido | "no existe" · "no es JSON válido (línea 12)" |
| Tiene `categorias` con exactamente `plano`, `optica`, `iluminacion`, `angulo`, `movimiento` | "falta la categoría optica" · "categoría desconocida camara" |
| Cada categoría tiene `obligatorio` (bool) y `valores` (lista de textos) | "la categoría angulo no tiene 'valores'" |
| `plano`, `optica` e `iluminacion` son obligatorias; `angulo` y `movimiento`, no | "plano debe ser obligatoria" |
| Cada categoría tiene al menos un valor; ningún valor está vacío ni repetido | "valor repetido 'paneo' en movimiento" |

Los campos `version` y `descripcion` son informativos.

## `config/estilo.txt`

| Regla | Problema |
| --- | --- |
| Existe | "no existe" |
| Recortado, no está vacío | "está vacío" |

## `config/instruccion-generacion.txt`

| Regla | Problema |
| --- | --- |
| Existe y no está vacío | "no existe" · "está vacío" |
| Contiene `{{escena}}` exactamente una vez | "falta el marcador {{escena}}" |
| Contiene `{{personajes}}` exactamente una vez | "falta el marcador {{personajes}}" |
| No contiene otros marcadores `{{…}}` | "marcador desconocido {{xyz}}" |

## `config/molde-prompt.json`

| Regla | Problema |
| --- | --- |
| Existe y es JSON válido | "no existe" · "no es JSON válido" |
| `secciones` tiene, en orden, `estilo`, `locacionEIluminacionBase`, `personajes`, `puestaEnEscena`, `audio`, `bloques` | "falta la sección audio" · "sección fuera de orden" |
| Solo usa variables del catálogo de [prompt.md](prompt.md) | "variable desconocida {{toma.camara}}" |
| `tituloBloque` contiene `Bloque {{bloque.numero}}:` | "falta la cadena de contrato 'Bloque N:'" |
| `separadorTomas` contiene `Corte a` | "falta la cadena de contrato 'Corte a'" |
| `cierreSinTextoEnPantalla` es `Sin subtítulos ni texto en pantalla.` | "falta la cadena de contrato 'Sin subtítulos ni texto en pantalla.'" |
| Los `[ … ]` están balanceados y no se anidan | "corchete sin cerrar en 'toma'" |

## Secretos

`Gemini:ApiKey` no está en `config/`: va en user-secrets o en una variable de entorno. Que falte
no impide arrancar (la app sirve para cargar y revisar sin IA). Al generar, una clave faltante
es un error de configuración (FR-018a).
