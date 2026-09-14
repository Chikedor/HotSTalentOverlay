# HotSTalentOverlay

**English** · [Español](#español)

A private, local OBS overlay for *Heroes of the Storm*. It watches the game's `StormSave` files, identifies your hero and chosen talents, and updates the overlay with the real in-game icons.

- Runs entirely on your Windows PC after setup.
- Automatically detects the installed HotS build.
- Extracts hero, talent, and icon data directly from the local CASC game files.
- Regenerates the catalog when the game build changes.
- Updates OBS live through Server-Sent Events—no browser polling or manual refresh.
- Never uploads your BattleTag, matches, or talent choices.

## Quick start

### Downloaded package

1. Download and extract `HotSTalentOverlay-win-x64.zip` from the latest release.
2. Run `HotSTalentOverlay.exe`.
3. Open <http://127.0.0.1:3874/>.
4. Wait for the status page to show **Todo listo**. The first catalog extraction can take around 30 seconds.

The Windows package is self-contained and does not require a separate .NET installation.

### Build from source

Open PowerShell in the repository and run:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\setup.ps1
```

The script downloads a repository-local .NET SDK and `HeroesDataParser`, restores dependencies, runs the test suite, and publishes the app. It does not modify HotS or install a Windows service.

## Add the overlay to OBS

1. Add a **Browser Source** in OBS.
2. Set the URL to `http://127.0.0.1:3874/overlay.html`.
3. Use a width of `1920` and height of `1080`.
4. Optionally enable **Shutdown source when not visible**.

Use **Load demo** on the dashboard to test the transparent overlay without starting a match.

## Player identification

The app normally identifies the local player from the account identifier in the `StormSave` path. If that is not possible, enter a BattleTag such as `Player#1234` under **Configuration** and save it.

## Local data and privacy

Runtime data stays under `%LOCALAPPDATA%\HotSTalentOverlay`:

```text
config.json
data/talents.json
assets/talents/
logs/latest.log
```

The web server only binds to `127.0.0.1` by default. No telemetry or cloud service is used.

## Troubleshooting

- **HotS not detected:** select the game directory containing `.build.info`.
- **Player not identified:** configure your BattleTag and check the Accounts directory.
- **Extraction error:** inspect `logs/latest.log`. A failed regeneration keeps the previous working catalog.
- **Parser/extractor incompatibility:** the dashboard reports the error instead of silently discarding it.
- **Port changed:** restart the application and update the OBS URL.

## Development

```powershell
.\setup.ps1 -SkipPublish
$env:HOTS_STORMSAVE_FIXTURE='C:\path\to\fixture.StormSave'
.\.dotnet\dotnet.exe test HotSTalentOverlay.slnx --configuration Release
```

The project is licensed under MIT. The vendored HeroesToolChest dependency is also MIT; see `THIRD_PARTY_NOTICES.md` for details.

---

## Español

Overlay privado y local para OBS y *Heroes of the Storm*. Vigila los archivos `StormSave`, identifica tu héroe y los talentos elegidos, y actualiza el overlay con los iconos reales del juego.

- Funciona íntegramente en tu PC con Windows después de prepararlo.
- Detecta automáticamente la build instalada de HotS.
- Extrae héroes, talentos e iconos directamente de los archivos CASC locales.
- Regenera el catálogo cuando cambia la build del juego.
- Actualiza OBS mediante Server-Sent Events, sin polling ni recargas manuales.
- Nunca sube tu BattleTag, partidas o elecciones de talentos.

## Inicio rápido

### Paquete descargado

1. Descarga y descomprime `HotSTalentOverlay-win-x64.zip` desde la última release.
2. Ejecuta `HotSTalentOverlay.exe`.
3. Abre <http://127.0.0.1:3874/>.
4. Espera hasta ver **Todo listo**. La primera extracción del catálogo puede tardar unos 30 segundos.

El paquete para Windows es autónomo y no requiere instalar .NET por separado.

### Compilar desde el código fuente

Abre PowerShell en el repositorio y ejecuta:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\setup.ps1
```

El script descarga una copia local de .NET y `HeroesDataParser`, restaura las dependencias, ejecuta los tests y publica la aplicación. No modifica HotS ni instala ningún servicio de Windows.

## Añadir el overlay a OBS

1. Añade una fuente **Navegador** en OBS.
2. Usa la URL `http://127.0.0.1:3874/overlay.html`.
3. Configura un ancho de `1920` y un alto de `1080`.
4. Opcionalmente, activa **Cerrar fuente cuando no sea visible**.

Pulsa **Cargar demo** en el panel para probar el overlay transparente sin iniciar una partida.

## Identificación del jugador

Normalmente la aplicación identifica al jugador local mediante el identificador de cuenta incluido en la ruta del `StormSave`. Si no fuera posible, escribe un BattleTag como `Jugador#1234` en **Configuración** y guárdalo.

## Datos locales y privacidad

Los datos de ejecución permanecen en `%LOCALAPPDATA%\HotSTalentOverlay`:

```text
config.json
data/talents.json
assets/talents/
logs/latest.log
```

El servidor web sólo escucha en `127.0.0.1` de forma predeterminada. No utiliza telemetría ni servicios en la nube.

## Diagnóstico

- **HotS no detectado:** selecciona la carpeta del juego que contiene `.build.info`.
- **No se identifica al jugador:** configura tu BattleTag y comprueba la carpeta Accounts.
- **Error de extracción:** revisa `logs/latest.log`. Si una regeneración falla, se conserva el último catálogo funcional.
- **Parser o extractor incompatible:** el panel muestra el error en lugar de ignorarlo silenciosamente.
- **Puerto modificado:** reinicia la aplicación y actualiza la URL de OBS.

## Desarrollo

```powershell
.\setup.ps1 -SkipPublish
$env:HOTS_STORMSAVE_FIXTURE='C:\ruta\al\fixture.StormSave'
.\.dotnet\dotnet.exe test HotSTalentOverlay.slnx --configuration Release
```

El proyecto usa licencia MIT. La dependencia incluida de HeroesToolChest también es MIT; consulta `THIRD_PARTY_NOTICES.md` para más información.
