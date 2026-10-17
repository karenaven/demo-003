# Contrato: prompt final de una escena

**Fuente**: spec FR-025 a FR-028a, PRD Anexo D, `config/molde-prompt.json`. El molde es la
fuente normativa de la redacción (FR-025a). Este contrato fija cómo se interpreta y qué datos
recibe cada variable.

## Interpretación del molde

| Construcción | Regla |
| --- | --- |
| `{{x.y}}` | Se reemplaza por el valor guardado, literal (nunca lo reescribe la IA). |
| `[ … ]` | Se omite completo, incluidos sus separadores, si alguna variable que contiene está vacía o es nula. |
| `.` después de una variable | Se omite si el valor ya termina en `.`, `!`, `?` o `…` (FR-026). |
| `separadorSecciones`, `separadorBloques`, `separadorTomas`, `separadorDialogos` | Se usan textualmente entre elementos, nunca al principio ni al final. |
| Sección con `titulo` | Se arma `titulo` + salto de línea + contenido. |
| Sección con `lineas` | Las líneas se unen con salto de línea. |

Una variable obligatoria vacía no puede ocurrir: los datos se validan al guardarse (RN-01). Si
ocurriera, es un error interno y no se muestra un prompt incompleto.

## Variables

| Variable | Origen |
| --- | --- |
| `estilo.texto` | `config/estilo.txt` |
| `escena.locacion`, `escena.iluminacion`, `escena.puestaEnEscena`, `escena.audio` | Escena |
| `personaje.*` (`nombre`, `edad`, `descripcionFisica`, `peinado`, `vestuario`, `heridas`, `personalidad`, `rol`) | Hoja efectiva (FR-032) |
| `bloque.numero` | `Clip.Numero` |
| `toma.plano`, `toma.optica`, `toma.iluminacion`, `toma.angulo`, `toma.movimiento`, `toma.accion` | Toma |
| `toma.dialogos` | Las `linea.original` referenciadas por la toma, en orden de `Posicion` (FR-026a), unidas con `separadorDialogos` |
| `linea.original` | `LineaDialogo.Original` |
| `clip.textoEnPantallaOriginal` | `Clip.TextoEnPantallaOriginal` |

## Armado

1. **Sección 1 (estilo).**
2. **Sección 2 (locación e iluminación base).**
3. **Sección 3 (personajes):** los personajes presentes en el orden de `PersonajeEnEscena.Orden`
   (FR-025b), cada uno con su hoja efectiva y la plantilla `porPersonaje`. Si no hay ninguno,
   la sección se omite completa, separador incluido (`sinPersonajes: omitirSeccion`).
4. **Secciones 4 y 5 (puesta en escena y audio).**
5. **Sección 6 (bloques):** un bloque por clip, en orden de número:
   - el título `Bloque {n}:`;
   - las tomas en orden, unidas con `separadorTomas` (`" Corte a "`);
   - un espacio y el cierre: `cierreConTextoEnPantalla` si el clip tiene texto en pantalla, o
     `cierreSinTextoEnPantalla`.

**Cadenas de contrato** que el molde no puede cambiar sin enmendar la spec: `Bloque N:`,
`Corte a` y `Sin subtítulos ni texto en pantalla.` (FR-025a). Se validan al iniciar
([config.md](config.md)).

## Cuándo se arma

- Solo para escenas en "generada" (FR-025c).
- Se arma cada vez que se muestra o se copia, desde la base y el molde cargado al iniciar; no se
  guarda (FR-015d). Después de editar una toma, el próximo armado ya refleja el cambio (FR-024).

## Ejemplo esperado (clip 1 de la escena del Anexo A)

Con una toma sin ángulo ni movimiento que referencia `c1-d1` y `c1-d2`:

```text
Bloque 1:
plano medio, óptica 50mm. Ana y José Daniel conversan; él sonríe pícaramente. ANA: "Mentiroso." JOSÉ DANIEL (sonriendo): "Bueno... pasó parecido." Iluminación: continuidad con la luz base. Sin subtítulos ni texto en pantalla.
```
