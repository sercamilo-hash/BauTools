# 🏗️ BauTools & ZoningFloorArea (Revit 2026 / .NET 8)
## Guía Integral de Desarrollo para Claude Code

Este documento contiene la **especificación técnica completa, arquitectura, modelos de datos, servicios Revit API, componentes UI en WPF y la secuencia de prompts paso a paso** para reconstruir o desarrollar desde cero el plugin **BauTools (ZoningFloorArea)** utilizando **Claude Code** (o cualquier agente de IA para desarrollo).

---

## 📑 Tabla de Contenidos
1. [Visión General y Stack Tecnológico](#1-visión-general-y-stack-tecnológico)
2. [Estructura del Proyecto y Archivos](#2-estructura-del-proyecto-y-archivos)
3. [Arquitectura Modular del Sistema](#3-arquitectura-modular-del-sistema)
   - [Paso 1: Multi-Building & Typical Floor Groups](#paso-1-multi-building--typical-floor-groups)
   - [Paso 2: Area Mapping & Non-Destructive Propagation](#paso-2-area-mapping--non-destructive-propagation)
   - [Paso 3: NYC Zoning Calculations & ZFA Matrix](#paso-3-nyc-zoning-calculations--zfa-matrix)
   - [Paso 4: Multi-Building Sheet Diagrammer & 1-8 Grid Engine](#paso-4-multi-building-sheet-diagrammer--1-8-grid-engine)
   - [Herramientas Independientes (NYC Lot, Level Generator, Renamer, Bubble Heads, 3D Generative)](#herramientas-independientes)
4. [Modelos de Datos y Contratos (C#)](#4-modelos-de-datos-y-contratos-c)
5. [Servicios Clave de Revit API 2026](#5-servicios-clave-de-revit-api-2026)
6. [Diseño UI / UX en WPF (Dark Theme)](#6-diseño-ui--ux-en-wpf-dark-theme)
7. [Scripts de Compilación y Despliegue (.NET 8)](#7-scripts-de-compilación-y-despliegue-net-8)
8. [Secuencia de Prompts para Claude Code](#8-secuencia-de-prompts-para-claude-code)

---

## 1. Visión General y Stack Tecnológico

| Parámetro | Valor / Tecnología |
|---|---|
| **Plataforma BIM** | Autodesk Revit 2026 (x64) |
| **Framework Base** | .NET 8.0 Windows Desktop (`net8.0-windows`) |
| **Lenguaje** | C# (compatible con compilador nativo / C# 5+) |
| **Librerías Revit** | `RevitAPI.dll`, `RevitAPIUI.dll` (Ubicación: `C:\Program Files\Autodesk\Revit 2026\`) |
| **Framework UI** | WPF (Windows Presentation Foundation) con renderizado por código C# puro o XAML |
| **Dual Deployment** | 1. Add-in Nativo Revit (`.addin` en `%APPDATA%\Autodesk\Revit\Addins\2026\`)<br>2. Extensión pyRevit (`BauTools.extension`) |
| **Persistencia** | Revit Extensible Storage (JSON serializado dentro del archivo `.rvt`) |
| **Servicios Externos** | NYC Planning Labs GeoSearch API & ArcGIS MapPLUTO REST API (JSON) |

---

## 2. Estructura del Proyecto y Archivos

```text
REVIT DEVADDINS/
├── BauTools.extension/               # Extensión pyRevit Bundle
│   ├── extension.json
│   ├── bin/
│   │   └── ZoningFloorArea.dll
│   └── BauTools.tab/
│       └── Zoning.panel/
│           ├── ZFAManager.pushbutton/
│           │   ├── button.py
│           │   └── icon.png
│           ├── NYCLot.pushbutton/
│           ├── LevelGen.pushbutton/
│           └── NeuralZoning.pushbutton/
│
└── ZoningFloorArea/                  # Código Fuente C# / .NET 8
    ├── ZoningFloorArea.csproj
    ├── App.cs                        # IExternalApplication (Ribbon UI nativo)
    ├── Commands/
    │   ├── ZoningFloorAreaCommand.cs # IExternalCommand principal (4 Pasos)
    │   ├── NycLotCommand.cs          # Importador de Lotes NYC MapPLUTO
    │   ├── LevelCreatorCommand.cs    # Generador de Niveles Métrico/Imperial
    │   ├── LevelRenamerCommand.cs    # Renombrador masivo de Niveles
    │   ├── BubbleHeadCommand.cs      # Control de Burbujas de Niveles
    │   └── NeuralGenerativeCommand.cs# Visor 3D Orbit & Setbacks Dormers
    │
    ├── Models/
    │   ├── BuildingDefinition.cs     # Edificios, grupos de plantas típicas, duplex
    │   ├── TypicalFloorGroup.cs      # Rangos de niveles típicos (Desde/Hasta)
    │   ├── MappingConfig.cs          # Esquemas de áreas, scope boxes, parámetros
    │   ├── SheetCompositionModel.cs  # Matrices 1-8, Viewports, Titleblocks, Paquetes
    │   ├── NycLotInfo.cs             # Datos MapPLUTO, linderos, polígonos, contexto
    │   └── NeuralGenerativeModel.cs  # Setbacks, dormers, volumetría 3D
    │
    ├── Services/
    │   ├── RevitFloorScanService.cs      # Escaneo de niveles, vistas y esquemas
    │   ├── RevitAreaPropagatorService.cs # Propagación no destructiva de áreas
    │   ├── RevitViewGeneratorService.cs  # Creación de vistas Floor/Area Plan + Scope Box
    │   ├── RevitSheetPlacementService.cs # Posicionamiento en matriz 1-8 en Titleblocks
    │   ├── SmartScaleAdvisorService.cs   # Asesor de escala geométrica automática
    │   ├── TypicalFloorStorageService.cs # Revit Extensible Storage (JSON)
    │   ├── ExcelZoningBridgeService.cs   # Exportación/Importación de matrices ZFA
    │   ├── RevitLotDrawerService.cs      # Dibujo de linderos, aceras y masas 3D DirectShape
    │   ├── NycPlutoService.cs            # Cliente HTTP para MapPLUTO y GeoSearch
    │   └── LevelCreatorService.cs        # Parser y creador de niveles
    │
    ├── ViewModels/
    │   └── MainViewModel.cs          # ViewModel principal orquestador MVVM
    │
    └── Views/
        ├── MainWindow.cs             # Ventana Principal (WPF Dark Theme - 4 Pasos)
        ├── NycLotWindow.cs            # Ventana interactiva MapPLUTO NYC
        ├── LevelCreatorWindow.cs     # Ventana de creación de niveles
        ├── LevelRenamerWindow.cs     # Ventana de renombrado de niveles
        └── GenerativeZoningWindow.cs # Ventana 3D Orbit Viewport con Dormers
```

---

## 3. Arquitectura Modular del Sistema

### Paso 1: Multi-Building & Typical Floor Groups
- **Objetivo**: Permitir al usuario definir 1 o múltiples edificios (`Building A`, `Building B`, `Building C`, etc.) y configurar sus rangos de pisos típicos.
- **Reglas de Negocio**:
  - Un edificio contiene una lista de `TypicalFloorGroup`.
  - Cada grupo define:
    - Piso fuente (`SourceLevelName`, ej: `Level 8`).
    - Rango destino (`FromLevelName` a `ToLevelName`, ej: `Level 8` a `Level 10`).
    - Soporte para **Módulos Duplex** (`SourceLevelNameLower` y `SourceLevelNameUpper`).
    - Modo nivel único (`IsSingleLevel`) para plantas atípicas.
  - Dimensiones del Footprint (`FootprintWidthFt`, `FootprintDepthFt`) y Scope Box asignado (`ScopeBoxName`).
  - Todo se persiste en el modelo mediante **Revit Extensible Storage**.

---

### Paso 2: Area Mapping & Non-Destructive Propagation
- **Objetivo**: Propagar los `Area`, `AreaBoundaryLine`, `Room` o elementos del piso típico origen hacia los niveles de su rango sin alterar los niveles atípicos.
- **Reglas de Negocio**:
  - Escaneo automático de esquemas de área en el modelo (`Gross Building`, `Zoning Deductions`, etc.).
  - Filtro por esquema de área y tipo de plano.
  - Verificación antes de sobrescribir elementos existentes en los niveles destino.
  - Creación de logs detallados de elementos propagados.

---

### Paso 3: NYC Zoning Calculations & ZFA Matrix
- **Objetivo**: Calcular la Matriz de Área de Zonificación (ZFA - *Zoning Floor Area*), Factor de Ocupación del Suelo (FAR), Deducciones permitidas y Superficie Bruta.
- **Reglas de Negocio**:
  - Cálculo de Gross Area total por edificio y piso.
  - Deducciones permitidas bajo normativa NYC ZR:
    - Espacios mecánicos (*Mechanical Deductions*).
    - Estacionamientos subterráneos / *Accessory Parking*.
    - Muros exteriores (*Exterior Wall Deductions*).
    - Espacios de carga y logística.
  - Multiplicación de áreas típicas por el factor del rango (`Count` de pisos en el grupo).
  - Cálculo de ZFA neta y comparación con el FAR máximo permitido del lote.
  - Exportación a Excel / CSV del reporte de zonificación.

---

### Paso 4: Multi-Building Sheet Diagrammer & 1-8 Grid Engine
- **Objetivo**: Generar automáticamente las vistas y diagramarlas en planos (*Sheets*) de Revit organizadas en matrices de 1 a 8 vistas por plano.
- **Reglas de Negocio**:
  1. **Jerarquía Multi-Edificio**:
     - Si hay $>1$ edificio, se genera el paquete **`Master Overall Floor Plan`** más los paquetes específicos de cada edificio.
  2. **Matrices 1 a 8 Vistas por Plano (`SheetLayoutMode`)**:
     - `1 Plan (1x1)`: 1 vista centrada (Ideal para Master Overall o Life Safety).
     - `2 Plans (1x2)`: 2 vistas horizontales.
     - `3 Plans (1x3)`: 3 vistas horizontales.
     - `4 Plans (2x2)`: Cuadrícula $2 \times 2$.
     - `6 Plans (2x3)`: Cuadrícula $2 \times 3$.
     - `8 Plans (2x4)`: Cuadrícula $2 \times 4$ (Máxima densidad).
  3. **Configuración Independiente por Paquete**:
     - Paquetes soportados: `Master`, `Architectural`, `Gross Area`, `Deductions`, `Life Safety`, `RCP`.
     - Cada paquete tiene su propio: *Grid Layout*, *View Template*, *Escala*, *Prefijo de Plano*, *Titleblock* y switch de inclusión.
  4. **Títulos Estandarizados en Plano (`Title on Sheet`)**:
     - Se asigna automáticamente el parámetro `BuiltInParameter.VIEW_DESCRIPTION`:
       - `BLDG A - 8TH TO 10TH FLOOR PLAN`
       - `BLDG A - 8TH TO 10TH DEDUCTIONS PLAN`
       - `BLDG A - 8TH TO 10TH LIFE SAFETY PLAN`
       - `BLDG A - 8TH TO 10TH REFLECTED CEILING PLAN`
       - `MASTER - 8TH TO 10TH OVERALL FLOOR PLAN`
  5. **Correspondencia Estricta con Scope Box**:
     - Cada vista asigna `BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP` al Scope Box del edificio correspondiente.
     - Las vistas Master asignan el Scope Box Master del conjunto.
  6. **Smart Scale Advisor**:
     - Mediante cálculo vectorial compara el ancho y profundidad del footprint con el área útil de cada celda del Titleblock y recomienda la escala idónea (`1/4"`, `3/16"`, `1/8"`, `3/32"`, `1/16"`, `1:50`, `1:100`, `1:200`) para evitar solapes.
  7. **Simulador de Lienzo en Tiempo Real**:
     - Previsualiza el plano con proporciones reales del Titleblock, slots coloreados, etiquetas de rango y badges de verificación de Scope Box.

---

### Herramientas Independientes

1. **NYC Lot & Block Boundary Drawer (`NycLotWindow.cs`)**:
   - Conexión REST a NYC MapPLUTO y PlanningLabs GeoSearch.
   - Búsqueda por dirección o por BBL (Borough-Block-Lot).
   - Generación en Revit de:
     - Linderos del lote sujeto (`ModelCurve` o `DetailLine` en color rojo).
     - Lotes colindantes (naranja) y contexto de la manzana (gris).
     - Acera perimetral con offset configurable (azul).
     - Masas volumétricas 3D de los edificios existentes mediante `DirectShape` con alturas reales de MapPLUTO.
     - Tabla nativa de Zonificación en Vista de Diseño (*Drafting View* a escala 1:1).

2. **Level Generator & Metric/Imperial Parser (`LevelCreatorService.cs`)**:
   - Creación masiva de niveles a partir de listas de texto.
   - Parsing inteligente de entradas métricas (`3.5m`, `3500mm`) e imperiales (`11'-6"`, `12.5'`).
   - Creación automática de vistas de planta y techo asociadas.

3. **Rename Levels Engine (`LevelRenamerService.cs`)**:
   - Reglas de renombrado con prefijos, sufijos, reemplazo de cadenas y numeración de dos dígitos (`01 - LEVEL`, `02 - LEVEL`).

4. **Bubble Head Visibility Automator (`BubbleHeadService.cs`)**:
   - Control en bloque de los extremos de visualización de burbujas en alzados y secciones (`Show/Hide Left`, `Show/Hide Right`, `Show Both`).

5. **Neural 3D Generative Zoning & Dormer Setbacks (`GenerativeZoningWindow.cs`)**:
   - Visor 3D interactivo con órbita 360° por mouse y zoom con rueda de desplazamiento (`Viewport3D` + `PerspectiveCamera`).
   - Controles paramétricos para:
     - Retiros de calle (*Street Front Setback*), fondo (*Rear Yard*) y laterales (*Side Yards*).
     - Pisos de basamento / podium.
     - Pisos de transición con buhardillas/retranqueos escalonados (*Dormer Transition Floors* & *Setback Step FT*).
     - Cobertura de torre y pisos de remate / ático de lujo (*Penthouse Crown*).

---

## 4. Modelos de Datos y Contratos (C#)

### `BuildingDefinition.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ZoningFloorArea.Models
{
    public class BuildingDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ScopeBoxName { get; set; }
        public double FootprintWidthFt { get; set; }
        public double FootprintDepthFt { get; set; }
        public ObservableCollection<TypicalFloorGroup> TypicalGroups { get; set; }

        public BuildingDefinition()
        {
            Id = Guid.NewGuid().ToString();
            Name = "Building A";
            ScopeBoxName = string.Empty;
            FootprintWidthFt = 100.0;
            FootprintDepthFt = 80.0;
            TypicalGroups = new ObservableCollection<TypicalFloorGroup>();
        }
    }
}
```

### `TypicalFloorGroup.cs`
```csharp
namespace ZoningFloorArea.Models
{
    public class TypicalFloorGroup
    {
        public string GroupId { get; set; }
        public string GroupName { get; set; }
        public string SourceLevelName { get; set; }
        public string FromLevelName { get; set; }
        public string ToLevelName { get; set; }
        public bool IsDuplexModule { get; set; }
        public string SourceLevelNameLower { get; set; }
        public string SourceLevelNameUpper { get; set; }
        public bool IsSingleLevel { get; set; }
        public int TypicalMultiplier { get; set; }

        public TypicalFloorGroup()
        {
            GroupId = System.Guid.NewGuid().ToString();
            GroupName = "Typical Range";
            SourceLevelName = string.Empty;
            FromLevelName = string.Empty;
            ToLevelName = string.Empty;
            TypicalMultiplier = 1;
        }
    }
}
```

### `SheetCompositionModel.cs`
```csharp
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace ZoningFloorArea.Models
{
    public enum SheetLayoutMode
    {
        Single1View = 1,   // 1x1
        Dual2Views = 2,    // 1x2
        Triple3Views = 3,  // 1x3
        Quad4Views = 4,    // 2x2
        Hex6Views = 6,     // 2x3
        Octo8Views = 8     // 2x4
    }

    public class PackageSetting
    {
        public string PackageKey { get; set; }
        public string DisplayName { get; set; }
        public bool IsEnabled { get; set; }
        public SheetLayoutMode LayoutMode { get; set; }
        public string SelectedViewTemplateName { get; set; }
        public int ScaleValue { get; set; }
        public string SheetNumberPrefix { get; set; }
        public string SheetNameSuffix { get; set; }
        public bool IncludeZoningTableOnSheet { get; set; }
        public string RecommendedScaleString { get; set; }

        public PackageSetting()
        {
            IsEnabled = true;
            LayoutMode = SheetLayoutMode.Quad4Views;
            ScaleValue = 96; // 1/8" = 1'-0"
            SheetNumberPrefix = "A-1";
            SheetNameSuffix = "FLOOR PLANS";
            RecommendedScaleString = "1/8\" = 1'-0\"";
        }
    }

    public class TitleblockItem
    {
        public ElementId FamilySymbolId { get; set; }
        public string Name { get; set; }
        public double WidthFt { get; set; }
        public double HeightFt { get; set; }
        public double UsableWidthFt { get { return WidthFt * 0.82; } }
        public double UsableHeightFt { get { return HeightFt * 0.88; } }
    }

    public class PlannedViewport
    {
        public string ViewName { get; set; }
        public string TitleOnSheet { get; set; }
        public string BuildingName { get; set; }
        public string LevelRangeLabel { get; set; }
        public string PackageType { get; set; }
        public int Scale { get; set; }
        public int SlotIndex { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public ElementId ExistingViewId { get; set; }
    }

    public class PlannedSheet
    {
        public string SheetNumber { get; set; }
        public string SheetName { get; set; }
        public string PackageType { get; set; }
        public string BuildingName { get; set; }
        public SheetLayoutMode LayoutMode { get; set; }
        public List<PlannedViewport> Viewports { get; set; }

        public PlannedSheet()
        {
            Viewports = new List<PlannedViewport>();
        }
    }
}
```

---

## 5. Servicios Clave de Revit API 2026

### 1. `SmartScaleAdvisorService.cs`
Calcula la escala ideal comparando las dimensiones del slot en papel con el footprint del edificio:
```csharp
using System;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class SmartScaleAdvisorService
    {
        public struct ScaleOption
        {
            public string Label;
            public int ScaleValue; // Factor de reducción (ej: 96 para 1/8" = 1'-0")
            public ScaleOption(string label, int val) { Label = label; ScaleValue = val; }
        }

        public static readonly ScaleOption[] StandardScales = new ScaleOption[]
        {
            new ScaleOption("1/4\" = 1'-0\"", 48),
            new ScaleOption("3/16\" = 1'-0\"", 64),
            new ScaleOption("1/8\" = 1'-0\"", 96),
            new ScaleOption("3/32\" = 1'-0\"", 128),
            new ScaleOption("1/16\" = 1'-0\"", 192),
            new ScaleOption("1:50 (Metric)", 50),
            new ScaleOption("1:100 (Metric)", 100),
            new ScaleOption("1:200 (Metric)", 200)
        };

        public ScaleOption RecommendScale(double bldgWidthFt, double bldgDepthFt, TitleblockItem tb, SheetLayoutMode layout)
        {
            if (bldgWidthFt <= 0) bldgWidthFt = 100.0;
            if (bldgDepthFt <= 0) bldgDepthFt = 80.0;
            if (tb == null) tb = new TitleblockItem { WidthFt = 3.0, HeightFt = 2.0 };

            int cols = 1;
            int rows = 1;
            switch (layout)
            {
                case SheetLayoutMode.Single1View: cols = 1; rows = 1; break;
                case SheetLayoutMode.Dual2Views:   cols = 2; rows = 1; break;
                case SheetLayoutMode.Triple3Views: cols = 3; rows = 1; break;
                case SheetLayoutMode.Quad4Views:   cols = 2; rows = 2; break;
                case SheetLayoutMode.Hex6Views:    cols = 3; rows = 2; break;
                case SheetLayoutMode.Octo8Views:   cols = 4; rows = 2; break;
            }

            double slotWidthPaperFt = (tb.UsableWidthFt / cols) * 0.85;
            double slotHeightPaperFt = (tb.UsableHeightFt / rows) * 0.85;

            foreach (var opt in StandardScales)
            {
                double reqWidthPaper = bldgWidthFt / opt.ScaleValue;
                double reqHeightPaper = bldgDepthFt / opt.ScaleValue;

                if (reqWidthPaper <= slotWidthPaperFt && reqHeightPaper <= slotHeightPaperFt)
                {
                    return opt;
                }
            }

            return StandardScales[4]; // 1/16" como fallback
        }
    }
}
```

### 2. `RevitSheetPlacementService.cs` (Cálculo de Coordenadas de Matriz)
```csharp
public static XYZ CalculateSlotCenter(int slotIndex, SheetLayoutMode mode, TitleblockItem tb)
{
    double tbWidth = tb != null ? tb.WidthFt : 3.0;
    double tbHeight = tb != null ? tb.HeightFt : 2.0;

    double marginL = 0.15 * tbWidth;
    double marginR = 0.10 * tbWidth;
    double marginB = 0.10 * tbHeight;
    double marginT = 0.10 * tbHeight;

    double usableW = tbWidth - marginL - marginR;
    double usableH = tbHeight - marginB - marginT;

    int cols = 1;
    int rows = 1;
    switch (mode)
    {
        case SheetLayoutMode.Single1View: cols = 1; rows = 1; break;
        case SheetLayoutMode.Dual2Views:   cols = 2; rows = 1; break;
        case SheetLayoutMode.Triple3Views: cols = 3; rows = 1; break;
        case SheetLayoutMode.Quad4Views:   cols = 2; rows = 2; break;
        case SheetLayoutMode.Hex6Views:    cols = 3; rows = 2; break;
        case SheetLayoutMode.Octo8Views:   cols = 4; rows = 2; break;
    }

    int col = slotIndex % cols;
    int row = slotIndex / cols;

    double slotW = usableW / cols;
    double slotH = usableH / rows;

    double centerX = marginL + (col + 0.5) * slotW;
    double centerY = (tbHeight - marginT) - (row + 0.5) * slotH;

    return new XYZ(centerX, centerY, 0);
}
```

---

## 6. Diseño UI / UX en WPF (Dark Theme)

- **Paleta de Colores Dark Mode**:
  - Fondo Principal: `#0F172A` (Slate 900)
  - Tarjetas y Contenedores: `#1E293B` (Slate 800)
  - Bordes y Separadores: `#334155` (Slate 700)
  - Acento Primario / Botones de Acción: `#2563EB` (Blue 600) / Hover: `#1D4ED8`
  - Acento Secundario / Confirmación: `#059669` (Emerald 600)
  - Texto Principal: `#F8FAFC` (Slate 50)
  - Texto Secundario: `#94A3B8` (Slate 400)
  - Badges de Scope Box: `#10B981` (Green 500)
- **Componentes**:
  - `TabControl` estilizado para navegar los 4 pasos.
  - `DataGrid` con plantillas oscuras y celdas editables.
  - Simulador de Lienzo con `Canvas`, dibujando márgenes del Titleblock y rectángulos de vista con etiquetas `BLDG A - 8TH TO 10TH FLOOR PLAN`.

---

## 7. Scripts de Compilación y Despliegue (.NET 8)

Para compilar sin depender de Visual Studio, utiliza este script en PowerShell:

```powershell
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$net8Core = (Get-ChildItem "C:\Program Files\dotnet\shared\Microsoft.NETCore.App" | Sort-Object Name -Descending | Select-Object -First 1).FullName
$net8Desktop = (Get-ChildItem "C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App" | Sort-Object Name -Descending | Select-Object -First 1).FullName

$revitApi = "C:\Program Files\Autodesk\Revit 2026\RevitAPI.dll"
$revitApiUi = "C:\Program Files\Autodesk\Revit 2026\RevitAPIUI.dll"

$srcFiles = Get-ChildItem -Path ".\ZoningFloorArea" -Recurse -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch '\\obj\\' -and $_.FullName -notmatch '\\bin\\' } |
    Select-Object -ExpandProperty FullName

$references = @(
    "$net8Core\System.Private.CoreLib.dll",
    "$net8Core\System.Runtime.dll",
    "$net8Core\System.Collections.dll",
    "$net8Core\System.Linq.dll",
    "$net8Core\System.ComponentModel.dll",
    "$net8Core\System.ComponentModel.Primitives.dll",
    "$net8Core\System.ComponentModel.TypeConverter.dll",
    "$net8Core\System.ObjectModel.dll",
    "$net8Core\System.IO.dll",
    "$net8Core\System.IO.FileSystem.dll",
    "$net8Core\System.Text.RegularExpressions.dll",
    "$net8Core\System.Text.Json.dll",
    "$net8Core\System.Net.Http.dll",
    "$net8Core\System.Net.Primitives.dll",
    "$net8Core\System.Private.Uri.dll",
    "$net8Core\System.Memory.dll",
    "$net8Core\System.Xml.ReaderWriter.dll",
    "$net8Core\System.Private.Xml.dll",
    "$net8Core\System.Runtime.Extensions.dll",
    "$net8Core\System.Console.dll",
    "$net8Core\System.Threading.dll",
    "$net8Desktop\PresentationCore.dll",
    "$net8Desktop\PresentationFramework.dll",
    "$net8Desktop\WindowsBase.dll",
    "$net8Desktop\System.Xaml.dll",
    "$net8Desktop\WindowsFormsIntegration.dll",
    "$revitApi",
    "$revitApiUi"
)

$refArgs = ($references | ForEach-Object { "/reference:`"$_`"" }) -join " "
$srcArgs = ($srcFiles | ForEach-Object { "`"$_`"" }) -join " "
$targetDll = ".\ZoningFloorArea\bin\Debug\net8.0-windows\ZoningFloorArea.dll"
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($targetDll)) | Out-Null

$cmd = "& `"$csc`" /target:library /platform:x64 /noconfig /nostdlib $refArgs /out:`"$targetDll`" $srcArgs"
Invoke-Expression $cmd

if ($LASTEXITCODE -eq 0) {
    Write-Output "COMPILATION SUCCESSFUL!"
    # Copiar a Addins de Revit 2026
    Copy-Item -Path $targetDll -Destination "$env:APPDATA\Autodesk\Revit\Addins\2026\ZoningFloorArea.dll" -Force
    # Copiar a bin de pyRevit bundle
    Copy-Item -Path $targetDll -Destination ".\BauTools.extension\bin\ZoningFloorArea.dll" -Force
}
```

---

## 8. Secuencia de Prompts para Claude Code

Copia y pega estos prompts secuencialmente en **Claude Code** para construir el plugin de forma modular y sin errores:

### 🔹 Prompt 1: Creación de la Estructura Base y Modelos
```text
Crea un proyecto C# .NET 8 para Revit 2026 llamado ZoningFloorArea.
Implementa los modelos en la carpeta Models/:
1. BuildingDefinition.cs (Edificios, FootprintWidthFt, FootprintDepthFt, ScopeBoxName, colección de TypicalFloorGroup).
2. TypicalFloorGroup.cs (GroupId, GroupName, SourceLevelName, FromLevelName, ToLevelName, IsDuplexModule, IsSingleLevel, TypicalMultiplier).
3. MappingConfig.cs (Esquemas de áreas, ViewBuildingParameterName, MasterScopeBoxName).
4. SheetCompositionModel.cs (SheetLayoutMode con Single1View=1, Dual2Views=2, Triple3Views=3, Quad4Views=4, Hex6Views=6, Octo8Views=8; PackageSetting; TitleblockItem; PlannedViewport; PlannedSheet).
5. NycLotInfo.cs (NycLotInfo, NycBlockContext, NycBuildingFootprint, NycSearchResult, LotGroupingMode).
Asegúrate de que todo el código sea compatible con .NET 8 y Revit 2026 API.
```

### 🔹 Prompt 2: Servicios de Análisis y Geometría
```text
Implementa en la carpeta Services/ los siguientes servicios:
1. SmartScaleAdvisorService.cs: Algoritmo que analiza el ancho y profundidad del footprint del edificio contra el área útil de cada slot en el Titleblock y recomienda la mejor escala arquitectónica/métrica (1/4", 3/16", 1/8", 3/32", 1/16", 1:50, 1:100, 1:200).
2. TypicalFloorStorageService.cs: Persistencia de edificios y rangos típicos en Revit Extensible Storage serializado en JSON.
3. RevitFloorScanService.cs: Escaneo de niveles, vistas existentes, View Templates, Titleblocks y Scope Boxes en el documento activo.
```

### 🔹 Prompt 3: Motor de Diagramación y Generación de Vistas (Paso 4)
```text
Implementa en Services/:
1. RevitViewGeneratorService.cs:
   - Generación de vistas FloorPlan y AreaPlan.
   - Si hay >1 edificio, genera vistas 'Master Overall Floor Plan' y vistas dependientes/específicas por edificio.
   - Asignación estricta de Scope Box a BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP.
   - Formateo del parámetro 'Title on Sheet' (BuiltInParameter.VIEW_DESCRIPTION) siguiendo la convención: '[BLDG] - [RANGE] [PACKAGE] PLAN'.
2. RevitSheetPlacementService.cs:
   - Cálculo de coordenadas (X, Y) para cuadrículas de 1 a 8 vistas por plano con respecto a los márgenes del Titleblock.
   - Creación y asignación de Viewports en los Sheets de Revit.
   - Colocación de tablas de zonificación filtradas por edificio y rango de piso.
```

### 🔹 Prompt 4: Suite de Herramientas Urbanas y de Modelado
```text
Implementa:
1. NycPlutoService.cs: Cliente HTTP asíncrono para consumir la API de GeoSearch y MapPLUTO de NYC Planning Labs.
2. RevitLotDrawerService.cs: Dibuja linderos de lote sujeto, colindantes y contexto en Revit, acera perimetral con offset, masas 3D DirectShape de edificios del entorno y genera una Vista de Diseño con la tabla de zonificación 1:1.
3. LevelCreatorService.cs: Parser y generador de niveles con soporte métrico/imperial.
4. LevelRenamerService.cs y BubbleHeadService.cs: Herramientas de renombrado y visibilidad de burbujas.
5. GenerativeZoningWindow.cs: Visor 3D interactivo con Viewport3D, PerspectiveCamera, órbita con mouse 360°, retiros de calle/fondo/laterales y buhardillas (dormers) escalonadas.
```

### 🔹 Prompt 5: ViewModel Principal y UI WPF Dark Theme
```text
Implementa:
1. ViewModels/MainViewModel.cs: Orquestador MVVM que conecta los 4 pasos, calcula los PlannedSheets dinámicamente con SmartScaleAdvisorService y ejecuta las transacciones de Revit.
2. Views/MainWindow.cs: Ventana principal en WPF Dark Theme (#0F172A / #1E293B) con 4 pestañas:
   - Paso 1: Edificios y Rangos Típicos.
   - Paso 2: Mapeo y Propagación No Destructiva de Áreas.
   - Paso 3: Matriz de Zonificación ZFA y Cálculos FAR.
   - Paso 4: Diagramador de Planos con selector de matriz 1-8 por paquete, asesor de escala, selector de Titleblock y simulador de lienzo en vivo.
3. App.cs y Commands/: Registro de comandos IExternalCommand e IExternalApplication con Ribbon panel nativo y scripts para pyRevit bundle.
```

### 🔹 Prompt 6: Compilación, Pruebas y Despliegue
```text
Genera y ejecuta el script de PowerShell para compilar ZoningFloorArea.dll contra las DLLs de Revit 2026 y .NET 8 Desktop Runtime.
Verifica que no haya errores de compilación ni referencias nulas.
Despliega la DLL compilada en:
- %APPDATA%\Autodesk\Revit\Addins\2026\ZoningFloorArea.dll
- BauTools.extension\bin\ZoningFloorArea.dll
```
