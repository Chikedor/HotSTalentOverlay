# Arquitectura

## Pipeline comprobado

```text
Instalación HotS (.build.info + CASC)
  └─ HeroesDataParser 5.0.4 ──> herodata JSON + PNG
       └─ catálogo atómico por talentId/build

Accounts/**/Saves/Rejoin/*.StormSave
  └─ FileSystemWatcher ──> debounce ──> espera/reintento ──> copia temporal
       └─ Heroes.StormReplayParser 2.2.1
            └─ jugador local ──> HeroTalent.TalentNameId ──> catálogo
                 └─ RuntimeState ──> SSE ──> overlay.html ──> OBS
                                      └─ preferencias visuales ──> variables CSS
```

## Decisiones

- Una sola aplicación web ASP.NET Core enlazada exclusivamente a `127.0.0.1`. La UI de control y el overlay comparten proceso y estado.
- SSE en lugar de WebSocket: flujo unidireccional, reconexión nativa del navegador y menos protocolo.
- IDs internos (`talentId` / `TalentNameId`) como clave. Los nombres localizados sólo se muestran.
- El propietario del replay es la primera opción para jugador local; después se compara el `ToonHandle` con el segmento de ruta `2-Hero-1-...`; BattleTag es fallback.
- Cada evento de filesystem reconstruye una foto completa de los talentos del jugador y reemplaza el estado. Esto hace la operación idempotente ante eventos duplicados y evita depender del orden de notificaciones.
- Antes de parsear se espera estabilidad de tamaño y se copia con `FileShare.ReadWrite|Delete`. Nunca se abre HotS para escritura.
- El nuevo catálogo se genera en staging. Sólo sustituye datos/iconos tras validar que todos los PNG referenciados existen; ante fallo se restaura el anterior.
- La versión se obtiene de `.build.info`; si difiere de `CatalogDocument.Build`, se regenera automáticamente.
- El idioma de la interfaz y las preferencias visuales se guardan en `config.json`, se validan en el backend y se propagan por SSE. La vista previa del panel usa el mismo overlay que OBS y aplica los cambios sin guardar mediante `postMessage`.

## Estado del ecosistema (verificado el 14-09-2026)

- [HeroesDataParser](https://github.com/HeroesToolChest/HeroesDataParser) está activo, es MIT, soporta lectura directa de instalación CASC y extracción `hero:i`; la versión 5.0.4 procesó correctamente la build local `2.55.17.98025` (90 héroes y 1.822 imágenes totales antes del filtrado).
- [Heroes.StormReplayParser](https://github.com/HeroesToolChest/Heroes.StormReplayParser) expone `StormReplay.Parse`, `StormReplayPregame.Parse`, tracker events y `HeroTalent.TalentNameId`. Se incluye un fork mínimo de la release MIT 2.2.1 porque los StormSave actuales usan `save.details`/`save.initData` y el dispatcher upstream no llama a su propio decodificador `TalentChosen`; ambas diferencias se comprobaron contra un StormSave 98025.
- `Heroes.ReplayParser` es el antecesor y su fork de Heroes Profile no es la elección: la actividad útil se concentra en HeroesToolChest.
- El [uploader de Heroes Profile](https://github.com/Heroes-Profile/HeroesProfile.Uploader) confirma la ruta Accounts, el watcher recursivo de `*.StormSave`, el watcher de `.battlelobby` y la necesidad de esperar/copiar archivos. No se usa ninguna API suya.
- Los repositorios pregenerados `heroes-data`/`heroes-images` son útiles como referencia, pero no son la fuente del runtime: romperían el requisito de actualización automática desde la instalación local.

## Límites conocidos

HotS decide cuándo persiste un nuevo StormSave; el overlay no puede mostrar un talento antes de que el juego lo escriba. Si Blizzard cambia MPQ/tracker/CASC de forma incompatible, la app mantiene el último catálogo válido y expone un error accionable. No se usa OCR como fallback silencioso.
