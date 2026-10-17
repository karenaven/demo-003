# Quickstart: validar RodajeIA v1

**Spec**: [spec.md](spec.md) · **Plan**: [plan.md](plan.md)

Guía para comprobar de punta a punta que la v1 funciona. Los detalles de formato están en
[contracts/](contracts/) y el modelo de datos en [data-model.md](data-model.md).

## Requisitos previos

- SDK de .NET 10 (`dotnet --list-sdks` muestra una versión 10.0.x).
- Una API key de Gemini, solo para los pasos con IA.

## Puesta en marcha

```bash
dotnet tool restore
dotnet restore
dotnet user-secrets set "Gemini:ApiKey" "<tu-api-key>" --project src/RodajeIA.Web
dotnet ef database update --project src/RodajeIA.Web
dotnet run --project src/RodajeIA.Web
```

Resultado esperado: la app arranca y muestra la lista de series (vacía). Si un archivo de
`config/` es inválido, no arranca y la consola dice cuál y por qué ([contracts/config.md](contracts/config.md)).

## Suite automatizada (sin red ni API key)

```bash
dotnet format --verify-no-changes
dotnet test
```

Resultado esperado: el formato no tiene cambios pendientes y todos los tests pasan. Hay al menos
un test por cada AC del PRD y por cada AC-S01 a AC-S39. La suite no llama a Gemini.

Mediciones (se corren a mano). Los tests de rendimiento y de Gemini son `Explicit` (xUnit v3):
`dotnet test` los muestra como omitidos y solo corren si se piden con `--explicit only`.

```bash
# SC-004 y SC-005: p95 de 100 ejecuciones, sin la primera
dotnet test --filter-trait "Categoria=Rendimiento" --explicit only --output Detailed
# SC-006 y SC-002: requieren API key; consumen cuota de Gemini (sin clave se omiten)
dotnet test --filter-trait "Categoria=Gemini" --explicit only --output Detailed
```

Anotá junto al resultado el equipo (CPU, RAM y sistema operativo) y el modelo usado
(spec, Condiciones de medición).

| Fecha | Criterio | Resultado | Equipo |
| --- | --- | --- | --- |
| 2026-10-17 | SC-004 (guion 10 × 20 clips) | p95 = 8,55 ms | WSL2 (Linux 6.18), 8 núcleos |
| 2026-10-17 | SC-005 (prompt de 20 clips) | p95 = 13,27 ms | WSL2 (Linux 6.18), 8 núcleos |
| — | SC-006 y SC-002 | Pendiente: no hay `Gemini:ApiKey` configurada | — |

## Recorrido manual (criterio de demo, SC-008: < 2 minutos)

1. **Serie y personajes:** crear la serie "Demo" y las hojas "Ana" y "José Daniel" con todos los
   campos.
   - Comprobar que crear "ANA" se rechaza porque el nombre ya existe (AC-S08).
   - Comprobar que crear "Audio" se rechaza porque es un nombre reservado (AC-S09).
2. **Episodio:** crear el episodio 1, "Piloto". Comprobar que crear otro episodio número 1 se
   rechaza (AC-11f).
3. **Guion inválido:** pegar la escena del Anexo A sin la línea `AUDIO:` y con `CLIP 1` antes de
   `PUESTA EN ESCENA:`.
   - Esperado: la lista de errores con línea y motivo. No se guarda nada (AC-03a, AC-S01).
4. **Guion válido:** pegar la escena del Anexo A completa.
   - Esperado: 1 escena en "pendiente" con 2 clips.
   - Ana y José Daniel están presentes y el mesero no (AC-04b, AC-05d).
   - No se llamó a la IA (AC-06j).
5. **Variante:** en la escena 1, crear una variante de vestuario para Ana, con el valor "abrigo
   gris" (AC-09h).
6. **Generar:** pulsar "Generar".
   - Esperado: el estado pasa a "Generando · intento 1 de 3" y después a "Generada", sin
     recargar (AC-S26).
   - Al abrir una segunda pestaña durante la generación, "Generar" aparece deshabilitado
     (AC-S14).
7. **Revisión y prompt:** abrir la escena.
   - Esperado: 2 bloques y sus tomas, con valores del vocabulario.
   - El prompt tiene las seis secciones, la hoja de Ana con "abrigo gris" y los diálogos
     literales (AC-08f, AC-S07).
   - Cada bloque cierra con "Sin subtítulos ni texto en pantalla." (AC-08g).
8. **Edición:** cambiar el plano de una toma.
   - Esperado: el prompt refleja el valor nuevo (AC-07b).
   - Reiniciar la app (`Ctrl+C` y `dotnet run`); la edición se conserva (AC-07e).
9. **Copiar:** pulsar "Copiar" y pegar en un editor.
   - Esperado: el texto pegado es idéntico al mostrado (AC-08d).

## Fallas de la IA (con el doble de prueba, en la suite)

Estos casos se validan en tests, sin red:
- Timeout en los 3 intentos (AC-S22).
- Clave faltante (AC-S23).
- Respuesta con un bloque de menos (AC-06e, AC-S21).
- Diálogos fuera de orden (AC-S18).
- Comillas en la acción (AC-S20).
- Cierre en medio de la generación (AC-S28).

Para verlos a mano, quitá la clave (`dotnet user-secrets remove "Gemini:ApiKey" --project
src/RodajeIA.Web`) y pulsá "Generar". Esperado: la primera escena queda en "Error: error de
configuración de la IA…" y las demás siguen en "pendiente" (FR-018a).
