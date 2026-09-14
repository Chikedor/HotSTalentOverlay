# HotSTalentOverlay

**English** · [Español](#español)

A private, local OBS overlay for *Heroes of the Storm*. It watches the game's `StormSave` files, identifies your hero and chosen talents, and updates the overlay with the real in-game icons.

- Runs entirely on your Windows PC after setup.
- Automatically detects the installed HotS build.
- Extracts hero, talent, and icon data directly from the local CASC game files.
- Regenerates the catalog when the game build changes.
- Updates OBS live through Server-Sent Events—no browser polling or manual refresh.
- Provides a complete dashboard in English and Spanish.
- Lets you personalize colors, borders, spacing, labels, and animations with a live preview.
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

## Customize the overlay

Open the dashboard and use **Appearance** to tailor the overlay without editing CSS:

- Choose the accent, empty-border, background, and text colors.
- Adjust the icon size, spacing, border width, radius, and style.
- Select how a new talent appears: fade, slide, pop, flip, or no animation.
- Add a subtle idle effect: breathing, glow, floating, or none.
- Show or hide the hero name, talent levels, and localized talent names.
- Preview every change instantly, then save it for OBS and future sessions.

The **Interface language** selector changes the dashboard between English and Spanish. **Talent language** controls the game data extracted from HotS and is intentionally configured separately.

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

## Known limitation: update delay

HotS decides when it writes live match state to `StormSave`. In testing, writes usually happen around every 90 seconds, so a newly selected talent can take roughly 90–120 seconds to appear. HotSTalentOverlay processes the file immediately after it changes, but it cannot reliably detect a choice before the game persists it. The project deliberately avoids process injection, memory reading, keyboard hooks, and resolution-dependent screen recognition.

## Credits and third-party work

HotSTalentOverlay builds on excellent open-source work by other developers:

- [HeroesDataParser](https://github.com/HeroesToolChest/HeroesDataParser) 5.0.4, created by **Kevin Oliva and contributors** (MIT), reads the local CASC installation and extracts hero metadata and talent icons.
- [Heroes.StormReplayParser](https://github.com/HeroesToolChest/Heroes.StormReplayParser) 2.2.1, created by **Kevin Oliva and contributors** (MIT), provides the replay and tracker-event decoding. This repository vendors a small, documented compatibility adaptation for current `StormSave` files.
- [Heroes.MpqTool](https://github.com/HeroesToolChest/Heroes.MpqTool) 1.2.0, created by **Kevin Oliva and contributors** (MIT), is used transitively to read Blizzard MPQ containers.
- [.NET and ASP.NET Core](https://github.com/dotnet) by **Microsoft and .NET contributors** (MIT) provide the application runtime and local web server.
- [xUnit.net](https://github.com/xunit/xunit) v3 by **James Newkirk, Brad Wilson, and contributors** (Apache-2.0) provides the test framework.

The [Heroes Profile Uploader](https://github.com/Heroes-Profile/HeroesProfile.Uploader) was used as an architectural reference for resilient `StormSave` watching, but none of its source code or services are included.

Thank you to every maintainer and contributor behind these projects. Full version, copyright, and license notices are in [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).

*Heroes of the Storm* and its assets are property of Blizzard Entertainment. HotSTalentOverlay is an independent community project and is not affiliated with or endorsed by Blizzard Entertainment.

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
- Ofrece un panel completo en español e inglés.
- Permite personalizar colores, bordes, espaciado, textos y animaciones con vista previa en directo.
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

## Personalizar el overlay

Abre el panel y usa **Apariencia** para adaptar el overlay sin editar CSS:

- Elige los colores de acento, borde vacío, fondo y texto.
- Ajusta el tamaño y la separación de los iconos, además del grosor, radio y estilo del borde.
- Selecciona cómo aparece un nuevo talento: fundido, deslizamiento, pop, giro o sin animación.
- Añade un efecto sutil en reposo: respiración, brillo, flotación o ninguno.
- Muestra u oculta el nombre del héroe, los niveles y los nombres localizados de los talentos.
- Previsualiza cada cambio al instante y guárdalo para OBS y las próximas sesiones.

El selector **Idioma de interfaz** cambia el panel entre español e inglés. **Idioma de talentos** controla los datos del juego extraídos de HotS y se configura por separado de forma intencionada.

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

## Limitación conocida: retraso de actualización

HotS decide cuándo escribe el estado de la partida en `StormSave`. En las pruebas, las escrituras suelen producirse aproximadamente cada 90 segundos, por lo que un talento recién elegido puede tardar unos 90–120 segundos en aparecer. HotSTalentOverlay procesa el archivo inmediatamente después del cambio, pero no puede conocer la elección de forma fiable antes de que el juego la guarde. El proyecto evita deliberadamente la inyección en el proceso, la lectura de memoria, los hooks de teclado y el reconocimiento de pantalla dependiente de la resolución.

## Créditos y trabajo de terceros

HotSTalentOverlay se apoya en el excelente trabajo open source de otros desarrolladores:

- [HeroesDataParser](https://github.com/HeroesToolChest/HeroesDataParser) 5.0.4, creado por **Kevin Oliva y colaboradores** (MIT), lee la instalación CASC local y extrae los metadatos de héroes y los iconos de talentos.
- [Heroes.StormReplayParser](https://github.com/HeroesToolChest/Heroes.StormReplayParser) 2.2.1, creado por **Kevin Oliva y colaboradores** (MIT), proporciona la decodificación de replays y eventos del tracker. Este repositorio incluye una pequeña adaptación de compatibilidad documentada para los `StormSave` actuales.
- [Heroes.MpqTool](https://github.com/HeroesToolChest/Heroes.MpqTool) 1.2.0, creado por **Kevin Oliva y colaboradores** (MIT), se utiliza de forma transitiva para leer los contenedores MPQ de Blizzard.
- [.NET y ASP.NET Core](https://github.com/dotnet) de **Microsoft y los colaboradores de .NET** (MIT) proporcionan el runtime y el servidor web local.
- [xUnit.net](https://github.com/xunit/xunit) v3 de **James Newkirk, Brad Wilson y colaboradores** (Apache-2.0) proporciona el framework de pruebas.

El [uploader de Heroes Profile](https://github.com/Heroes-Profile/HeroesProfile.Uploader) se utilizó como referencia arquitectónica para la vigilancia robusta de `StormSave`, pero no se incluye su código ni se utilizan sus servicios.

Gracias a todos los mantenedores y colaboradores de estos proyectos. Los avisos completos de versiones, copyright y licencias están en [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).

*Heroes of the Storm* y sus recursos pertenecen a Blizzard Entertainment. HotSTalentOverlay es un proyecto comunitario independiente, sin afiliación ni respaldo de Blizzard Entertainment.

## Desarrollo

```powershell
.\setup.ps1 -SkipPublish
$env:HOTS_STORMSAVE_FIXTURE='C:\ruta\al\fixture.StormSave'
.\.dotnet\dotnet.exe test HotSTalentOverlay.slnx --configuration Release
```

El proyecto usa licencia MIT. La dependencia incluida de HeroesToolChest también es MIT; consulta `THIRD_PARTY_NOTICES.md` para más información.
