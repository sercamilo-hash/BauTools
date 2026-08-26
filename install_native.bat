@echo off
chcp 65001 > nul
echo ========================================================
echo   BauTools Suite — Revit Native Add-in Auto-Installer
echo   Author: Arch Sergio Castro
echo ========================================================
echo.

set TARGET_DIR=%APPDATA%\Autodesk\Revit\Addins\2026

echo [1/3] Creando directorio de Addins Revit 2026...
if not exist "%TARGET_DIR%\ZoningFloorArea" mkdir "%TARGET_DIR%\ZoningFloorArea"

echo [2/3] Copiando DLL y Addin Manifest...
copy /Y "%~dp0ZoningFloorArea\ZoningFloorArea.addin" "%TARGET_DIR%\ZoningFloorArea.addin" > nul
copy /Y "%~dp0ZoningFloorArea\ZoningFloorArea.dll" "%TARGET_DIR%\ZoningFloorArea\ZoningFloorArea.dll" > nul

echo [3/3] Verificando instalación...
if exist "%TARGET_DIR%\ZoningFloorArea\ZoningFloorArea.dll" (
    echo.
    echo ✅ ¡INSTALACIÓN NATIVA EXITOSA!
    echo BauTools ha sido instalado en: %TARGET_DIR%
    echo Al abrir Revit 2026, verás la pestaña [BauTools] directamente en el Ribbon.
) else (
    echo ❌ Ocurrió un problema al copiar los archivos.
)
echo.
pause