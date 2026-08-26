@echo off
chcp 65001 > nul
echo ========================================================
echo   BauTools Suite — pyRevit Extension Auto-Installer
echo   Author: Arch Sergio Castro
echo ========================================================
echo.

set TARGET_DIR=%APPDATA%\pyRevit\Extensions\BauTools.extension

echo [1/3] Creando directorio de pyRevit Extensions...
if not exist "%APPDATA%\pyRevit\Extensions" mkdir "%APPDATA%\pyRevit\Extensions"

echo [2/3] Copiando BauTools.extension...
xcopy /E /I /Y "%~dp0BauTools.extension" "%TARGET_DIR%" > nul

echo [3/3] Verificando instalación...
if exist "%TARGET_DIR%\extension.json" (
    echo.
    echo ✅ ¡INSTALACIÓN DE PYREVIT EXITOSA!
    echo BauTools ha sido instalado en: %TARGET_DIR%
    echo Al abrir Revit, verás la pestaña [BauTools] en tu barra de pyRevit.
) else (
    echo ❌ Ocurrió un problema al copiar los archivos.
)
echo.
pause