# HotSTalentOverlay

Overlay local para OBS que lee los `StormSave` de Heroes of the Storm, detecta los talentos elegidos y muestra sus iconos reales. No sube BattleTag, partidas ni talentos a Internet; después de la instalación funciona offline.

## 1. Instalar

La forma más sencilla es descomprimir `HotSTalentOverlay-win-x64.zip`. Incluye la aplicación y el extractor; no requiere instalar .NET.

Para compilar desde el repositorio, abre PowerShell en esta carpeta y ejecuta:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\setup.ps1
```

El script descarga una copia local de .NET y `HeroesDataParser`, restaura dependencias, ejecuta tests y compila. No instala servicios ni modifica HotS.

## 2. Ejecutar

En el paquete, ejecuta `HotSTalentOverlay.exe`. Desde el repositorio, ejecuta `.\run.ps1`.

Abre <http://127.0.0.1:3874/>. En el primer arranque se detecta HotS y se extraen automáticamente héroes, talentos e iconos. Puede tardar unos 30 segundos. Los siguientes arranques usan caché; al cambiar la build, el catálogo se regenera solo.

## 3. Configurar BattleTag

En **Configuración**, escribe `Nombre#1234` y pulsa **Guardar**. Normalmente la app identifica al jugador automáticamente por el identificador de cuenta incluido en la ruta del `StormSave`; BattleTag es el fallback sencillo cuando eso no sea posible.

## 4. Añadir a OBS

1. En OBS, añade una fuente **Navegador**.
2. URL: `http://127.0.0.1:3874/overlay.html`
3. Ancho: `1920`; alto: `1080`.
4. Activa **Cerrar fuente cuando no sea visible** sólo si quieres liberar la conexión SSE al ocultarla.
5. No hace falta recargar OBS: los cambios llegan por Server-Sent Events.

## 5. Saber si funciona

La pantalla debe mostrar **Todo listo**, la build, el número de héroes/talentos y **Detector en directo: Activo**. Pulsa **Cargar demo** para comprobar el overlay sin iniciar una partida. Durante la partida, cada talento aparecerá tras la escritura del siguiente `StormSave` (HotS puede introducir un pequeño retraso).

Los datos locales están en `%LOCALAPPDATA%\HotSTalentOverlay`: `config.json`, `data/talents.json`, `assets/talents/` y `logs/latest.log`.

### Diagnóstico

- **HotS no detectado:** indica la carpeta que contiene `.build.info`.
- **No se identifica al jugador:** configura BattleTag y confirma que la carpeta Accounts es la correcta.
- **Error de extracción:** revisa `logs/latest.log`; el catálogo anterior se conserva si una regeneración falla.
- **Formato interno incompatible:** se muestra el error del parser/extractor en la interfaz, no se ignora silenciosamente.
- Si cambias el puerto, reinicia la app y actualiza la URL de OBS.

## Desarrollo

```powershell
.\setup.ps1 -SkipPublish
$env:HOTS_STORMSAVE_FIXTURE='C:\ruta\a\fixture.StormSave'
.\.dotnet\dotnet.exe test HotSTalentOverlay.slnx --configuration Release
```

Licencia del código del proyecto: MIT. Dependencias de HeroesToolChest: MIT; consulta `THIRD_PARTY_NOTICES.md`.
