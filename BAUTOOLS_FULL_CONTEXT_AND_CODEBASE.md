# 🏢 BAUTOOLS & ZONING FLOOR AREA (REVIT 2026 / .NET 8)
## 📦 PAQUETE COMPLETO DE CONTEXTO, ARQUITECTURA Y CÓDIGO FUENTE PARA CLAUDE

> **Propósito de este archivo**: Este documento contiene la totalidad del contexto arquitectónico, reglas de negocio, requerimientos del usuario, especificaciones de la API de Revit 2026, y **el código fuente completo de todos los archivos** del plugin BauTools / ZoningFloorArea. Puede ser cargado directamente en Claude (Proyectos, Chat o Claude Code) para tener el 100% del conocimiento y estado del proyecto.

---

## 📑 TABLA DE CONTENIDOS
1. [Resumen Ejecutivo y Stack Tecnológico](#1-resumen-ejecutivo-y-stack-tecnológico)
2. [Reglas de Negocio y Flujo de Trabajo](#2-reglas-de-negocio-y-flujo-de-trabajo)
   - [Paso 1: Multi-Building & Typical Floor Groups](#paso-1-multi-building--typical-floor-groups)
   - [Paso 2: Area Mapping & Non-Destructive Propagation](#paso-2-area-mapping--non-destructive-propagation)
   - [Paso 3: NYC Zoning Calculations & ZFA Matrix](#paso-3-nyc-zoning-calculations--zfa-matrix)
   - [Paso 4: Multi-Building Sheet Diagrammer & 1-8 Grid Engine](#paso-4-multi-building-sheet-diagrammer--1-8-grid-engine)
   - [Suite de Herramientas Independientes](#suite-de-herramientas-independientes)
3. [Guía de Compilación (.NET 8 + Revit 2026 API)](#3-guía-de-compilación-net-8--revit-2026-api)
4. [Estructura del Repositorio y Archivos](#4-estructura-del-repositorio-y-archivos)
5. [Código Fuente Completo de los Archivos](#5-código-fuente-completo-de-los-archivos)

---

## 1. Resumen Ejecutivo y Stack Tecnológico
- **Plataforma BIM**: Autodesk Revit 2026 (x64)
- **Framework**: .NET 8.0 Windows Desktop (`net8.0-windows`)
- **Lenguaje**: C# (.NET 8)
- **UI**: WPF (Windows Presentation Foundation) con paleta Dark Theme Slate (`#0F172A`, `#1E293B`, `#2563EB`)
- **Revit API DLLs**: `RevitAPI.dll`, `RevitAPIUI.dll` (Ubicadas en `C:\Program Files\Autodesk\Revit 2026\`)
- **Persistencia**: Revit Extensible Storage (almacenamiento de edificios y rangos típicos en JSON dentro del `.rvt`)
- **Servicios Externos**: NYC Planning Labs GeoSearch API y NYC ArcGIS MapPLUTO REST API
- **Dual Deployment**: Add-in nativo (`.addin` + `.dll`) y extensión pyRevit (`BauTools.extension`)
- **Repositorio GitHub**: `https://github.com/sercamilo-hash/BauTools.git`

---

## 2. Reglas de Negocio y Flujo de Trabajo

### Paso 1: Multi-Building & Typical Floor Groups
- Permite definir múltiples edificios (ej: `Building A`, `Building B`, `Building C`) o un único edificio.
- Cada edificio contiene grupos de plantas típicas (`TypicalFloorGroup`).
- Soporte para plantas estándar (`SourceLevelName` -> rango `FromLevelName` a `ToLevelName`), plantas atípicas (`IsSingleLevel`) y módulos duplex (`SourceLevelNameLower` y `SourceLevelNameUpper`).
- Cada edificio define su Footprint (`FootprintWidthFt`, `FootprintDepthFt`) y su Scope Box asignado (`ScopeBoxName`).

### Paso 2: Area Mapping & Non-Destructive Propagation
- Escaneo y selección de esquemas de área en el modelo (`Gross Building`, `Zoning Deductions`, etc.).
- Propagación no destructiva: replica los elementos de área y límites (`Area`, `AreaBoundaryLine`, `Room`) desde el piso típico fuente hacia los pisos de su rango, respetando y protegiendo las plantas atípicas.

### Paso 3: NYC Zoning Calculations & ZFA Matrix
- Matriz de cálculo de Superficie Bruta (Gross Floor Area), Deducciones permitidas bajo normativa de zonificación de NYC (ZR) y Área de Zonificación Neta (ZFA).
- Deducciones soportadas: Espacios mecánicos (*Mechanical Deductions*), estacionamiento accesorio (*Accessory Parking*), muros exteriores (*Exterior Walls*), áreas de carga (*Loading Berths*).
- Multiplicador dinámico por el conteo de pisos del grupo típico.
- Comparación contra el FAR (*Floor Area Ratio*) máximo permitido del lote y exportación a Excel.

### Paso 4: Multi-Building Sheet Diagrammer & 1-8 Grid Engine
- Generación y diagramación automática de vistas en planos de Revit (*Sheets*).
- **Jerarquía Multi-Edificio**: Cuando existen múltiples edificios, genera automáticamente el paquete **`Master Overall Floor Plan`** más los paquetes de cada edificio.
- **Matriz de Grid Configurable por Paquete (1 a 8 Vistas)**:
  * `1 Plan (1x1)`: 1 vista centrada (Ideal para Master Overall o Life Safety).
  * `2 Plans (1x2)`: 2 vistas por plano.
  * `3 Plans (1x3)`: 3 vistas horizontales.
  * `4 Plans (2x2)`: 4 vistas en cuadrícula 2x2.
  * `6 Plans (2x3)`: 6 vistas en cuadrícula 2x3.
  * `8 Plans (2x4)`: 8 vistas en cuadrícula 2x4 (Máxima densidad).
- **Títulos Estandarizados (`Title on Sheet`)**: Asigna `BuiltInParameter.VIEW_DESCRIPTION` como `BLDG A - 8TH TO 10TH FLOOR PLAN`, `BLDG A - 8TH TO 10TH DEDUCTIONS PLAN`, `MASTER - 8TH TO 10TH OVERALL FLOOR PLAN`, etc.
- **Correspondencia con Scope Box**: Cada vista asigna `BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP` al Scope Box del edificio correspondiente (o Master).
- **Smart Scale Advisor**: Analiza las dimensiones del footprint vs el área útil del slot en el Titleblock y recomienda la mejor escala (`1/4"`, `3/16"`, `1/8"`, `3/32"`, `1/16"`, `1:50`, `1:100`, `1:200`).
- **Simulador de Lienzo en Tiempo Real**: Visualización interactiva en WPF con proporciones reales del Titleblock, slots de vistas y badges de Scope Box.

### Suite de Herramientas Independientes
- **NYC Development Lot & Block Boundary Drawer (`NycLotWindow.cs`)**: Consulta APIs de NYC (GeoSearch & MapPLUTO), dibuja linderos, aceras, masas volumétricas 3D de edificios circundantes mediante `DirectShape` y crea una Vista de Diseño con la tabla de zonificación 1:1.
- **Level Generator (`BatchLevelGeneratorWindow.cs` / `LevelCreatorService.cs`)**: Creación masiva de niveles con soporte para entradas métricas e imperiales.
- **Level Renamer (`RenameLevelsWindow.cs` / `LevelRenamerService.cs`)**: Renombrado masivo con prefijos, sufijos, búsquedas y formateo numérico.
- **Bubble Heads Visibility (`BubbleHeadsWindow.cs`)**: Control de visibilidad de burbujas en extremos de niveles en alzados/secciones.
- **Neural 3D Generative Zoning & Dormers (`GenerativeZoningWindow.cs`)**: Visor 3D interactivo con órbita 360°, retiros de calle/fondo/laterales y buhardillas (*dormers*) escalonadas.

---

## 4. Estructura del Repositorio y Archivos

- [`ZoningFloorArea\App.cs`](#zoningfloorarea-app-cs)
- [`ZoningFloorArea\Command.cs`](#zoningfloorarea-command-cs)
- [`ZoningFloorArea\CommandBatchLevelGenerator.cs`](#zoningfloorarea-commandbatchlevelgenerator-cs)
- [`ZoningFloorArea\CommandBubbleHeads.cs`](#zoningfloorarea-commandbubbleheads-cs)
- [`ZoningFloorArea\CommandGenerativeZoning.cs`](#zoningfloorarea-commandgenerativezoning-cs)
- [`ZoningFloorArea\CommandNycLot.cs`](#zoningfloorarea-commandnyclot-cs)
- [`ZoningFloorArea\CommandRenameLevels.cs`](#zoningfloorarea-commandrenamelevels-cs)
- [`ZoningFloorArea\Models\AreaDataModel.cs`](#zoningfloorarea-models-areadatamodel-cs)
- [`ZoningFloorArea\Models\BuildingDefinition.cs`](#zoningfloorarea-models-buildingdefinition-cs)
- [`ZoningFloorArea\Models\LevelCreationItem.cs`](#zoningfloorarea-models-levelcreationitem-cs)
- [`ZoningFloorArea\Models\LevelRenameItem.cs`](#zoningfloorarea-models-levelrenameitem-cs)
- [`ZoningFloorArea\Models\LevelZoningRow.cs`](#zoningfloorarea-models-levelzoningrow-cs)
- [`ZoningFloorArea\Models\MappingConfig.cs`](#zoningfloorarea-models-mappingconfig-cs)
- [`ZoningFloorArea\Models\NeuralGenerativeModel.cs`](#zoningfloorarea-models-neuralgenerativemodel-cs)
- [`ZoningFloorArea\Models\NycLotInfo.cs`](#zoningfloorarea-models-nyclotinfo-cs)
- [`ZoningFloorArea\Models\ProjectZoningResult.cs`](#zoningfloorarea-models-projectzoningresult-cs)
- [`ZoningFloorArea\Models\SheetCompositionModel.cs`](#zoningfloorarea-models-sheetcompositionmodel-cs)
- [`ZoningFloorArea\Models\TypicalFloorGroup.cs`](#zoningfloorarea-models-typicalfloorgroup-cs)
- [`ZoningFloorArea\Models\ZoningComplianceModel.cs`](#zoningfloorarea-models-zoningcompliancemodel-cs)
- [`ZoningFloorArea\Models\ZoningTableResult.cs`](#zoningfloorarea-models-zoningtableresult-cs)
- [`ZoningFloorArea\Properties\AssemblyInfo.cs`](#zoningfloorarea-properties-assemblyinfo-cs)
- [`ZoningFloorArea\Services\ExcelExporter.cs`](#zoningfloorarea-services-excelexporter-cs)
- [`ZoningFloorArea\Services\ExcelZoningBridgeService.cs`](#zoningfloorarea-services-excelzoningbridgeservice-cs)
- [`ZoningFloorArea\Services\LevelCreatorService.cs`](#zoningfloorarea-services-levelcreatorservice-cs)
- [`ZoningFloorArea\Services\LevelRenamerService.cs`](#zoningfloorarea-services-levelrenamerservice-cs)
- [`ZoningFloorArea\Services\NeuralGenerativeSolver.cs`](#zoningfloorarea-services-neuralgenerativesolver-cs)
- [`ZoningFloorArea\Services\NycPlutoService.cs`](#zoningfloorarea-services-nycplutoservice-cs)
- [`ZoningFloorArea\Services\RevitAreaDuplicator.cs`](#zoningfloorarea-services-revitareaduplicator-cs)
- [`ZoningFloorArea\Services\RevitAreaExtractor.cs`](#zoningfloorarea-services-revitareaextractor-cs)
- [`ZoningFloorArea\Services\RevitLotDrawerService.cs`](#zoningfloorarea-services-revitlotdrawerservice-cs)
- [`ZoningFloorArea\Services\RevitMassingBakerService.cs`](#zoningfloorarea-services-revitmassingbakerservice-cs)
- [`ZoningFloorArea\Services\RevitSheetPlacementService.cs`](#zoningfloorarea-services-revitsheetplacementservice-cs)
- [`ZoningFloorArea\Services\RevitSheetTableDrawer.cs`](#zoningfloorarea-services-revitsheettabledrawer-cs)
- [`ZoningFloorArea\Services\RevitViewGeneratorService.cs`](#zoningfloorarea-services-revitviewgeneratorservice-cs)
- [`ZoningFloorArea\Services\SmartScaleAdvisorService.cs`](#zoningfloorarea-services-smartscaleadvisorservice-cs)
- [`ZoningFloorArea\Services\TypicalFloorStorageService.cs`](#zoningfloorarea-services-typicalfloorstorageservice-cs)
- [`ZoningFloorArea\Services\ZoningCalculator.cs`](#zoningfloorarea-services-zoningcalculator-cs)
- [`ZoningFloorArea\Tests\ApiInspector.cs`](#zoningfloorarea-tests-apiinspector-cs)
- [`ZoningFloorArea\Tests\ZoningTest.cs`](#zoningfloorarea-tests-zoningtest-cs)
- [`ZoningFloorArea\ViewModels\MainViewModel.cs`](#zoningfloorarea-viewmodels-mainviewmodel-cs)
- [`ZoningFloorArea\Views\BatchLevelGeneratorWindow.cs`](#zoningfloorarea-views-batchlevelgeneratorwindow-cs)
- [`ZoningFloorArea\Views\BubbleHeadsWindow.cs`](#zoningfloorarea-views-bubbleheadswindow-cs)
- [`ZoningFloorArea\Views\GenerativeZoningWindow.cs`](#zoningfloorarea-views-generativezoningwindow-cs)
- [`ZoningFloorArea\Views\MainWindow.cs`](#zoningfloorarea-views-mainwindow-cs)
- [`ZoningFloorArea\Views\NycLotWindow.cs`](#zoningfloorarea-views-nyclotwindow-cs)
- [`ZoningFloorArea\Views\RenameLevelsWindow.cs`](#zoningfloorarea-views-renamelevelswindow-cs)

---

## 5. Código Fuente Completo de los Archivos

### `ZoningFloorArea\App.cs`
```csharp
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace ZoningFloorArea
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "BauTools";
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch
            {
                // Tab might already exist
            }

            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName(assemblyPath);

            // ══════════════════════════════════════════════════════════
            // ── Panel 1: Zoning & Area ──
            // ══════════════════════════════════════════════════════════
            RibbonPanel zoningPanel = application.CreateRibbonPanel(tabName, "Zoning & Area");

            // ── Button 1: Consolidated ZFA Calculator ──
            PushButtonData zfaBtnData = new PushButtonData(
                "cmdZoningFloorArea",
                "ZFA\nCalculator",
                assemblyPath,
                "ZoningFloorArea.Command"
            )
            {
                ToolTip = "Zoning Floor Area (ZFA) Calculator with Typical Floors management, smart non-destructive propagation, multi-building matrix, and automated Revit drafting views. BauTools by Arch Sergio Castro."
            };

            string zfaIcon32 = Path.Combine(assemblyDir, "Resources", "icon_zfa_32.png");
            string zfaIcon16 = Path.Combine(assemblyDir, "Resources", "icon_zfa_16.png");

            if (File.Exists(zfaIcon32))
            {
                zfaBtnData.LargeImage = new BitmapImage(new Uri(zfaIcon32));
            }
            if (File.Exists(zfaIcon16))
            {
                zfaBtnData.Image = new BitmapImage(new Uri(zfaIcon16));
            }

            zoningPanel.AddItem(zfaBtnData);

            // ── Button 2: NYC Lot Boundary ──
            PushButtonData nycLotBtnData = new PushButtonData(
                "cmdNycLotBoundary",
                "NYC Lot\nBoundary",
                assemblyPath,
                "ZoningFloorArea.CommandNycLot"
            )
            {
                ToolTip = "Search NYC tax lots via MapPLUTO & GeoSearch by Address or BBL. Automatically draws native Property Lines, adjacent lots, sidewalk curbs, and surrounding street titles in Revit. BauTools by Arch Sergio Castro."
            };

            if (File.Exists(zfaIcon32))
            {
                nycLotBtnData.LargeImage = new BitmapImage(new Uri(zfaIcon32));
            }
            if (File.Exists(zfaIcon16))
            {
                nycLotBtnData.Image = new BitmapImage(new Uri(zfaIcon16));
            }

            zoningPanel.AddItem(nycLotBtnData);

            // ── Button 3: Neural Generative Zoning ──
            PushButtonData genZoningBtnData = new PushButtonData(
                "cmdGenerativeZoning",
                "Neural\nGenerative",
                assemblyPath,
                "ZoningFloorArea.CommandGenerativeZoning"
            )
            {
                ToolTip = "Neural Generative Zoning & Massing Optimizer. Evaluates optimal buildable envelope, revenue, and mandatory housing, and bakes 3D massing options into Revit Design Options. BauTools by Arch Sergio Castro."
            };

            if (File.Exists(zfaIcon32)) genZoningBtnData.LargeImage = new BitmapImage(new Uri(zfaIcon32));
            if (File.Exists(zfaIcon16)) genZoningBtnData.Image = new BitmapImage(new Uri(zfaIcon16));

            zoningPanel.AddItem(genZoningBtnData);

            // ══════════════════════════════════════════════════════════
            // ── Panel 2: Levels & Views ──
            // ══════════════════════════════════════════════════════════
            RibbonPanel viewPanel = application.CreateRibbonPanel(tabName, "Levels & Views");

            // ── Button 1: Create Levels (Batch Generator) ──
            PushButtonData createLevelsBtnData = new PushButtonData(
                "cmdCreateLevels",
                "Create\nLevels",
                assemblyPath,
                "ZoningFloorArea.CommandBatchLevelGenerator"
            )
            {
                ToolTip = "Batch level generator for multi-story buildings with automatic floor-to-floor calculations, subgrade cellars, roof, bulkhead, and associated plan views. BauTools by Arch Sergio Castro."
            };

            string createLevelsIcon32 = Path.Combine(assemblyDir, "Resources", "icon_create_levels_32.png");
            string createLevelsIcon16 = Path.Combine(assemblyDir, "Resources", "icon_create_levels_16.png");

            if (File.Exists(createLevelsIcon32))
            {
                createLevelsBtnData.LargeImage = new BitmapImage(new Uri(createLevelsIcon32));
            }
            if (File.Exists(createLevelsIcon16))
            {
                createLevelsBtnData.Image = new BitmapImage(new Uri(createLevelsIcon16));
            }

            viewPanel.AddItem(createLevelsBtnData);

            // ── Button 2: Rename Levels ──
            PushButtonData renameLevelsBtnData = new PushButtonData(
                "cmdRenameLevels",
                "Rename\nLevels",
                assemblyPath,
                "ZoningFloorArea.CommandRenameLevels"
            )
            {
                ToolTip = "Batch rename project levels using standard ordinal nomenclature (01 1ST FL., 02 2ND FL.), Cellar, Roof, and Bulkhead. BauTools by Arch Sergio Castro."
            };

            string levelsIcon32 = Path.Combine(assemblyDir, "Resources", "icon_levels_32.png");
            string levelsIcon16 = Path.Combine(assemblyDir, "Resources", "icon_levels_16.png");

            if (File.Exists(levelsIcon32))
            {
                renameLevelsBtnData.LargeImage = new BitmapImage(new Uri(levelsIcon32));
            }
            if (File.Exists(levelsIcon16))
            {
                renameLevelsBtnData.Image = new BitmapImage(new Uri(levelsIcon16));
            }

            viewPanel.AddItem(renameLevelsBtnData);

            // ── Button 3: Bubble Heads ──
            PushButtonData bubbleBtnData = new PushButtonData(
                "cmdBubbleHeads",
                "Bubble\nHeads",
                assemblyPath,
                "ZoningFloorArea.CommandBubbleHeads"
            )
            {
                ToolTip = "Toggle Bubble Heads visibility for Grids and Levels in the active Elevation or Section view. BauTools by Arch Sergio Castro."
            };

            string bubbleIcon32 = Path.Combine(assemblyDir, "Resources", "icon_bubble_32.png");
            string bubbleIcon16 = Path.Combine(assemblyDir, "Resources", "icon_bubble_16.png");

            if (File.Exists(bubbleIcon32))
            {
                bubbleBtnData.LargeImage = new BitmapImage(new Uri(bubbleIcon32));
            }
            if (File.Exists(bubbleIcon16))
            {
                bubbleBtnData.Image = new BitmapImage(new Uri(bubbleIcon16));
            }

            viewPanel.AddItem(bubbleBtnData);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}

```

### `ZoningFloorArea\Command.cs`
```csharp
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ZoningFloorArea.Views;

namespace ZoningFloorArea
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;

                MainWindow window = new MainWindow(doc);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}

```

### `ZoningFloorArea\CommandBatchLevelGenerator.cs`
```csharp
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ZoningFloorArea.Views;

namespace ZoningFloorArea
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CommandBatchLevelGenerator : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;

                BatchLevelGeneratorWindow window = new BatchLevelGeneratorWindow(doc);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}

```

### `ZoningFloorArea\CommandBubbleHeads.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ZoningFloorArea.Views;

namespace ZoningFloorArea
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CommandBubbleHeads : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;
                View activeView = doc.ActiveView;

                if (activeView == null)
                {
                    TaskDialog.Show("BauTools - Bubble Heads", "No active view is currently open.");
                    return Result.Cancelled;
                }

                int gridCount = new FilteredElementCollector(doc, activeView.Id)
                    .OfClass(typeof(Grid))
                    .GetElementCount();

                int levelCount = new FilteredElementCollector(doc, activeView.Id)
                    .OfClass(typeof(Level))
                    .GetElementCount();

                if (gridCount == 0 && levelCount == 0)
                {
                    TaskDialog.Show("BauTools - Bubble Heads",
                        string.Format("No visible Grids or Levels found in active view '{0}'.\n\nPlease open a Floor Plan, Elevation, or Section view that contains datum elements and try again.", activeView.Name));
                    return Result.Cancelled;
                }

                BubbleHeadsWindow window = new BubbleHeadsWindow(doc, activeView);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}

```

### `ZoningFloorArea\CommandGenerativeZoning.cs`
```csharp
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ZoningFloorArea.Views;

namespace ZoningFloorArea
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CommandGenerativeZoning : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;
                GenerativeZoningWindow win = new GenerativeZoningWindow(doc);
                win.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
```

### `ZoningFloorArea\CommandNycLot.cs`
```csharp
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ZoningFloorArea.Views;

namespace ZoningFloorArea
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CommandNycLot : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                if (uiDoc == null || uiDoc.Document == null)
                {
                    message = "Please open a Revit document before running NYC Lot Boundary.";
                    return Result.Failed;
                }

                Document doc = uiDoc.Document;

                NycLotWindow window = new NycLotWindow(doc);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}

```

### `ZoningFloorArea\CommandRenameLevels.cs`
```csharp
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ZoningFloorArea.Views;

namespace ZoningFloorArea
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CommandRenameLevels : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;

                RenameLevelsWindow window = new RenameLevelsWindow(doc);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}

```

### `ZoningFloorArea\Models\AreaDataModel.cs`
```csharp
using System.Collections.Generic;

namespace ZoningFloorArea.Models
{
    public class AreaDataModel
    {
        public string ElementId { get; set; }
        public string Name { get; set; }
        public double AreaValue { get; set; }
        public string LevelName { get; set; }
        public double LevelElevation { get; set; }
        public string AreaSchemeName { get; set; }
        public string DeductionType { get; set; }
        public string UsageCategory { get; set; }
        public string BuildingName { get; set; }

        public AreaDataModel()
        {
            ElementId = string.Empty;
            Name = string.Empty;
            LevelName = string.Empty;
            AreaSchemeName = string.Empty;
            DeductionType = string.Empty;
            UsageCategory = "Residential";
            BuildingName = "BUILDING C";
        }
    }
}

```

### `ZoningFloorArea\Models\BuildingDefinition.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ZoningFloorArea.Models
{
    public class BuildingDefinition : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _scopeBoxName;
        private double _footprintWidthFt;
        private double _footprintDepthFt;
        private ObservableCollection<TypicalFloorGroup> _typicalGroups;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ScopeBoxName
        {
            get { return _scopeBoxName; }
            set
            {
                if (_scopeBoxName != value)
                {
                    _scopeBoxName = value;
                    OnPropertyChanged("ScopeBoxName");
                }
            }
        }

        public double FootprintWidthFt
        {
            get { return _footprintWidthFt; }
            set
            {
                if (_footprintWidthFt != value)
                {
                    _footprintWidthFt = value;
                    OnPropertyChanged("FootprintWidthFt");
                }
            }
        }

        public double FootprintDepthFt
        {
            get { return _footprintDepthFt; }
            set
            {
                if (_footprintDepthFt != value)
                {
                    _footprintDepthFt = value;
                    OnPropertyChanged("FootprintDepthFt");
                }
            }
        }

        public string Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged("Id");
                }
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public ObservableCollection<TypicalFloorGroup> TypicalGroups
        {
            get { return _typicalGroups; }
            set
            {
                if (_typicalGroups != value)
                {
                    _typicalGroups = value;
                    OnPropertyChanged("TypicalGroups");
                }
            }
        }

        public BuildingDefinition()
        {
            _id = Guid.NewGuid().ToString();
            _name = "Building 1";
            _scopeBoxName = "(None)";
            _footprintWidthFt = 150.0;
            _footprintDepthFt = 100.0;
            _typicalGroups = new ObservableCollection<TypicalFloorGroup>();
        }

        public BuildingDefinition(string name)
        {
            _id = Guid.NewGuid().ToString();
            _name = name;
            _scopeBoxName = "(None)";
            _footprintWidthFt = 150.0;
            _footprintDepthFt = 100.0;
            _typicalGroups = new ObservableCollection<TypicalFloorGroup>();
        }

        public TypicalFloorGroup GetGroupForLevel(string levelName)
        {
            if (string.IsNullOrEmpty(levelName) || _typicalGroups == null) return null;

            foreach (TypicalFloorGroup g in _typicalGroups)
            {
                if (g.IsSingleLevel)
                {
                    if (string.Equals(g.SourceLevelName, levelName, StringComparison.OrdinalIgnoreCase))
                    {
                        return g;
                    }
                }
                else
                {
                    if (string.Equals(g.SourceLevelName, levelName, StringComparison.OrdinalIgnoreCase))
                    {
                        return g;
                    }
                }
            }
            return null;
        }

        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}
```

### `ZoningFloorArea\Models\LevelCreationItem.cs`
```csharp
using System.ComponentModel;

namespace ZoningFloorArea.Models
{
    public class LevelCreationItem : INotifyPropertyChanged
    {
        private int _index;
        public int Index
        {
            get { return _index; }
            set
            {
                if (_index != value)
                {
                    _index = value;
                    OnPropertyChanged("Index");
                }
            }
        }

        private string _levelName;
        public string LevelName
        {
            get { return _levelName; }
            set
            {
                if (_levelName != value)
                {
                    _levelName = value;
                    OnPropertyChanged("LevelName");
                }
            }
        }

        private double _elevationFeet;
        public double ElevationFeet
        {
            get { return _elevationFeet; }
            set
            {
                if (_elevationFeet != value)
                {
                    _elevationFeet = value;
                    OnPropertyChanged("ElevationFeet");
                }
            }
        }

        private string _elevationDisplay;
        public string ElevationDisplay
        {
            get { return _elevationDisplay; }
            set
            {
                if (_elevationDisplay != value)
                {
                    _elevationDisplay = value;
                    OnPropertyChanged("ElevationDisplay");
                }
            }
        }

        private string _levelType;
        public string LevelType
        {
            get { return _levelType; }
            set
            {
                if (_levelType != value)
                {
                    _levelType = value;
                    OnPropertyChanged("LevelType");
                }
            }
        }

        private bool _isIncluded;
        public bool IsIncluded
        {
            get { return _isIncluded; }
            set
            {
                if (_isIncluded != value)
                {
                    _isIncluded = value;
                    OnPropertyChanged("IsIncluded");
                }
            }
        }

        private bool _createFloorPlan;
        public bool CreateFloorPlan
        {
            get { return _createFloorPlan; }
            set
            {
                if (_createFloorPlan != value)
                {
                    _createFloorPlan = value;
                    OnPropertyChanged("CreateFloorPlan");
                }
            }
        }

        private bool _createCeilingPlan;
        public bool CreateCeilingPlan
        {
            get { return _createCeilingPlan; }
            set
            {
                if (_createCeilingPlan != value)
                {
                    _createCeilingPlan = value;
                    OnPropertyChanged("CreateCeilingPlan");
                }
            }
        }

        public LevelCreationItem()
        {
            _levelName = string.Empty;
            _elevationDisplay = string.Empty;
            _levelType = "Typical";
            _isIncluded = true;
            _createFloorPlan = true;
            _createCeilingPlan = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

```

### `ZoningFloorArea\Models\LevelRenameItem.cs`
```csharp
using System.ComponentModel;
using Autodesk.Revit.DB;

namespace ZoningFloorArea.Models
{
    public class LevelRenameItem : INotifyPropertyChanged
    {
        public Level LevelElement { get; private set; }
        public ElementId LevelId
        {
            get { return LevelElement.Id; }
        }

        public double RawElevation
        {
            get { return LevelElement.Elevation; }
        }
        public string ElevationDisplay { get; set; }

        public string CurrentName
        {
            get { return LevelElement.Name; }
        }

        private string _proposedName = string.Empty;
        public string ProposedName
        {
            get { return _proposedName; }
            set
            {
                if (_proposedName != value)
                {
                    _proposedName = value;
                    OnPropertyChanged("ProposedName");
                    OnPropertyChanged("IsChanged");
                }
            }
        }

        public bool IsChanged
        {
            get { return !string.Equals(CurrentName, ProposedName); }
        }

        private bool _isSelected = true;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        public LevelRenameItem(Level level, string elevationFormatted)
        {
            LevelElement = level;
            ElevationDisplay = elevationFormatted;
            _proposedName = level.Name;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

```

### `ZoningFloorArea\Models\LevelZoningRow.cs`
```csharp
using System;
using System.Collections.Generic;

namespace ZoningFloorArea.Models
{
    public class LevelZoningRow
    {
        public string LevelName { get; set; }
        public double LevelElevation { get; set; }
        public string UsageCategory { get; set; }
        public string GroupName { get; set; }
        public string GroupColorHex { get; set; }

        public double GrossFloorArea { get; set; }
        public Dictionary<string, double> Deductions { get; set; }
        public double TotalDeductions { get; set; }

        public double NetArea
        {
            get { return Math.Max(0, GrossFloorArea - TotalDeductions); }
        }

        public double UlebPercent { get; set; }

        public double UlebAmount
        {
            get { return NetArea * UlebPercent; }
        }

        public double ZoningFloorArea
        {
            get { return Math.Max(0, NetArea - UlebAmount); }
        }

        public double LotArea { get; set; }

        public double Far
        {
            get { return LotArea > 0 ? ZoningFloorArea / LotArea : 0; }
        }

        public double this[string categoryName]
        {
            get { return GetDeduction(categoryName); }
        }

        public LevelZoningRow()
        {
            LevelName = string.Empty;
            UsageCategory = "Residential";
            GroupName = string.Empty;
            GroupColorHex = "#94A3B8";
            Deductions = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            UlebPercent = 0.05;
            LotArea = 1.0;
        }

        public double GetDeduction(string categoryName)
        {
            double val;
            if (Deductions.TryGetValue(categoryName, out val))
                return val;
            return 0.0;
        }

        public void SetDeduction(string categoryName, double val)
        {
            Deductions[categoryName] = val;
            RecalculateTotalDeductions();
        }

        public void RecalculateTotalDeductions()
        {
            double sum = 0;
            foreach (KeyValuePair<string, double> kvp in Deductions)
            {
                sum += kvp.Value;
            }
            TotalDeductions = sum;
        }
    }
}

```

### `ZoningFloorArea\Models\MappingConfig.cs`
```csharp
namespace ZoningFloorArea.Models
{
    public enum UnitDisplayMode
    {
        SquareFeet,
        SquareMeters
    }

    public class MappingConfig
    {
        public string GrossAreaSchemeName { get; set; }
        public string DeductionAreaSchemeName { get; set; }
        public string DeductionTypeParameterName { get; set; }
        public string UsageCategoryParameterName { get; set; }
        public string BuildingParameterName { get; set; }
        public string MasterScopeBoxName { get; set; }
        public string ViewBuildingParameterName { get; set; }

        public string BuildingName { get; set; }
        public double LotArea { get; set; }
        public double UlebPercent { get; set; }

        public UnitDisplayMode DisplayUnit { get; set; }

        public MappingConfig()
        {
            GrossAreaSchemeName = "Gross Building";
            DeductionAreaSchemeName = "Rentable";
            DeductionTypeParameterName = "Deduction";
            UsageCategoryParameterName = "Comments";
            BuildingParameterName = "Building";
            MasterScopeBoxName = "";
            ViewBuildingParameterName = "Building";

            BuildingName = "BUILDING C";
            LotArea = 34500.0;
            UlebPercent = 0.05;
            DisplayUnit = UnitDisplayMode.SquareFeet;
        }
    }
}

```

### `ZoningFloorArea\Models\NeuralGenerativeModel.cs`
```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ZoningFloorArea.Models
{
    public enum MassFloorUsage
    {
        CommercialPodium,
        DormerSetbackTransition,
        TypicalResidential,
        InclusionaryHousing,
        PenthouseLuxury,
        RoofTerrace
    }

    public enum BuildingTypology
    {
        PodiumCentralTower,
        SteppedWeddingCake,
        SlenderPencilTower,
        LShapedCourtyard,
        TwinTowers
    }

    public class MassingFloorBlock
    {
        public int LevelIndex { get; set; }
        public string LevelName { get; set; }
        public double ElevationFt { get; set; }
        public double HeightFt { get; set; }
        public double WidthFt { get; set; }
        public double DepthFt { get; set; }
        public double OffsetXFt { get; set; }
        public double OffsetYFt { get; set; }
        public double AreaSqFt { get { return WidthFt * DepthFt; } }
        public MassFloorUsage UsageType { get; set; }
        public string ColorHex { get; set; }

        public MassingFloorBlock()
        {
            ColorHex = "#3B82F6";
        }
    }

    public class GenerativeScenario : INotifyPropertyChanged
    {
        private bool _isSelectedForBake;

        public string Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Icon { get; set; }
        public string ColorHex { get; set; }
        
        public bool IsSelectedForBake
        {
            get { return _isSelectedForBake; }
            set { _isSelectedForBake = value; OnPropertyChanged("IsSelectedForBake"); }
        }

        public double TotalZfa { get; set; }
        public double FarUtilizationPercent { get; set; }
        public double HighFloorPercentage { get; set; }
        public int MihUnitsEstimate { get; set; }
        public double EstimatedFacadeArea { get; set; }
        public double EstimatedRevenueMillions { get; set; }
        public int TotalFloors { get; set; }
        public int PodiumFloors { get; set; }
        public int DormerFloors { get; set; }
        public int TowerFloors { get; set; }
        public double TotalHeightFt { get; set; }
        public bool IsHeightExceeded { get; set; }

        public List<MassingFloorBlock> Floors { get; set; }

        public GenerativeScenario()
        {
            Floors = new List<MassingFloorBlock>();
            ColorHex = "#3B82F6";
            _isSelectedForBake = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class GenerativeInputParameters : INotifyPropertyChanged
    {
        private double _lotAreaSqFt;
        private double _lotWidthFt;
        private double _lotDepthFt;
        private double _baseFar;
        private double _maxHeightFt;
        private double _setbackFrontFt;
        private double _setbackRearFt;
        private double _setbackSidesFt;
        private double _mihPercent;
        private double _floorHeightPodium;
        private double _floorHeightTower;

        // Dynamic 3D Morphing Sliders
        private BuildingTypology _selectedTypology;
        private int _podiumFloors;
        private double _podiumCoveragePercent;
        private double _towerCoveragePercent;
        private int _dormerFloors;
        private double _dormerSetbackDepthFt;
        private int _penthouseFloors;

        public double LotAreaSqFt
        {
            get { return _lotAreaSqFt; }
            set { _lotAreaSqFt = value; OnPropertyChanged("LotAreaSqFt"); }
        }

        public double LotWidthFt
        {
            get { return _lotWidthFt; }
            set { _lotWidthFt = value; OnPropertyChanged("LotWidthFt"); }
        }

        public double LotDepthFt
        {
            get { return _lotDepthFt; }
            set { _lotDepthFt = value; OnPropertyChanged("LotDepthFt"); }
        }

        public double BaseFar
        {
            get { return _baseFar; }
            set { _baseFar = value; OnPropertyChanged("BaseFar"); }
        }

        public double MaxHeightFt
        {
            get { return _maxHeightFt; }
            set { _maxHeightFt = value; OnPropertyChanged("MaxHeightFt"); }
        }

        public double SetbackFrontFt
        {
            get { return _setbackFrontFt; }
            set { _setbackFrontFt = value; OnPropertyChanged("SetbackFrontFt"); }
        }

        public double SetbackRearFt
        {
            get { return _setbackRearFt; }
            set { _setbackRearFt = value; OnPropertyChanged("SetbackRearFt"); }
        }

        public double SetbackSidesFt
        {
            get { return _setbackSidesFt; }
            set { _setbackSidesFt = value; OnPropertyChanged("SetbackSidesFt"); }
        }

        public double MihPercent
        {
            get { return _mihPercent; }
            set { _mihPercent = value; OnPropertyChanged("MihPercent"); }
        }

        public double FloorHeightPodium
        {
            get { return _floorHeightPodium; }
            set { _floorHeightPodium = value; OnPropertyChanged("FloorHeightPodium"); }
        }

        public double FloorHeightTower
        {
            get { return _floorHeightTower; }
            set { _floorHeightTower = value; OnPropertyChanged("FloorHeightTower"); }
        }

        public BuildingTypology SelectedTypology
        {
            get { return _selectedTypology; }
            set { _selectedTypology = value; OnPropertyChanged("SelectedTypology"); }
        }

        public int PodiumFloors
        {
            get { return _podiumFloors; }
            set { _podiumFloors = value; OnPropertyChanged("PodiumFloors"); }
        }

        public double PodiumCoveragePercent
        {
            get { return _podiumCoveragePercent; }
            set { _podiumCoveragePercent = value; OnPropertyChanged("PodiumCoveragePercent"); }
        }

        public double TowerCoveragePercent
        {
            get { return _towerCoveragePercent; }
            set { _towerCoveragePercent = value; OnPropertyChanged("TowerCoveragePercent"); }
        }

        public int DormerFloors
        {
            get { return _dormerFloors; }
            set { _dormerFloors = value; OnPropertyChanged("DormerFloors"); }
        }

        public double DormerSetbackDepthFt
        {
            get { return _dormerSetbackDepthFt; }
            set { _dormerSetbackDepthFt = value; OnPropertyChanged("DormerSetbackDepthFt"); }
        }

        public int PenthouseFloors
        {
            get { return _penthouseFloors; }
            set { _penthouseFloors = value; OnPropertyChanged("PenthouseFloors"); }
        }

        public GenerativeInputParameters()
        {
            _lotAreaSqFt = 15000.0;
            _lotWidthFt = 150.0;
            _lotDepthFt = 100.0;
            _baseFar = 10.0;
            _maxHeightFt = 250.0;
            _setbackFrontFt = 15.0;
            _setbackRearFt = 20.0;
            _setbackSidesFt = 10.0;
            _mihPercent = 25.0;
            _floorHeightPodium = 15.0;
            _floorHeightTower = 11.0;

            _selectedTypology = BuildingTypology.PodiumCentralTower;
            _podiumFloors = 3;
            _podiumCoveragePercent = 80.0;
            _towerCoveragePercent = 45.0;
            _dormerFloors = 2;
            _dormerSetbackDepthFt = 12.0;
            _penthouseFloors = 2;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
```

### `ZoningFloorArea\Models\NycLotInfo.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace ZoningFloorArea.Models
{
    public enum LotElementType
    {
        ModelCurves = 0,
        DetailLines = 1,
        RoomSeparators = 2,
        AreaBoundaryLines = 3
    }

    public enum LotAnchorCorner
    {
        Southwest = 0,
        Northwest = 1,
        Southeast = 2,
        Northeast = 3,
        Center = 4
    }

    public enum LotGroupingMode
    {
        SingleGroup = 0,
        SplitSubjectAndContext = 1,
        NoGrouping = 2,
        SplitGroups = 1,
        NoGroup = 2
    }

    public class NycSearchResult
    {
        public string Label { get; set; }
        public string Borough { get; set; }
        public string Block { get; set; }
        public string Lot { get; set; }
        public string Bbl { get; set; }
        public string Address { get; set; }
        public string HouseNumber { get; set; }
        public string Street { get; set; }
        public string PostalCode { get; set; }

        public NycSearchResult()
        {
            Label = string.Empty;
            Borough = string.Empty;
            Block = string.Empty;
            Lot = string.Empty;
            Bbl = string.Empty;
            Address = string.Empty;
            HouseNumber = string.Empty;
            Street = string.Empty;
            PostalCode = string.Empty;
        }

        public override string ToString()
        {
            return Label;
        }
    }

    public class NycBuildingFootprint
    {
        public string Bin { get; set; }
        public string Address { get; set; }
        public double HeightRoofFt { get; set; }
        public double GroundElevationFt { get; set; }
        public int NumFloors { get; set; }
        public int YearBuilt { get; set; }
        public bool IsSubjectBuilding { get; set; }
        public bool IsSubjectLotBuilding { get { return IsSubjectBuilding; } set { IsSubjectBuilding = value; } }
        public List<List<XYZ>> PolygonRings { get; set; }

        public double EffectiveHeightFt
        {
            get
            {
                if (HeightRoofFt > 5.0) return HeightRoofFt;
                if (NumFloors > 0) return NumFloors * 11.5;
                return 30.0;
            }
        }

        public NycBuildingFootprint()
        {
            Bin = string.Empty;
            Address = string.Empty;
            PolygonRings = new List<List<XYZ>>();
        }
    }

    public class NycLotInfo
    {
        public string Bbl { get; set; }
        public string Borough { get; set; }
        public string Block { get; set; }
        public string Lot { get; set; }
        public string Address { get; set; }
        public string ZipCode { get; set; }

        public double LotAreaSqFt { get; set; }
        public double TotalBldgAreaSqFt { get; set; }
        public double ResAreaSqFt { get; set; }
        public double ComAreaSqFt { get; set; }
        public double OfficeAreaSqFt { get; set; }
        public double RetailAreaSqFt { get; set; }
        public double GarageAreaSqFt { get; set; }
        public double StorageAreaSqFt { get; set; }
        public double FactoryAreaSqFt { get; set; }
        public double OtherAreaSqFt { get; set; }

        public int NumFloors { get; set; }
        public int NumBuildings { get; set; }
        public int YearBuilt { get; set; }

        public double BuiltFar { get; set; }
        public double ResFar { get; set; }
        public double ResidFar { get { return ResFar; } set { ResFar = value; } }
        public double CommFar { get; set; }
        public double FacilFar { get; set; }

        public double LotFrontageFt { get { return WidthFt; } set { } }
        public double LotDepthFt { get { return DepthFt; } set { } }
        public double BldgAreaSqFt { get { return TotalBldgAreaSqFt; } set { TotalBldgAreaSqFt = value; } }

        public string ZoningDistrict1 { get; set; }
        public string ZoningDistrict2 { get; set; }
        public string CommercialOverlay1 { get; set; }
        public string CommercialOverlay2 { get; set; }
        public string SpecialDistrict1 { get; set; }
        public string SpecialDistrict2 { get; set; }

        public string LandUse { get; set; }
        public string OwnerName { get; set; }
        public string BuildingClass { get; set; }

        public bool IsAdjacent { get; set; }
        public List<List<XYZ>> PolygonRings { get; set; }

        public double MinX
        {
            get
            {
                var pts = PolygonRings.SelectMany(r => r).ToList();
                return pts.Count > 0 ? pts.Min(p => p.X) : 0.0;
            }
        }
        public double MaxX
        {
            get
            {
                var pts = PolygonRings.SelectMany(r => r).ToList();
                return pts.Count > 0 ? pts.Max(p => p.X) : 0.0;
            }
        }
        public double MinY
        {
            get
            {
                var pts = PolygonRings.SelectMany(r => r).ToList();
                return pts.Count > 0 ? pts.Min(p => p.Y) : 0.0;
            }
        }
        public double MaxY
        {
            get
            {
                var pts = PolygonRings.SelectMany(r => r).ToList();
                return pts.Count > 0 ? pts.Max(p => p.Y) : 0.0;
            }
        }

        public double WidthFt { get { return Math.Max(0, MaxX - MinX); } }
        public double DepthFt { get { return Math.Max(0, MaxY - MinY); } }

        public NycLotInfo()
        {
            Bbl = string.Empty;
            Borough = string.Empty;
            Address = string.Empty;
            ZipCode = string.Empty;
            ZoningDistrict1 = string.Empty;
            ZoningDistrict2 = string.Empty;
            CommercialOverlay1 = string.Empty;
            CommercialOverlay2 = string.Empty;
            SpecialDistrict1 = string.Empty;
            SpecialDistrict2 = string.Empty;
            LandUse = string.Empty;
            OwnerName = string.Empty;
            BuildingClass = string.Empty;
            PolygonRings = new List<List<XYZ>>();
        }

        public XYZ GetAnchorPoint(LotAnchorCorner corner)
        {
            switch (corner)
            {
                case LotAnchorCorner.Southwest:
                    return new XYZ(MinX, MinY, 0);
                case LotAnchorCorner.Northwest:
                    return new XYZ(MinX, MaxY, 0);
                case LotAnchorCorner.Southeast:
                    return new XYZ(MaxX, MinY, 0);
                case LotAnchorCorner.Northeast:
                    return new XYZ(MaxX, MaxY, 0);
                case LotAnchorCorner.Center:
                    return new XYZ((MinX + MaxX) / 2.0, (MinY + MaxY) / 2.0, 0);
                default:
                    return new XYZ(MinX, MinY, 0);
            }
        }

        public string GetZoningSummary()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(ZoningDistrict1)) parts.Add(ZoningDistrict1);
            if (!string.IsNullOrWhiteSpace(ZoningDistrict2)) parts.Add(ZoningDistrict2);
            if (!string.IsNullOrWhiteSpace(CommercialOverlay1)) parts.Add(string.Format("Overlay: {0}", CommercialOverlay1));
            if (!string.IsNullOrWhiteSpace(SpecialDistrict1)) parts.Add(string.Format("Special: {0}", SpecialDistrict1));
            return parts.Count > 0 ? string.Join(" / ", parts.ToArray()) : "N/A";
        }
    }

    public class NycBlockContext
    {
        public string Borough { get; set; }
        public string BlockNumber { get; set; }
        public NycLotInfo SubjectLot { get; set; }
        public List<NycLotInfo> OtherLots { get; set; }
        public List<NycBuildingFootprint> Buildings { get; set; }

        public List<NycLotInfo> AdjacentLots
        {
            get { return OtherLots.Where(l => l.IsAdjacent).ToList(); }
        }

        public List<NycLotInfo> RemainingBlockLots
        {
            get { return OtherLots.Where(l => !l.IsAdjacent && l.Bbl != SubjectLot.Bbl).ToList(); }
        }

        public List<NycLotInfo> AllLots
        {
            get
            {
                var list = new List<NycLotInfo>();
                if (SubjectLot != null) list.Add(SubjectLot);
                if (OtherLots != null) list.AddRange(OtherLots.Where(l => SubjectLot == null || l.Bbl != SubjectLot.Bbl));
                return list;
            }
        }

        public double MinX
        {
            get { var lots = AllLots; return lots.Count > 0 ? lots.Min(l => l.MinX) : 0.0; }
        }
        public double MaxX
        {
            get { var lots = AllLots; return lots.Count > 0 ? lots.Max(l => l.MaxX) : 0.0; }
        }
        public double MinY
        {
            get { var lots = AllLots; return lots.Count > 0 ? lots.Min(l => l.MinY) : 0.0; }
        }
        public double MaxY
        {
            get { var lots = AllLots; return lots.Count > 0 ? lots.Max(l => l.MaxY) : 0.0; }
        }

        public double WidthFt { get { return Math.Max(0, MaxX - MinX); } }
        public double DepthFt { get { return Math.Max(0, MaxY - MinY); } }

        public NycBlockContext()
        {
            Borough = string.Empty;
            BlockNumber = string.Empty;
            SubjectLot = new NycLotInfo();
            OtherLots = new List<NycLotInfo>();
            Buildings = new List<NycBuildingFootprint>();
        }

        public void CalculateAdjacency(double toleranceFt)
        {
            if (SubjectLot == null || SubjectLot.PolygonRings.Count == 0) return;

            var subjectPoints = SubjectLot.PolygonRings.SelectMany(r => r).ToList();

            foreach (var other in OtherLots)
            {
                if (other.Bbl == SubjectLot.Bbl)
                {
                    other.IsAdjacent = false;
                    continue;
                }

                bool isAdj = false;
                foreach (var ring in other.PolygonRings)
                {
                    foreach (var pt in ring)
                    {
                        foreach (var subPt in subjectPoints)
                        {
                            double dist = Math.Sqrt(Math.Pow(pt.X - subPt.X, 2) + Math.Pow(pt.Y - subPt.Y, 2));
                            if (dist <= toleranceFt)
                            {
                                isAdj = true;
                                break;
                            }
                        }
                        if (isAdj) break;
                    }
                    if (isAdj) break;
                }
                other.IsAdjacent = isAdj;
            }
        }

        public void CalculateAdjacency()
        {
            CalculateAdjacency(1.0);
        }

        public Dictionary<string, string> GetSurroundingStreetNames()
        {
            var dict = new Dictionary<string, string>();
            var addresses = AllLots.Select(l => l.Address).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList();
            if (addresses.Count > 0)
            {
                dict["Streets"] = string.Join(", ", addresses.Take(4).ToArray());
            }
            return dict;
        }
    }
}
```

### `ZoningFloorArea\Models\ProjectZoningResult.cs`
```csharp
using System.Collections.Generic;

namespace ZoningFloorArea.Models
{
    public class ProjectZoningResult
    {
        public string ProjectName { get; set; }
        public double LotArea { get; set; }
        public double UlebPercent { get; set; }

        public List<ZoningTableResult> BuildingTables { get; set; }
        public ZoningTableResult OverallSummary { get; set; }

        public double TotalProjectZoningFloorArea
        {
            get
            {
                double sum = 0;
                if (BuildingTables != null)
                {
                    foreach (ZoningTableResult table in BuildingTables)
                    {
                        sum += table.TotalZoningFloorArea;
                    }
                }
                return sum;
            }
        }

        public double TotalProjectFar
        {
            get { return LotArea > 0 ? TotalProjectZoningFloorArea / LotArea : 0; }
        }

        public ProjectZoningResult()
        {
            ProjectName = "PROJECT ZONING SUMMARY";
            LotArea = 34500.0;
            UlebPercent = 0.05;

            BuildingTables = new List<ZoningTableResult>();
            OverallSummary = new ZoningTableResult();
            OverallSummary.BuildingName = "ALL BUILDINGS TOTAL";
        }
    }
}

```

### `ZoningFloorArea\Models\SheetCompositionModel.cs`
```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace ZoningFloorArea.Models
{
    public enum SheetLayoutMode
    {
        Single1View = 1,
        Dual2Views = 2,
        Triple3Views = 3,
        Quad4Views = 4,
        Hex6Views = 6,
        Octo8Views = 8
    }

    public enum ViewPlanKind
    {
        FloorPlan = 0,    // Standard Architectural Floor Plan
        AreaPlan = 1,     // Area Plan (associated with an AreaScheme)
        CeilingPlan = 2   // Reflected Ceiling Plan (RCP)
    }

    public enum ViewPackageType
    {
        MasterOverall = 0,
        Architectural = 1,
        CeilingPlanRCP = 2,
        GrossArea = 3,
        Deductions = 4,
        EgressLifeSafety = 5,
        Custom = 6
    }

    public class TitleblockItem
    {
        public string Name { get; set; }
        public ElementId FamilySymbolId { get; set; }
        public double WidthInches { get; set; }
        public double HeightInches { get; set; }
        public double UsableWidthInches { get; set; }
        public double UsableHeightInches { get; set; }

        public TitleblockItem()
        {
            WidthInches = 36.0;
            HeightInches = 24.0;
            UsableWidthInches = 31.0;
            UsableHeightInches = 22.0;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class ViewTemplateItem
    {
        public string Name { get; set; }
        public ElementId TemplateId { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class PackageSetting
    {
        public ViewPackageType PackageType { get; set; }
        public ViewPlanKind ViewKind { get; set; }
        public string SelectedAreaSchemeName { get; set; }
        public ElementId SelectedAreaSchemeId { get; set; }
        public string DisplayName { get; set; }
        public string Icon { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsCustomPackage { get; set; }
        public string SheetPrefix { get; set; }
        public int StartNumber { get; set; }
        public string ViewTemplateName { get; set; }
        public ElementId SelectedTemplateId { get; set; }
        public SheetLayoutMode LayoutMode { get; set; }
        public int ScaleValue { get; set; }
        public string ScaleDisplay { get; set; }
        public string RecommendedScaleDisplay { get; set; }
        public bool IncludeSummaryTableOnSheet { get; set; }

        public PackageSetting(ViewPackageType type, string name, string icon, string prefix, int startNum, SheetLayoutMode defaultLayout, int defaultScale, string scaleDisp, ViewPlanKind viewKind = ViewPlanKind.FloorPlan, string defaultScheme = "")
        {
            PackageType = type;
            ViewKind = viewKind;
            SelectedAreaSchemeName = defaultScheme ?? string.Empty;
            SelectedAreaSchemeId = ElementId.InvalidElementId;
            DisplayName = name;
            Icon = icon;
            IsEnabled = true;
            SheetPrefix = prefix;
            StartNumber = startNum;
            ViewTemplateName = "(None)";
            SelectedTemplateId = ElementId.InvalidElementId;
            LayoutMode = defaultLayout;
            ScaleValue = defaultScale;
            ScaleDisplay = scaleDisp;
            RecommendedScaleDisplay = scaleDisp;
            IncludeSummaryTableOnSheet = (type == ViewPackageType.GrossArea || type == ViewPackageType.Deductions);
        }
    }

    public class PlannedViewport
    {
        public string LevelName { get; set; }
        public string LevelRangeLabel { get; set; }
        public string BuildingName { get; set; }
        public string ScopeBoxName { get; set; }
        public string ViewName { get; set; }
        public string FormattedTitleOnSheet { get; set; }
        public ViewPackageType PackageType { get; set; }
        public ViewPlanKind ViewKind { get; set; }
        public string AreaSchemeName { get; set; }
        public int GridIndex { get; set; } // 0 to 7
        public ElementId ExistingViewId { get; set; }

        public PlannedViewport()
        {
            ViewKind = ViewPlanKind.FloorPlan;
            AreaSchemeName = string.Empty;
            GridIndex = 0;
            ExistingViewId = ElementId.InvalidElementId;
        }
    }

    public class PlannedSheet
    {
        public string SheetNumber { get; set; }
        public string SheetName { get; set; }
        public string BuildingName { get; set; }
        public string ScopeBoxName { get; set; }
        public ViewPackageType PackageType { get; set; }
        public SheetLayoutMode LayoutMode { get; set; }
        public int ScaleValue { get; set; }
        public string ScaleDisplay { get; set; }
        public bool HasSummaryTable { get; set; }
        public List<PlannedViewport> Viewports { get; set; }

        public PlannedSheet()
        {
            Viewports = new List<PlannedViewport>();
            LayoutMode = SheetLayoutMode.Quad4Views;
            ScaleValue = 96;
            ScaleDisplay = "1/8\" = 1'-0\"";
            HasSummaryTable = false;
        }

        public string Summary
        {
            get
            {
                return string.Format("{0} - {1} ({2} View(s) @ {3})", SheetNumber, SheetName, Viewports.Count, ScaleDisplay);
            }
        }
    }
}
```

### `ZoningFloorArea\Models\TypicalFloorGroup.cs`
```csharp
using System;
using System.ComponentModel;

namespace ZoningFloorArea.Models
{
    public class TypicalFloorGroup : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _colorHex;
        private string _sourceLevelName;
        private string _sourceLevelNameLower;
        private string _sourceLevelNameUpper;
        private string _fromLevelName;
        private string _toLevelName;
        private bool _isSingleFloorOnly;
        private bool _isDuplexModule;
        private int _order;

        public event PropertyChangedEventHandler PropertyChanged;

        public TypicalFloorGroup()
        {
            _id = Guid.NewGuid().ToString();
            _name = "Typical Floor";
            _colorHex = "#3B82F6";
            _sourceLevelName = string.Empty;
            _sourceLevelNameLower = string.Empty;
            _sourceLevelNameUpper = string.Empty;
            _fromLevelName = string.Empty;
            _toLevelName = string.Empty;
            _isSingleFloorOnly = false;
            _isDuplexModule = false;
            _order = 1;
        }

        public string Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged("Id");
                }
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public string ColorHex
        {
            get { return _colorHex; }
            set
            {
                if (_colorHex != value)
                {
                    _colorHex = value;
                    OnPropertyChanged("ColorHex");
                }
            }
        }

        public string SourceLevelName
        {
            get { return _sourceLevelName; }
            set
            {
                if (_sourceLevelName != value)
                {
                    _sourceLevelName = value;
                    OnPropertyChanged("SourceLevelName");
                    if (_isSingleFloorOnly)
                    {
                        FromLevelName = value;
                        ToLevelName = value;
                    }
                }
            }
        }

        public string SourceLevelNameLower
        {
            get { return _sourceLevelNameLower; }
            set
            {
                if (_sourceLevelNameLower != value)
                {
                    _sourceLevelNameLower = value;
                    OnPropertyChanged("SourceLevelNameLower");
                }
            }
        }

        public string SourceLevelNameUpper
        {
            get { return _sourceLevelNameUpper; }
            set
            {
                if (_sourceLevelNameUpper != value)
                {
                    _sourceLevelNameUpper = value;
                    OnPropertyChanged("SourceLevelNameUpper");
                }
            }
        }

        public bool IsDuplexModule
        {
            get { return _isDuplexModule; }
            set
            {
                if (_isDuplexModule != value)
                {
                    _isDuplexModule = value;
                    OnPropertyChanged("IsDuplexModule");
                }
            }
        }

        public string FromLevelName
        {
            get { return _fromLevelName; }
            set
            {
                if (_fromLevelName != value)
                {
                    _fromLevelName = value;
                    OnPropertyChanged("FromLevelName");
                    OnPropertyChanged("IsSingleLevel");
                }
            }
        }

        public string ToLevelName
        {
            get { return _toLevelName; }
            set
            {
                if (_toLevelName != value)
                {
                    _toLevelName = value;
                    OnPropertyChanged("ToLevelName");
                    OnPropertyChanged("IsSingleLevel");
                }
            }
        }

        public bool IsSingleFloorOnly
        {
            get { return _isSingleFloorOnly; }
            set
            {
                if (_isSingleFloorOnly != value)
                {
                    _isSingleFloorOnly = value;
                    if (value && !string.IsNullOrEmpty(_sourceLevelName))
                    {
                        _fromLevelName = _sourceLevelName;
                        _toLevelName = _sourceLevelName;
                        OnPropertyChanged("FromLevelName");
                        OnPropertyChanged("ToLevelName");
                    }
                    OnPropertyChanged("IsSingleFloorOnly");
                    OnPropertyChanged("IsSingleLevel");
                }
            }
        }

        public int Order
        {
            get { return _order; }
            set
            {
                if (_order != value)
                {
                    _order = value;
                    OnPropertyChanged("Order");
                }
            }
        }

        public bool IsSingleLevel
        {
            get
            {
                if (_isSingleFloorOnly) return true;
                return !string.IsNullOrEmpty(_fromLevelName) && 
                       string.Equals(_fromLevelName, _toLevelName, StringComparison.OrdinalIgnoreCase);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

```

### `ZoningFloorArea\Models\ZoningComplianceModel.cs`
```csharp
using System;
using System.ComponentModel;

namespace ZoningFloorArea.Models
{
    public class ZoningLotData : INotifyPropertyChanged
    {
        private string _projectName;
        private string _address;
        private string _blockLot;
        private string _zoningDistrict;
        private string _lotType;
        private double _lotAreaSqFt;
        private double _lotWidthFt;
        private double _lotDepthFt;
        private double _baseResidentialFar;
        private double _baseCommercialFar;
        private double _baseCommunityFacilityFar;
        private double _inclusionaryBonusFar;
        private double _otherBonusFar;
        private double _maxBuildingHeightFt;

        public string ProjectName
        {
            get { return _projectName ?? "My Building Project"; }
            set { _projectName = value; OnPropertyChanged("ProjectName"); }
        }

        public string Address
        {
            get { return _address ?? ""; }
            set { _address = value; OnPropertyChanged("Address"); }
        }

        public string BlockLot
        {
            get { return _blockLot ?? ""; }
            set { _blockLot = value; OnPropertyChanged("BlockLot"); }
        }

        public string ZoningDistrict
        {
            get { return _zoningDistrict ?? "R10"; }
            set { _zoningDistrict = value; OnPropertyChanged("ZoningDistrict"); }
        }

        public string LotType
        {
            get { return _lotType ?? "Corner Lot"; }
            set { _lotType = value; OnPropertyChanged("LotType"); }
        }

        public double LotAreaSqFt
        {
            get { return _lotAreaSqFt; }
            set
            {
                _lotAreaSqFt = value;
                OnPropertyChanged("LotAreaSqFt");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
        }

        public double LotWidthFt
        {
            get { return _lotWidthFt; }
            set { _lotWidthFt = value; OnPropertyChanged("LotWidthFt"); }
        }

        public double LotDepthFt
        {
            get { return _lotDepthFt; }
            set { _lotDepthFt = value; OnPropertyChanged("LotDepthFt"); }
        }

        public double BaseResidentialFar
        {
            get { return _baseResidentialFar; }
            set
            {
                _baseResidentialFar = value;
                OnPropertyChanged("BaseResidentialFar");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
        }

        public double BaseCommercialFar
        {
            get { return _baseCommercialFar; }
            set
            {
                _baseCommercialFar = value;
                OnPropertyChanged("BaseCommercialFar");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
        }

        public double BaseCommunityFacilityFar
        {
            get { return _baseCommunityFacilityFar; }
            set
            {
                _baseCommunityFacilityFar = value;
                OnPropertyChanged("BaseCommunityFacilityFar");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
        }

        public double InclusionaryBonusFar
        {
            get { return _inclusionaryBonusFar; }
            set
            {
                _inclusionaryBonusFar = value;
                OnPropertyChanged("InclusionaryBonusFar");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
        }

        public double OtherBonusFar
        {
            get { return _otherBonusFar; }
            set
            {
                _otherBonusFar = value;
                OnPropertyChanged("OtherBonusFar");
                OnPropertyChanged("TotalAllowableFar");
                OnPropertyChanged("TotalAllowableZfa");
            }
        }

        public double MaxBuildingHeightFt
        {
            get { return _maxBuildingHeightFt; }
            set { _maxBuildingHeightFt = value; OnPropertyChanged("MaxBuildingHeightFt"); }
        }

        public double TotalAllowableFar
        {
            get
            {
                return BaseResidentialFar + BaseCommercialFar + InclusionaryBonusFar + OtherBonusFar;
            }
        }

        public double TotalAllowableZfa
        {
            get
            {
                return LotAreaSqFt * TotalAllowableFar;
            }
        }

        public ZoningLotData()
        {
            _projectName = "My Building Project";
            _zoningDistrict = "R10";
            _lotType = "Corner Lot";
            _lotAreaSqFt = 15000.0;
            _lotWidthFt = 150.0;
            _lotDepthFt = 100.0;
            _baseResidentialFar = 10.0;
            _baseCommercialFar = 0.0;
            _baseCommunityFacilityFar = 10.0;
            _inclusionaryBonusFar = 2.0;
            _otherBonusFar = 0.0;
            _maxBuildingHeightFt = 250.0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ZoningComplianceReport : INotifyPropertyChanged
    {
        private double _allowableZfa;
        private double _proposedZfa;
        private double _remainingZfa;
        private double _utilizationPercent;
        private bool _isOverbuilt;
        private string _statusSummary;
        private string _colorHex;

        public double AllowableZfa
        {
            get { return _allowableZfa; }
            set { _allowableZfa = value; OnPropertyChanged("AllowableZfa"); }
        }

        public double ProposedZfa
        {
            get { return _proposedZfa; }
            set { _proposedZfa = value; OnPropertyChanged("ProposedZfa"); }
        }

        public double RemainingZfa
        {
            get { return _remainingZfa; }
            set { _remainingZfa = value; OnPropertyChanged("RemainingZfa"); }
        }

        public double UtilizationPercent
        {
            get { return _utilizationPercent; }
            set { _utilizationPercent = value; OnPropertyChanged("UtilizationPercent"); }
        }

        public bool IsOverbuilt
        {
            get { return _isOverbuilt; }
            set { _isOverbuilt = value; OnPropertyChanged("IsOverbuilt"); }
        }

        public string StatusSummary
        {
            get { return _statusSummary ?? "Ready to Evaluate"; }
            set { _statusSummary = value; OnPropertyChanged("StatusSummary"); }
        }

        public string ColorHex
        {
            get { return _colorHex ?? "#10B981"; }
            set { _colorHex = value; OnPropertyChanged("ColorHex"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
```

### `ZoningFloorArea\Models\ZoningTableResult.cs`
```csharp
using System.Collections.Generic;

namespace ZoningFloorArea.Models
{
    public class ZoningTableResult
    {
        public string BuildingName { get; set; }
        public double LotArea { get; set; }
        public double UlebPercent { get; set; }

        public List<string> DeductionCategories { get; set; }
        public List<LevelZoningRow> ResidentialRows { get; set; }
        public List<LevelZoningRow> CommercialRows { get; set; }

        public LevelZoningRow ResidentialSubtotal { get; set; }
        public LevelZoningRow CommercialSubtotal { get; set; }
        public LevelZoningRow GrandTotal { get; set; }

        public double TotalZoningFloorArea
        {
            get
            {
                double resZfa = ResidentialSubtotal != null ? ResidentialSubtotal.ZoningFloorArea : 0;
                double comZfa = CommercialSubtotal != null ? CommercialSubtotal.ZoningFloorArea : 0;
                return resZfa + comZfa;
            }
        }

        public double TotalFar
        {
            get { return LotArea > 0 ? TotalZoningFloorArea / LotArea : 0; }
        }

        public ZoningTableResult()
        {
            BuildingName = "BUILDING C";
            LotArea = 34500.0;
            UlebPercent = 0.05;

            DeductionCategories = new List<string>
            {
                "CHASE WALLS",
                "STAIRS",
                "MECHANICAL",
                "BYCYCLE PARKING",
                "AMENITIES",
                "CORRIDOR",
                "REFUSE"
            };

            ResidentialRows = new List<LevelZoningRow>();
            CommercialRows = new List<LevelZoningRow>();

            ResidentialSubtotal = new LevelZoningRow { LevelName = "SUBTOTAL", UsageCategory = "Residential" };
            CommercialSubtotal = new LevelZoningRow { LevelName = "SUBTOTAL", UsageCategory = "Commercial" };
            GrandTotal = new LevelZoningRow { LevelName = "TOTAL" };
        }
    }
}

```

### `ZoningFloorArea\Properties\AssemblyInfo.cs`
```csharp
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("ZoningFloorArea")]
[assembly: AssemblyDescription("BauTools - Autodesk Revit Add-in for Zoning Floor Area (ZFA) and Deductions Table Generation by Arch Sergio Castro")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("BauTools - Arch Sergio Castro")]
[assembly: AssemblyProduct("BauTools")]
[assembly: AssemblyCopyright("Copyright © BauTools 2026")]
[assembly: AssemblyTrademark("BauTools")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("a7e492b1-5821-4f18-a621-8f9f7438c821")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

```

### `ZoningFloorArea\Services\ExcelExporter.cs`
```csharp
using System;
using System.IO;
using System.Text;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class ExcelExporter
    {
        public static void ExportProjectToExcelXml(ProjectZoningResult project, string filePath)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");

            sb.AppendLine(" <Styles>");
            sb.AppendLine("  <Style ss:ID=\"Default\" ss:Name=\"Normal\">");
            sb.AppendLine("   <Alignment ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Font ss:FontName=\"Arial\" ss:Size=\"9\"/>");
            sb.AppendLine("  </Style>");
            sb.AppendLine("  <Style ss:ID=\"HeaderMain\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Font ss:FontName=\"Arial\" ss:Size=\"11\" ss:Bold=\"1\"/>");
            sb.AppendLine("   <Interior ss:Color=\"#E0E0E0\" ss:Pattern=\"Solid\"/>");
            sb.AppendLine("   <Borders><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/></Borders>");
            sb.AppendLine("  </Style>");
            sb.AppendLine("  <Style ss:ID=\"HeaderSub\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\" ss:WrapText=\"1\"/>");
            sb.AppendLine("   <Font ss:FontName=\"Arial\" ss:Size=\"8\" ss:Bold=\"1\"/>");
            sb.AppendLine("   <Interior ss:Color=\"#F2F2F2\" ss:Pattern=\"Solid\"/>");
            sb.AppendLine("   <Borders>");
            sb.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            sb.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            sb.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            sb.AppendLine("   </Borders>");
            sb.AppendLine("  </Style>");
            sb.AppendLine("  <Style ss:ID=\"CellNum\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <NumberFormat ss:Format=\"#,##0.00\"/>");
            sb.AppendLine("   <Borders>");
            sb.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            sb.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            sb.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            sb.AppendLine("   </Borders>");
            sb.AppendLine("  </Style>");
            sb.AppendLine("  <Style ss:ID=\"CellSubtotal\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Font ss:FontName=\"Arial\" ss:Size=\"9\" ss:Bold=\"1\"/>");
            sb.AppendLine("   <NumberFormat ss:Format=\"#,##0.00\"/>");
            sb.AppendLine("   <Interior ss:Color=\"#E6F0FA\" ss:Pattern=\"Solid\"/>");
            sb.AppendLine("   <Borders>");
            sb.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/>");
            sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"2\"/>");
            sb.AppendLine("   </Borders>");
            sb.AppendLine("  </Style>");
            sb.AppendLine("  <Style ss:ID=\"CellTotal\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Font ss:FontName=\"Arial\" ss:Size=\"9\" ss:Bold=\"1\"/>");
            sb.AppendLine("   <NumberFormat ss:Format=\"#,##0.00\"/>");
            sb.AppendLine("   <Interior ss:Color=\"#CCCCCC\" ss:Pattern=\"Solid\"/>");
            sb.AppendLine("   <Borders>");
            sb.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"2\"/>");
            sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Double\" ss:Weight=\"3\"/>");
            sb.AppendLine("   </Borders>");
            sb.AppendLine("  </Style>");
            sb.AppendLine(" </Styles>");

            // 1. Export Worksheets for Each Individual Building
            foreach (ZoningTableResult bldgTable in project.BuildingTables)
            {
                AppendTableWorksheet(sb, bldgTable, bldgTable.BuildingName);
            }

            // 2. Export Overall Project Summary Worksheet
            AppendTableWorksheet(sb, project.OverallSummary, "PROJECT TOTAL SUMMARY");

            sb.AppendLine("</Workbook>");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        public static void ExportToExcelXml(ZoningTableResult table, string filePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");
            sb.AppendLine(" <Styles>");
            sb.AppendLine("  <Style ss:ID=\"Default\" ss:Name=\"Normal\"><Alignment ss:Vertical=\"Center\"/><Font ss:FontName=\"Arial\" ss:Size=\"9\"/></Style>");
            sb.AppendLine("  <Style ss:ID=\"HeaderMain\"><Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\"/><Font ss:FontName=\"Arial\" ss:Size=\"11\" ss:Bold=\"1\"/><Interior ss:Color=\"#E0E0E0\" ss:Pattern=\"Solid\"/><Borders><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/></Borders></Style>");
            sb.AppendLine("  <Style ss:ID=\"HeaderSub\"><Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\" ss:WrapText=\"1\"/><Font ss:FontName=\"Arial\" ss:Size=\"8\" ss:Bold=\"1\"/><Interior ss:Color=\"#F2F2F2\" ss:Pattern=\"Solid\"/><Borders><Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/></Borders></Style>");
            sb.AppendLine("  <Style ss:ID=\"CellNum\"><Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\"/><NumberFormat ss:Format=\"#,##0.00\"/><Borders><Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/></Borders></Style>");
            sb.AppendLine("  <Style ss:ID=\"CellSubtotal\"><Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\"/><Font ss:FontName=\"Arial\" ss:Size=\"9\" ss:Bold=\"1\"/><NumberFormat ss:Format=\"#,##0.00\"/><Interior ss:Color=\"#E6F0FA\" ss:Pattern=\"Solid\"/><Borders><Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\"/><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"2\"/></Borders></Style>");
            sb.AppendLine("  <Style ss:ID=\"CellTotal\"><Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\"/><Font ss:FontName=\"Arial\" ss:Size=\"9\" ss:Bold=\"1\"/><NumberFormat ss:Format=\"#,##0.00\"/><Interior ss:Color=\"#CCCCCC\" ss:Pattern=\"Solid\"/><Borders><Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"2\"/><Border ss:Position=\"Bottom\" ss:LineStyle=\"Double\" ss:Weight=\"3\"/></Borders></Style>");
            sb.AppendLine(" </Styles>");

            AppendTableWorksheet(sb, table, table.BuildingName);
            sb.AppendLine("</Workbook>");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static void AppendTableWorksheet(StringBuilder sb, ZoningTableResult table, string sheetName)
        {
            string cleanSheetName = sheetName.Replace(":", "_").Replace("\\", "_").Replace("/", "_").Replace("?", "_").Replace("*", "_");
            if (cleanSheetName.Length > 30) cleanSheetName = cleanSheetName.Substring(0, 30);

            sb.AppendLine(string.Format(" <Worksheet ss:Name=\"{0}\">", cleanSheetName));
            sb.AppendLine("  <Table>");

            int totalCols = 2 + table.DeductionCategories.Count + 4 + 4 + 2;
            sb.AppendLine("   <Row ss:Height=\"24\">");
            sb.AppendLine(string.Format("    <Cell ss:MergeAcross=\"{0}\" ss:StyleID=\"HeaderMain\"><Data ss:Type=\"String\">FLOOR AREA CALCULATIONS - {1}</Data></Cell>", totalCols - 1, table.BuildingName.ToUpper()));
            sb.AppendLine("   </Row>");

            int resColSpan = 2 + table.DeductionCategories.Count + 4;
            sb.AppendLine("   <Row ss:Height=\"20\">");
            sb.AppendLine(string.Format("    <Cell ss:MergeAcross=\"{0}\" ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">RESIDENTIAL</Data></Cell>", resColSpan - 1));
            sb.AppendLine("    <Cell ss:MergeAcross=\"3\" ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">COMMERCIAL</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">TOTAL ZONING FLOOR AREA</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">TOTAL FAR</Data></Cell>");
            sb.AppendLine("   </Row>");

            sb.AppendLine("   <Row ss:Height=\"24\">");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">LEVEL</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">GROSS FLOOR AREA</Data></Cell>");

            foreach (string dedCat in table.DeductionCategories)
            {
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">{0}</Data></Cell>", dedCat.ToUpper()));
            }

            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">NET AREA</Data></Cell>");
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">{0}% ULEB</Data></Cell>", (int)(table.UlebPercent * 100)));
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">ZONING FLOOR AREA</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">FAR</Data></Cell>");

            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">GROSS FLOOR AREA</Data></Cell>");
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">{0}% ULEB</Data></Cell>", (int)(table.UlebPercent * 100)));
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">ZONING FLOOR AREA</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">FAR</Data></Cell>");

            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">TOTAL ZFA</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderSub\"><Data ss:Type=\"String\">TOTAL FAR</Data></Cell>");
            sb.AppendLine("   </Row>");

            int rowCount = Math.Max(table.ResidentialRows.Count, table.CommercialRows.Count);

            for (int i = 0; i < rowCount; i++)
            {
                LevelZoningRow rRes = i < table.ResidentialRows.Count ? table.ResidentialRows[i] : new LevelZoningRow();
                LevelZoningRow rCom = i < table.CommercialRows.Count ? table.CommercialRows[i] : new LevelZoningRow();

                double totalZfa = rRes.ZoningFloorArea + rCom.ZoningFloorArea;
                double totalFar = rRes.Far + rCom.Far;

                sb.AppendLine("   <Row ss:Height=\"18\">");
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"String\">{0}</Data></Cell>", rRes.LevelName));
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", rRes.GrossFloorArea));

                foreach (string cat in table.DeductionCategories)
                {
                    double val = rRes.GetDeduction(cat);
                    sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", val));
                }

                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", rRes.NetArea));
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", rRes.UlebAmount));
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", rRes.ZoningFloorArea));
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", rRes.Far));

                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", rCom.GrossFloorArea));
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", rCom.UlebAmount));
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", rCom.ZoningFloorArea));
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", rCom.Far));

                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", totalZfa));
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellNum\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", totalFar));

                sb.AppendLine("   </Row>");
            }

            LevelZoningRow sRes = table.ResidentialSubtotal;
            LevelZoningRow sCom = table.CommercialSubtotal;
            double subTotalZfa = sRes.ZoningFloorArea + sCom.ZoningFloorArea;
            double subTotalFar = sRes.Far + sCom.Far;

            sb.AppendLine("   <Row ss:Height=\"20\">");
            sb.AppendLine("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"String\">SUBTOTAL</Data></Cell>");
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sRes.GrossFloorArea));
            foreach (string cat in table.DeductionCategories)
            {
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sRes.GetDeduction(cat)));
            }
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sRes.NetArea));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sRes.UlebAmount));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sRes.ZoningFloorArea));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sRes.Far));

            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sCom.GrossFloorArea));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sCom.UlebAmount));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sCom.ZoningFloorArea));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sCom.Far));

            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", subTotalZfa));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellSubtotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", subTotalFar));
            sb.AppendLine("   </Row>");

            LevelZoningRow gTot = table.GrandTotal;
            sb.AppendLine("   <Row ss:Height=\"22\">");
            sb.AppendLine("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"String\">TOTAL</Data></Cell>");
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", gTot.GrossFloorArea));
            foreach (string cat in table.DeductionCategories)
            {
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", gTot.GetDeduction(cat)));
            }
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", gTot.NetArea));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", gTot.UlebAmount));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", gTot.ZoningFloorArea));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", gTot.Far));

            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sCom.GrossFloorArea));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sCom.UlebAmount));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sCom.ZoningFloorArea));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", sCom.Far));

            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", table.TotalZoningFloorArea));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"CellTotal\"><Data ss:Type=\"Number\">{0:F2}</Data></Cell>", table.TotalFar));
            sb.AppendLine("   </Row>");

            sb.AppendLine("  </Table>");
            sb.AppendLine(" </Worksheet>");
        }
    }
}

```

### `ZoningFloorArea\Services\ExcelZoningBridgeService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class ExcelZoningBridgeService
    {
        public bool ExportZoningTemplate(string filePath, ZoningLotData lot)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            if (lot == null) lot = new ZoningLotData();

            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\"?>");
                sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
                sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
                sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
                sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
                sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");

                // Styles
                sb.AppendLine(" <Styles>");
                sb.AppendLine("  <Style ss:ID=\"Default\" ss:Name=\"Normal\"><Font ss:FontName=\"Segoe UI\" ss:Size=\"11\" ss:Color=\"#1E293B\"/></Style>");
                sb.AppendLine("  <Style ss:ID=\"HeaderTitle\"><Font ss:FontName=\"Segoe UI\" ss:Size=\"15\" ss:Bold=\"1\" ss:Color=\"#1E40AF\"/><Interior ss:Color=\"#EFF6FF\" ss:Pattern=\"Solid\"/></Style>");
                sb.AppendLine("  <Style ss:ID=\"SectionHeader\"><Font ss:FontName=\"Segoe UI\" ss:Size=\"12\" ss:Bold=\"1\" ss:Color=\"#FFFFFF\"/><Interior ss:Color=\"#3B82F6\" ss:Pattern=\"Solid\"/></Style>");
                sb.AppendLine("  <Style ss:ID=\"FieldLabel\"><Font ss:FontName=\"Segoe UI\" ss:Size=\"10\" ss:Bold=\"1\" ss:Color=\"#475569\"/><Interior ss:Color=\"#F8FAFC\" ss:Pattern=\"Solid\"/><Borders><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#CBD5E1\"/></Borders></Style>");
                sb.AppendLine("  <Style ss:ID=\"FieldValue\"><Font ss:FontName=\"Segoe UI\" ss:Size=\"10\" ss:Color=\"#0F172A\"/><Borders><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#CBD5E1\"/></Borders></Style>");
                sb.AppendLine("  <Style ss:ID=\"FieldFormula\"><Font ss:FontName=\"Segoe UI\" ss:Size=\"10\" ss:Bold=\"1\" ss:Color=\"#15803D\"/><Interior ss:Color=\"#F0FDF4\" ss:Pattern=\"Solid\"/><Borders><Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#86EFAC\"/></Borders></Style>");
                sb.AppendLine(" </Styles>");

                sb.AppendLine(" <Worksheet ss:Name=\"Zoning Lot Input\">");
                sb.AppendLine("  <Table ss:DefaultColumnWidth=\"180\">");
                sb.AppendLine("   <Column ss:Width=\"220\"/>");
                sb.AppendLine("   <Column ss:Width=\"180\"/>");
                sb.AppendLine("   <Column ss:Width=\"280\"/>");

                // Title
                sb.AppendLine("   <Row ss:Height=\"30\">");
                sb.AppendLine("    <Cell ss:MergeAcross=\"2\" ss:StyleID=\"HeaderTitle\"><Data ss:Type=\"String\">BauTools — Project Zoning &amp; Lot Information</Data></Cell>");
                sb.AppendLine("   </Row>");
                sb.AppendLine("   <Row ss:Height=\"8\"><Cell ss:MergeAcross=\"2\"/></Row>");

                // Section 1: General
                sb.AppendLine("   <Row ss:Height=\"22\"><Cell ss:MergeAcross=\"2\" ss:StyleID=\"SectionHeader\"><Data ss:Type=\"String\">1. GENERAL PROJECT DETAILS</Data></Cell></Row>");
                AppendRow(sb, "Project Name", lot.ProjectName, "Descriptive name of the development project");
                AppendRow(sb, "Project Address", lot.Address, "Street address / borough");
                AppendRow(sb, "Tax Block / Lot", lot.BlockLot, "e.g. Block 1234, Lot 56");
                sb.AppendLine("   <Row ss:Height=\"8\"><Cell ss:MergeAcross=\"2\"/></Row>");

                // Section 2: Parcel Dimensions
                sb.AppendLine("   <Row ss:Height=\"22\"><Cell ss:MergeAcross=\"2\" ss:StyleID=\"SectionHeader\"><Data ss:Type=\"String\">2. LOT &amp; PARCEL DIMENSIONS</Data></Cell></Row>");
                AppendNumericRow(sb, "Lot Area (Sq Ft)", lot.LotAreaSqFt, "Total land area of the zoning lot");
                AppendNumericRow(sb, "Lot Frontage / Width (Ft)", lot.LotWidthFt, "Street frontage width");
                AppendNumericRow(sb, "Lot Depth (Ft)", lot.LotDepthFt, "Depth of property");
                AppendRow(sb, "Zoning District", lot.ZoningDistrict, "Primary zoning district (e.g. R8, R10, C6-4)");
                AppendRow(sb, "Lot Type", lot.LotType, "Corner Lot / Interior Lot / Through Lot");
                sb.AppendLine("   <Row ss:Height=\"8\"><Cell ss:MergeAcross=\"2\"/></Row>");

                // Section 3: FAR Allowances
                sb.AppendLine("   <Row ss:Height=\"22\"><Cell ss:MergeAcross=\"2\" ss:StyleID=\"SectionHeader\"><Data ss:Type=\"String\">3. FLOOR AREA RATIO (FAR) ALLOWANCES</Data></Cell></Row>");
                AppendNumericRow(sb, "Base Residential FAR", lot.BaseResidentialFar, "Standard residential FAR limit");
                AppendNumericRow(sb, "Base Commercial FAR", lot.BaseCommercialFar, "Commercial overlay / retail FAR allowance");
                AppendNumericRow(sb, "Base Community Facility FAR", lot.BaseCommunityFacilityFar, "Medical, educational, or community use FAR");
                AppendNumericRow(sb, "Inclusionary Housing Bonus FAR", lot.InclusionaryBonusFar, "Affordable housing / IH bonus FAR");
                AppendNumericRow(sb, "Other / Plaza Bonus FAR", lot.OtherBonusFar, "Public plaza or transit improvement bonus");

                // Formulas
                sb.AppendLine("   <Row ss:Height=\"20\">");
                sb.AppendLine("    <Cell ss:StyleID=\"FieldLabel\"><Data ss:Type=\"String\">Total Allowable FAR</Data></Cell>");
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"FieldFormula\"><Data ss:Type=\"Number\">{0:N2}</Data></Cell>", lot.TotalAllowableFar));
                sb.AppendLine("    <Cell ss:StyleID=\"FieldValue\"><Data ss:Type=\"String\">Sum of Base FAR + Bonuses</Data></Cell>");
                sb.AppendLine("   </Row>");

                sb.AppendLine("   <Row ss:Height=\"20\">");
                sb.AppendLine("    <Cell ss:StyleID=\"FieldLabel\"><Data ss:Type=\"String\">Max Allowable ZFA (Sq Ft)</Data></Cell>");
                sb.AppendLine(string.Format("    <Cell ss:StyleID=\"FieldFormula\"><Data ss:Type=\"Number\">{0:N2}</Data></Cell>", lot.TotalAllowableZfa));
                sb.AppendLine("    <Cell ss:StyleID=\"FieldValue\"><Data ss:Type=\"String\">Lot Area × Total Allowable FAR</Data></Cell>");
                sb.AppendLine("   </Row>");
                sb.AppendLine("   <Row ss:Height=\"8\"><Cell ss:MergeAcross=\"2\"/></Row>");

                // Section 4: Height & Envelopes
                sb.AppendLine("   <Row ss:Height=\"22\"><Cell ss:MergeAcross=\"2\" ss:StyleID=\"SectionHeader\"><Data ss:Type=\"String\">4. HEIGHT &amp; ENVELOPE LIMITS</Data></Cell></Row>");
                AppendNumericRow(sb, "Max Building Height (Ft)", lot.MaxBuildingHeightFt, "Maximum permissible height / sky exposure plane");

                sb.AppendLine("  </Table>");
                sb.AppendLine(" </Worksheet>");
                sb.AppendLine("</Workbook>");

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void AppendRow(StringBuilder sb, string label, string value, string notes)
        {
            sb.AppendLine("   <Row ss:Height=\"20\">");
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"FieldLabel\"><Data ss:Type=\"String\">{0}</Data></Cell>", CleanXml(label)));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"FieldValue\"><Data ss:Type=\"String\">{0}</Data></Cell>", CleanXml(value)));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"FieldValue\"><Data ss:Type=\"String\">{0}</Data></Cell>", CleanXml(notes)));
            sb.AppendLine("   </Row>");
        }

        private void AppendNumericRow(StringBuilder sb, string label, double value, string notes)
        {
            sb.AppendLine("   <Row ss:Height=\"20\">");
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"FieldLabel\"><Data ss:Type=\"String\">{0}</Data></Cell>", CleanXml(label)));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"FieldValue\"><Data ss:Type=\"Number\">{0}</Data></Cell>", value));
            sb.AppendLine(string.Format("    <Cell ss:StyleID=\"FieldValue\"><Data ss:Type=\"String\">{0}</Data></Cell>", CleanXml(notes)));
            sb.AppendLine("   </Row>");
        }

        private string CleanXml(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        public ZoningLotData ImportZoningFromExcel(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

            ZoningLotData lot = new ZoningLotData();
            string content = File.ReadAllText(filePath);

            try
            {
                // Parse either XML Spreadsheet format or CSV / Text format
                if (content.Contains("<Workbook") || content.Contains("<?xml"))
                {
                    ParseXmlSpreadsheet(content, lot);
                }
                else
                {
                    ParseDelimitedText(content, lot);
                }
                return lot;
            }
            catch
            {
                return null;
            }
        }

        private void ParseXmlSpreadsheet(string xmlContent, ZoningLotData lot)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContent);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ss", "urn:schemas-microsoft-com:office:spreadsheet");

            XmlNodeList rows = doc.SelectNodes("//ss:Row", nsmgr);
            if (rows == null) return;

            foreach (XmlNode row in rows)
            {
                XmlNodeList cells = row.SelectNodes("ss:Cell", nsmgr);
                if (cells == null || cells.Count < 2) continue;

                string key = GetCellText(cells[0]);
                string val = GetCellText(cells[1]);

                AssignField(lot, key, val);
            }
        }

        private void ParseDelimitedText(string text, ZoningLotData lot)
        {
            string[] lines = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string[] parts = line.Split(new char[] { ',', '\t', ';' });
                if (parts.Length < 2) continue;

                string key = parts[0].Trim().Trim('\"');
                string val = parts[1].Trim().Trim('\"');

                AssignField(lot, key, val);
            }
        }

        private string GetCellText(XmlNode cell)
        {
            if (cell == null) return "";
            XmlNode data = cell.SelectSingleNode("*[local-name()='Data']");
            return data != null ? data.InnerText.Trim() : cell.InnerText.Trim();
        }

        private void AssignField(ZoningLotData lot, string key, string val)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(val)) return;

            string k = key.ToLowerInvariant();

            if (k.Contains("project name")) lot.ProjectName = val;
            else if (k.Contains("project address") || k.Contains("address")) lot.Address = val;
            else if (k.Contains("block") || k.Contains("lot / block") || k.Contains("tax block")) lot.BlockLot = val;
            else if (k.Contains("lot area"))
            {
                double num;
                if (double.TryParse(CleanNumber(val), out num)) lot.LotAreaSqFt = num;
            }
            else if (k.Contains("frontage") || k.Contains("lot width"))
            {
                double num;
                if (double.TryParse(CleanNumber(val), out num)) lot.LotWidthFt = num;
            }
            else if (k.Contains("lot depth"))
            {
                double num;
                if (double.TryParse(CleanNumber(val), out num)) lot.LotDepthFt = num;
            }
            else if (k.Contains("zoning district") || k.Contains("zoning")) lot.ZoningDistrict = val;
            else if (k.Contains("lot type")) lot.LotType = val;
            else if (k.Contains("base residential"))
            {
                double num;
                if (double.TryParse(CleanNumber(val), out num)) lot.BaseResidentialFar = num;
            }
            else if (k.Contains("base commercial") || k.Contains("commercial far"))
            {
                double num;
                if (double.TryParse(CleanNumber(val), out num)) lot.BaseCommercialFar = num;
            }
            else if (k.Contains("community facility"))
            {
                double num;
                if (double.TryParse(CleanNumber(val), out num)) lot.BaseCommunityFacilityFar = num;
            }
            else if (k.Contains("inclusionary") || k.Contains("ih bonus"))
            {
                double num;
                if (double.TryParse(CleanNumber(val), out num)) lot.InclusionaryBonusFar = num;
            }
            else if (k.Contains("other bonus") || k.Contains("plaza bonus"))
            {
                double num;
                if (double.TryParse(CleanNumber(val), out num)) lot.OtherBonusFar = num;
            }
            else if (k.Contains("height") || k.Contains("max building height"))
            {
                double num;
                if (double.TryParse(CleanNumber(val), out num)) lot.MaxBuildingHeightFt = num;
            }
        }

        private string CleanNumber(string s)
        {
            if (string.IsNullOrEmpty(s)) return "0";
            return Regex.Replace(s, @"[^\d\.\-]", "");
        }
    }
}
```

### `ZoningFloorArea\Services\LevelCreatorService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public static class LevelCreatorService
    {
        public static string GetOrdinal(int number)
        {
            if (number <= 0) return number.ToString();

            int rem100 = number % 100;
            if (rem100 >= 11 && rem100 <= 13)
            {
                return string.Format("{0}TH", number);
            }

            switch (number % 10)
            {
                case 1: return string.Format("{0}ST", number);
                case 2: return string.Format("{0}ND", number);
                case 3: return string.Format("{0}RD", number);
                default: return string.Format("{0}TH", number);
            }
        }

        public static string FormatLength(Document doc, double lengthFeet)
        {
            try
            {
                return UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, lengthFeet, false);
            }
            catch
            {
                int feet = (int)Math.Truncate(lengthFeet);
                double remainingInches = Math.Abs((lengthFeet - feet) * 12.0);
                if (Math.Abs(lengthFeet) < 0.0001) return "0'-0\"";
                return string.Format("{0}'-{1:F0}\"", feet, remainingInches);
            }
        }

        public static bool TryParseLength(Document doc, string input, out double resultFeet)
        {
            resultFeet = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;

            string clean = input.Trim();

            try
            {
                double val;
                if (UnitFormatUtils.TryParse(doc.GetUnits(), SpecTypeId.Length, clean, out val))
                {
                    resultFeet = val;
                    return true;
                }
            }
            catch
            {
            }

            Match metricMatch = Regex.Match(clean, @"^([+-]?\d+(?:\.\d+)?)\s*(m|mm|cm|meters|metros)?$", RegexOptions.IgnoreCase);
            if (metricMatch.Success)
            {
                double val;
                if (double.TryParse(metricMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                {
                    string unit = metricMatch.Groups[2].Value.ToLower();
                    if (unit == "mm")
                        resultFeet = (val / 1000.0) * 3.280839895013123;
                    else if (unit == "cm")
                        resultFeet = (val / 100.0) * 3.280839895013123;
                    else if (unit == "m" || unit == "meters" || unit == "metros")
                        resultFeet = val * 3.280839895013123;
                    else
                    {
                        bool isMetric = doc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId() != UnitTypeId.Feet;
                        resultFeet = isMetric ? val * 3.280839895013123 : val;
                    }
                    return true;
                }
            }

            Match feetInchesMatch = Regex.Match(clean, @"^([+-]?\d+)'(?:\s*([0-9.]+)\"")?$", RegexOptions.IgnoreCase);
            if (feetInchesMatch.Success)
            {
                double f;
                if (double.TryParse(feetInchesMatch.Groups[1].Value, out f))
                {
                    double inches = 0;
                    double inc;
                    if (feetInchesMatch.Groups[2].Success && double.TryParse(feetInchesMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out inc))
                    {
                        inches = inc;
                    }
                    resultFeet = f + (inches / 12.0);
                    return true;
                }
            }

            double simpleVal;
            if (double.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out simpleVal) ||
                double.TryParse(clean, NumberStyles.Float, CultureInfo.CurrentCulture, out simpleVal))
            {
                resultFeet = simpleVal;
                return true;
            }

            return false;
        }

        public static List<LevelCreationItem> BuildPlannedLevels(
            Document doc,
            double baseElevationFeet,
            int startFloorNumber,
            int floorCount,
            double typicalHeightFeet,
            int cellarCount,
            double cellarHeightFeet,
            bool includeRoof,
            double roofHeightFeet,
            bool includeBulkhead,
            double bulkheadHeightFeet,
            bool createViewsDefault,
            bool createCeilingViewsDefault,
            bool useTwoDigits)
        {
            List<LevelCreationItem> list = new List<LevelCreationItem>();

            for (int i = cellarCount; i >= 1; i--)
            {
                double elev = baseElevationFeet - (i * cellarHeightFeet);
                string name;
                if (i == 1)
                {
                    name = useTwoDigits ? "00 CELLAR" : "CELLAR";
                }
                else
                {
                    int cNum = i - 1;
                    name = string.Format("CELLAR {0}", cNum);
                }

                LevelCreationItem item = new LevelCreationItem();
                item.LevelName = name;
                item.ElevationFeet = elev;
                item.ElevationDisplay = FormatLength(doc, elev);
                item.LevelType = "Cellar";
                item.CreateFloorPlan = createViewsDefault;
                item.CreateCeilingPlan = createCeilingViewsDefault;
                list.Add(item);
            }

            int currentFloorNum = startFloorNumber;
            double currentElev = baseElevationFeet;

            for (int f = 0; f < floorCount; f++)
            {
                string prefix = useTwoDigits ? string.Format("{0:D2} ", currentFloorNum) : string.Format("{0} ", currentFloorNum);
                string ordinal = GetOrdinal(currentFloorNum);
                string name = string.Format("{0}{1} FL.", prefix, ordinal);

                LevelCreationItem item = new LevelCreationItem();
                item.LevelName = name;
                item.ElevationFeet = currentElev;
                item.ElevationDisplay = FormatLength(doc, currentElev);
                item.LevelType = "Typical";
                item.CreateFloorPlan = createViewsDefault;
                item.CreateCeilingPlan = createCeilingViewsDefault;
                list.Add(item);

                currentElev += typicalHeightFeet;
                currentFloorNum++;
            }

            if (includeRoof)
            {
                double roofElev = (floorCount > 0) ? (currentElev - typicalHeightFeet + roofHeightFeet) : (baseElevationFeet + roofHeightFeet);
                currentElev = roofElev;

                string prefix = useTwoDigits ? string.Format("{0:D2} ", currentFloorNum) : "";
                string name = string.Format("{0}ROOF", prefix);

                LevelCreationItem item = new LevelCreationItem();
                item.LevelName = name;
                item.ElevationFeet = roofElev;
                item.ElevationDisplay = FormatLength(doc, roofElev);
                item.LevelType = "Roof";
                item.CreateFloorPlan = createViewsDefault;
                item.CreateCeilingPlan = createCeilingViewsDefault;
                list.Add(item);

                currentFloorNum++;
            }

            if (includeBulkhead)
            {
                double bulkheadElev = currentElev + bulkheadHeightFeet;

                string prefix = useTwoDigits ? string.Format("{0:D2} ", currentFloorNum) : "";
                string name = string.Format("{0}BULKHEAD", prefix);

                LevelCreationItem item = new LevelCreationItem();
                item.LevelName = name;
                item.ElevationFeet = bulkheadElev;
                item.ElevationDisplay = FormatLength(doc, bulkheadElev);
                item.LevelType = "Bulkhead";
                item.CreateFloorPlan = createViewsDefault;
                item.CreateCeilingPlan = createCeilingViewsDefault;
                list.Add(item);
            }

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Index = i + 1;
            }

            return list;
        }

        public static Tuple<int, int, List<string>> CreateLevelsInRevit(
            Document doc,
            List<LevelCreationItem> items,
            bool createCeilingPlans)
        {
            int levelsCreated = 0;
            int viewsCreated = 0;
            List<string> errors = new List<string>();

            if (items == null || items.Count == 0)
                return Tuple.Create(0, 0, errors);

            HashSet<string> existingNames = new HashSet<string>(
                new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .Select(l => l.Name),
                StringComparer.OrdinalIgnoreCase);

            ViewFamilyType floorPlanVft = null;
            FilteredElementCollector vftCollector = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType));
            foreach (ViewFamilyType vft in vftCollector)
            {
                if (vft.ViewFamily == ViewFamily.FloorPlan)
                {
                    floorPlanVft = vft;
                    break;
                }
            }

            ViewFamilyType ceilingPlanVft = null;
            if (createCeilingPlans)
            {
                foreach (ViewFamilyType vft in vftCollector)
                {
                    if (vft.ViewFamily == ViewFamily.CeilingPlan)
                    {
                        ceilingPlanVft = vft;
                        break;
                    }
                }

                if (ceilingPlanVft == null)
                {
                    errors.Add("Warning: No CeilingPlan (RCP) ViewFamilyType found in this Revit project.");
                }
            }

            using (Transaction tx = new Transaction(doc, "BauTools: Batch Create Levels"))
            {
                tx.Start();

                foreach (LevelCreationItem item in items)
                {
                    if (!item.IsIncluded) continue;

                    try
                    {
                        Level newLevel = Level.Create(doc, item.ElevationFeet);
                        levelsCreated++;

                        string targetName = item.LevelName;
                        int duplicateSuffix = 1;
                        while (existingNames.Contains(targetName))
                        {
                            targetName = string.Format("{0} ({1})", item.LevelName, duplicateSuffix++);
                        }

                        try
                        {
                            newLevel.Name = targetName;
                            existingNames.Add(targetName);
                        }
                        catch (Exception nameEx)
                        {
                            errors.Add(string.Format("Level at {0}: could not assign name '{1}': {2}", item.ElevationDisplay, targetName, nameEx.Message));
                        }

                        if (item.CreateFloorPlan && floorPlanVft != null)
                        {
                            try
                            {
                                ViewPlan floorPlan = ViewPlan.Create(doc, floorPlanVft.Id, newLevel.Id);
                                viewsCreated++;
                            }
                            catch (Exception viewEx)
                            {
                                errors.Add(string.Format("Could not create Floor Plan for '{0}': {1}", newLevel.Name, viewEx.Message));
                            }
                        }

                        if ((item.CreateCeilingPlan || createCeilingPlans) && ceilingPlanVft != null)
                        {
                            try
                            {
                                ViewPlan ceilingPlan = ViewPlan.Create(doc, ceilingPlanVft.Id, newLevel.Id);
                                viewsCreated++;
                            }
                            catch (Exception rcpEx)
                            {
                                errors.Add(string.Format("Could not create RCP (Ceiling Plan) for '{0}': {1}", newLevel.Name, rcpEx.Message));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(string.Format("Error creating level '{0}' at {1}: {2}", item.LevelName, item.ElevationDisplay, ex.Message));
                    }
                }

                tx.Commit();
            }

            return Tuple.Create(levelsCreated, viewsCreated, errors);
        }
    }
}

```

### `ZoningFloorArea\Services\LevelRenamerService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public static class LevelRenamerService
    {
        public static string GetOrdinal(int number)
        {
            if (number <= 0) return number.ToString();

            int rem100 = number % 100;
            if (rem100 >= 11 && rem100 <= 13)
            {
                return string.Format("{0}TH", number);
            }

            switch (number % 10)
            {
                case 1: return string.Format("{0}ST", number);
                case 2: return string.Format("{0}ND", number);
                case 3: return string.Format("{0}RD", number);
                default: return string.Format("{0}TH", number);
            }
        }

        public static void CalculateProposedNames(
            List<LevelRenameItem> items,
            LevelRenameItem baseLevelItem,
            int numberOfFloors,
            bool includeRoof,
            bool includeBulkhead,
            bool useTwoDigitPrefix)
        {
            if (items == null || items.Count == 0) return;

            // Sort all items strictly by elevation
            List<LevelRenameItem> sorted = items.OrderBy(x => x.RawElevation).ToList();

            int baseIndex = sorted.IndexOf(baseLevelItem);
            if (baseIndex < 0)
            {
                // Default to first level >= 0.0 or index 0
                baseIndex = sorted.FindIndex(x => x.RawElevation >= -0.001);
                if (baseIndex < 0) baseIndex = 0;
            }

            // 1. Process underground levels (below baseIndex)
            // sorted[baseIndex - 1] is immediately below ground (CELLAR)
            // sorted[baseIndex - 2] is deeper (CELLAR 1 / SUB-CELLAR)
            int cellarCount = baseIndex;
            for (int i = 0; i < cellarCount; i++)
            {
                int depthFromGround = baseIndex - i; // 1 for immediately below, 2 for lower...
                
                string cellarName;
                if (depthFromGround == 1)
                {
                    cellarName = useTwoDigitPrefix ? "00 CELLAR" : "CELLAR";
                }
                else
                {
                    int cellarNum = depthFromGround - 1;
                    cellarName = string.Format("CELLAR {0}", cellarNum);
                }

                sorted[i].ProposedName = cellarName;
            }

            // 2. Process floors from baseLevel upwards
            int aboveCount = sorted.Count - baseIndex;
            int floorNumber = 1;

            for (int i = baseIndex; i < sorted.Count; i++)
            {
                int aboveIndex = i - baseIndex; // 0, 1, 2...

                if (aboveIndex < numberOfFloors)
                {
                    // Regular floor
                    string prefix = useTwoDigitPrefix ? string.Format("{0:D2} ", floorNumber) : string.Format("{0} ", floorNumber);
                    string ordinal = GetOrdinal(floorNumber);
                    sorted[i].ProposedName = string.Format("{0}{1} FL.", prefix, ordinal);
                    floorNumber++;
                }
                else
                {
                    // Upper levels (Roof, Bulkhead, etc.)
                    int extraIndex = aboveIndex - numberOfFloors; // 0 for first extra, 1 for second...

                    if (extraIndex == 0 && includeRoof)
                    {
                        string prefix = useTwoDigitPrefix ? string.Format("{0:D2} ", floorNumber) : "";
                        sorted[i].ProposedName = string.Format("{0}ROOF", prefix);
                        floorNumber++;
                    }
                    else if ((extraIndex == 1 && includeRoof && includeBulkhead) ||
                             (extraIndex == 0 && !includeRoof && includeBulkhead))
                    {
                        string prefix = useTwoDigitPrefix ? string.Format("{0:D2} ", floorNumber) : "";
                        sorted[i].ProposedName = string.Format("{0}BULKHEAD", prefix);
                        floorNumber++;
                    }
                    else
                    {
                        string prefix = useTwoDigitPrefix ? string.Format("{0:D2} ", floorNumber) : "";
                        sorted[i].ProposedName = string.Format("{0}UPPER LEVEL {1}", prefix, extraIndex + 1);
                        floorNumber++;
                    }
                }
            }
        }

        public static Tuple<int, List<string>> ApplyRenaming(
            Document doc,
            List<LevelRenameItem> items)
        {
            int renamed = 0;
            List<string> errors = new List<string>();

            List<LevelRenameItem> toRename = items.Where(x => x.IsSelected && x.IsChanged).ToList();
            if (toRename.Count == 0) return Tuple.Create(0, errors);

            using (Transaction tx = new Transaction(doc, "BauTools: Rename Levels"))
            {
                tx.Start();

                // Phase 1: Temporary unique names to avoid collisions in Revit
                foreach (LevelRenameItem item in toRename)
                {
                    try
                    {
                        item.LevelElement.Name = string.Format("_BAU_TEMP_{0}", Guid.NewGuid().ToString("N").Substring(0, 8));
                    }
                    catch (Exception ex)
                    {
                        errors.Add(string.Format("Error temporal en '{0}': {1}", item.CurrentName, ex.Message));
                    }
                }

                // Phase 2: Assign final proposed names
                foreach (LevelRenameItem item in toRename)
                {
                    try
                    {
                        item.LevelElement.Name = item.ProposedName;
                        renamed++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add(string.Format("Error al asignar '{0}' a '{1}': {2}", item.ProposedName, item.CurrentName, ex.Message));
                    }
                }

                tx.Commit();
            }

            return Tuple.Create(renamed, errors);
        }
    }
}

```

### `ZoningFloorArea\Services\NeuralGenerativeSolver.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class NeuralGenerativeSolver
    {
        public List<GenerativeScenario> SolveScenarios(GenerativeInputParameters p)
        {
            List<GenerativeScenario> results = new List<GenerativeScenario>();
            if (p == null) p = new GenerativeInputParameters();

            double targetCapZfa = p.LotAreaSqFt * p.BaseFar;
            double effectiveWidth = Math.Max(30.0, p.LotWidthFt - (p.SetbackSidesFt * 2));
            double effectiveDepth = Math.Max(30.0, p.LotDepthFt - p.SetbackFrontFt - p.SetbackRearFt);

            // 0. Active Custom Interactive Scenario (Morphed by user sliders with Dormers & 3D Setbacks)
            results.Add(BuildCustomInteractiveScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            // 1. Scenario 1: Max Buildable ZFA (Zero Wasted Air Rights)
            results.Add(BuildMaxZfaScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            // 2. Scenario 2: Max Sales / High-Floor Revenue Premium
            results.Add(BuildMaxRevenueScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            // 3. Scenario 3: Mandatory Inclusionary Housing (MIH)
            results.Add(BuildMihScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            // 4. Scenario 4: Solar Terraces & Sky Exposure Setbacks
            results.Add(BuildSolarTerraceScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            // 5. Scenario 5: Compact & Minimum Façade Cost
            results.Add(BuildCompactCostScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            return results;
        }

        public GenerativeScenario BuildCustomInteractiveScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_interactive_custom",
                Title = "Live Morphed 3D Mass",
                Subtitle = "Interactive 3D Orbit • Dormers & Setback Controls",
                Icon = "🎛️",
                ColorHex = "#0284C7"
            };

            double podiumCoverageRatio = Math.Min(1.0, Math.Max(0.3, p.PodiumCoveragePercent / 100.0));
            double towerCoverageRatio = Math.Min(1.0, Math.Max(0.15, p.TowerCoveragePercent / 100.0));

            double podiumW = Math.Min(p.LotWidthFt, p.LotWidthFt * Math.Sqrt(podiumCoverageRatio));
            double podiumD = Math.Min(p.LotDepthFt, p.LotDepthFt * Math.Sqrt(podiumCoverageRatio));

            double towerW = Math.Max(25.0, effW * Math.Sqrt(towerCoverageRatio / Math.Max(0.01, podiumCoverageRatio)));
            double towerD = Math.Max(25.0, effD * Math.Sqrt(towerCoverageRatio / Math.Max(0.01, podiumCoverageRatio)));

            double currentElev = 0.0;
            int lvlIdx = 1;

            // 1. Podiums / Base Height
            for (int i = 0; i < p.PodiumFloors; i++)
            {
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("FL. {0:D2} (Base/Podium)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightPodium,
                    WidthFt = podiumW,
                    DepthFt = podiumD,
                    OffsetXFt = 0.0,
                    OffsetYFt = 0.0,
                    UsageType = MassFloorUsage.CommercialPodium,
                    ColorHex = "#3B82F6"
                });
                currentElev += p.FloorHeightPodium;
                lvlIdx++;
            }

            // 2. Dormers / Sky Exposure Transition Floors
            for (int d = 0; d < p.DormerFloors; d++)
            {
                double stepFactor = (double)(d + 1) / (p.DormerFloors + 1);
                double dW = podiumW - ((podiumW - towerW) * stepFactor);
                double dD = podiumD - ((podiumD - towerD) * stepFactor) - (p.DormerSetbackDepthFt * stepFactor);

                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("FL. {0:D2} (Dormer/Setback)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightTower,
                    WidthFt = Math.Max(towerW, dW),
                    DepthFt = Math.Max(towerD, dD),
                    OffsetXFt = 0.0,
                    OffsetYFt = (p.SetbackRearFt - p.SetbackFrontFt) * 0.25,
                    UsageType = MassFloorUsage.DormerSetbackTransition,
                    ColorHex = "#06B6D4"
                });
                currentElev += p.FloorHeightTower;
                lvlIdx++;
            }

            // 3. Residential Tower Floors
            double currentZfa = s.Floors.Sum(f => f.AreaSqFt);
            double remainingZfa = targetCap - currentZfa;
            double towerFloorArea = Math.Max(500.0, towerW * towerD);
            int estTowerFloors = Math.Max(1, (int)Math.Floor(remainingZfa / towerFloorArea));

            int maxPossibleFloors = Math.Max(1, (int)Math.Floor((p.MaxHeightFt - currentElev) / p.FloorHeightTower));
            int actualTowerFloors = Math.Min(estTowerFloors, maxPossibleFloors);

            int mihFloors = (int)Math.Ceiling(actualTowerFloors * (p.MihPercent / 100.0));

            for (int i = 0; i < actualTowerFloors; i++)
            {
                bool isPenthouse = (i >= actualTowerFloors - p.PenthouseFloors && p.PenthouseFloors > 0);
                bool isMih = (!isPenthouse && i < mihFloors);

                MassFloorUsage uType = MassFloorUsage.TypicalResidential;
                string cHex = "#8B5CF6";

                if (isPenthouse)
                {
                    uType = MassFloorUsage.PenthouseLuxury;
                    cHex = "#F59E0B";
                }
                else if (isMih)
                {
                    uType = MassFloorUsage.InclusionaryHousing;
                    cHex = "#D97706";
                }

                double flrH = isPenthouse ? p.FloorHeightTower + 3.0 : p.FloorHeightTower;

                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("FL. {0:D2} {1}", lvlIdx, isPenthouse ? "(Luxury Penthouse)" : (isMih ? "(MIH Affordable)" : "(Market Tower)")),
                    ElevationFt = currentElev,
                    HeightFt = flrH,
                    WidthFt = towerW,
                    DepthFt = towerD,
                    OffsetXFt = 0.0,
                    OffsetYFt = (p.SetbackRearFt - p.SetbackFrontFt) * 0.5,
                    UsageType = uType,
                    ColorHex = cHex
                });

                currentElev += flrH;
                lvlIdx++;
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = p.PodiumFloors;
            s.DormerFloors = p.DormerFloors;
            s.TowerFloors = actualTowerFloors;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = actualTowerFloors > 0 ? ((double)Math.Max(0, actualTowerFloors - 5) / actualTowerFloors) * 100.0 : 0;
            s.MihUnitsEstimate = (int)(s.Floors.Where(f => f.UsageType == MassFloorUsage.InclusionaryHousing).Sum(f => f.AreaSqFt) / 720.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 950.0 + (s.TotalZfa * (s.HighFloorPercentage / 100.0) * 450.0)) / 1000000.0;

            return s;
        }

        private GenerativeScenario BuildMaxZfaScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_max_zfa",
                Title = "Max Buildable ZFA",
                Subtitle = "Zero Wasted Air Rights • 99.8% FAR Cap",
                Icon = "🏢",
                ColorHex = "#2563EB"
            };

            double podiumW = Math.Min(p.LotWidthFt, effW * 1.15);
            double podiumD = Math.Min(p.LotDepthFt, effD * 1.15);
            int podiumFloors = 3;
            double towerW = effW * 0.90;
            double towerD = effD * 0.85;

            double podiumAreaPerFloor = podiumW * podiumD;
            double towerAreaPerFloor = towerW * towerD;
            double accumulatedZfa = podiumAreaPerFloor * podiumFloors;

            double remainingCap = targetCap - accumulatedZfa;
            int towerFloors = Math.Max(1, (int)Math.Floor(remainingCap / towerAreaPerFloor));

            double currentElev = 0.0;
            int lvlIdx = 1;

            for (int i = 0; i < podiumFloors; i++)
            {
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} (Podium)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightPodium,
                    WidthFt = podiumW,
                    DepthFt = podiumD,
                    UsageType = MassFloorUsage.CommercialPodium,
                    ColorHex = "#3B82F6"
                });
                currentElev += p.FloorHeightPodium;
                lvlIdx++;
            }

            for (int i = 0; i < towerFloors; i++)
            {
                bool isPenthouse = (i >= towerFloors - 2);
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} (Tower)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = isPenthouse ? p.FloorHeightTower + 2.0 : p.FloorHeightTower,
                    WidthFt = towerW,
                    DepthFt = towerD,
                    UsageType = isPenthouse ? MassFloorUsage.PenthouseLuxury : MassFloorUsage.TypicalResidential,
                    ColorHex = isPenthouse ? "#F59E0B" : "#8B5CF6"
                });
                currentElev += (isPenthouse ? p.FloorHeightTower + 2.0 : p.FloorHeightTower);
                lvlIdx++;
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = podiumFloors;
            s.TowerFloors = towerFloors;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = 48.0;
            s.MihUnitsEstimate = (int)(s.TotalZfa * 0.25 / 750.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 980.0) / 1000000.0;

            return s;
        }

        private GenerativeScenario BuildMaxRevenueScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_max_sales",
                Title = "Max Sales & Revenue",
                Subtitle = "Slender Tower • 68% Area in High-Value Floors",
                Icon = "💰",
                ColorHex = "#059669"
            };

            double podiumW = effW;
            double podiumD = effD;
            int podiumFloors = 2;
            double towerW = effW * 0.72;
            double towerD = effD * 0.72;

            double podiumAreaPerFloor = podiumW * podiumD;
            double towerAreaPerFloor = towerW * towerD;
            double accumulatedZfa = podiumAreaPerFloor * podiumFloors;

            double remainingCap = targetCap * 0.96 - accumulatedZfa;
            int towerFloors = Math.Max(1, (int)Math.Floor(remainingCap / towerAreaPerFloor));

            double currentElev = 0.0;
            int lvlIdx = 1;

            for (int i = 0; i < podiumFloors; i++)
            {
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} (Lobby/Retail)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightPodium + 3.0,
                    WidthFt = podiumW,
                    DepthFt = podiumD,
                    UsageType = MassFloorUsage.CommercialPodium,
                    ColorHex = "#3B82F6"
                });
                currentElev += p.FloorHeightPodium + 3.0;
                lvlIdx++;
            }

            for (int i = 0; i < towerFloors; i++)
            {
                bool isPenthouse = (i >= towerFloors - 3);
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} (High Views)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = isPenthouse ? 14.0 : p.FloorHeightTower,
                    WidthFt = towerW,
                    DepthFt = towerD,
                    UsageType = isPenthouse ? MassFloorUsage.PenthouseLuxury : MassFloorUsage.TypicalResidential,
                    ColorHex = isPenthouse ? "#F59E0B" : "#10B981"
                });
                currentElev += isPenthouse ? 14.0 : p.FloorHeightTower;
                lvlIdx++;
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = podiumFloors;
            s.TowerFloors = towerFloors;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = 68.5;
            s.MihUnitsEstimate = (int)(s.TotalZfa * 0.20 / 750.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 1220.0) / 1000000.0;

            return s;
        }

        private GenerativeScenario BuildMihScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_mih",
                Title = "Mandatory Housing (MIH)",
                Subtitle = "Affordable Ratio Optimized • +2.0 Bonus FAR",
                Icon = "🏘️",
                ColorHex = "#D97706"
            };

            double bonusCap = targetCap * 1.20;
            double podiumW = effW * 1.05;
            double podiumD = effD * 1.05;
            int podiumFloors = 2;
            double towerW = effW * 0.85;
            double towerD = effD * 0.85;

            double towerAreaPerFloor = towerW * towerD;
            int towerFloors = Math.Max(1, (int)Math.Floor((bonusCap - (podiumW * podiumD * podiumFloors)) / towerAreaPerFloor));

            double currentElev = 0.0;
            int lvlIdx = 1;

            for (int i = 0; i < podiumFloors; i++)
            {
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} (Podium)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightPodium,
                    WidthFt = podiumW,
                    DepthFt = podiumD,
                    UsageType = MassFloorUsage.CommercialPodium,
                    ColorHex = "#3B82F6"
                });
                currentElev += p.FloorHeightPodium;
                lvlIdx++;
            }

            int mihFloorCount = (int)Math.Ceiling(towerFloors * (p.MihPercent / 100.0));

            for (int i = 0; i < towerFloors; i++)
            {
                bool isMih = (i < mihFloorCount);
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} {1}", lvlIdx, isMih ? "(MIH Affordable)" : "(Market Rate)"),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightTower,
                    WidthFt = towerW,
                    DepthFt = towerD,
                    UsageType = isMih ? MassFloorUsage.InclusionaryHousing : MassFloorUsage.TypicalResidential,
                    ColorHex = isMih ? "#F59E0B" : "#8B5CF6"
                });
                currentElev += p.FloorHeightTower;
                lvlIdx++;
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = podiumFloors;
            s.TowerFloors = towerFloors;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = 42.0;
            s.MihUnitsEstimate = (int)(s.Floors.Where(f => f.UsageType == MassFloorUsage.InclusionaryHousing).Sum(f => f.AreaSqFt) / 720.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 880.0) / 1000000.0;

            return s;
        }

        private GenerativeScenario BuildSolarTerraceScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_terraces",
                Title = "Solar Terraces & Setbacks",
                Subtitle = "Stepped Profile • Private Roof Terraces",
                Icon = "☀️",
                ColorHex = "#0284C7"
            };

            int tierCount = 4;
            int floorsPerTier = 4;
            double currentElev = 0.0;
            int lvlIdx = 1;

            double curW = effW * 1.1;
            double curD = effD * 1.1;

            for (int t = 0; t < tierCount; t++)
            {
                for (int f = 0; f < floorsPerTier; f++)
                {
                    bool isPodium = (lvlIdx <= 2);
                    s.Floors.Add(new MassingFloorBlock
                    {
                        LevelIndex = lvlIdx,
                        LevelName = string.Format("Level {0:D2} (Tier {1})", lvlIdx, t + 1),
                        ElevationFt = currentElev,
                        HeightFt = isPodium ? p.FloorHeightPodium : p.FloorHeightTower,
                        WidthFt = curW,
                        DepthFt = curD,
                        UsageType = isPodium ? MassFloorUsage.CommercialPodium : MassFloorUsage.TypicalResidential,
                        ColorHex = isPodium ? "#3B82F6" : "#0284C7"
                    });
                    currentElev += isPodium ? p.FloorHeightPodium : p.FloorHeightTower;
                    lvlIdx++;
                }

                curW = Math.Max(28.0, curW * 0.85);
                curD = Math.Max(28.0, curD * 0.85);
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = 2;
            s.TowerFloors = s.TotalFloors - 2;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = 38.0;
            s.MihUnitsEstimate = (int)(s.TotalZfa * 0.20 / 750.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 1040.0) / 1000000.0;

            return s;
        }

        private GenerativeScenario BuildCompactCostScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_compact",
                Title = "Compact & Minimum Cost",
                Subtitle = "High Efficiency • Minimal Façade Perimeter",
                Icon = "📉",
                ColorHex = "#475569"
            };

            double w = effW * 0.95;
            double d = effD * 0.95;
            double floorArea = w * d;
            int totalFloors = Math.Max(1, (int)Math.Floor(targetCap * 0.98 / floorArea));

            double currentElev = 0.0;
            int lvlIdx = 1;

            for (int i = 0; i < totalFloors; i++)
            {
                bool isGround = (i == 0);
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2}", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = isGround ? p.FloorHeightPodium : p.FloorHeightTower,
                    WidthFt = w,
                    DepthFt = d,
                    UsageType = isGround ? MassFloorUsage.CommercialPodium : MassFloorUsage.TypicalResidential,
                    ColorHex = isGround ? "#3B82F6" : "#64748B"
                });
                currentElev += isGround ? p.FloorHeightPodium : p.FloorHeightTower;
                lvlIdx++;
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = 1;
            s.TowerFloors = totalFloors - 1;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = 45.0;
            s.MihUnitsEstimate = (int)(s.TotalZfa * 0.20 / 750.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 910.0) / 1000000.0;

            return s;
        }
    }
}
```

### `ZoningFloorArea\Services\NycPlutoService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class NycPlutoService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static NycPlutoService()
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(25);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BauTools-Revit-Addin/1.0");
        }

        public async Task<List<NycSearchResult>> SearchAddressAsync(string query)
        {
            List<NycSearchResult> results = new List<NycSearchResult>();
            if (string.IsNullOrWhiteSpace(query))
                return results;

            try
            {
                string url = string.Format("https://geosearch.planninglabs.nyc/v1/autocomplete?text={0}", Uri.EscapeDataString(query));
                using (HttpResponseMessage response = await _httpClient.GetAsync(url))
                {
                    if (!response.IsSuccessStatusCode)
                        return results;

                    string json = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        JsonElement features;
                        if (doc.RootElement.TryGetProperty("features", out features) && features.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement feature in features.EnumerateArray())
                            {
                                JsonElement props;
                                if (!feature.TryGetProperty("properties", out props))
                                    continue;

                                string label = GetString(props, "label");
                                string houseNumber = GetString(props, "housenumber");
                                string street = GetString(props, "street");
                                string borough = GetString(props, "borough");
                                string postalCode = GetString(props, "postalcode");

                                string bbl = GetString(props, "pad_bbl");
                                if (string.IsNullOrEmpty(bbl))
                                    bbl = GetString(props, "bbl");

                                JsonElement addendum;
                                if (string.IsNullOrEmpty(bbl) && props.TryGetProperty("addendum", out addendum))
                                {
                                    JsonElement pad;
                                    if (addendum.TryGetProperty("pad", out pad))
                                    {
                                        bbl = GetString(pad, "bbl");
                                    }
                                }

                                NycSearchResult item = new NycSearchResult();
                                item.Label = label;
                                item.Address = string.Format("{0} {1}", houseNumber, street).Trim();
                                item.HouseNumber = houseNumber;
                                item.Street = street;
                                item.Borough = borough;
                                item.PostalCode = postalCode;
                                item.Bbl = bbl;
                                results.Add(item);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return results;
        }

        public async Task<NycLotInfo> GetLotByBblAsync(string bbl)
        {
            string cleanBbl = NormalizeBbl(bbl);
            if (string.IsNullOrEmpty(cleanBbl))
                return null;

            try
            {
                string queryUrl = string.Format("https://services5.arcgis.com/GfwWNkhOj9bNBqoJ/arcgis/rest/services/MAPPLUTO/FeatureServer/0/query?where=BBL%3D{0}&outFields=*&outSR=2263&f=geojson", cleanBbl);

                using (HttpResponseMessage response = await _httpClient.GetAsync(queryUrl))
                {
                    if (!response.IsSuccessStatusCode)
                        return null;

                    string json = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        JsonElement features;
                        if (!doc.RootElement.TryGetProperty("features", out features) || features.ValueKind != JsonValueKind.Array)
                            return null;

                        JsonElement.ArrayEnumerator featureEnumerator = features.EnumerateArray();
                        if (!featureEnumerator.MoveNext())
                            return null;

                        return ParseFeature(featureEnumerator.Current);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public async Task<NycBlockContext> GetBlockContextAsync(NycLotInfo subjectLot)
        {
            NycBlockContext context = new NycBlockContext();
            context.Borough = subjectLot.Borough;
            context.BlockNumber = subjectLot.Block;
            context.SubjectLot = subjectLot;

            if (string.IsNullOrWhiteSpace(subjectLot.Block))
                return context;

            try
            {
                string boroCode = GetBoroughCode(subjectLot.Borough);
                string queryUrl = string.Format("https://services5.arcgis.com/GfwWNkhOj9bNBqoJ/arcgis/rest/services/MAPPLUTO/FeatureServer/0/query?where=Block%3D{0}+AND+Borough%3D%27{1}%27&outFields=*&outSR=2263&f=geojson", subjectLot.Block, boroCode);

                using (HttpResponseMessage response = await _httpClient.GetAsync(queryUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            JsonElement features;
                            if (doc.RootElement.TryGetProperty("features", out features) && features.ValueKind == JsonValueKind.Array)
                            {
                                foreach (JsonElement feature in features.EnumerateArray())
                                {
                                    NycLotInfo lot = ParseFeature(feature);
                                    if (lot != null && lot.Bbl != subjectLot.Bbl)
                                    {
                                        context.OtherLots.Add(lot);
                                    }
                                }
                            }
                        }
                    }
                }

                context.CalculateAdjacency();
            }
            catch
            {
            }

            return context;
        }

        private static NycLotInfo ParseFeature(JsonElement feature)
        {
            JsonElement props;
            if (!feature.TryGetProperty("properties", out props))
                return null;

            string bbl = GetString(props, "BBL");
            NycLotInfo lotInfo = new NycLotInfo();
            lotInfo.Bbl = bbl;
            lotInfo.Address = GetString(props, "Address");
            lotInfo.Borough = GetBoroughName(GetString(props, "Borough"));
            lotInfo.Block = GetString(props, "Block");
            lotInfo.Lot = GetString(props, "Lot");
            lotInfo.ZipCode = GetString(props, "ZipCode");
            lotInfo.ZoningDistrict1 = GetString(props, "ZoneDist1");
            lotInfo.ZoningDistrict2 = GetString(props, "ZoneDist2");
            lotInfo.CommercialOverlay1 = GetString(props, "Overlay1");
            lotInfo.CommercialOverlay2 = GetString(props, "Overlay2");
            lotInfo.SpecialDistrict1 = GetString(props, "SPDist1");
            lotInfo.SpecialDistrict2 = GetString(props, "SPDist2");
            lotInfo.ResidFar = GetDouble(props, "ResidFAR");
            lotInfo.CommFar = GetDouble(props, "CommFAR");
            lotInfo.FacilFar = GetDouble(props, "FacilFAR");
            lotInfo.BuiltFar = GetDouble(props, "BuiltFAR");
            lotInfo.LotAreaSqFt = GetDouble(props, "LotArea");
            lotInfo.BldgAreaSqFt = GetDouble(props, "BldgArea");
            lotInfo.LotFrontageFt = GetDouble(props, "LotFront");
            lotInfo.LotDepthFt = GetDouble(props, "LotDepth");
            lotInfo.NumFloors = (int)GetDouble(props, "NumFloors");
            lotInfo.YearBuilt = (int)GetDouble(props, "YearBuilt");
            lotInfo.LandUse = GetString(props, "LandUse");
            lotInfo.OwnerName = GetString(props, "OwnerName");
            lotInfo.BuildingClass = GetString(props, "BldgClass");

            JsonElement geom;
            if (feature.TryGetProperty("geometry", out geom))
            {
                string geomType = GetString(geom, "type");
                JsonElement coords;
                if (geom.TryGetProperty("coordinates", out coords) && coords.ValueKind == JsonValueKind.Array)
                {
                    if (string.Equals(geomType, "Polygon", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (JsonElement ring in coords.EnumerateArray())
                        {
                            List<XYZ> ringPoints = ParseRing(ring);
                            if (ringPoints.Count >= 3)
                            {
                                lotInfo.PolygonRings.Add(ringPoints);
                            }
                        }
                    }
                    else if (string.Equals(geomType, "MultiPolygon", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (JsonElement poly in coords.EnumerateArray())
                        {
                            foreach (JsonElement ring in poly.EnumerateArray())
                            {
                                List<XYZ> ringPoints = ParseRing(ring);
                                if (ringPoints.Count >= 3)
                                {
                                    lotInfo.PolygonRings.Add(ringPoints);
                                }
                            }
                        }
                    }
                }
            }

            return lotInfo;
        }

        private static List<XYZ> ParseRing(JsonElement ring)
        {
            List<XYZ> points = new List<XYZ>();
            foreach (JsonElement pt in ring.EnumerateArray())
            {
                if (pt.ValueKind == JsonValueKind.Array)
                {
                    JsonElement.ArrayEnumerator ptEnum = pt.EnumerateArray();
                    if (ptEnum.MoveNext())
                    {
                        double x = ptEnum.Current.GetDouble();
                        if (ptEnum.MoveNext())
                        {
                            double y = ptEnum.Current.GetDouble();
                            points.Add(new XYZ(x, y, 0));
                        }
                    }
                }
            }
            return points;
        }

        private static string NormalizeBbl(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            string digits = string.Empty;
            foreach (char c in raw)
            {
                if (char.IsDigit(c)) digits += c;
            }
            return digits.Length == 10 ? digits : string.Empty;
        }

        private static string GetBoroughName(string codeOrAbbr)
        {
            if (string.IsNullOrWhiteSpace(codeOrAbbr)) return "Unknown";
            string upper = codeOrAbbr.Trim().ToUpperInvariant();
            if (upper == "1" || upper == "MN" || upper == "MANHATTAN") return "Manhattan";
            if (upper == "2" || upper == "BX" || upper == "BRONX") return "Bronx";
            if (upper == "3" || upper == "BK" || upper == "BROOKLYN") return "Brooklyn";
            if (upper == "4" || upper == "QN" || upper == "QUEENS") return "Queens";
            if (upper == "5" || upper == "SI" || upper == "STATEN ISLAND") return "Staten Island";
            return codeOrAbbr;
        }

        public static string GetBoroughCode(string nameOrAbbr)
        {
            if (string.IsNullOrWhiteSpace(nameOrAbbr)) return "MN";
            string upper = nameOrAbbr.Trim().ToUpperInvariant();
            if (upper.Contains("MANHATTAN") || upper == "1" || upper == "MN") return "MN";
            if (upper.Contains("BRONX") || upper == "2" || upper == "BX") return "BX";
            if (upper.Contains("BROOKLYN") || upper == "3" || upper == "BK") return "BK";
            if (upper.Contains("QUEENS") || upper == "4" || upper == "QN") return "QN";
            if (upper.Contains("STATEN") || upper == "5" || upper == "SI") return "SI";
            return "MN";
        }

        private static string GetString(JsonElement elem, string propName)
        {
            JsonElement val;
            if (elem.TryGetProperty(propName, out val))
            {
                if (val.ValueKind == JsonValueKind.String)
                {
                    string s = val.GetString();
                    return s != null ? s.Trim() : string.Empty;
                }
                if (val.ValueKind == JsonValueKind.Number)
                    return val.ToString();
            }
            return string.Empty;
        }

        private static double GetDouble(JsonElement elem, string propName)
        {
            JsonElement val;
            if (elem.TryGetProperty(propName, out val))
            {
                double d;
                if (val.ValueKind == JsonValueKind.Number && val.TryGetDouble(out d))
                    return d;
                double parsed;
                if (val.ValueKind == JsonValueKind.String && double.TryParse(val.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
                    return parsed;
            }
            return 0.0;
        }
    }
}

```

### `ZoningFloorArea\Services\RevitAreaDuplicator.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class RevitAreaDuplicator
    {
        private readonly Document _doc;

        public RevitAreaDuplicator(Document doc)
        {
            _doc = doc;
        }

        public List<Level> GetAllLevels()
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();
        }

        public List<string> GetLevelsInRange(string fromLevelName, string toLevelName)
        {
            List<Level> levels = GetAllLevels();
            if (levels.Count == 0) return new List<string>();

            Level fromLvl = levels.FirstOrDefault(l => string.Equals(l.Name, fromLevelName, StringComparison.OrdinalIgnoreCase));
            Level toLvl = levels.FirstOrDefault(l => string.Equals(l.Name, toLevelName, StringComparison.OrdinalIgnoreCase));

            if (fromLvl == null || toLvl == null) return new List<string>();

            double minElev = Math.Min(fromLvl.Elevation, toLvl.Elevation) - 0.001;
            double maxElev = Math.Max(fromLvl.Elevation, toLvl.Elevation) + 0.001;

            return levels
                .Where(l => l.Elevation >= minElev && l.Elevation <= maxElev)
                .OrderBy(l => l.Elevation)
                .Select(l => l.Name)
                .ToList();
        }

        public string GetLevelAreaSummary(string levelName, string grossSchemeName, string dedSchemeName)
        {
            if (string.IsNullOrEmpty(levelName)) return "No level selected";

            List<Level> levels = GetAllLevels();
            Level lvl = levels.FirstOrDefault(l => string.Equals(l.Name, levelName, StringComparison.OrdinalIgnoreCase));
            if (lvl == null) return "Level not found";

            int totalAreas = 0;
            double totalSqFt = 0;

            FilteredElementCollector collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Areas)
                .WhereElementIsNotElementType();

            foreach (Area a in collector.Cast<Area>())
            {
                if (a.LevelId == lvl.Id && a.Area > 0)
                {
                    totalAreas++;
                    totalSqFt += a.Area;
                }
            }

            if (totalAreas == 0)
            {
                return "⚠️ 0 Areas modeled (Empty level)";
            }

            return string.Format("🟢 {0} Area(s) modeled ({1:N0} SF)", totalAreas, totalSqFt);
        }

        public string PropagateMultipleGroups(
            List<TypicalFloorGroup> groups,
            MappingConfig config,
            bool propagateGross,
            bool propagateDeductions)
        {
            if (groups == null || groups.Count == 0)
            {
                return "No typical floor groups defined.";
            }

            int totalCreatedAreas = 0;
            int totalLevelsUpdated = 0;
            int processedGroups = 0;

            List<Level> allLevels = GetAllLevels();

            using (Transaction tx = new Transaction(_doc, "BauTools: Propagate Typical Floor Areas"))
            {
                tx.Start();

                foreach (TypicalFloorGroup group in groups)
                {
                    if (group.IsSingleLevel)
                        continue; // Single floor: skip propagation

                    if (group.IsDuplexModule)
                    {
                        Level srcLower = allLevels.FirstOrDefault(l => string.Equals(l.Name, group.SourceLevelNameLower, StringComparison.OrdinalIgnoreCase));
                        Level srcUpper = allLevels.FirstOrDefault(l => string.Equals(l.Name, group.SourceLevelNameUpper, StringComparison.OrdinalIgnoreCase));

                        List<string> targetLevels = GetLevelsInRange(group.FromLevelName, group.ToLevelName);
                        if (targetLevels.Count == 0) continue;

                        int groupCreatedCount = 0;
                        int duplexLevelsUpdated = 0;

                        for (int i = 0; i < targetLevels.Count; i++)
                        {
                            string targetName = targetLevels[i];
                            bool isLowerStep = (i % 2 == 0);
                            Level activeSrc = isLowerStep ? srcLower : srcUpper;

                            if (activeSrc == null || string.Equals(targetName, activeSrc.Name, StringComparison.OrdinalIgnoreCase))
                                continue;

                            List<string> singleTargetList = new List<string>();
                            singleTargetList.Add(targetName);

                            if (propagateGross)
                            {
                                groupCreatedCount += PropagateSchemeAreas(activeSrc, singleTargetList, config.GrossAreaSchemeName, config, group.Name);
                            }
                            if (propagateDeductions)
                            {
                                groupCreatedCount += PropagateSchemeAreas(activeSrc, singleTargetList, config.DeductionAreaSchemeName, config, group.Name);
                            }
                            duplexLevelsUpdated++;
                        }

                        totalCreatedAreas += groupCreatedCount;
                        totalLevelsUpdated += duplexLevelsUpdated;
                        processedGroups++;
                    }
                    else
                    {
                        Level sourceLevel = allLevels.FirstOrDefault(l => string.Equals(l.Name, group.SourceLevelName, StringComparison.OrdinalIgnoreCase));
                        if (sourceLevel == null) continue;

                        List<string> targetLevels = GetLevelsInRange(group.FromLevelName, group.ToLevelName);
                        List<string> actualTargets = targetLevels.Where(n => !string.Equals(n, sourceLevel.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                        if (actualTargets.Count == 0) continue;

                        int groupCreatedCount = 0;

                        if (propagateGross)
                        {
                            groupCreatedCount += PropagateSchemeAreas(sourceLevel, actualTargets, config.GrossAreaSchemeName, config, group.Name);
                        }

                        if (propagateDeductions)
                        {
                            groupCreatedCount += PropagateSchemeAreas(sourceLevel, actualTargets, config.DeductionAreaSchemeName, config, group.Name);
                        }

                        totalCreatedAreas += groupCreatedCount;
                        totalLevelsUpdated += actualTargets.Count;
                        processedGroups++;
                    }
                }

                tx.Commit();
            }

            if (processedGroups == 0)
            {
                return "All defined groups are single-level or have no target floors to duplicate.";
            }

            return string.Format("Successfully propagated {0} typical group(s) across {1} target floor(s). Created {2} area elements without recreating views.", 
                processedGroups, totalLevelsUpdated, totalCreatedAreas);
        }

        public string ClearPropagatedAreas(
            List<TypicalFloorGroup> groups,
            MappingConfig config,
            bool clearGross,
            bool clearDeductions)
        {
            if (groups == null || groups.Count == 0)
            {
                return "No typical floor groups defined to clear.";
            }

            int clearedLevelsCount = 0;
            int clearedElementsCount = 0;
            List<Level> allLevels = GetAllLevels();

            using (Transaction tx = new Transaction(_doc, "BauTools: Clear Propagated Areas"))
            {
                tx.Start();

                foreach (TypicalFloorGroup group in groups)
                {
                    if (group.IsSingleLevel)
                        continue; // Single floor: untouched

                    Level sourceLevel = allLevels.FirstOrDefault(l => string.Equals(l.Name, group.SourceLevelName, StringComparison.OrdinalIgnoreCase));
                    List<string> targetLevels = GetLevelsInRange(group.FromLevelName, group.ToLevelName);
                    List<string> actualTargets = targetLevels.Where(n => sourceLevel == null || !string.Equals(n, sourceLevel.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                    foreach (string targetLevelName in actualTargets)
                    {
                        Level targetLevel = allLevels.FirstOrDefault(l => string.Equals(l.Name, targetLevelName, StringComparison.OrdinalIgnoreCase));
                        if (targetLevel == null) continue;

                        int lvlDeleted = 0;

                        if (clearGross && !string.IsNullOrEmpty(config.GrossAreaSchemeName))
                        {
                            AreaScheme grossScheme = GetAreaSchemeByName(config.GrossAreaSchemeName);
                            if (grossScheme != null)
                            {
                                ViewPlan vp = GetExistingAreaViewPlan(targetLevel, grossScheme);
                                if (vp != null)
                                {
                                    lvlDeleted += ClearViewAreasAndLines(vp, targetLevel, grossScheme);
                                }
                            }
                        }

                        if (clearDeductions && !string.IsNullOrEmpty(config.DeductionAreaSchemeName))
                        {
                            AreaScheme dedScheme = GetAreaSchemeByName(config.DeductionAreaSchemeName);
                            if (dedScheme != null)
                            {
                                ViewPlan vp = GetExistingAreaViewPlan(targetLevel, dedScheme);
                                if (vp != null)
                                {
                                    lvlDeleted += ClearViewAreasAndLines(vp, targetLevel, dedScheme);
                                }
                            }
                        }

                        if (lvlDeleted > 0)
                        {
                            clearedElementsCount += lvlDeleted;
                            clearedLevelsCount++;
                        }
                    }
                }

                tx.Commit();
            }

            if (clearedLevelsCount == 0)
            {
                return "No propagated areas were found to clear. Source modeled levels remain intact.";
            }

            return string.Format("Successfully cleared {0} propagated element(s) across {1} target floor(s). Source floors and views were 100% preserved.",
                clearedElementsCount, clearedLevelsCount);
        }

        private int PropagateSchemeAreas(Level sourceLevel, List<string> targetLevelNames, string schemeName, MappingConfig config, string groupName)
        {
            if (string.IsNullOrEmpty(schemeName)) return 0;

            AreaScheme scheme = GetAreaSchemeByName(schemeName);
            if (scheme == null) return 0;

            ViewPlan sourceAreaView = GetExistingAreaViewPlan(sourceLevel, scheme);
            if (sourceAreaView == null) return 0;

            List<ElementId> sourceBoundaryLineIds = GetAreaBoundaryLinesInView(sourceAreaView);
            List<Area> sourceAreas = GetAreasInView(sourceAreaView, sourceLevel, scheme);

            if (sourceAreas.Count == 0 && sourceBoundaryLineIds.Count == 0) return 0;

            int createdAreasCount = 0;
            List<Level> allLevels = GetAllLevels();

            foreach (string targetLevelName in targetLevelNames)
            {
                if (string.Equals(targetLevelName, sourceLevel.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                Level targetLevel = allLevels.FirstOrDefault(l => string.Equals(l.Name, targetLevelName, StringComparison.OrdinalIgnoreCase));
                if (targetLevel == null) continue;

                ViewPlan targetAreaView = GetOrCreateAreaViewPlan(targetLevel, scheme);
                if (targetAreaView == null) continue;

                // 1. Clear target areas and lines without touching views
                ClearViewAreasAndLines(targetAreaView, targetLevel, scheme);

                // 2. Copy boundary lines
                if (sourceBoundaryLineIds.Count > 0)
                {
                    CopyPasteOptions options = new CopyPasteOptions();
                    ElementTransformUtils.CopyElements(sourceAreaView, sourceBoundaryLineIds, targetAreaView, Transform.Identity, options);
                }

                // 3. Create target areas
                foreach (Area srcArea in sourceAreas)
                {
                    LocationPoint loc = srcArea.Location as LocationPoint;
                    if (loc == null) continue;

                    UV pt = new UV(loc.Point.X, loc.Point.Y);
                    Area targetArea = _doc.Create.NewArea(targetAreaView, pt);

                    if (targetArea != null)
                    {
                        CopyAreaParameters(srcArea, targetArea, config);
                        
                        if (!string.IsNullOrEmpty(groupName))
                        {
                            Parameter pComm = targetArea.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                            if (pComm != null && !pComm.IsReadOnly)
                            {
                                pComm.Set(groupName);
                            }
                        }
                        createdAreasCount++;
                    }
                }
            }

            return createdAreasCount;
        }

        private AreaScheme GetAreaSchemeByName(string schemeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(AreaScheme));
            foreach (AreaScheme s in collector)
            {
                if (string.Equals(s.Name, schemeName, StringComparison.OrdinalIgnoreCase))
                    return s;
            }
            return null;
        }

        private ViewPlan GetExistingAreaViewPlan(Level level, AreaScheme scheme)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(ViewPlan));
            foreach (ViewPlan vp in collector)
            {
                if (!vp.IsTemplate && vp.LevelId == level.Id && vp.AreaScheme != null && vp.AreaScheme.Id == scheme.Id)
                {
                    return vp;
                }
            }
            return null;
        }

        private ViewPlan GetOrCreateAreaViewPlan(Level level, AreaScheme scheme)
        {
            ViewPlan existing = GetExistingAreaViewPlan(level, scheme);
            if (existing != null)
            {
                return existing;
            }

            try
            {
                ViewPlan newVp = ViewPlan.CreateAreaPlan(_doc, scheme.Id, level.Id);
                return newVp;
            }
            catch
            {
                return null;
            }
        }

        private List<ElementId> GetAreaBoundaryLinesInView(ViewPlan view)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc, view.Id)
                .OfCategory(BuiltInCategory.OST_AreaSchemeLines)
                .WhereElementIsNotElementType();

            return collector.Select(e => e.Id).ToList();
        }

        private List<Area> GetAreasInView(ViewPlan view, Level level, AreaScheme scheme)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Areas)
                .WhereElementIsNotElementType();

            List<Area> result = new List<Area>();
            foreach (Area a in collector.Cast<Area>())
            {
                if (a.Area > 0 && a.LevelId == level.Id && a.AreaScheme != null && a.AreaScheme.Id == scheme.Id)
                {
                    result.Add(a);
                }
            }
            return result;
        }

        private int ClearViewAreasAndLines(ViewPlan view, Level level, AreaScheme scheme)
        {
            List<ElementId> toDelete = new List<ElementId>();

            // 1. Boundary lines in view
            FilteredElementCollector lines = new FilteredElementCollector(_doc, view.Id)
                .OfCategory(BuiltInCategory.OST_AreaSchemeLines)
                .WhereElementIsNotElementType();
            toDelete.AddRange(lines.Select(e => e.Id));

            // 2. Areas assigned to this level and scheme
            FilteredElementCollector areas = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Areas)
                .WhereElementIsNotElementType();
            foreach (Area a in areas.Cast<Area>())
            {
                if (a.LevelId == level.Id && a.AreaScheme != null && a.AreaScheme.Id == scheme.Id)
                {
                    toDelete.Add(a.Id);
                }
            }

            if (toDelete.Count > 0)
            {
                _doc.Delete(toDelete);
                return toDelete.Count;
            }

            return 0;
        }

        private void CopyAreaParameters(Area srcArea, Area targetArea, MappingConfig config)
        {
            Parameter pNameSrc = srcArea.get_Parameter(BuiltInParameter.ROOM_NAME);
            Parameter pNameTgt = targetArea.get_Parameter(BuiltInParameter.ROOM_NAME);
            if (pNameSrc != null && pNameTgt != null && !pNameTgt.IsReadOnly)
            {
                pNameTgt.Set(pNameSrc.AsString() ?? string.Empty);
            }

            CopyParamByName(srcArea, targetArea, config.DeductionTypeParameterName);
            CopyParamByName(srcArea, targetArea, config.BuildingParameterName);
            CopyParamByName(srcArea, targetArea, config.UsageCategoryParameterName);
            CopyParamByName(srcArea, targetArea, "Comments");
            CopyParamByName(srcArea, targetArea, "Deduction");
            CopyParamByName(srcArea, targetArea, "Building");
        }

        private void CopyParamByName(Area src, Area tgt, string paramName)
        {
            if (string.IsNullOrEmpty(paramName)) return;

            Parameter pSrc = src.LookupParameter(paramName);
            Parameter pTgt = tgt.LookupParameter(paramName);

            if (pSrc != null && pTgt != null && !pTgt.IsReadOnly)
            {
                if (pSrc.StorageType == StorageType.String)
                {
                    pTgt.Set(pSrc.AsString() ?? string.Empty);
                }
                else if (pSrc.StorageType == StorageType.Double)
                {
                    pTgt.Set(pSrc.AsDouble());
                }
                else if (pSrc.StorageType == StorageType.Integer)
                {
                    pTgt.Set(pSrc.AsInteger());
                }
            }
        }
    }
}

```

### `ZoningFloorArea\Services\RevitAreaExtractor.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class RevitAreaExtractor
    {
        private readonly Document _doc;

        public RevitAreaExtractor(Document doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            _doc = doc;
        }

        public List<string> GetAreaSchemeNames()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(AreaScheme));
            List<string> list = new List<string>();
            foreach (AreaScheme s in collector)
            {
                list.Add(s.Name);
            }
            list.Sort();
            return list;
        }

        public List<string> GetAvailableAreaParameters()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Areas)
                .WhereElementIsNotElementType();

            Area sampleArea = null;
            foreach (Area a in collector)
            {
                sampleArea = a;
                break;
            }

            if (sampleArea == null)
            {
                return new List<string> { "Building", "Deduction", "Name", "Comments", "Area Type", "Number" };
            }

            List<string> paramNames = new List<string>();
            paramNames.Add("Building");
            paramNames.Add("Deduction");

            foreach (Parameter p in sampleArea.Parameters)
            {
                if (p.Definition != null && !paramNames.Contains(p.Definition.Name))
                {
                    paramNames.Add(p.Definition.Name);
                }
            }
            return paramNames;
        }

        public List<AreaDataModel> ExtractAreas(MappingConfig config)
        {
            List<AreaDataModel> results = new List<AreaDataModel>();

            FilteredElementCollector collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Areas)
                .WhereElementIsNotElementType();

            foreach (Area area in collector)
            {
                if (area.Area <= 0) continue;

                string schemeName = area.AreaScheme != null ? area.AreaScheme.Name : string.Empty;
                string levelName = area.Level != null ? area.Level.Name : "Unassigned";
                double levelElevation = area.Level != null ? area.Level.Elevation : 0.0;

                // Extract Building Name
                string bldgName = GetParameterStringValue(area, config.BuildingParameterName);
                if (string.IsNullOrEmpty(bldgName))
                {
                    bldgName = GetParameterStringValue(area, "Building");
                }
                if (string.IsNullOrEmpty(bldgName))
                {
                    bldgName = string.IsNullOrEmpty(config.BuildingName) ? "BUILDING C" : config.BuildingName;
                }

                // Extract Deduction Type
                string deductionType = GetParameterStringValue(area, config.DeductionTypeParameterName);
                if (string.IsNullOrEmpty(deductionType))
                {
                    deductionType = GetParameterStringValue(area, "Deduction");
                }
                if (string.IsNullOrEmpty(deductionType))
                {
                    deductionType = area.Name;
                }

                string usageCategory = GetParameterStringValue(area, config.UsageCategoryParameterName);
                if (string.IsNullOrEmpty(usageCategory))
                {
                    usageCategory = "Residential";
                }
                else if (usageCategory.IndexOf("Commercial", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    usageCategory = "Commercial";
                }
                else
                {
                    usageCategory = "Residential";
                }

                AreaDataModel model = new AreaDataModel();
                model.ElementId = area.Id.ToString();
                model.Name = area.Name;
                model.AreaValue = area.Area;
                model.LevelName = levelName;
                model.LevelElevation = levelElevation;
                model.AreaSchemeName = schemeName;
                model.DeductionType = deductionType;
                model.UsageCategory = usageCategory;
                model.BuildingName = bldgName.Trim().ToUpper();

                results.Add(model);
            }

            return results;
        }

        private string GetParameterStringValue(Area area, string paramName)
        {
            if (string.IsNullOrEmpty(paramName)) return string.Empty;

            Parameter p = area.LookupParameter(paramName);
            if (p == null && string.Equals(paramName, "Name", StringComparison.OrdinalIgnoreCase))
            {
                p = area.get_Parameter(BuiltInParameter.ROOM_NAME);
            }

            if (p == null) return string.Empty;

            switch (p.StorageType)
            {
                case StorageType.String:
                    return p.AsString() ?? string.Empty;
                case StorageType.ElementId:
                    Element el = _doc.GetElement(p.AsElementId());
                    return el != null ? el.Name : string.Empty;
                default:
                    return p.AsValueString() ?? string.Empty;
            }
        }
    }
}

```

### `ZoningFloorArea\Services\RevitLotDrawerService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class LotDrawOptions
    {
        public LotElementType ElementType { get; set; }
        public LotAnchorCorner AnchorCorner { get; set; }
        public bool AlignWithPbp { get; set; }

        // Toggles
        public bool EnsureLevel1Placement { get; set; }
        public bool DrawSubjectLot { get; set; }
        public bool DrawAdjacentLots { get; set; }
        public bool DrawRemainingBlockLots { get; set; }
        public bool DrawSidewalks { get; set; }
        public double SidewalkWidthFt { get; set; }
        public bool PlaceStreetTextNotes { get; set; }

        // Grouping
        public LotGroupingMode GroupingMode { get; set; }
        public bool PinCreatedGroup { get; set; }

        // Zoning Drafting Table View (Proposal B)
        public bool GenerateZoningDraftingTable { get; set; }

        // 3D Building Masses (Extrusions with real NYC heights)
        public bool Create3DBuildingMasses { get; set; }
        public bool ExtrudeSubjectLotBuilding { get; set; }

        // Custom Line Style Names
        public string SubjectLineStyle { get; set; }
        public string AdjacentLineStyle { get; set; }
        public string BlockContextLineStyle { get; set; }
        public string SidewalkLineStyle { get; set; }

        public Level TargetLevel { get; set; }

        public LotDrawOptions()
        {
            ElementType = LotElementType.ModelCurves;
            AnchorCorner = LotAnchorCorner.Southwest;
            AlignWithPbp = true;
            EnsureLevel1Placement = true;
            DrawSubjectLot = true;
            DrawAdjacentLots = true;
            DrawRemainingBlockLots = true;
            DrawSidewalks = true;
            SidewalkWidthFt = 12.0;
            PlaceStreetTextNotes = true;
            GroupingMode = LotGroupingMode.SingleGroup;
            PinCreatedGroup = false;
            GenerateZoningDraftingTable = true;
            Create3DBuildingMasses = true;
            ExtrudeSubjectLotBuilding = false;
            SubjectLineStyle = RevitLotDrawerService.STYLE_SUBJECT_RED;
            AdjacentLineStyle = RevitLotDrawerService.STYLE_ADJACENT_ORANGE;
            BlockContextLineStyle = RevitLotDrawerService.STYLE_CONTEXT_GRAY;
            SidewalkLineStyle = RevitLotDrawerService.STYLE_SIDEWALK_BLUE;
            TargetLevel = null;
        }
    }

    public class LotDrawResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string GroupName { get; set; }
        public string DraftingViewName { get; set; }
        public int SubjectCurvesCount { get; set; }
        public int AdjacentLotsCount { get; set; }
        public int AdjacentCurvesCount { get; set; }
        public int ContextLotsCount { get; set; }
        public int ContextCurvesCount { get; set; }
        public int SidewalkCurvesCount { get; set; }
        public int BuildingMassesCount { get; set; }
        public int TextNotesCount { get; set; }

        public LotDrawResult()
        {
            Message = string.Empty;
            GroupName = string.Empty;
            DraftingViewName = string.Empty;
        }
    }

    public class RevitLotDrawerService
    {
        private readonly Document _doc;

        public const string STYLE_SUBJECT_RED     = "NYC Lot - Subject (Red)";
        public const string STYLE_ADJACENT_ORANGE = "NYC Lot - Adjacent (Orange)";
        public const string STYLE_CONTEXT_GRAY    = "NYC Block - Context (Gray)";
        public const string STYLE_SIDEWALK_BLUE   = "NYC Sidewalk - Curb (Blue)";
        public const string SUBCAT_CONTEXT_BLDG   = "NYC Context Building";
        public const string MATERIAL_CONTEXT_MASS = "NYC - Urban Context";

        public RevitLotDrawerService(Document doc)
        {
            _doc = doc;
        }

        public XYZ GetProjectBasePointPosition()
        {
            try
            {
                var basePoint = new FilteredElementCollector(_doc)
                    .OfCategory(BuiltInCategory.OST_ProjectBasePoint)
                    .WhereElementIsNotElementType()
                    .Cast<BasePoint>()
                    .FirstOrDefault();

                if (basePoint != null)
                {
                    return basePoint.Position;
                }
            }
            catch
            {
                // Fallback
            }
            return XYZ.Zero;
        }

        public Level GetLevel1()
        {
            var levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            if (levels.Count == 0) return null;

            var lvl1 = levels.FirstOrDefault(l =>
                l.Name.Contains("1", StringComparison.OrdinalIgnoreCase) ||
                l.Name.Contains("FIRST", StringComparison.OrdinalIgnoreCase) ||
                l.Name.Contains("NIVEL 1", StringComparison.OrdinalIgnoreCase) ||
                l.Name.Contains("LEVEL 1", StringComparison.OrdinalIgnoreCase) ||
                l.Name.Contains("01", StringComparison.OrdinalIgnoreCase));

            return lvl1 ?? levels[0];
        }

        public List<string> GetAvailableLineStyles()
        {
            var styles = new List<string>
            {
                STYLE_SUBJECT_RED,
                STYLE_ADJACENT_ORANGE,
                STYLE_CONTEXT_GRAY,
                STYLE_SIDEWALK_BLUE
            };

            try
            {
                Categories categories = _doc.Settings.Categories;
                Category linesCat = categories.get_Item(BuiltInCategory.OST_Lines);
                if (linesCat != null)
                {
                    foreach (Category subCat in linesCat.SubCategories)
                    {
                        if (!styles.Contains(subCat.Name, StringComparer.OrdinalIgnoreCase))
                        {
                            styles.Add(subCat.Name);
                        }
                    }
                }
            }
            catch
            {
                // Fallback
            }

            return styles;
        }

        public LotDrawResult DrawLotWithContext(NycBlockContext blockContext, LotDrawOptions options)
        {
            var subjectLot = blockContext.SubjectLot;
            if (subjectLot.PolygonRings.Count == 0)
            {
                return new LotDrawResult
                {
                    Success = false,
                    Message = "No polygon geometry found for this NYC Tax Lot."
                };
            }

            XYZ pbpPos = options.AlignWithPbp ? GetProjectBasePointPosition() : XYZ.Zero;
            XYZ anchorPoint = subjectLot.GetAnchorPoint(options.AnchorCorner);

            Level level1 = options.TargetLevel != null ? options.TargetLevel : GetLevel1();
            double zElevation = level1 != null ? level1.Elevation : pbpPos.Z;

            double offsetX = pbpPos.X - anchorPoint.X;
            double offsetY = pbpPos.Y - anchorPoint.Y;
            double tolerance = _doc.Application.ShortCurveTolerance;

            using (Transaction tx = new Transaction(_doc, string.Format("BauTools - NYC Lot {0} & 3D Masses Group", subjectLot.Bbl)))
            {
                tx.Start();

            try
            {
                EnsurePresetStylesExist();

                GraphicsStyle styleSubject = ResolveLineStyle(options.SubjectLineStyle, STYLE_SUBJECT_RED, new Color(220, 38, 38), 4);
                GraphicsStyle styleAdjacent = ResolveLineStyle(options.AdjacentLineStyle, STYLE_ADJACENT_ORANGE, new Color(234, 88, 12), 2);
                GraphicsStyle styleContext = ResolveLineStyle(options.BlockContextLineStyle, STYLE_CONTEXT_GRAY, new Color(148, 163, 184), 1);
                GraphicsStyle styleSidewalk = ResolveLineStyle(options.SidewalkLineStyle, STYLE_SIDEWALK_BLUE, new Color(2, 132, 199), 2);

                Plane plane = level1 != null
                    ? Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, level1.Elevation))
                    : Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, zElevation));
                SketchPlane sketchPlane = SketchPlane.Create(_doc, plane);

                var subjectElementsToGroup = new List<ElementId>();
                var contextElementsToGroup = new List<ElementId>();

                int subjectCurvesCount = 0;
                int adjacentLotsCount = 0;
                int adjacentCurvesCount = 0;
                int contextLotsCount = 0;
                int contextCurvesCount = 0;
                int sidewalkCurvesCount = 0;
                int buildingMassesCount = 0;
                int textNotesCount = 0;

                // 1. Build Subject Lot Curves
                var subjectLoops = BuildCurveLoops(subjectLot.PolygonRings, offsetX, offsetY, zElevation, tolerance);

                // 2. Draw Subject Lot Lines (on Level 1)
                if (options.DrawSubjectLot && subjectLoops.Count > 0)
                {
                    var ids = DrawLoopsWithIds(subjectLoops, options.ElementType, sketchPlane, styleSubject,
                        string.Format("NYC Development Lot {0} ({1}) - Zoning: {2} - Area: {3:N0} SF", subjectLot.Bbl, subjectLot.Address, subjectLot.GetZoningSummary(), subjectLot.LotAreaSqFt));
                    subjectCurvesCount = ids.Count;
                    subjectElementsToGroup.AddRange(ids);
                }

                // 3. Draw Adjacent Lots (Immediate Neighbors)
                if (options.DrawAdjacentLots && blockContext.AdjacentLots.Count > 0)
                {
                    foreach (var adjLot in blockContext.AdjacentLots)
                    {
                        var adjLoops = BuildCurveLoops(adjLot.PolygonRings, offsetX, offsetY, zElevation, tolerance);
                        if (adjLoops.Count > 0)
                        {
                            var ids = DrawLoopsWithIds(adjLoops, options.ElementType, sketchPlane, styleAdjacent,
                                string.Format("NYC Adjacent Lot {0} (BBL: {1}, {2})", adjLot.Lot, adjLot.Bbl, adjLot.Address));
                            if (ids.Count > 0)
                            {
                                adjacentCurvesCount += ids.Count;
                                adjacentLotsCount++;
                                contextElementsToGroup.AddRange(ids);
                            }
                        }
                    }
                }

                // 4. Draw Remaining Block Lots
                if (options.DrawRemainingBlockLots && blockContext.RemainingBlockLots.Count > 0)
                {
                    foreach (var lot in blockContext.RemainingBlockLots)
                    {
                        var contextLoops = BuildCurveLoops(lot.PolygonRings, offsetX, offsetY, zElevation, tolerance);
                        if (contextLoops.Count > 0)
                        {
                            var ids = DrawLoopsWithIds(contextLoops, options.ElementType, sketchPlane, styleContext,
                                string.Format("NYC Block {0} - Lot {1} ({2})", blockContext.BlockNumber, lot.Lot, lot.Address));
                            if (ids.Count > 0)
                            {
                                contextCurvesCount += ids.Count;
                                contextLotsCount++;
                                contextElementsToGroup.AddRange(ids);
                            }
                        }
                    }
                }

                // 5. Draw Sidewalk Curbs (12 ft perimeter buffer on Level 1)
                if (options.DrawSidewalks)
                {
                    double swOffset = options.SidewalkWidthFt > 0 ? options.SidewalkWidthFt : 12.0;

                    double bMinX = blockContext.AllLots.Count > 1 ? blockContext.MinX : subjectLot.MinX;
                    double bMaxX = blockContext.AllLots.Count > 1 ? blockContext.MaxX : subjectLot.MaxX;
                    double bMinY = blockContext.AllLots.Count > 1 ? blockContext.MinY : subjectLot.MinY;
                    double bMaxY = blockContext.AllLots.Count > 1 ? blockContext.MaxY : subjectLot.MaxY;

                    double swMinX = bMinX + offsetX - swOffset;
                    double swMaxX = bMaxX + offsetX + swOffset;
                    double swMinY = bMinY + offsetY - swOffset;
                    double swMaxY = bMaxY + offsetY + swOffset;

                    XYZ p1 = new XYZ(swMinX, swMinY, zElevation);
                    XYZ p2 = new XYZ(swMaxX, swMinY, zElevation);
                    XYZ p3 = new XYZ(swMaxX, swMaxY, zElevation);
                    XYZ p4 = new XYZ(swMinX, swMaxY, zElevation);

                    var sidewalkLoop = new CurveLoop();
                    sidewalkLoop.Append(Line.CreateBound(p1, p2));
                    sidewalkLoop.Append(Line.CreateBound(p2, p3));
                    sidewalkLoop.Append(Line.CreateBound(p3, p4));
                    sidewalkLoop.Append(Line.CreateBound(p4, p1));

                    var ids = DrawLoopsWithIds(new List<CurveLoop> { sidewalkLoop }, LotElementType.ModelCurves, sketchPlane, styleSidewalk,
                        string.Format("NYC Block {0} - Sidewalk Curb Perimeter ({1} ft width)", blockContext.BlockNumber, swOffset));
                    sidewalkCurvesCount = ids.Count;
                    contextElementsToGroup.AddRange(ids);
                }

                // 6. Generate 3D Building Masses in Generic Models > NYC Context Building
                if (options.Create3DBuildingMasses)
                {
                    Category subCat = GetOrCreateContextSubcategory();

                    if (blockContext.Buildings.Count > 0)
                    {
                        foreach (var bldg in blockContext.Buildings)
                        {
                            if (bldg.IsSubjectLotBuilding && !options.ExtrudeSubjectLotBuilding)
                                continue;

                            var bldgLoops = BuildNormalizedBuildingLoops(bldg.PolygonRings, offsetX, offsetY, zElevation, tolerance);
                            if (bldgLoops.Count > 0 && bldg.EffectiveHeightFt > 5.0)
                            {
                                try
                                {
                                    Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(bldgLoops, XYZ.BasisZ, bldg.EffectiveHeightFt);
                                    if (solid != null && solid.Volume > 0.01)
                                    {
                                        ElementId catId = (subCat != null && subCat.Id != ElementId.InvalidElementId)
                                            ? subCat.Id
                                            : new ElementId(BuiltInCategory.OST_GenericModel);

                                        DirectShape ds = DirectShape.CreateElement(_doc, catId);
                                        ds.SetShape(new List<GeometryObject> { solid });
                                        ds.Name = string.Format("NYC Building BIN {0} ({1:F0} ft)", bldg.Bin, bldg.EffectiveHeightFt);
                                        var comm = ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                                        if (comm != null) comm.Set(string.Format("Address: {0} | BIN: {1} | Roof Height: {2:F1} ft | Floors: {3} | Year: {4}", bldg.Address, bldg.Bin, bldg.HeightRoofFt, bldg.NumFloors, bldg.YearBuilt));

                                        if (bldg.IsSubjectLotBuilding)
                                            subjectElementsToGroup.Add(ds.Id);
                                        else
                                            contextElementsToGroup.Add(ds.Id);

                                        buildingMassesCount++;
                                    }
                                }
                                catch
                                {
                                    // Skip invalid solid
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var lot in blockContext.AllLots)
                        {
                            if (lot.Bbl == subjectLot.Bbl && !options.ExtrudeSubjectLotBuilding)
                                continue;

                            var lotLoops = BuildCurveLoops(lot.PolygonRings, offsetX, offsetY, zElevation, tolerance);
                            double height = lot.NumFloors > 0 ? lot.NumFloors * 12.0 : 36.0;
                            if (lotLoops.Count > 0 && height > 5.0)
                            {
                                try
                                {
                                    Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(lotLoops, XYZ.BasisZ, height);
                                    if (solid != null && solid.Volume > 0.01)
                                    {
                                        ElementId catId = (subCat != null && subCat.Id != ElementId.InvalidElementId)
                                            ? subCat.Id
                                            : new ElementId(BuiltInCategory.OST_GenericModel);

                                        DirectShape ds = DirectShape.CreateElement(_doc, catId);
                                        ds.SetShape(new List<GeometryObject> { solid });
                                        ds.Name = string.Format("NYC Lot {0} Mass ({1:F0} ft)", lot.Bbl, height);
                                        var comm = ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                                        if (comm != null) comm.Set(string.Format("Lot: {0} | BBL: {1} | Height: {2:F0} ft | Floors: {3} | Zoning: {4}", lot.Address, lot.Bbl, height, lot.NumFloors, lot.GetZoningSummary()));

                                        if (lot.Bbl == subjectLot.Bbl)
                                            subjectElementsToGroup.Add(ds.Id);
                                        else
                                            contextElementsToGroup.Add(ds.Id);

                                        buildingMassesCount++;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }

                // 7. Place Surrounding Street Titles as Text Notes
                if (options.PlaceStreetTextNotes)
                {
                    var streets = blockContext.GetSurroundingStreetNames();
                    double swOffset = options.SidewalkWidthFt > 0 ? options.SidewalkWidthFt : 12.0;
                    double textDistance = swOffset + 18.0;

                    double bMinX = blockContext.AllLots.Count > 1 ? blockContext.MinX : subjectLot.MinX;
                    double bMaxX = blockContext.AllLots.Count > 1 ? blockContext.MaxX : subjectLot.MaxX;
                    double bMinY = blockContext.AllLots.Count > 1 ? blockContext.MinY : subjectLot.MinY;
                    double bMaxY = blockContext.AllLots.Count > 1 ? blockContext.MaxY : subjectLot.MaxY;

                    double midX = (bMinX + bMaxX) / 2.0 + offsetX;
                    double midY = (bMinY + bMaxY) / 2.0 + offsetY;

                    double northY = bMaxY + offsetY + textDistance;
                    double southY = bMinY + offsetY - textDistance;
                    double eastX = bMaxX + offsetX + textDistance;
                    double westX = bMinX + offsetX - textDistance;

                    string northSt;
                    if (streets.TryGetValue("North", out northSt) && !string.IsNullOrWhiteSpace(northSt))
                    {
                        if (CreateTextAnnotation(new XYZ(midX, northY, zElevation), northSt.ToUpperInvariant()))
                            textNotesCount++;
                    }
                    string southSt;
                    if (streets.TryGetValue("South", out southSt) && !string.IsNullOrWhiteSpace(southSt))
                    {
                        if (CreateTextAnnotation(new XYZ(midX, southY, zElevation), southSt.ToUpperInvariant()))
                            textNotesCount++;
                    }
                    string eastSt;
                    if (streets.TryGetValue("East", out eastSt) && !string.IsNullOrWhiteSpace(eastSt))
                    {
                        if (CreateTextAnnotation(new XYZ(eastX, midY, zElevation), eastSt.ToUpperInvariant()))
                            textNotesCount++;
                    }
                    string westSt;
                    if (streets.TryGetValue("West", out westSt) && !string.IsNullOrWhiteSpace(westSt))
                    {
                        if (CreateTextAnnotation(new XYZ(westX, midY, zElevation), westSt.ToUpperInvariant()))
                            textNotesCount++;
                    }
                }

                // 8. Group Creation (Single vs Split Groups)
                string groupResultSummary = string.Empty;
                if (options.GroupingMode == LotGroupingMode.SingleGroup)
                {
                    var allElements = new List<ElementId>();
                    allElements.AddRange(subjectElementsToGroup);
                    allElements.AddRange(contextElementsToGroup);

                    if (allElements.Count > 0)
                    {
                        string baseName = !string.IsNullOrWhiteSpace(subjectLot.Address)
                            ? subjectLot.Address.Trim().ToUpperInvariant()
                            : string.Format("NYC Lot - BBL {0}", subjectLot.Bbl);

                        var grp = CreateAndNameGroup(allElements, baseName, string.Format("BBL: {0} | Zoning: {1} | Area: {2:N0} SF | Block: {3}", subjectLot.Bbl, subjectLot.GetZoningSummary(), subjectLot.LotAreaSqFt, blockContext.BlockNumber), options.PinCreatedGroup);
                        if (grp != null)
                        {
                            groupResultSummary = string.Format("📦 Group: [{0}]", grp.GroupType.Name);
                        }
                    }
                }
                else if (options.GroupingMode == LotGroupingMode.SplitGroups)
                {
                    var groupNames = new List<string>();

                    if (subjectElementsToGroup.Count > 0)
                    {
                        string lotName = !string.IsNullOrWhiteSpace(subjectLot.Address)
                            ? string.Format("NYC Lot - {0}", subjectLot.Address.Trim().ToUpperInvariant())
                            : string.Format("NYC Lot - BBL {0}", subjectLot.Bbl);

                        var grp1 = CreateAndNameGroup(subjectElementsToGroup, lotName, string.Format("Development Lot {0} | Zoning: {1} | Area: {2:N0} SF", subjectLot.Bbl, subjectLot.GetZoningSummary(), subjectLot.LotAreaSqFt), options.PinCreatedGroup);
                        if (grp1 != null) groupNames.Add(grp1.GroupType.Name);
                    }

                    if (contextElementsToGroup.Count > 0)
                    {
                        string ctxName = string.Format("NYC Context - Block {0}", blockContext.BlockNumber);
                        var grp2 = CreateAndNameGroup(contextElementsToGroup, ctxName, string.Format("NYC Context Block {0} ({1} adjacent, {2} block lots, {3} 3D masses)", blockContext.BlockNumber, adjacentLotsCount, contextLotsCount, buildingMassesCount), options.PinCreatedGroup);
                        if (grp2 != null) groupNames.Add(grp2.GroupType.Name);
                    }

                    if (groupNames.Count > 0)
                    {
                        groupResultSummary = string.Format("📦 Groups: [{0}]", string.Join("] & [", groupNames));
                    }
                }

                // 9. Generate Native Revit Zoning Summary Table (Drafting View - Proposal B)
                string draftingViewName = string.Empty;
                if (options.GenerateZoningDraftingTable)
                {
                    try
                    {
                        var dv = CreateZoningSummaryDraftingView(subjectLot, blockContext);
                        if (dv != null)
                        {
                            draftingViewName = dv.Name;
                        }
                    }
                    catch
                    {
                        // Fallback
                    }
                }

                _doc.Regenerate();
                tx.Commit();

                string levelName = level1 != null ? level1.Name : "Level 1";
                string dvMsg = !string.IsNullOrEmpty(draftingViewName) ? string.Format("\n📊 Zoning Table View: [{0}]", draftingViewName) : "";

                return new LotDrawResult
                {
                    Success = true,
                    GroupName = groupResultSummary,
                    DraftingViewName = draftingViewName,
                    SubjectCurvesCount = subjectCurvesCount,
                    AdjacentLotsCount = adjacentLotsCount,
                    AdjacentCurvesCount = adjacentCurvesCount,
                    ContextLotsCount = contextLotsCount,
                    ContextCurvesCount = contextCurvesCount,
                    SidewalkCurvesCount = sidewalkCurvesCount,
                    BuildingMassesCount = buildingMassesCount,
                    TextNotesCount = textNotesCount,
                    Message = string.Format("Successfully created on [{0}]: Development Lot {1} + {2} adjacent lots + {3} block lots + {4} 3D building masses + {5} street titles.\n\n{6}{7}", levelName, subjectLot.Bbl, adjacentLotsCount, contextLotsCount, buildingMassesCount, textNotesCount, groupResultSummary, dvMsg)
                };
            }
            catch (Exception ex)
            {
                if (tx.HasStarted())
                    tx.RollBack();

                return new LotDrawResult
                {
                    Success = false,
                    Message = string.Format("Error drawing block context in Revit: {0}", ex.Message)
                };
            }
            }
        }

        private Group CreateAndNameGroup(List<ElementId> elementIds, string baseName, string comments, bool pinGroup)
        {
            if (elementIds.Count == 0) return null;

            try
            {
                Group createdGroup = _doc.Create.NewGroup(elementIds);
                if (createdGroup != null)
                {
                    string uniqueName = baseName;
                    int suffix = 1;
                    while (GroupTypeExists(uniqueName))
                    {
                        uniqueName = string.Format("{0} ({1})", baseName, suffix++);
                    }

                    try
                    {
                        createdGroup.GroupType.Name = uniqueName;
                    }
                    catch { }

                    try
                    {
                        var comm = createdGroup.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                        if (comm != null) comm.Set(comments);
                    }
                    catch { }

                    if (pinGroup)
                    {
                        createdGroup.Pinned = true;
                    }

                    return createdGroup;
                }
            }
            catch
            {
                // Fallback
            }
            return null;
        }

        /// <summary>
        /// Creates a professional native Drafting View containing the full NYC Zoning & MapPLUTO calculation table.
        /// </summary>
        private ViewDrafting CreateZoningSummaryDraftingView(NycLotInfo lot, NycBlockContext blockContext)
        {
            ViewFamilyType draftingVft = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(v => v.ViewFamily == ViewFamily.Drafting);

            if (draftingVft == null) return null;

            string baseViewName = !string.IsNullOrWhiteSpace(lot.Address)
                ? string.Format("NYC Zoning - {0}", lot.Address.Trim().ToUpperInvariant())
                : string.Format("NYC Zoning - BBL {0}", lot.Bbl);

            string viewName = baseViewName;
            int counter = 1;
            while (new FilteredElementCollector(_doc).OfClass(typeof(ViewDrafting)).Cast<ViewDrafting>().Any(v => v.Name.Equals(viewName, StringComparison.OrdinalIgnoreCase)))
            {
                viewName = string.Format("{0} ({1})", baseViewName, counter++);
            }

            ViewDrafting dv = ViewDrafting.Create(_doc, draftingVft.Id);
            dv.Name = viewName;
            dv.Scale = 1; // 1:1

            ElementId textTypeId = _doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);

            // Table dimensions (in feet inside drafting view)
            double tableWidth = 6.0;
            double col1W = 2.0;
            double col2W = 1.2;
            double col3W = 1.4;
            double col4W = 1.4;
            double rowH = 0.28;
            double headerH = 0.40;
            double titleH = 0.45;
            double startX = 0.0;
            double curY = 0.0;

            // 1. Title
            DrawRect(dv, startX, curY - titleH, tableWidth, titleH);
            AddCellText(dv, startX, curY - titleH, tableWidth, titleH,
                string.Format("NYC ZONING & PLUTO URBAN ANALYSIS — {0}", lot.Address.ToUpperInvariant()), textTypeId, HorizontalTextAlignment.Center);
            curY -= titleH;

            // 2. Identification Subheaders
            DrawRect(dv, startX, curY - rowH, tableWidth, rowH);
            AddCellText(dv, startX, curY - rowH, tableWidth, rowH,
                string.Format("BBL: {0}   |   Borough: {1}   |   Block: {2}   |   Lot: {3}   |   ZIP: {4}", lot.Bbl, lot.Borough, lot.Block, lot.Lot, lot.ZipCode), textTypeId, HorizontalTextAlignment.Left);
            curY -= rowH;

            DrawRect(dv, startX, curY - rowH, tableWidth, rowH);
            AddCellText(dv, startX, curY - rowH, tableWidth, rowH,
                string.Format("Zoning District(s): {0}   |   Owner: {1}", lot.GetZoningSummary(), string.IsNullOrEmpty(lot.OwnerName) ? "N/A" : lot.OwnerName), textTypeId, HorizontalTextAlignment.Left);
            curY -= rowH;

            DrawRect(dv, startX, curY - rowH, tableWidth, rowH);
            AddCellText(dv, startX, curY - rowH, tableWidth, rowH,
                string.Format("Land Use: {0}   |   Bldg Class: {1}   |   Year Built: {2}   |   Floors: {3}", lot.LandUse, lot.BuildingClass, lot.YearBuilt > 0 ? lot.YearBuilt.ToString() : "N/A", lot.NumFloors), textTypeId, HorizontalTextAlignment.Left);
            curY -= rowH;

            // 3. Matrix Table Header
            DrawRect(dv, startX, curY - headerH, col1W, headerH);
            AddCellText(dv, startX, curY - headerH, col1W, headerH, "ZONING METRIC", textTypeId, HorizontalTextAlignment.Center);

            DrawRect(dv, startX + col1W, curY - headerH, col2W, headerH);
            AddCellText(dv, startX + col1W, curY - headerH, col2W, headerH, "FAR RATIO", textTypeId, HorizontalTextAlignment.Center);

            DrawRect(dv, startX + col1W + col2W, curY - headerH, col3W, headerH);
            AddCellText(dv, startX + col1W + col2W, curY - headerH, col3W, headerH, "LOT AREA (SF)", textTypeId, HorizontalTextAlignment.Center);

            DrawRect(dv, startX + col1W + col2W + col3W, curY - headerH, col4W, headerH);
            AddCellText(dv, startX + col1W + col2W + col3W, curY - headerH, col4W, headerH, "MAX ALLOWABLE GFA", textTypeId, HorizontalTextAlignment.Center);
            curY -= headerH;

            // 4. Matrix Rows
            double maxResGfa = lot.LotAreaSqFt * lot.ResFar;
            double maxComGfa = lot.LotAreaSqFt * lot.CommFar;
            double maxFacGfa = lot.LotAreaSqFt * lot.FacilFar;

            DrawMatrixRow(dv, startX, ref curY, col1W, col2W, col3W, col4W, rowH, "Residential FAR", lot.ResFar.ToString("F2"), string.Format("{0:N0} SF", lot.LotAreaSqFt), maxResGfa > 0 ? string.Format("{0:N0} SF", maxResGfa) : "Not Permitted", textTypeId);
            DrawMatrixRow(dv, startX, ref curY, col1W, col2W, col3W, col4W, rowH, "Commercial FAR", lot.CommFar.ToString("F2"), string.Format("{0:N0} SF", lot.LotAreaSqFt), maxComGfa > 0 ? string.Format("{0:N0} SF", maxComGfa) : "Not Permitted", textTypeId);
            DrawMatrixRow(dv, startX, ref curY, col1W, col2W, col3W, col4W, rowH, "Community Facility FAR", lot.FacilFar.ToString("F2"), string.Format("{0:N0} SF", lot.LotAreaSqFt), maxFacGfa > 0 ? string.Format("{0:N0} SF", maxFacGfa) : "Not Permitted", textTypeId);
            DrawMatrixRow(dv, startX, ref curY, col1W, col2W, col3W, col4W, rowH, "Built / Existing FAR", lot.BuiltFar.ToString("F2"), string.Format("{0:N0} SF", lot.LotAreaSqFt), string.Format("{0:N0} SF (Existing)", lot.TotalBldgAreaSqFt), textTypeId);
            DrawMatrixRow(dv, startX, ref curY, col1W, col2W, col3W, col4W, rowH, "Lot Dimensions (W x D)", "-", string.Format("{0:N0} SF", lot.LotAreaSqFt), string.Format("{0:F1} ft x {1:F1} ft", lot.WidthFt, lot.DepthFt), textTypeId);

            // 5. Context Summary Footer
            var streets = blockContext.GetSurroundingStreetNames();
            string streetStr = streets.Count > 0 ? string.Join(", ", streets.Values) : "N/A";
            DrawRect(dv, startX, curY - rowH, tableWidth, rowH);
            AddCellText(dv, startX, curY - rowH, tableWidth, rowH,
                string.Format("Block {0} Context: {1} Lots | {2} 3D Buildings | Streets: {3}", blockContext.BlockNumber, blockContext.AllLots.Count, blockContext.Buildings.Count, streetStr), textTypeId, HorizontalTextAlignment.Left);
            curY -= rowH;

            return dv;
        }

        private void DrawMatrixRow(ViewDrafting dv, double startX, ref double curY, double c1, double c2, double c3, double c4, double rowH, string t1, string t2, string t3, string t4, ElementId textTypeId)
        {
            DrawRect(dv, startX, curY - rowH, c1, rowH);
            AddCellText(dv, startX, curY - rowH, c1, rowH, t1, textTypeId, HorizontalTextAlignment.Left);

            DrawRect(dv, startX + c1, curY - rowH, c2, rowH);
            AddCellText(dv, startX + c1, curY - rowH, c2, rowH, t2, textTypeId, HorizontalTextAlignment.Center);

            DrawRect(dv, startX + c1 + c2, curY - rowH, c3, rowH);
            AddCellText(dv, startX + c1 + c2, curY - rowH, c3, rowH, t3, textTypeId, HorizontalTextAlignment.Center);

            DrawRect(dv, startX + c1 + c2 + c3, curY - rowH, c4, rowH);
            AddCellText(dv, startX + c1 + c2 + c3, curY - rowH, c4, rowH, t4, textTypeId, HorizontalTextAlignment.Center);

            curY -= rowH;
        }

        private void DrawRect(ViewDrafting dv, double x, double y, double width, double height)
        {
            XYZ p1 = new XYZ(x, y, 0);
            XYZ p2 = new XYZ(x + width, y, 0);
            XYZ p3 = new XYZ(x + width, y + height, 0);
            XYZ p4 = new XYZ(x, y + height, 0);

            _doc.Create.NewDetailCurve(dv, Line.CreateBound(p1, p2));
            _doc.Create.NewDetailCurve(dv, Line.CreateBound(p2, p3));
            _doc.Create.NewDetailCurve(dv, Line.CreateBound(p3, p4));
            _doc.Create.NewDetailCurve(dv, Line.CreateBound(p4, p1));
        }

        private void AddCellText(ViewDrafting dv, double x, double y, double width, double height, string text, ElementId textTypeId, HorizontalTextAlignment align)
        {
            try
            {
                double posX = align == HorizontalTextAlignment.Center ? x + (width / 2.0) : x + 0.08;
                double posY = y + (height / 2.0);

                var opts = new TextNoteOptions
                {
                    HorizontalAlignment = align,
                    TypeId = textTypeId
                };

                TextNote.Create(_doc, dv.Id, new XYZ(posX, posY, 0), text, opts);
            }
            catch { }
        }

        private bool GroupTypeExists(string name)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(GroupType))
                .Cast<GroupType>()
                .Any(gt => gt.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public Material GetOrCreateUrbanContextMaterial()
        {
            try
            {
                var existingMat = new FilteredElementCollector(_doc)
                    .OfClass(typeof(Material))
                    .Cast<Material>()
                    .FirstOrDefault(m => m.Name.Equals(MATERIAL_CONTEXT_MASS, StringComparison.OrdinalIgnoreCase));

                if (existingMat != null)
                    return existingMat;

                ElementId matId = Material.Create(_doc, MATERIAL_CONTEXT_MASS);
                Material newMat = (Material)_doc.GetElement(matId);
                newMat.Color = new Color(225, 229, 238);
                newMat.Transparency = 10;
                return newMat;
            }
            catch
            {
                return null;
            }
        }

        public Category GetOrCreateContextSubcategory()
        {
            try
            {
                Categories categories = _doc.Settings.Categories;
                Category genModels = categories.get_Item(BuiltInCategory.OST_GenericModel);
                if (genModels.SubCategories.Contains(SUBCAT_CONTEXT_BLDG))
                {
                    return genModels.SubCategories.get_Item(SUBCAT_CONTEXT_BLDG);
                }

                Category newSub = categories.NewSubcategory(genModels, SUBCAT_CONTEXT_BLDG);
                newSub.LineColor = new Color(100, 116, 139);
                Material mat = GetOrCreateUrbanContextMaterial();
                if (mat != null)
                {
                    newSub.Material = mat;
                }
                return newSub;
            }
            catch
            {
                return _doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel);
            }
        }

        private List<ElementId> DrawLoopsWithIds(List<CurveLoop> loops, LotElementType elemType, SketchPlane sketchPlane, GraphicsStyle lineStyle, string comments)
        {
            var ids = new List<ElementId>();
            View activeView = _doc.ActiveView;
            ViewPlan vp = activeView as ViewPlan;

            foreach (var loop in loops)
            {
                foreach (Curve curve in loop)
                {
                    if (elemType == LotElementType.ModelCurves && sketchPlane != null)
                    {
                        ModelCurve mc = _doc.Create.NewModelCurve(curve, sketchPlane);
                        if (mc != null)
                        {
                            if (lineStyle != null) try { mc.LineStyle = lineStyle; } catch { }
                            var comm = mc.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                            if (comm != null) comm.Set(comments);
                            ids.Add(mc.Id);
                        }
                    }
                    else if (elemType == LotElementType.DetailLines && IsPlanView(activeView))
                    {
                        DetailCurve dc = _doc.Create.NewDetailCurve(activeView, curve);
                        if (dc != null)
                        {
                            if (lineStyle != null) try { dc.LineStyle = lineStyle; } catch { }
                            var comm = dc.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                            if (comm != null) comm.Set(comments);
                            ids.Add(dc.Id);
                        }
                    }
                    else if (elemType == LotElementType.AreaBoundaryLines && vp != null && activeView.ViewType == ViewType.AreaPlan)
                    {
                        if (sketchPlane != null)
                        {
                            ModelCurve ac = _doc.Create.NewAreaBoundaryLine(sketchPlane, curve, vp);
                            if (ac != null) ids.Add(ac.Id);
                        }
                    }
                }
            }
            return ids;
        }

        private bool CreateTextAnnotation(XYZ position, string text)
        {
            try
            {
                View activeView = _doc.ActiveView;

                if (IsPlanView(activeView))
                {
                    ElementId defaultTextTypeId = _doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
                    if (defaultTextTypeId != ElementId.InvalidElementId)
                    {
                        TextNoteOptions opts = new TextNoteOptions
                        {
                            HorizontalAlignment = HorizontalTextAlignment.Center,
                            TypeId = defaultTextTypeId
                        };

                        XYZ viewPos = new XYZ(position.X, position.Y, 0);
                        TextNote tn = TextNote.Create(_doc, activeView.Id, viewPos, text, opts);
                        return tn != null;
                    }
                }
            }
            catch
            {
                // Fallback
            }

            return false;
        }

        private void EnsurePresetStylesExist()
        {
            GetOrCreateLineStyle(STYLE_SUBJECT_RED, new Color(220, 38, 38), 4);
            GetOrCreateLineStyle(STYLE_ADJACENT_ORANGE, new Color(234, 88, 12), 2);
            GetOrCreateLineStyle(STYLE_CONTEXT_GRAY, new Color(148, 163, 184), 1);
            GetOrCreateLineStyle(STYLE_SIDEWALK_BLUE, new Color(2, 132, 199), 2);
        }

        private GraphicsStyle ResolveLineStyle(string requestedName, string fallbackPreset, Color fallbackColor, int fallbackWeight)
        {
            try
            {
                Categories categories = _doc.Settings.Categories;
                Category linesCat = categories.get_Item(BuiltInCategory.OST_Lines);
                if (linesCat != null)
                {
                    if (!string.IsNullOrWhiteSpace(requestedName) && linesCat.SubCategories.Contains(requestedName))
                    {
                        return linesCat.SubCategories.get_Item(requestedName).GetGraphicsStyle(GraphicsStyleType.Projection);
                    }
                }
            }
            catch { }

            return GetOrCreateLineStyle(fallbackPreset, fallbackColor, fallbackWeight);
        }

        public GraphicsStyle GetOrCreateLineStyle(string styleName, Color color, int weight)
        {
            try
            {
                Categories categories = _doc.Settings.Categories;
                Category linesCat = categories.get_Item(BuiltInCategory.OST_Lines);
                if (linesCat != null)
                {
                    if (linesCat.SubCategories.Contains(styleName))
                    {
                        Category existingSub = linesCat.SubCategories.get_Item(styleName);
                        return existingSub.GetGraphicsStyle(GraphicsStyleType.Projection);
                    }

                    Category newSubCat = categories.NewSubcategory(linesCat, styleName);
                    newSubCat.LineColor = color;
                    newSubCat.SetLineWeight(weight, GraphicsStyleType.Projection);
                    return newSubCat.GetGraphicsStyle(GraphicsStyleType.Projection);
                }
            }
            catch { }

            Category defaultLines = _doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            return defaultLines.GetGraphicsStyle(GraphicsStyleType.Projection);
        }

        private static List<CurveLoop> BuildNormalizedBuildingLoops(List<List<XYZ>> rings, double offsetX, double offsetY, double zElevation, double tolerance)
        {
            var loops = new List<CurveLoop>();
            if (rings == null || rings.Count == 0) return loops;

            for (int r = 0; r < rings.Count; r++)
            {
                var ring = rings[r];
                var cleaned = CleanAndTransformRing(ring, offsetX, offsetY, zElevation, tolerance);
                if (cleaned.Count < 3) continue;

                var loop = new CurveLoop();
                bool valid = true;

                for (int i = 0; i < cleaned.Count; i++)
                {
                    XYZ p1 = cleaned[i];
                    XYZ p2 = cleaned[(i + 1) % cleaned.Count];
                    if (p1.DistanceTo(p2) < tolerance) continue;

                    try
                    {
                        loop.Append(Line.CreateBound(p1, p2));
                    }
                    catch
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid && !loop.IsOpen() && loop.Count() >= 3)
                {
                    if (r == 0)
                    {
                        if (!loop.IsCounterclockwise(XYZ.BasisZ))
                        {
                            loop = CurveLoop.CreateViaCopy(loop);
                            loop.Flip();
                        }
                        loops.Add(loop);
                    }
                    else
                    {
                        if (loop.IsCounterclockwise(XYZ.BasisZ))
                        {
                            loop = CurveLoop.CreateViaCopy(loop);
                            loop.Flip();
                        }
                        loops.Add(loop);
                    }
                }
            }
            return loops;
        }

        private static List<CurveLoop> BuildCurveLoops(List<List<XYZ>> rings, double offsetX, double offsetY, double zElevation, double tolerance)
        {
            var loops = new List<CurveLoop>();
            foreach (var ring in rings)
            {
                var cleaned = CleanAndTransformRing(ring, offsetX, offsetY, zElevation, tolerance);
                if (cleaned.Count < 3) continue;

                var loop = new CurveLoop();
                bool valid = true;

                for (int i = 0; i < cleaned.Count; i++)
                {
                    XYZ p1 = cleaned[i];
                    XYZ p2 = cleaned[(i + 1) % cleaned.Count];
                    if (p1.DistanceTo(p2) < tolerance) continue;

                    try
                    {
                        loop.Append(Line.CreateBound(p1, p2));
                    }
                    catch
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid && !loop.IsOpen() && loop.Count() >= 3)
                {
                    loops.Add(loop);
                }
            }
            return loops;
        }

        private static List<XYZ> CleanAndTransformRing(List<XYZ> rawPoints, double offsetX, double offsetY, double z, double tolerance)
        {
            var result = new List<XYZ>();
            if (rawPoints == null || rawPoints.Count == 0) return result;

            for (int i = 0; i < rawPoints.Count; i++)
            {
                var pt = rawPoints[i];
                var transformed = new XYZ(pt.X + offsetX, pt.Y + offsetY, z);

                if (result.Count > 0 && result[result.Count - 1].DistanceTo(transformed) < tolerance)
                    continue;

                result.Add(transformed);
            }

            if (result.Count > 1 && result[0].DistanceTo(result[result.Count - 1]) < tolerance)
            {
                result.RemoveAt(result.Count - 1);
            }

            return result;
        }

        private static bool IsPlanView(View view)
        {
            if (view == null) return false;
            return view.ViewType == ViewType.FloorPlan ||
                   view.ViewType == ViewType.AreaPlan ||
                   view.ViewType == ViewType.CeilingPlan ||
                   view.ViewType == ViewType.EngineeringPlan ||
                   view.ViewType == ViewType.DraftingView;
        }
    }
}

```

### `ZoningFloorArea\Services\RevitMassingBakerService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class RevitMassingBakerService
    {
        private readonly Document _doc;

        public RevitMassingBakerService(Document doc)
        {
            _doc = doc;
        }

        public int BakeScenariosIntoDesignOptions(
            List<GenerativeScenario> scenariosToBake,
            bool assignDesignOptions,
            bool createLevels,
            string optionSetName)
        {
            if (scenariosToBake == null || scenariosToBake.Count == 0) return 0;
            int totalShapesCreated = 0;

            using (Transaction tx = new Transaction(_doc, "Bake Generative Massing Options"))
            {
                tx.Start();

                try
                {
                    // 1. Create Levels if requested
                    if (createLevels)
                    {
                        CreateProjectLevelsFromScenarios(scenariosToBake);
                    }

                    // 2. Fetch existing Design Options in project if any
                    List<DesignOption> existingOptions = new List<DesignOption>();
                    if (assignDesignOptions)
                    {
                        FilteredElementCollector optColl = new FilteredElementCollector(_doc).OfClass(typeof(DesignOption));
                        existingOptions = optColl.Cast<DesignOption>().ToList();
                    }

                    // 3. For each selected scenario, create DirectShape mass volumes
                    for (int sIdx = 0; sIdx < scenariosToBake.Count; sIdx++)
                    {
                        GenerativeScenario s = scenariosToBake[sIdx];
                        
                        // Try matching existing design option by name or index
                        ElementId designOptionId = ElementId.InvalidElementId;
                        if (existingOptions.Count > 0)
                        {
                            DesignOption matched = existingOptions.FirstOrDefault(o => o.Name.IndexOf(s.Title, StringComparison.OrdinalIgnoreCase) >= 0);
                            if (matched != null)
                            {
                                designOptionId = matched.Id;
                            }
                            else if (sIdx < existingOptions.Count)
                            {
                                designOptionId = existingOptions[sIdx].Id;
                            }
                        }

                        // Create 3D Solid DirectShapes for each floor block
                        foreach (MassingFloorBlock f in s.Floors)
                        {
                            DirectShape ds = CreateFloorMassDirectShape(f, s.Title);
                            if (ds != null)
                            {
                                totalShapesCreated++;

                                if (designOptionId != ElementId.InvalidElementId)
                                {
                                    Parameter pOpt = ds.get_Parameter(BuiltInParameter.DESIGN_OPTION_PARAM);
                                    if (pOpt != null && !pOpt.IsReadOnly)
                                    {
                                        pOpt.Set(designOptionId);
                                    }
                                }
                            }
                        }
                    }

                    tx.Commit();
                }
                catch
                {
                    if (tx.HasStarted()) tx.RollBack();
                    throw;
                }
            }

            return totalShapesCreated;
        }

        private DirectShape CreateFloorMassDirectShape(MassingFloorBlock f, string scenarioTitle)
        {
            try
            {
                double halfW = f.WidthFt / 2.0;
                double halfD = f.DepthFt / 2.0;

                XYZ p0 = new XYZ(-halfW, -halfD, f.ElevationFt);
                XYZ p1 = new XYZ(halfW, -halfD, f.ElevationFt);
                XYZ p2 = new XYZ(halfW, halfD, f.ElevationFt);
                XYZ p3 = new XYZ(-halfW, halfD, f.ElevationFt);

                Line l0 = Line.CreateBound(p0, p1);
                Line l1 = Line.CreateBound(p1, p2);
                Line l2 = Line.CreateBound(p2, p3);
                Line l3 = Line.CreateBound(p3, p0);

                CurveLoop loop = new CurveLoop();
                loop.Append(l0);
                loop.Append(l1);
                loop.Append(l2);
                loop.Append(l3);

                List<CurveLoop> loops = new List<CurveLoop> { loop };
                Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(loops, XYZ.BasisZ, f.HeightFt);

                if (solid == null) return null;

                DirectShape ds = DirectShape.CreateElement(_doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.SetShape(new GeometryObject[] { solid });
                ds.Name = string.Format("{0} - {1}", scenarioTitle, f.LevelName);

                Parameter pComments = ds.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (pComments != null && !pComments.IsReadOnly)
                {
                    pComments.Set(string.Format("BauTools Generative | Option: {0} | Usage: {1} | Area: {2:N0} SF", scenarioTitle, f.UsageType, f.AreaSqFt));
                }

                return ds;
            }
            catch
            {
                return null;
            }
        }

        private void CreateProjectLevelsFromScenarios(List<GenerativeScenario> scenarios)
        {
            FilteredElementCollector lvlColl = new FilteredElementCollector(_doc).OfClass(typeof(Level));
            List<Level> existingLevels = lvlColl.Cast<Level>().ToList();

            GenerativeScenario maxScenario = scenarios.OrderByDescending(s => s.Floors.Count).FirstOrDefault();
            if (maxScenario == null) return;

            foreach (MassingFloorBlock f in maxScenario.Floors)
            {
                bool exists = existingLevels.Any(l => Math.Abs(l.Elevation - f.ElevationFt) < 0.1);
                if (!exists)
                {
                    try
                    {
                        Level newLvl = Level.Create(_doc, f.ElevationFt);
                        string safeName = string.Format("FL. {0:D2}", f.LevelIndex);
                        int counter = 1;
                        while (existingLevels.Any(l => string.Equals(l.Name, safeName, StringComparison.OrdinalIgnoreCase)))
                        {
                            safeName = string.Format("FL. {0:D2} ({1})", f.LevelIndex, counter++);
                        }
                        newLvl.Name = safeName;
                        existingLevels.Add(newLvl);
                    }
                    catch
                    {
                        // Ignore level naming conflicts
                    }
                }
            }
        }
    }
}
```

### `ZoningFloorArea\Services\RevitSheetPlacementService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class SheetItem
    {
        public string SheetNumber { get; set; }
        public string SheetName { get; set; }
        public ElementId SheetId { get; set; }

        public string DisplayName
        {
            get { return string.Format("{0} - {1}", SheetNumber, SheetName); }
        }
    }

    public class RevitSheetPlacementService
    {
        private readonly Document _doc;

        public RevitSheetPlacementService(Document doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            _doc = doc;
        }

        public List<TitleblockItem> GetAvailableTitleblocks()
        {
            List<TitleblockItem> list = new List<TitleblockItem>();
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(_doc)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .WhereElementIsElementType();

                foreach (FamilySymbol sym in collector.Cast<FamilySymbol>())
                {
                    if (sym != null)
                    {
                        string fn = sym.FamilyName;
                        string sn = sym.Name;
                        string disp = string.IsNullOrEmpty(sn) || sn == fn ? fn : string.Format("{0} - {1}", fn, sn);

                        double wIn = 36.0;
                        double hIn = 24.0;

                        if (disp.IndexOf("30x42", StringComparison.OrdinalIgnoreCase) >= 0 || disp.IndexOf("42x30", StringComparison.OrdinalIgnoreCase) >= 0 || disp.IndexOf("Arch E", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            wIn = 42.0; hIn = 30.0;
                        }
                        else if (disp.IndexOf("36x24", StringComparison.OrdinalIgnoreCase) >= 0 || disp.IndexOf("24x36", StringComparison.OrdinalIgnoreCase) >= 0 || disp.IndexOf("Arch D", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            wIn = 36.0; hIn = 24.0;
                        }
                        else if (disp.IndexOf("A0", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            wIn = 46.8; hIn = 33.1;
                        }
                        else if (disp.IndexOf("A1", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            wIn = 33.1; hIn = 23.4;
                        }

                        list.Add(new TitleblockItem
                        {
                            Name = disp,
                            FamilySymbolId = sym.Id,
                            WidthInches = wIn,
                            HeightInches = hIn,
                            UsableWidthInches = wIn - 4.5, // Minus title block sidebar / border
                            UsableHeightInches = hIn - 2.0
                        });
                    }
                }
            }
            catch
            {
            }

            if (list.Count == 0)
            {
                list.Add(new TitleblockItem { Name = "Standard 36\" x 24\" (Arch D)" });
            }

            return list.OrderBy(t => t.Name).ToList();
        }

        public List<ViewTemplateItem> GetAvailableViewTemplates()
        {
            List<ViewTemplateItem> list = new List<ViewTemplateItem>();
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(_doc)
                    .OfClass(typeof(View))
                    .WhereElementIsNotElementType();

                foreach (View v in collector.Cast<View>())
                {
                    if (v != null && v.IsTemplate && (v.ViewType == ViewType.FloorPlan || v.ViewType == ViewType.CeilingPlan || v.ViewType == ViewType.AreaPlan))
                    {
                        list.Add(new ViewTemplateItem
                        {
                            Name = v.Name,
                            TemplateId = v.Id
                        });
                    }
                }
            }
            catch
            {
            }
            return list.OrderBy(v => v.Name).ToList();
        }

        public List<SheetItem> GetExistingSheets()
        {
            List<SheetItem> list = new List<SheetItem>();
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewSheet))
                    .WhereElementIsNotElementType();

                foreach (ViewSheet vs in collector.Cast<ViewSheet>())
                {
                    if (vs != null && !vs.IsTemplate)
                    {
                        list.Add(new SheetItem
                        {
                            SheetNumber = vs.SheetNumber,
                            SheetName = vs.Name,
                            SheetId = vs.Id
                        });
                    }
                }
            }
            catch
            {
            }

            return list.OrderBy(s => s.SheetNumber).ToList();
        }

        public XYZ GetViewportCenter(SheetLayoutMode mode, int gridIndex, TitleblockItem tb)
        {
            double wFt = (tb != null ? tb.WidthInches : 36.0) / 12.0;
            double hFt = (tb != null ? tb.HeightInches : 24.0) / 12.0;

            // Usable drawing area left margin and bottom margin in feet
            double startX = 0.35; // Left margin
            double usableW = wFt - 0.70; // Right title block sidebar buffer
            double usableH = hFt - 0.35;

            int rows = 1;
            int cols = 1;

            switch (mode)
            {
                case SheetLayoutMode.Single1View:
                    rows = 1; cols = 1; break;
                case SheetLayoutMode.Dual2Views:
                    rows = 1; cols = 2; break;
                case SheetLayoutMode.Triple3Views:
                    rows = 1; cols = 3; break;
                case SheetLayoutMode.Quad4Views:
                    rows = 2; cols = 2; break;
                case SheetLayoutMode.Hex6Views:
                    rows = 2; cols = 3; break;
                case SheetLayoutMode.Octo8Views:
                    rows = 2; cols = 4; break;
            }

            int colIdx = gridIndex % cols;
            int rowIdx = gridIndex / cols;

            double cellW = usableW / cols;
            double cellH = usableH / rows;

            double centerX = startX + (colIdx * cellW) + (cellW / 2.0);
            // Revit Sheet Y=0 is at bottom, so row 0 is top row
            double centerY = (usableH - (rowIdx * cellH)) - (cellH / 2.0);

            return new XYZ(centerX, centerY, 0);
        }

        public int ComposePlannedSheets(
            List<PlannedSheet> plannedSheets,
            ElementId titleblockId,
            bool repositionIfExists,
            Dictionary<string, ElementId> createdViewsByName,
            TitleblockItem titleblockItem)
        {
            if (plannedSheets == null || plannedSheets.Count == 0) return 0;

            int placedViewCount = 0;

            using (Transaction tx = new Transaction(_doc, "BauTools: Compose Sheets & Viewports"))
            {
                tx.Start();

                foreach (PlannedSheet ps in plannedSheets)
                {
                    ViewSheet sheet = GetOrCreateSheet(ps.SheetNumber, ps.SheetName, titleblockId);
                    if (sheet == null) continue;

                    for (int i = 0; i < ps.Viewports.Count; i++)
                    {
                        PlannedViewport vp = ps.Viewports[i];
                        ElementId viewId = ElementId.InvalidElementId;

                        if (createdViewsByName != null && createdViewsByName.ContainsKey(vp.ViewName))
                        {
                            viewId = createdViewsByName[vp.ViewName];
                        }
                        else if (vp.ExistingViewId != ElementId.InvalidElementId)
                        {
                            viewId = vp.ExistingViewId;
                        }

                        if (viewId == ElementId.InvalidElementId) continue;

                        XYZ slotCenter = GetViewportCenter(ps.LayoutMode, vp.GridIndex, titleblockItem);

                        // Check if viewport already placed on this sheet
                        Viewport existingVp = GetViewportForViewOnSheet(sheet, viewId);

                        if (existingVp != null)
                        {
                            if (repositionIfExists)
                            {
                                try
                                {
                                    existingVp.SetBoxCenter(slotCenter);
                                    placedViewCount++;
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            if (Viewport.CanAddViewToSheet(_doc, sheet.Id, viewId))
                            {
                                try
                                {
                                    Viewport newVp = Viewport.Create(_doc, sheet.Id, viewId, slotCenter);
                                    placedViewCount++;
                                }
                                catch { }
                            }
                        }
                    }
                }

                tx.Commit();
            }

            return placedViewCount;
        }

        public int PlaceViewsOnSheet(ElementId sheetId, List<ElementId> viewIds)
        {
            if (sheetId == ElementId.InvalidElementId || viewIds == null || viewIds.Count == 0) return 0;
            ViewSheet sheet = _doc.GetElement(sheetId) as ViewSheet;
            if (sheet == null) return 0;

            int count = 0;
            using (Transaction tx = new Transaction(_doc, "BauTools: Place Views on Sheet"))
            {
                tx.Start();
                XYZ center = new XYZ(1.5, 1.5, 0);
                foreach (ElementId vId in viewIds)
                {
                    if (Viewport.CanAddViewToSheet(_doc, sheet.Id, vId))
                    {
                        try
                        {
                            Viewport.Create(_doc, sheet.Id, vId, center);
                            count++;
                        }
                        catch { }
                    }
                }
                tx.Commit();
            }
            return count;
        }

        private ViewSheet GetOrCreateSheet(string sheetNumber, string sheetName, ElementId titleblockId)
        {
            ViewSheet existing = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .FirstOrDefault(s => string.Equals(s.SheetNumber, sheetNumber, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                if (!string.IsNullOrEmpty(sheetName))
                {
                    try { existing.Name = sheetName; } catch { }
                }
                return existing;
            }

            try
            {
                ViewSheet newSheet = ViewSheet.Create(_doc, titleblockId);
                newSheet.SheetNumber = sheetNumber;
                newSheet.Name = sheetName;
                return newSheet;
            }
            catch
            {
                return null;
            }
        }

        private Viewport GetViewportForViewOnSheet(ViewSheet sheet, ElementId viewId)
        {
            if (sheet == null || viewId == ElementId.InvalidElementId) return null;

            ICollection<ElementId> vpIds = sheet.GetAllViewports();
            foreach (ElementId vId in vpIds)
            {
                Viewport vp = _doc.GetElement(vId) as Viewport;
                if (vp != null && vp.ViewId == viewId)
                {
                    return vp;
                }
            }
            return null;
        }
    }
}
```

### `ZoningFloorArea\Services\RevitSheetTableDrawer.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class RevitSheetTableDrawer
    {
        private readonly Document _doc;

        public RevitSheetTableDrawer(Document doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            _doc = doc;
        }

        /// <summary>
        /// Creates a native Revit Drafting View containing the graphic matrix table.
        /// </summary>
        public ViewDrafting CreateZoningTableDraftingView(ZoningTableResult table, string viewName)
        {
            if (string.IsNullOrEmpty(viewName)) viewName = "Zoning Floor Area Table";

            using (Transaction tx = new Transaction(_doc, "Generate Native Revit Zoning Table"))
            {
                tx.Start();

                ViewFamilyType draftingVft = null;
                FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(ViewFamilyType));
                foreach (ViewFamilyType vft in collector)
                {
                    if (vft.ViewFamily == ViewFamily.Drafting)
                    {
                        draftingVft = vft;
                        break;
                    }
                }

                if (draftingVft == null)
                {
                    tx.RollBack();
                    throw new InvalidOperationException("No Drafting ViewFamilyType found in current Revit document.");
                }

                ViewDrafting draftingView = ViewDrafting.Create(_doc, draftingVft.Id);
                draftingView.Name = GetUniqueViewName(viewName);
                draftingView.Scale = 1;

                double colWidthLevel = 1.0;
                double colWidthGross = 1.3;
                double colWidthDed = 1.1;
                double colWidthNet = 1.2;
                double colWidthUleb = 1.1;
                double colWidthZfa = 1.4;
                double colWidthFar = 0.8;

                int dedCount = table.DeductionCategories.Count;

                double resWidth = colWidthLevel + colWidthGross + (dedCount * colWidthDed) + colWidthNet + colWidthUleb + colWidthZfa + colWidthFar;
                double comWidth = colWidthGross + colWidthUleb + colWidthZfa + colWidthFar;
                double totalWidth = resWidth + comWidth + colWidthZfa + colWidthFar;

                double rowHeight = 0.3;
                double headerHeight = 0.4;
                double startX = 0.0;
                double currentY = 0.0;

                ElementId textTypeId = _doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);

                // Title
                DrawRectangle(_doc, draftingView, startX, currentY - headerHeight, totalWidth, headerHeight);
                CreateCellText(_doc, draftingView, startX, currentY - headerHeight, totalWidth, headerHeight,
                    string.Format("FLOOR AREA CALCULATIONS - {0}", table.BuildingName.ToUpper()), textTypeId, HorizontalTextAlignment.Center, true);

                currentY -= headerHeight;

                // Headers
                DrawRectangle(_doc, draftingView, startX, currentY - headerHeight, resWidth, headerHeight);
                CreateCellText(_doc, draftingView, startX, currentY - headerHeight, resWidth, headerHeight, "RESIDENTIAL", textTypeId, HorizontalTextAlignment.Center, true);

                DrawRectangle(_doc, draftingView, startX + resWidth, currentY - headerHeight, comWidth, headerHeight);
                CreateCellText(_doc, draftingView, startX + resWidth, currentY - headerHeight, comWidth, headerHeight, "COMMERCIAL", textTypeId, HorizontalTextAlignment.Center, true);

                DrawRectangle(_doc, draftingView, startX + resWidth + comWidth, currentY - headerHeight, colWidthZfa, headerHeight);
                CreateCellText(_doc, draftingView, startX + resWidth + comWidth, currentY - headerHeight, colWidthZfa, headerHeight, "TOTAL ZONING FLOOR AREA", textTypeId, HorizontalTextAlignment.Center, true);

                DrawRectangle(_doc, draftingView, startX + resWidth + comWidth + colWidthZfa, currentY - headerHeight, colWidthFar, headerHeight);
                CreateCellText(_doc, draftingView, startX + resWidth + comWidth + colWidthZfa, currentY - headerHeight, colWidthFar, headerHeight, "TOTAL FAR", textTypeId, HorizontalTextAlignment.Center, true);

                currentY -= headerHeight;

                // Sub-headers
                double xCursor = startX;

                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthLevel, headerHeight, "LEVEL", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthGross, headerHeight, "GROSS FLOOR\nAREA", textTypeId);

                double dedsSpanWidth = dedCount * colWidthDed;
                DrawRectangle(_doc, draftingView, xCursor, currentY, dedsSpanWidth, headerHeight / 2);
                CreateCellText(_doc, draftingView, xCursor, currentY, dedsSpanWidth, headerHeight / 2, "DEDUCTIONS", textTypeId, HorizontalTextAlignment.Center, true);

                double dedX = xCursor;
                foreach (string cat in table.DeductionCategories)
                {
                    dedX = DrawColumnHeader(_doc, draftingView, dedX, currentY - headerHeight / 2, colWidthDed, headerHeight / 2, cat, textTypeId);
                }
                xCursor += dedsSpanWidth;

                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthNet, headerHeight, "NET AREA", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthUleb, headerHeight, string.Format("{0}% ULEB", (int)(table.UlebPercent * 100)), textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthZfa, headerHeight, "ZONING FLOOR\nAREA", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthFar, headerHeight, "FAR", textTypeId);

                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthGross, headerHeight, "GROSS FLOOR\nAREA", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthUleb, headerHeight, string.Format("{0}% ULEB", (int)(table.UlebPercent * 100)), textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthZfa, headerHeight, "ZONING FLOOR\nAREA", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthFar, headerHeight, "FAR", textTypeId);

                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthZfa, headerHeight, "TOTAL ZFA", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthFar, headerHeight, "TOTAL FAR", textTypeId);

                currentY -= headerHeight;

                // Data Rows
                int rowCount = Math.Max(table.ResidentialRows.Count, table.CommercialRows.Count);

                for (int i = 0; i < rowCount; i++)
                {
                    LevelZoningRow rRes = i < table.ResidentialRows.Count ? table.ResidentialRows[i] : new LevelZoningRow();
                    LevelZoningRow rCom = i < table.CommercialRows.Count ? table.CommercialRows[i] : new LevelZoningRow();

                    xCursor = startX;

                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthLevel, rowHeight, rRes.LevelName, textTypeId, HorizontalTextAlignment.Center, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthGross, rowHeight, FormatNum(rRes.GrossFloorArea), textTypeId, HorizontalTextAlignment.Right, false);

                    foreach (string cat in table.DeductionCategories)
                    {
                        double val = rRes.GetDeduction(cat);
                        xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthDed, rowHeight, FormatNum(val), textTypeId, HorizontalTextAlignment.Right, false);
                    }

                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthNet, rowHeight, FormatNum(rRes.NetArea), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthUleb, rowHeight, FormatNum(rRes.UlebAmount), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthZfa, rowHeight, FormatNum(rRes.ZoningFloorArea), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthFar, rowHeight, FormatNum(rRes.Far), textTypeId, HorizontalTextAlignment.Right, false);

                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthGross, rowHeight, FormatNum(rCom.GrossFloorArea), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthUleb, rowHeight, FormatNum(rCom.UlebAmount), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthZfa, rowHeight, FormatNum(rCom.ZoningFloorArea), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthFar, rowHeight, FormatNum(rCom.Far), textTypeId, HorizontalTextAlignment.Right, false);

                    double totZfa = rRes.ZoningFloorArea + rCom.ZoningFloorArea;
                    double totFar = rRes.Far + rCom.Far;
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthZfa, rowHeight, FormatNum(totZfa), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthFar, rowHeight, FormatNum(totFar), textTypeId, HorizontalTextAlignment.Right, false);

                    currentY -= rowHeight;
                }

                // Subtotal
                LevelZoningRow sRes = table.ResidentialSubtotal;
                LevelZoningRow sCom = table.CommercialSubtotal;
                xCursor = startX;

                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthLevel, rowHeight, "SUBTOTAL", textTypeId, HorizontalTextAlignment.Center, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthGross, rowHeight, FormatNum(sRes.GrossFloorArea), textTypeId, HorizontalTextAlignment.Right, true);

                foreach (string cat in table.DeductionCategories)
                {
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthDed, rowHeight, FormatNum(sRes.GetDeduction(cat)), textTypeId, HorizontalTextAlignment.Right, true);
                }

                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthNet, rowHeight, FormatNum(sRes.NetArea), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthUleb, rowHeight, FormatNum(sRes.UlebAmount), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthZfa, rowHeight, FormatNum(sRes.ZoningFloorArea), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthFar, rowHeight, FormatNum(sRes.Far), textTypeId, HorizontalTextAlignment.Right, true);

                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthGross, rowHeight, FormatNum(sCom.GrossFloorArea), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthUleb, rowHeight, FormatNum(sCom.UlebAmount), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthZfa, rowHeight, FormatNum(sCom.ZoningFloorArea), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthFar, rowHeight, FormatNum(sCom.Far), textTypeId, HorizontalTextAlignment.Right, true);

                double subTotZfa = sRes.ZoningFloorArea + sCom.ZoningFloorArea;
                double subTotFar = sRes.Far + sCom.Far;
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthZfa, rowHeight, FormatNum(subTotZfa), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthFar, rowHeight, FormatNum(subTotFar), textTypeId, HorizontalTextAlignment.Right, true);

                currentY -= rowHeight;

                // Grand Total
                LevelZoningRow gTot = table.GrandTotal;
                xCursor = startX;

                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthLevel, rowHeight, "TOTAL", textTypeId, HorizontalTextAlignment.Center, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthGross, rowHeight, FormatNum(gTot.GrossFloorArea), textTypeId, HorizontalTextAlignment.Right, true);

                foreach (string cat in table.DeductionCategories)
                {
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthDed, rowHeight, FormatNum(gTot.GetDeduction(cat)), textTypeId, HorizontalTextAlignment.Right, true);
                }

                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthNet, rowHeight, FormatNum(gTot.NetArea), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthUleb, rowHeight, FormatNum(gTot.UlebAmount), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthZfa, rowHeight, FormatNum(gTot.ZoningFloorArea), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthFar, rowHeight, FormatNum(gTot.Far), textTypeId, HorizontalTextAlignment.Right, true);

                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthGross, rowHeight, FormatNum(sCom.GrossFloorArea), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthUleb, rowHeight, FormatNum(sCom.UlebAmount), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthZfa, rowHeight, FormatNum(sCom.ZoningFloorArea), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthFar, rowHeight, FormatNum(sCom.Far), textTypeId, HorizontalTextAlignment.Right, true);

                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthZfa, rowHeight, FormatNum(table.TotalZoningFloorArea), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthFar, rowHeight, FormatNum(table.TotalFar), textTypeId, HorizontalTextAlignment.Right, true);

                tx.Commit();
                return draftingView;
            }
        }

        /// <summary>
        /// Generates native Revit Schedule views (ViewSchedule) under Schedules/Quantities category in Project Browser.
        /// </summary>
        public List<ViewSchedule> CreateNativeAreaSchedules(ProjectZoningResult project, MappingConfig config)
        {
            List<ViewSchedule> createdSchedules = new List<ViewSchedule>();

            using (Transaction tx = new Transaction(_doc, "Generate Native Revit Area Schedules"))
            {
                tx.Start();

                // 1. Gross Areas Schedule
                ViewSchedule grossSchedule = ViewSchedule.CreateSchedule(_doc, new ElementId(BuiltInCategory.OST_Areas));
                grossSchedule.Name = GetUniqueViewName("Zoning - Gross Building Areas Schedule");
                AddStandardAreaFields(_doc, grossSchedule, config);
                createdSchedules.Add(grossSchedule);

                // 2. Deductions Schedule
                ViewSchedule deductionSchedule = ViewSchedule.CreateSchedule(_doc, new ElementId(BuiltInCategory.OST_Areas));
                deductionSchedule.Name = GetUniqueViewName("Zoning - Deductions Area Schedule");
                AddStandardAreaFields(_doc, deductionSchedule, config);
                createdSchedules.Add(deductionSchedule);

                tx.Commit();
            }

            return createdSchedules;
        }

        private void AddStandardAreaFields(Document doc, ViewSchedule schedule, MappingConfig config)
        {
            ScheduleDefinition def = schedule.Definition;
            IList<SchedulableField> fields = def.GetSchedulableFields();

            foreach (SchedulableField sf in fields)
            {
                string fieldName = sf.GetName(doc);
                if (fieldName == "Level" || fieldName == "Name" || fieldName == "Area" || fieldName == "Comments" || fieldName == "Area Scheme")
                {
                    def.AddField(sf);
                }
            }
        }

        private double DrawColumnHeader(Document doc, View view, double x, double y, double w, double h, string text, ElementId textTypeId)
        {
            DrawRectangle(doc, view, x, y - h, w, h);
            CreateCellText(doc, view, x, y - h, w, h, text, textTypeId, HorizontalTextAlignment.Center, true);
            return x + w;
        }

        private double DrawCell(Document doc, View view, double x, double y, double w, double h, string text, ElementId textTypeId, HorizontalTextAlignment align, bool isBold)
        {
            DrawRectangle(doc, view, x, y, w, h);
            CreateCellText(doc, view, x, y, w, h, text, textTypeId, align, isBold);
            return x + w;
        }

        private void DrawRectangle(Document doc, View view, double x, double y, double w, double h)
        {
            XYZ p1 = new XYZ(x, y, 0);
            XYZ p2 = new XYZ(x + w, y, 0);
            XYZ p3 = new XYZ(x + w, y + h, 0);
            XYZ p4 = new XYZ(x, y + h, 0);

            doc.Create.NewDetailCurve(view, Line.CreateBound(p1, p2));
            doc.Create.NewDetailCurve(view, Line.CreateBound(p2, p3));
            doc.Create.NewDetailCurve(view, Line.CreateBound(p3, p4));
            doc.Create.NewDetailCurve(view, Line.CreateBound(p4, p1));
        }

        private void CreateCellText(Document doc, View view, double x, double y, double w, double h, string text, ElementId typeId, HorizontalTextAlignment align, bool isBold)
        {
            if (string.IsNullOrEmpty(text)) return;

            double posX = align == HorizontalTextAlignment.Right ? x + w - 0.08 : (align == HorizontalTextAlignment.Center ? x + w / 2 : x + 0.08);
            double posY = y + h / 2;

            TextNoteOptions opts = new TextNoteOptions(typeId);
            opts.HorizontalAlignment = align;

            TextNote.Create(doc, view.Id, new XYZ(posX, posY, 0), text, opts);
        }

        private string FormatNum(double val)
        {
            return val > 0 ? val.ToString("N2") : "0.00";
        }

        private string GetUniqueViewName(string baseName)
        {
            string name = baseName;
            int counter = 1;
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(View));
            while (ContainsViewName(collector, name))
            {
                name = string.Format("{0} ({1})", baseName, counter++);
            }
            return name;
        }

        private bool ContainsViewName(FilteredElementCollector collector, string name)
        {
            foreach (View v in collector)
            {
                if (string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}

```

### `ZoningFloorArea\Services\RevitViewGeneratorService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class GeneratedViewResult
    {
        public View MasterView { get; set; }
        public List<View> DependentViews { get; set; }
        public string RangeLabel { get; set; }
        public string ViewTypeLabel { get; set; }

        public GeneratedViewResult()
        {
            DependentViews = new List<View>();
        }
    }

    public class RevitViewGeneratorService
    {
        private readonly Document _doc;

        public RevitViewGeneratorService(Document doc)
        {
            if (doc == null) throw new ArgumentNullException("doc");
            _doc = doc;
        }

        public List<string> GetAvailableScopeBoxes()
        {
            List<string> list = new List<string>();
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(_doc)
                    .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType();

                foreach (Element elem in collector)
                {
                    if (elem != null && !string.IsNullOrEmpty(elem.Name))
                    {
                        list.Add(elem.Name);
                    }
                }
            }
            catch
            {
            }
            list.Sort();
            return list;
        }

        public List<string> GetAvailableAreaSchemes()
        {
            List<string> list = new List<string>();
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(AreaScheme));
                foreach (AreaScheme s in collector)
                {
                    if (s != null && !string.IsNullOrEmpty(s.Name) && !list.Contains(s.Name))
                    {
                        list.Add(s.Name);
                    }
                }
            }
            catch
            {
            }
            list.Sort();
            return list;
        }

        public List<string> GetAvailableViewStringParameters()
        {
            List<string> paramsList = new List<string>();
            paramsList.Add("Building");
            paramsList.Add("Comments");
            paramsList.Add("Sub-Discipline");
            paramsList.Add("Edificio");
            paramsList.Add("Title on Sheet");

            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewPlan))
                    .WhereElementIsNotElementType();

                ViewPlan sampleView = collector.Cast<ViewPlan>().FirstOrDefault(v => !v.IsTemplate);
                if (sampleView != null)
                {
                    foreach (Parameter p in sampleView.Parameters)
                    {
                        if (p != null && p.StorageType == StorageType.String && !p.IsReadOnly)
                        {
                            string pName = p.Definition.Name;
                            if (!paramsList.Contains(pName))
                            {
                                paramsList.Add(pName);
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return paramsList;
        }

        public Dictionary<string, ElementId> GeneratePackageViews(
            List<BuildingDefinition> targetBuildings,
            MappingConfig config,
            List<PackageSetting> packageSettings,
            int globalViewScale,
            bool onlyTypicalRanges)
        {
            Dictionary<string, ElementId> createdMap = new Dictionary<string, ElementId>(StringComparer.OrdinalIgnoreCase);
            if (targetBuildings == null || targetBuildings.Count == 0 || packageSettings == null || packageSettings.Count == 0)
                return createdMap;

            Dictionary<string, ElementId> scopeBoxMap = GetScopeBoxElementMap();
            ViewFamilyType floorPlanVft = GetViewFamilyType(ViewFamily.FloorPlan);
            ViewFamilyType ceilingPlanVft = GetViewFamilyType(ViewFamily.CeilingPlan);

            ElementId masterScopeBoxId = ElementId.InvalidElementId;
            if (!string.IsNullOrEmpty(config.MasterScopeBoxName) && scopeBoxMap.ContainsKey(config.MasterScopeBoxName))
            {
                masterScopeBoxId = scopeBoxMap[config.MasterScopeBoxName];
            }

            using (Transaction tx = new Transaction(_doc, "BauTools: Generate Architectural Packages"))
            {
                tx.Start();

                foreach (PackageSetting pkg in packageSettings)
                {
                    if (!pkg.IsEnabled) continue;

                    int effectiveScale = pkg.ScaleValue > 0 ? pkg.ScaleValue : (globalViewScale > 0 ? globalViewScale : 96);

                    // Case A: Master Overall Campus Package
                    if (pkg.PackageType == ViewPackageType.MasterOverall)
                    {
                        HashSet<string> processedMasterLevels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (BuildingDefinition bldg in targetBuildings)
                        {
                            foreach (TypicalFloorGroup group in bldg.TypicalGroups)
                            {
                                string srcLevelName = group.IsDuplexModule ? group.SourceLevelNameLower : group.SourceLevelName;
                                if (string.IsNullOrEmpty(srcLevelName) || processedMasterLevels.Contains(srcLevelName)) continue;
                                processedMasterLevels.Add(srcLevelName);

                                Level srcLevel = GetLevelByName(srcLevelName);
                                if (srcLevel == null) continue;

                                string rangeLabel = GetGroupRangeLabel(group);
                                string kindSuffix = (pkg.ViewKind == ViewPlanKind.AreaPlan) ? "AREA PLAN" : "FLOOR PLAN";
                                string viewName = string.Format("FL. {0} - MASTER OVERALL {1}", rangeLabel, kindSuffix);
                                string titleOnSheet = string.Format("MASTER - {0} OVERALL {1}", rangeLabel.ToUpperInvariant(), kindSuffix);

                                ViewPlan plan = CreateOrDuplicatePlanView(pkg.ViewKind, pkg.SelectedAreaSchemeName, srcLevel, floorPlanVft, ceilingPlanVft, config);

                                if (plan != null)
                                {
                                    plan.Name = GetUniqueViewName(viewName);
                                    plan.Scale = effectiveScale;

                                    if (pkg.SelectedTemplateId != ElementId.InvalidElementId)
                                    {
                                        try { plan.ViewTemplateId = pkg.SelectedTemplateId; } catch { }
                                    }

                                    if (masterScopeBoxId != ElementId.InvalidElementId)
                                    {
                                        AssignScopeBoxToView(plan, masterScopeBoxId);
                                    }

                                    SetTitleOnSheetParameter(plan, titleOnSheet);
                                    SetViewBuildingParameter(plan, config.ViewBuildingParameterName, "Master");
                                    createdMap[plan.Name] = plan.Id;
                                }
                            }
                        }
                        continue;
                    }

                    // Case B: Building-Specific Packages (Gross, Deductions, Life Safety, RCP, Architectural)
                    foreach (BuildingDefinition bldg in targetBuildings)
                    {
                        ElementId bldgScopeBoxId = ElementId.InvalidElementId;
                        if (!string.IsNullOrEmpty(bldg.ScopeBoxName) && scopeBoxMap.ContainsKey(bldg.ScopeBoxName))
                        {
                            bldgScopeBoxId = scopeBoxMap[bldg.ScopeBoxName];
                        }

                        foreach (TypicalFloorGroup group in bldg.TypicalGroups)
                        {
                            string srcLevelName = group.IsDuplexModule ? group.SourceLevelNameLower : group.SourceLevelName;
                            if (string.IsNullOrEmpty(srcLevelName)) continue;

                            Level srcLevel = GetLevelByName(srcLevelName);
                            if (srcLevel == null) continue;

                            string rangeLabel = GetGroupRangeLabel(group);
                            string bldgTag = bldg.Name.ToUpperInvariant();

                            string viewName = "";
                            string titleOnSheet = "";

                            switch (pkg.PackageType)
                            {
                                case ViewPackageType.GrossArea:
                                    viewName = string.Format("FL. {0} - GROSS AREA PLAN ({1})", rangeLabel, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} GROSS AREA PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                    break;

                                case ViewPackageType.Deductions:
                                    viewName = string.Format("FL. {0} - DEDUCTIONS PLAN ({1})", rangeLabel, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} DEDUCTIONS PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                    break;

                                case ViewPackageType.Architectural:
                                    if (pkg.ViewKind == ViewPlanKind.AreaPlan)
                                    {
                                        string schName = !string.IsNullOrEmpty(pkg.SelectedAreaSchemeName) ? pkg.SelectedAreaSchemeName : "Area";
                                        viewName = string.Format("FL. {0} - {1} PLAN ({2})", rangeLabel, schName.ToUpperInvariant(), bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} {2} PLAN", bldgTag, rangeLabel.ToUpperInvariant(), schName.ToUpperInvariant());
                                    }
                                    else
                                    {
                                        viewName = string.Format("FL. {0} - ARCHITECTURAL PLAN ({1})", rangeLabel, bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} FLOOR PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                    }
                                    break;

                                case ViewPackageType.CeilingPlanRCP:
                                    viewName = string.Format("FL. {0} - CEILING PLAN RCP ({1})", rangeLabel, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} REFLECTED CEILING PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                    break;

                                case ViewPackageType.EgressLifeSafety:
                                    if (pkg.ViewKind == ViewPlanKind.AreaPlan)
                                    {
                                        viewName = string.Format("FL. {0} - LIFE SAFETY AREA PLAN ({1})", rangeLabel, bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} LIFE SAFETY AREA PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                    }
                                    else
                                    {
                                        viewName = string.Format("FL. {0} - LIFE SAFETY PLAN ({1})", rangeLabel, bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} LIFE SAFETY PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                    }
                                    break;

                                case ViewPackageType.Custom:
                                default:
                                    string pkgTitle = !string.IsNullOrEmpty(pkg.DisplayName) ? pkg.DisplayName.ToUpperInvariant() : "CUSTOM";
                                    if (pkg.ViewKind == ViewPlanKind.AreaPlan)
                                    {
                                        string schName = !string.IsNullOrEmpty(pkg.SelectedAreaSchemeName) ? pkg.SelectedAreaSchemeName.ToUpperInvariant() : "AREA";
                                        viewName = string.Format("FL. {0} - {1} [{2}] ({3})", rangeLabel, pkgTitle, schName, bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} {2}", bldgTag, rangeLabel.ToUpperInvariant(), pkgTitle);
                                    }
                                    else if (pkg.ViewKind == ViewPlanKind.CeilingPlan)
                                    {
                                        viewName = string.Format("FL. {0} - {1} RCP ({2})", rangeLabel, pkgTitle, bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} {2}", bldgTag, rangeLabel.ToUpperInvariant(), pkgTitle);
                                    }
                                    else
                                    {
                                        viewName = string.Format("FL. {0} - {1} ({2})", rangeLabel, pkgTitle, bldgTag);
                                        titleOnSheet = string.Format("{0} - {1} {2}", bldgTag, rangeLabel.ToUpperInvariant(), pkgTitle);
                                    }
                                    break;
                            }

                            ViewPlan plan = CreateOrDuplicatePlanView(pkg.ViewKind, pkg.SelectedAreaSchemeName, srcLevel, floorPlanVft, ceilingPlanVft, config);

                            if (plan != null)
                            {
                                plan.Name = GetUniqueViewName(viewName);
                                plan.Scale = effectiveScale;

                                if (pkg.SelectedTemplateId != ElementId.InvalidElementId)
                                {
                                    try { plan.ViewTemplateId = pkg.SelectedTemplateId; } catch { }
                                }

                                if (bldgScopeBoxId != ElementId.InvalidElementId)
                                {
                                    AssignScopeBoxToView(plan, bldgScopeBoxId);
                                }
                                else if (masterScopeBoxId != ElementId.InvalidElementId)
                                {
                                    AssignScopeBoxToView(plan, masterScopeBoxId);
                                }

                                SetTitleOnSheetParameter(plan, titleOnSheet);
                                SetViewBuildingParameter(plan, config.ViewBuildingParameterName, bldg.Name);
                                createdMap[plan.Name] = plan.Id;
                            }
                        }
                    }
                }

                tx.Commit();
            }

            return createdMap;
        }

        private ViewPlan CreateOrDuplicatePlanView(
            ViewPlanKind viewKind,
            string areaSchemeName,
            Level srcLevel,
            ViewFamilyType floorPlanVft,
            ViewFamilyType ceilingPlanVft,
            MappingConfig config)
        {
            if (srcLevel == null) return null;

            ViewPlan plan = null;

            if (viewKind == ViewPlanKind.AreaPlan)
            {
                AreaScheme scheme = null;
                if (!string.IsNullOrEmpty(areaSchemeName))
                {
                    scheme = GetAreaSchemeByName(areaSchemeName);
                }
                if (scheme == null && !string.IsNullOrEmpty(config.GrossAreaSchemeName))
                {
                    scheme = GetAreaSchemeByName(config.GrossAreaSchemeName);
                }
                if (scheme == null)
                {
                    scheme = new FilteredElementCollector(_doc)
                        .OfClass(typeof(AreaScheme))
                        .Cast<AreaScheme>()
                        .FirstOrDefault();
                }

                if (scheme != null)
                {
                    try
                    {
                        plan = ViewPlan.CreateAreaPlan(_doc, scheme.Id, srcLevel.Id);
                    }
                    catch
                    {
                        ViewPlan existing = FindExistingAreaPlan(scheme.Id, srcLevel.Id);
                        if (existing != null)
                        {
                            try
                            {
                                ElementId dupId = existing.Duplicate(ViewDuplicateOption.WithDetailing);
                                plan = _doc.GetElement(dupId) as ViewPlan;
                            }
                            catch
                            {
                                try
                                {
                                    ElementId dupId = existing.Duplicate(ViewDuplicateOption.Duplicate);
                                    plan = _doc.GetElement(dupId) as ViewPlan;
                                }
                                catch { }
                            }
                        }
                    }
                }
                else
                {
                    if (floorPlanVft != null)
                    {
                        try { plan = ViewPlan.Create(_doc, floorPlanVft.Id, srcLevel.Id); }
                        catch
                        {
                            ViewPlan existing = FindExistingFloorPlan(srcLevel.Id);
                            if (existing != null)
                            {
                                ElementId dupId = existing.Duplicate(ViewDuplicateOption.WithDetailing);
                                plan = _doc.GetElement(dupId) as ViewPlan;
                            }
                        }
                    }
                }
            }
            else if (viewKind == ViewPlanKind.CeilingPlan)
            {
                if (ceilingPlanVft != null)
                {
                    try { plan = ViewPlan.Create(_doc, ceilingPlanVft.Id, srcLevel.Id); }
                    catch
                    {
                        ViewPlan existing = FindExistingCeilingPlan(srcLevel.Id);
                        if (existing != null)
                        {
                            ElementId dupId = existing.Duplicate(ViewDuplicateOption.WithDetailing);
                            plan = _doc.GetElement(dupId) as ViewPlan;
                        }
                    }
                }
            }
            else // FloorPlan
            {
                if (floorPlanVft != null)
                {
                    try { plan = ViewPlan.Create(_doc, floorPlanVft.Id, srcLevel.Id); }
                    catch
                    {
                        ViewPlan existing = FindExistingFloorPlan(srcLevel.Id);
                        if (existing != null)
                        {
                            ElementId dupId = existing.Duplicate(ViewDuplicateOption.WithDetailing);
                            plan = _doc.GetElement(dupId) as ViewPlan;
                        }
                    }
                }
            }

            return plan;
        }

        private ViewPlan FindExistingAreaPlan(ElementId schemeId, ElementId levelId)
        {
            try
            {
                return new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewPlan))
                    .Cast<ViewPlan>()
                    .FirstOrDefault(v => !v.IsTemplate && 
                                         v.ViewType == ViewType.AreaPlan && 
                                         v.GenLevel != null && v.GenLevel.Id == levelId &&
                                         v.AreaScheme != null && v.AreaScheme.Id == schemeId);
            }
            catch
            {
                return null;
            }
        }

        private ViewPlan FindExistingFloorPlan(ElementId levelId)
        {
            try
            {
                return new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewPlan))
                    .Cast<ViewPlan>()
                    .FirstOrDefault(v => !v.IsTemplate && 
                                         v.ViewType == ViewType.FloorPlan && 
                                         v.GenLevel != null && v.GenLevel.Id == levelId);
            }
            catch
            {
                return null;
            }
        }

        private ViewPlan FindExistingCeilingPlan(ElementId levelId)
        {
            try
            {
                return new FilteredElementCollector(_doc)
                    .OfClass(typeof(ViewPlan))
                    .Cast<ViewPlan>()
                    .FirstOrDefault(v => !v.IsTemplate && 
                                         v.ViewType == ViewType.CeilingPlan && 
                                         v.GenLevel != null && v.GenLevel.Id == levelId);
            }
            catch
            {
                return null;
            }
        }

        private void SetTitleOnSheetParameter(View view, string titleText)
        {
            if (view == null || string.IsNullOrEmpty(titleText)) return;
            try
            {
                Parameter p = view.get_Parameter(BuiltInParameter.VIEW_DESCRIPTION);
                if (p != null && !p.IsReadOnly)
                {
                    p.Set(titleText);
                }
            }
            catch
            {
            }
        }

        public string GetGroupRangeLabel(TypicalFloorGroup g)
        {
            if (g == null) return "TYPICAL";
            if (g.IsSingleLevel) return g.SourceLevelName ?? "TYP";
            if (g.IsDuplexModule)
            {
                return string.Format("{0}-{1} (DUPLEX)", g.FromLevelName, g.ToLevelName);
            }
            return string.Format("{0} TO {1}", g.FromLevelName, g.ToLevelName);
        }

        private Level GetLevelByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => string.Equals(l.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private AreaScheme GetAreaSchemeByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(AreaScheme))
                .Cast<AreaScheme>()
                .FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private ViewFamilyType GetViewFamilyType(ViewFamily vf)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == vf);
        }

        private Dictionary<string, ElementId> GetScopeBoxElementMap()
        {
            Dictionary<string, ElementId> map = new Dictionary<string, ElementId>(StringComparer.OrdinalIgnoreCase);
            try
            {
                FilteredElementCollector collector = new FilteredElementCollector(_doc)
                    .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                    .WhereElementIsNotElementType();

                foreach (Element elem in collector)
                {
                    if (elem != null && !string.IsNullOrEmpty(elem.Name))
                    {
                        map[elem.Name] = elem.Id;
                    }
                }
            }
            catch
            {
            }
            return map;
        }

        private void AssignScopeBoxToView(View view, ElementId scopeBoxId)
        {
            if (view == null || scopeBoxId == ElementId.InvalidElementId) return;
            try
            {
                Parameter p = view.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP);
                if (p != null && !p.IsReadOnly)
                {
                    p.Set(scopeBoxId);
                }
            }
            catch
            {
            }
        }

        private void SetViewBuildingParameter(View view, string paramName, string buildingName)
        {
            if (view == null || string.IsNullOrEmpty(paramName) || string.IsNullOrEmpty(buildingName)) return;
            try
            {
                Parameter p = view.LookupParameter(paramName);
                if (p != null && p.StorageType == StorageType.String && !p.IsReadOnly)
                {
                    p.Set(buildingName);
                }
            }
            catch
            {
            }
        }

        private string GetUniqueViewName(string baseName)
        {
            string candidate = baseName;
            int counter = 2;
            while (IsViewNameExists(candidate))
            {
                candidate = string.Format("{0} ({1})", baseName, counter);
                counter++;
            }
            return candidate;
        }

        public List<GeneratedViewResult> GenerateMasterAndDependentViews(
            List<BuildingDefinition> buildings,
            MappingConfig config,
            bool createArchPlans,
            bool createGrossPlans,
            bool createDedPlans,
            bool typicalMasterOnly)
        {
            List<GeneratedViewResult> results = new List<GeneratedViewResult>();
            if (buildings == null || buildings.Count == 0) return results;

            Dictionary<string, ElementId> scopeBoxMap = GetScopeBoxElementMap();
            ViewFamilyType floorPlanVft = GetViewFamilyType(ViewFamily.FloorPlan);
            ViewFamilyType areaPlanVft = GetViewFamilyType(ViewFamily.AreaPlan);

            AreaScheme grossScheme = !string.IsNullOrEmpty(config.GrossAreaSchemeName) ? GetAreaSchemeByName(config.GrossAreaSchemeName) : null;
            AreaScheme dedScheme = !string.IsNullOrEmpty(config.DeductionAreaSchemeName) ? GetAreaSchemeByName(config.DeductionAreaSchemeName) : null;

            ElementId masterScopeBoxId = ElementId.InvalidElementId;
            if (!string.IsNullOrEmpty(config.MasterScopeBoxName) && scopeBoxMap.ContainsKey(config.MasterScopeBoxName))
            {
                masterScopeBoxId = scopeBoxMap[config.MasterScopeBoxName];
            }

            using (Transaction tx = new Transaction(_doc, "BauTools: Create Master & Dependent Views"))
            {
                tx.Start();

                HashSet<string> processedLevels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (BuildingDefinition bldg in buildings)
                {
                    foreach (TypicalFloorGroup group in bldg.TypicalGroups)
                    {
                        string srcLevelName = group.IsDuplexModule ? group.SourceLevelNameLower : group.SourceLevelName;
                        if (string.IsNullOrEmpty(srcLevelName)) continue;

                        Level srcLevel = GetLevelByName(srcLevelName);
                        if (srcLevel == null) continue;

                        string rangeLabel = GetGroupRangeLabel(group);

                        if (createArchPlans && floorPlanVft != null)
                        {
                            string key = string.Format("ARCH_{0}", srcLevelName);
                            if (!processedLevels.Contains(key))
                            {
                                processedLevels.Add(key);
                                ViewPlan masterView = ViewPlan.Create(_doc, floorPlanVft.Id, srcLevel.Id);
                                masterView.Name = GetUniqueViewName(string.Format("FL. {0} - MASTER OVERALL FLOOR PLAN", rangeLabel));
                                if (masterScopeBoxId != ElementId.InvalidElementId) AssignScopeBoxToView(masterView, masterScopeBoxId);

                                GeneratedViewResult gvr = new GeneratedViewResult
                                {
                                    MasterView = masterView,
                                    RangeLabel = rangeLabel,
                                    ViewTypeLabel = "Architectural"
                                };

                                if (buildings.Count > 1)
                                {
                                    foreach (BuildingDefinition subBldg in buildings)
                                    {
                                        ElementId depId = masterView.Duplicate(ViewDuplicateOption.AsDependent);
                                        View depView = _doc.GetElement(depId) as View;
                                        if (depView != null)
                                        {
                                            depView.Name = GetUniqueViewName(string.Format("FL. {0} - {1} FLOOR PLAN", rangeLabel, subBldg.Name.ToUpperInvariant()));
                                            if (!string.IsNullOrEmpty(subBldg.ScopeBoxName) && scopeBoxMap.ContainsKey(subBldg.ScopeBoxName))
                                            {
                                                AssignScopeBoxToView(depView, scopeBoxMap[subBldg.ScopeBoxName]);
                                            }
                                            SetViewBuildingParameter(depView, config.ViewBuildingParameterName, subBldg.Name);
                                            gvr.DependentViews.Add(depView);
                                        }
                                    }
                                }
                                results.Add(gvr);
                            }
                        }
                    }
                }

                tx.Commit();
            }

            return results;
        }

        private bool IsViewNameExists(string name)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Any(v => !v.IsTemplate && string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
```

### `ZoningFloorArea\Services\SmartScaleAdvisorService.cs`
```csharp
using System;
using System.Collections.Generic;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class ScaleOption
    {
        public int ScaleValue { get; set; }
        public string DisplayName { get; set; }
        public double Factor { get; set; } // e.g. 1/96 = 0.0104167

        public ScaleOption(int val, string name)
        {
            ScaleValue = val;
            DisplayName = name;
            Factor = 1.0 / val;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class SmartScaleAdvisorService
    {
        public static readonly List<ScaleOption> StandardScales = new List<ScaleOption>
        {
            new ScaleOption(24, "1/2\" = 1'-0\" (1:24)"),
            new ScaleOption(32, "3/8\" = 1'-0\" (1:32)"),
            new ScaleOption(48, "1/4\" = 1'-0\" (1:48)"),
            new ScaleOption(64, "3/16\" = 1'-0\" (1:64)"),
            new ScaleOption(96, "1/8\" = 1'-0\" (1:96)"),
            new ScaleOption(128, "3/32\" = 1'-0\" (1:128)"),
            new ScaleOption(192, "1/16\" = 1'-0\" (1:192)"),
            new ScaleOption(50, "1:50 Metric"),
            new ScaleOption(100, "1:100 Metric"),
            new ScaleOption(200, "1:200 Metric")
        };

        public ScaleOption RecommendScale(
            double buildingWidthFt,
            double buildingDepthFt,
            TitleblockItem titleblock,
            SheetLayoutMode layoutMode)
        {
            if (buildingWidthFt <= 0) buildingWidthFt = 150.0;
            if (buildingDepthFt <= 0) buildingDepthFt = 100.0;
            if (titleblock == null) titleblock = new TitleblockItem();

            int rows = 1;
            int cols = 1;

            switch (layoutMode)
            {
                case SheetLayoutMode.Single1View:
                    rows = 1; cols = 1; break;
                case SheetLayoutMode.Dual2Views:
                    rows = 1; cols = 2; break;
                case SheetLayoutMode.Triple3Views:
                    rows = 1; cols = 3; break;
                case SheetLayoutMode.Quad4Views:
                    rows = 2; cols = 2; break;
                case SheetLayoutMode.Hex6Views:
                    rows = 2; cols = 3; break;
                case SheetLayoutMode.Octo8Views:
                    rows = 2; cols = 4; break;
            }

            // Usable slot in inches on paper (with margin for annotations/titles)
            double slotW_in = (titleblock.UsableWidthInches / cols) - 1.5;
            double slotH_in = (titleblock.UsableHeightInches / rows) - 2.0;

            if (slotW_in <= 2.0) slotW_in = 2.0;
            if (slotH_in <= 2.0) slotH_in = 2.0;

            // Iterate scales from largest to smallest to find the optimal fit
            ScaleOption bestFit = StandardScales[4]; // Default 1/8"

            foreach (ScaleOption s in StandardScales)
            {
                // Converted to paper inches
                double drawnW_in = (buildingWidthFt * 12.0) / s.ScaleValue;
                double drawnH_in = (buildingDepthFt * 12.0) / s.ScaleValue;

                if (drawnW_in <= slotW_in && drawnH_in <= slotH_in)
                {
                    return s;
                }
            }

            return StandardScales[6]; // Fallback to 1/16"
        }
    }
}
```

### `ZoningFloorArea\Services\TypicalFloorStorageService.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class TypicalFloorStorageService
    {
        private static readonly Guid SCHEMA_GUID = new Guid("A4D59E72-8C1B-4E33-9F52-D9A3B5C7E120");
        private const string SCHEMA_NAME = "BauToolsTypicalFloorsSchema";
        private const string FIELD_NAME = "TypicalFloorsJson";
        private const string FIELD_BUILDINGS = "BuildingsJson";

        private static Schema GetOrCreateSchema()
        {
            Schema existing = Schema.Lookup(SCHEMA_GUID);
            if (existing != null)
            {
                return existing;
            }

            SchemaBuilder builder = new SchemaBuilder(SCHEMA_GUID);
            builder.SetSchemaName(SCHEMA_NAME);
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.SetVendorId("BauTools");
            builder.SetApplicationGUID(new Guid("F3B1A2C4-D5E6-4F7A-8B9C-0D1E2F3A4B5C"));

            FieldBuilder field = builder.AddSimpleField(FIELD_NAME, typeof(string));
            field.SetDocumentation("JSON serialized list of TypicalFloorGroup definitions for BauTools ZFA.");

            return builder.Finish();
        }

        public List<BuildingDefinition> LoadBuildings(Document doc)
        {
            List<BuildingDefinition> result = new List<BuildingDefinition>();
            if (doc == null) return result;

            try
            {
                Schema schema = GetOrCreateSchema();
                if (schema == null) return result;

                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(DataStorage));

                foreach (Element elem in collector)
                {
                    DataStorage storage = elem as DataStorage;
                    if (storage != null)
                    {
                        Entity entity = storage.GetEntity(schema);
                        if (entity != null && entity.IsValid())
                        {
                            string json = entity.Get<string>(schema.GetField(FIELD_NAME));
                            if (!string.IsNullOrEmpty(json))
                            {
                                // First check if json is List<BuildingDefinition>
                                try
                                {
                                    List<BuildingDefinition> bldgs = JsonSerializer.Deserialize<List<BuildingDefinition>>(json);
                                    if (bldgs != null && bldgs.Count > 0 && bldgs[0].TypicalGroups != null)
                                    {
                                        return bldgs;
                                    }
                                }
                                catch
                                {
                                    // Fallback: legacy flat List<TypicalFloorGroup>
                                    List<TypicalFloorGroup> legacyGroups = JsonSerializer.Deserialize<List<TypicalFloorGroup>>(json);
                                    if (legacyGroups != null && legacyGroups.Count > 0)
                                    {
                                        BuildingDefinition defaultBldg = new BuildingDefinition("Building 1");
                                        defaultBldg.TypicalGroups = new ObservableCollection<TypicalFloorGroup>(legacyGroups);
                                        result.Add(defaultBldg);
                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            if (result.Count == 0)
            {
                result.Add(new BuildingDefinition("Building 1"));
            }

            return result;
        }

        public bool SaveBuildings(Document doc, List<BuildingDefinition> buildings)
        {
            if (doc == null || buildings == null) return false;

            try
            {
                Schema schema = GetOrCreateSchema();
                if (schema == null) return false;

                string json = JsonSerializer.Serialize(buildings);

                using (Transaction t = new Transaction(doc, "BauTools - Save Multi-Building Definitions"))
                {
                    t.Start();

                    DataStorage targetStorage = null;
                    FilteredElementCollector collector = new FilteredElementCollector(doc)
                        .OfClass(typeof(DataStorage));

                    foreach (Element elem in collector)
                    {
                        DataStorage storage = elem as DataStorage;
                        if (storage != null)
                        {
                            Entity entity = storage.GetEntity(schema);
                            if (entity != null && entity.IsValid())
                            {
                                targetStorage = storage;
                                break;
                            }
                        }
                    }

                    if (targetStorage == null)
                    {
                        targetStorage = DataStorage.Create(doc);
                    }

                    Entity newEntity = new Entity(schema);
                    newEntity.Set(schema.GetField(FIELD_NAME), json);
                    targetStorage.SetEntity(newEntity);

                    t.Commit();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

```

### `ZoningFloorArea\Services\ZoningCalculator.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class ZoningCalculator
    {
        private const double SQFT_TO_SQM = 0.09290304;

        public ProjectZoningResult ComputeProjectZoning(List<AreaDataModel> allAreas, MappingConfig config, List<TypicalFloorGroup> groups)
        {
            ProjectZoningResult projectResult = new ProjectZoningResult();
            projectResult.LotArea = config.LotArea;
            projectResult.UlebPercent = config.UlebPercent;

            // Group areas by Building Name
            Dictionary<string, List<AreaDataModel>> bldgGroups = new Dictionary<string, List<AreaDataModel>>(StringComparer.OrdinalIgnoreCase);

            foreach (AreaDataModel a in allAreas)
            {
                string bName = string.IsNullOrEmpty(a.BuildingName) ? config.BuildingName : a.BuildingName;
                if (!bldgGroups.ContainsKey(bName))
                {
                    bldgGroups[bName] = new List<AreaDataModel>();
                }
                bldgGroups[bName].Add(a);
            }

            if (bldgGroups.Count == 0)
            {
                bldgGroups[config.BuildingName] = allAreas;
            }

            List<ZoningTableResult> bldgTables = new List<ZoningTableResult>();

            foreach (KeyValuePair<string, List<AreaDataModel>> kvp in bldgGroups)
            {
                MappingConfig bConfig = new MappingConfig();
                bConfig.GrossAreaSchemeName = config.GrossAreaSchemeName;
                bConfig.DeductionAreaSchemeName = config.DeductionAreaSchemeName;
                bConfig.DeductionTypeParameterName = config.DeductionTypeParameterName;
                bConfig.UsageCategoryParameterName = config.UsageCategoryParameterName;
                bConfig.BuildingParameterName = config.BuildingParameterName;
                bConfig.BuildingName = kvp.Key;
                bConfig.LotArea = config.LotArea;
                bConfig.UlebPercent = config.UlebPercent;
                bConfig.DisplayUnit = config.DisplayUnit;

                ZoningTableResult t = ComputeZoningTable(kvp.Value, bConfig, groups);
                bldgTables.Add(t);
            }

            projectResult.BuildingTables = bldgTables;
            projectResult.OverallSummary = ComputeProjectGrandTotal(bldgTables, config);

            return projectResult;
        }

        public ZoningTableResult ComputeZoningTable(List<AreaDataModel> allAreas, MappingConfig config, List<TypicalFloorGroup> groups)
        {
            double unitFactor = config.DisplayUnit == UnitDisplayMode.SquareMeters ? SQFT_TO_SQM : 1.0;
            double lotAreaConverted = config.LotArea * unitFactor;

            ZoningTableResult result = new ZoningTableResult();
            result.BuildingName = config.BuildingName;
            result.LotArea = lotAreaConverted;
            result.UlebPercent = config.UlebPercent;

            // 1. Separate gross building areas and deduction areas
            List<AreaDataModel> grossAreas = new List<AreaDataModel>();
            List<AreaDataModel> deductionAreas = new List<AreaDataModel>();

            foreach (AreaDataModel a in allAreas)
            {
                if (string.Equals(a.AreaSchemeName, config.GrossAreaSchemeName, StringComparison.OrdinalIgnoreCase))
                {
                    grossAreas.Add(a);
                }
                else if (string.Equals(a.AreaSchemeName, config.DeductionAreaSchemeName, StringComparison.OrdinalIgnoreCase))
                {
                    deductionAreas.Add(a);
                }
            }

            if (deductionAreas.Count == 0)
            {
                foreach (AreaDataModel a in allAreas)
                {
                    if (!string.Equals(a.AreaSchemeName, config.GrossAreaSchemeName, StringComparison.OrdinalIgnoreCase))
                    {
                        deductionAreas.Add(a);
                    }
                }
            }

            // 2. Base Deduction Categories
            List<string> baseCategories = new List<string>
            {
                "CHASE WALLS",
                "STAIRS",
                "MECHANICAL",
                "BYCYCLE PARKING",
                "AMENITIES",
                "CORRIDOR",
                "REFUSE"
            };

            List<string> finalCategories = new List<string>(baseCategories);

            foreach (AreaDataModel d in deductionAreas)
            {
                if (!string.IsNullOrEmpty(d.DeductionType))
                {
                    string trimmedType = d.DeductionType.Trim().ToUpperInvariant();
                    bool exists = false;
                    foreach (string cat in finalCategories)
                    {
                        if (string.Equals(cat, trimmedType, StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        finalCategories.Add(trimmedType);
                    }
                }
            }

            result.DeductionCategories = finalCategories;

            // 3. Get all levels sorted by elevation
            List<AreaDataModel> sortedAreas = allAreas.OrderBy(a => a.LevelElevation).ToList();
            List<string> levelNames = new List<string>();
            Dictionary<string, double> levelElevations = new Dictionary<string, double>();

            foreach (AreaDataModel a in sortedAreas)
            {
                if (!levelNames.Contains(a.LevelName))
                {
                    levelNames.Add(a.LevelName);
                    levelElevations[a.LevelName] = a.LevelElevation;
                }
            }

            if (levelNames.Count == 0)
            {
                return result;
            }

            // 4. Build Level Rows for Residential and Commercial
            List<LevelZoningRow> resRows = new List<LevelZoningRow>();
            List<LevelZoningRow> comRows = new List<LevelZoningRow>();

            foreach (string lvlName in levelNames)
            {
                double lvlElev = levelElevations[lvlName];
                TypicalFloorGroup matchingGroup = FindMatchingGroup(lvlName, lvlElev, levelElevations, groups);

                // Residential Row
                double resGrossSqFt = 0;
                foreach (AreaDataModel a in grossAreas)
                {
                    if (string.Equals(a.LevelName, lvlName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(a.UsageCategory, "Residential", StringComparison.OrdinalIgnoreCase))
                    {
                        resGrossSqFt += a.AreaValue;
                    }
                }

                LevelZoningRow resRow = new LevelZoningRow();
                resRow.LevelName = lvlName;
                resRow.LevelElevation = lvlElev;
                resRow.UsageCategory = "Residential";
                resRow.GrossFloorArea = resGrossSqFt * unitFactor;
                resRow.UlebPercent = config.UlebPercent;
                resRow.LotArea = lotAreaConverted;

                if (matchingGroup != null)
                {
                    resRow.GroupName = matchingGroup.Name;
                    resRow.GroupColorHex = matchingGroup.ColorHex;
                }

                foreach (string cat in result.DeductionCategories)
                {
                    double dedSqFt = 0;
                    foreach (AreaDataModel d in deductionAreas)
                    {
                        if (string.Equals(d.LevelName, lvlName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(d.UsageCategory, "Residential", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(d.DeductionType, cat, StringComparison.OrdinalIgnoreCase))
                        {
                            dedSqFt += d.AreaValue;
                        }
                    }
                    resRow.SetDeduction(cat, dedSqFt * unitFactor);
                }

                resRows.Add(resRow);

                // Commercial Row
                double comGrossSqFt = 0;
                foreach (AreaDataModel a in grossAreas)
                {
                    if (string.Equals(a.LevelName, lvlName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(a.UsageCategory, "Commercial", StringComparison.OrdinalIgnoreCase))
                    {
                        comGrossSqFt += a.AreaValue;
                    }
                }

                LevelZoningRow comRow = new LevelZoningRow();
                comRow.LevelName = lvlName;
                comRow.LevelElevation = lvlElev;
                comRow.UsageCategory = "Commercial";
                comRow.GrossFloorArea = comGrossSqFt * unitFactor;
                comRow.UlebPercent = config.UlebPercent;
                comRow.LotArea = lotAreaConverted;

                if (matchingGroup != null)
                {
                    comRow.GroupName = matchingGroup.Name;
                    comRow.GroupColorHex = matchingGroup.ColorHex;
                }

                foreach (string cat in result.DeductionCategories)
                {
                    double dedSqFt = 0;
                    foreach (AreaDataModel d in deductionAreas)
                    {
                        if (string.Equals(d.LevelName, lvlName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(d.UsageCategory, "Commercial", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(d.DeductionType, cat, StringComparison.OrdinalIgnoreCase))
                        {
                            dedSqFt += d.AreaValue;
                        }
                    }
                    comRow.SetDeduction(cat, dedSqFt * unitFactor);
                }

                comRows.Add(comRow);
            }

            result.ResidentialRows = resRows;
            result.CommercialRows = comRows;

            // 5. Calculate Subtotals and Grand Total
            result.ResidentialSubtotal = CalculateSubtotal("SUBTOTAL", "Residential", resRows, result.DeductionCategories, config.UlebPercent, lotAreaConverted);
            result.CommercialSubtotal = CalculateSubtotal("SUBTOTAL", "Commercial", comRows, result.DeductionCategories, config.UlebPercent, lotAreaConverted);
            result.GrandTotal = CalculateGrandTotal("TOTAL", result.ResidentialSubtotal, result.CommercialSubtotal, result.DeductionCategories, config.UlebPercent, lotAreaConverted);

            return result;
        }

        private TypicalFloorGroup FindMatchingGroup(string lvlName, double lvlElev, Dictionary<string, double> levelElevations, List<TypicalFloorGroup> groups)
        {
            if (groups == null || groups.Count == 0) return null;

            foreach (TypicalFloorGroup g in groups)
            {
                if (string.IsNullOrEmpty(g.FromLevelName) || string.IsNullOrEmpty(g.ToLevelName))
                {
                    if (string.Equals(g.SourceLevelName, lvlName, StringComparison.OrdinalIgnoreCase))
                        return g;
                    continue;
                }

                double fromElev, toElev;
                if (levelElevations.TryGetValue(g.FromLevelName, out fromElev) && levelElevations.TryGetValue(g.ToLevelName, out toElev))
                {
                    double minE = Math.Min(fromElev, toElev);
                    double maxE = Math.Max(fromElev, toElev);

                    if (lvlElev >= minE - 0.001 && lvlElev <= maxE + 0.001)
                    {
                        return g;
                    }
                }
                else
                {
                    if (string.Equals(g.FromLevelName, lvlName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(g.ToLevelName, lvlName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(g.SourceLevelName, lvlName, StringComparison.OrdinalIgnoreCase))
                    {
                        return g;
                    }
                }
            }
            return null;
        }

        private ZoningTableResult ComputeProjectGrandTotal(List<ZoningTableResult> bldgTables, MappingConfig config)
        {
            ZoningTableResult summary = new ZoningTableResult();
            summary.BuildingName = "ALL BUILDINGS TOTAL";
            summary.LotArea = config.LotArea;
            summary.UlebPercent = config.UlebPercent;

            double resGross = 0;
            double comGross = 0;
            Dictionary<string, double> resDeds = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, double> comDeds = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (ZoningTableResult t in bldgTables)
            {
                resGross += t.ResidentialSubtotal.GrossFloorArea;
                comGross += t.CommercialSubtotal.GrossFloorArea;

                foreach (string cat in summary.DeductionCategories)
                {
                    if (!resDeds.ContainsKey(cat)) resDeds[cat] = 0;
                    if (!comDeds.ContainsKey(cat)) comDeds[cat] = 0;

                    resDeds[cat] += t.ResidentialSubtotal.GetDeduction(cat);
                    comDeds[cat] += t.CommercialSubtotal.GetDeduction(cat);
                }
            }

            summary.ResidentialSubtotal.GrossFloorArea = resGross;
            summary.CommercialSubtotal.GrossFloorArea = comGross;

            foreach (string cat in summary.DeductionCategories)
            {
                summary.ResidentialSubtotal.SetDeduction(cat, resDeds[cat]);
                summary.CommercialSubtotal.SetDeduction(cat, comDeds[cat]);
            }

            summary.GrandTotal.GrossFloorArea = resGross + comGross;
            foreach (string cat in summary.DeductionCategories)
            {
                summary.GrandTotal.SetDeduction(cat, resDeds[cat] + comDeds[cat]);
            }

            return summary;
        }

        private LevelZoningRow CalculateSubtotal(string label, string usageCat, List<LevelZoningRow> rows, List<string> categories, double ulebPercent, double lotArea)
        {
            LevelZoningRow subtotal = new LevelZoningRow();
            subtotal.LevelName = label;
            subtotal.UsageCategory = usageCat;
            subtotal.UlebPercent = ulebPercent;
            subtotal.LotArea = lotArea;

            double gross = 0;
            Dictionary<string, double> dedSums = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (string cat in categories)
            {
                dedSums[cat] = 0;
            }

            foreach (LevelZoningRow r in rows)
            {
                gross += r.GrossFloorArea;
                foreach (string cat in categories)
                {
                    dedSums[cat] += r.GetDeduction(cat);
                }
            }

            subtotal.GrossFloorArea = gross;
            foreach (string cat in categories)
            {
                subtotal.SetDeduction(cat, dedSums[cat]);
            }

            return subtotal;
        }

        private LevelZoningRow CalculateGrandTotal(string label, LevelZoningRow resSub, LevelZoningRow comSub, List<string> categories, double ulebPercent, double lotArea)
        {
            LevelZoningRow grandTotal = new LevelZoningRow();
            grandTotal.LevelName = label;
            grandTotal.UsageCategory = "Project Total";
            grandTotal.UlebPercent = ulebPercent;
            grandTotal.LotArea = lotArea;

            grandTotal.GrossFloorArea = resSub.GrossFloorArea + comSub.GrossFloorArea;

            foreach (string cat in categories)
            {
                double totalDed = resSub.GetDeduction(cat) + comSub.GetDeduction(cat);
                grandTotal.SetDeduction(cat, totalDed);
            }

            return grandTotal;
        }
    }
}

```

### `ZoningFloorArea\Tests\ApiInspector.cs`
```csharp
using System;
using System.IO;
using System.Reflection;

namespace ZoningFloorArea.Tests
{
    public class ApiInspector
    {
        public static void Inspect()
        {
            string dllPath = @"g:\Other computers\My Laptop\ENT\REVIT DEVADDINS\ZoningFloorArea\lib\RevitAPI.dll";
            if (!File.Exists(dllPath))
            {
                Console.WriteLine("DLL not found: " + dllPath);
                return;
            }

            try
            {
                Assembly asm = Assembly.LoadFrom(dllPath);
                Console.WriteLine("Assembly Loaded: " + asm.FullName);

                foreach (Type t in asm.GetTypes())
                {
                    if (t.Name.Contains("PropertyLine", StringComparison.OrdinalIgnoreCase) || 
                        t.Name.Contains("SiteProperty", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("=== TYPE: " + t.FullName + " ===");
                        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                        {
                            Console.WriteLine("  Method: " + m.Name + " (" + string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name + " " + p.Name)) + ")");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}

```

### `ZoningFloorArea\Tests\ZoningTest.cs`
```csharp
using System;
using System.Collections.Generic;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;

namespace ZoningFloorArea.Tests
{
    public class ZoningTest
    {
        public static void RunMockVerification()
        {
            List<AreaDataModel> mockAreas = new List<AreaDataModel>();
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Gross Building", LevelName = "1st", LevelElevation = 10, AreaValue = 1737.94, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "1st", LevelElevation = 10, DeductionType = "AMENITIES", AreaValue = 250.91, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "1st", LevelElevation = 10, DeductionType = "CORRIDOR", AreaValue = 376.94, UsageCategory = "Residential" });

            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Gross Building", LevelName = "3rd", LevelElevation = 30, AreaValue = 4428.28, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "3rd", LevelElevation = 30, DeductionType = "CHASE WALLS", AreaValue = 109.49, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "3rd", LevelElevation = 30, DeductionType = "STAIRS", AreaValue = 31.28, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "3rd", LevelElevation = 30, DeductionType = "MECHANICAL", AreaValue = 16.67, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "3rd", LevelElevation = 30, DeductionType = "CORRIDOR", AreaValue = 416.72, UsageCategory = "Residential" });
            mockAreas.Add(new AreaDataModel { AreaSchemeName = "Rentable", LevelName = "3rd", LevelElevation = 30, DeductionType = "REFUSE", AreaValue = 24.00, UsageCategory = "Residential" });

            MappingConfig config = new MappingConfig();
            config.BuildingName = "BUILDING C";
            config.GrossAreaSchemeName = "Gross Building";
            config.DeductionAreaSchemeName = "Rentable";
            config.LotArea = 34500.0;
            config.UlebPercent = 0.05;

            List<TypicalFloorGroup> groups = new List<TypicalFloorGroup>();

            ZoningCalculator calc = new ZoningCalculator();
            ZoningTableResult result = calc.ComputeZoningTable(mockAreas, config, groups);

            Console.WriteLine("=== ZFA CALCULATOR TEST RESULTS ===");
            Console.WriteLine(string.Format("Building: {0}", result.BuildingName));
            Console.WriteLine(string.Format("Lot Area: {0:N2} sq ft", result.LotArea));
            Console.WriteLine(string.Format("Residential Rows: {0}", result.ResidentialRows.Count));

            foreach (LevelZoningRow r in result.ResidentialRows)
            {
                Console.WriteLine(string.Format("Level: {0} | Gross: {1:N2} | Deductions: {2:N2} | Net: {3:N2} | ULEB (5%): {4:N2} | ZFA: {5:N2} | FAR: {6:N2}", r.LevelName, r.GrossFloorArea, r.TotalDeductions, r.NetArea, r.UlebAmount, r.ZoningFloorArea, r.Far));
            }

            Console.WriteLine(string.Format("Subtotal ZFA: {0:N2}", result.ResidentialSubtotal.ZoningFloorArea));
            Console.WriteLine(string.Format("Total ZFA: {0:N2}", result.TotalZoningFloorArea));
            Console.WriteLine(string.Format("Total FAR: {0:N2}", result.TotalFar));
        }
    }
}

```

### `ZoningFloorArea\ViewModels\MainViewModel.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;

namespace ZoningFloorArea.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null) throw new ArgumentNullException("execute");
            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<object> execute) : this(execute, null)
        {
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }

    public class LevelPickerItem
    {
        public string LevelName { get; set; }
        public bool IsAvailable { get; set; }
        public string OccupiedByGroupName { get; set; }

        public string DisplayText
        {
            get
            {
                if (IsAvailable)
                {
                    return LevelName;
                }
                return string.Format("{0}  🔒 (In: {1})", LevelName, OccupiedByGroupName);
            }
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }

    public class BuildingFilterItem : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged("Name"); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");
                if (SelectionChanged != null) SelectionChanged();
            }
        }

        public Action SelectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LevelTowerItem : INotifyPropertyChanged
    {
        private string _levelName;
        private double _elevation;
        private string _elevationDisplay;
        private string _assignedGroupName;
        private string _colorHex;
        private bool _isSingleFloor;

        public string LevelName
        {
            get { return _levelName; }
            set { _levelName = value; OnPropertyChanged("LevelName"); }
        }

        public double Elevation
        {
            get { return _elevation; }
            set { _elevation = value; OnPropertyChanged("Elevation"); }
        }

        public string ElevationDisplay
        {
            get { return _elevationDisplay; }
            set { _elevationDisplay = value; OnPropertyChanged("ElevationDisplay"); }
        }

        public string AssignedGroupName
        {
            get { return _assignedGroupName; }
            set { _assignedGroupName = value; OnPropertyChanged("AssignedGroupName"); }
        }

        public string ColorHex
        {
            get { return _colorHex; }
            set { _colorHex = value; OnPropertyChanged("ColorHex"); }
        }

        public bool IsSingleFloor
        {
            get { return _isSingleFloor; }
            set { _isSingleFloor = value; OnPropertyChanged("IsSingleFloor"); }
        }

        public bool IsAssigned
        {
            get { return !string.IsNullOrEmpty(_assignedGroupName); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Document _doc;
        private readonly RevitAreaExtractor _extractor;
        private readonly ZoningCalculator _calculator;
        private readonly RevitSheetTableDrawer _sheetDrawer;
        private readonly RevitAreaDuplicator _duplicator;
        private readonly TypicalFloorStorageService _storageService;
        private readonly RevitViewGeneratorService _viewGenService;
        private readonly RevitSheetPlacementService _sheetPlaceService;
        private readonly ExcelZoningBridgeService _excelBridgeService;
        private readonly SmartScaleAdvisorService _scaleAdvisor;

        public MappingConfig Config { get; set; }
        public ObservableCollection<string> AreaSchemes { get; set; }
        public ObservableCollection<string> AvailableParameters { get; set; }
        public ObservableCollection<string> AvailableLevels { get; set; }
        public ObservableCollection<string> AvailableScopeBoxes { get; set; }
        public ObservableCollection<string> AvailableViewParameters { get; set; }
        public ObservableCollection<SheetItem> AvailableSheets { get; set; }
        public ObservableCollection<BuildingFilterItem> BuildingItems { get; set; }
        public ObservableCollection<ZoningTableResult> DisplayedTables { get; set; }

        public ObservableCollection<BuildingDefinition> Buildings { get; set; }
        public ObservableCollection<LevelTowerItem> TowerLevels { get; set; }
        public List<GeneratedViewResult> LastGeneratedViews { get; set; }

        private SheetItem _selectedSheet;
        public SheetItem SelectedSheet
        {
            get { return _selectedSheet; }
            set { _selectedSheet = value; OnPropertyChanged("SelectedSheet"); }
        }

        public Action<string, bool> OnToastNotification;

        private BuildingDefinition _selectedBuilding;
        public BuildingDefinition SelectedBuilding
        {
            get { return _selectedBuilding; }
            set
            {
                if (_selectedBuilding != value)
                {
                    _selectedBuilding = value;
                    OnPropertyChanged("SelectedBuilding");
                    OnPropertyChanged("TypicalGroups");
                    RefreshTowerLevels();
                }
            }
        }

        public ObservableCollection<TypicalFloorGroup> TypicalGroups
        {
            get
            {
                return _selectedBuilding != null ? _selectedBuilding.TypicalGroups : new ObservableCollection<TypicalFloorGroup>();
            }
        }

        private int _currentStep;
        public int CurrentStep
        {
            get { return _currentStep; }
            set
            {
                if (_currentStep != value)
                {
                    _currentStep = value;
                    OnPropertyChanged("CurrentStep");
                }
            }
        }

        private bool _propagateGrossArea;
        public bool PropagateGrossArea
        {
            get { return _propagateGrossArea; }
            set { _propagateGrossArea = value; OnPropertyChanged("PropagateGrossArea"); }
        }

        private bool _propagateDeductionsArea;
        public bool PropagateDeductionsArea
        {
            get { return _propagateDeductionsArea; }
            set { _propagateDeductionsArea = value; OnPropertyChanged("PropagateDeductionsArea"); }
        }

        public ObservableCollection<TitleblockItem> AvailableTitleblocks { get; set; }
        private TitleblockItem _selectedTitleblock;
        public TitleblockItem SelectedTitleblock
        {
            get { return _selectedTitleblock; }
            set { _selectedTitleblock = value; OnPropertyChanged("SelectedTitleblock"); }
        }

        public ObservableCollection<ViewTemplateItem> AvailableViewTemplates { get; set; }
        public ObservableCollection<PackageSetting> PackageSettings { get; set; }
        public ObservableCollection<PlannedSheet> PlannedSheets { get; set; }

        private SheetLayoutMode _selectedLayoutMode;
        public SheetLayoutMode SelectedLayoutMode
        {
            get { return _selectedLayoutMode; }
            set
            {
                _selectedLayoutMode = value;
                OnPropertyChanged("SelectedLayoutMode");
                ComputePlannedSheets();
            }
        }

        private int _selectedViewScale;
        public int SelectedViewScale
        {
            get { return _selectedViewScale; }
            set { _selectedViewScale = value; OnPropertyChanged("SelectedViewScale"); }
        }

        private bool _onlyTypicalRanges;
        public bool OnlyTypicalRanges
        {
            get { return _onlyTypicalRanges; }
            set
            {
                _onlyTypicalRanges = value;
                OnPropertyChanged("OnlyTypicalRanges");
                ComputePlannedSheets();
            }
        }

        private bool _repositionIfExists;
        public bool RepositionIfExists
        {
            get { return _repositionIfExists; }
            set { _repositionIfExists = value; OnPropertyChanged("RepositionIfExists"); }
        }

        private ZoningLotData _lotData;
        public ZoningLotData LotData
        {
            get { return _lotData; }
            set
            {
                _lotData = value;
                OnPropertyChanged("LotData");
                EvaluateCompliance();
            }
        }

        private ZoningComplianceReport _complianceReport;
        public ZoningComplianceReport ComplianceReport
        {
            get { return _complianceReport; }
            set
            {
                _complianceReport = value;
                OnPropertyChanged("ComplianceReport");
            }
        }

        private ProjectZoningResult _projectResult;
        public ProjectZoningResult ProjectResult
        {
            get { return _projectResult; }
            set
            {
                _projectResult = value;
                OnPropertyChanged("ProjectResult");
            }
        }

        private ZoningTableResult _selectedTableResult;
        public ZoningTableResult SelectedTableResult
        {
            get { return _selectedTableResult; }
            set
            {
                _selectedTableResult = value;
                OnPropertyChanged("SelectedTableResult");
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get { return _statusMessage; }
            set
            {
                _statusMessage = value;
                OnPropertyChanged("StatusMessage");
            }
        }

        public ICommand CalculateCommand { get; private set; }
        public ICommand ExportExcelCommand { get; private set; }
        public ICommand CreateRevitViewsCommand { get; private set; }
        public ICommand PropagateAreasCommand { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel(Document doc)
        {
            _doc = doc;
            _extractor = new RevitAreaExtractor(doc);
            _calculator = new ZoningCalculator();
            _sheetDrawer = new RevitSheetTableDrawer(doc);
            _duplicator = new RevitAreaDuplicator(doc);
            _storageService = new TypicalFloorStorageService();
            _viewGenService = new RevitViewGeneratorService(doc);
            _sheetPlaceService = new RevitSheetPlacementService(doc);
            _excelBridgeService = new ExcelZoningBridgeService();
            _scaleAdvisor = new SmartScaleAdvisorService();
            _lotData = new ZoningLotData();
            _complianceReport = new ZoningComplianceReport();

            Config = new MappingConfig();
            AreaSchemes = new ObservableCollection<string>();
            AvailableParameters = new ObservableCollection<string>();
            AvailableLevels = new ObservableCollection<string>();
            AvailableScopeBoxes = new ObservableCollection<string>();
            AvailableViewParameters = new ObservableCollection<string>();
            AvailableSheets = new ObservableCollection<SheetItem>();
            BuildingItems = new ObservableCollection<BuildingFilterItem>();
            DisplayedTables = new ObservableCollection<ZoningTableResult>();
            TowerLevels = new ObservableCollection<LevelTowerItem>();
            LastGeneratedViews = new List<GeneratedViewResult>();

            _propagateGrossArea = true;
            _propagateDeductionsArea = true;
            _currentStep = 0; // Step 1 default

            InitializeData();

            CalculateCommand = new RelayCommand(p => CalculateTable());
            ExportExcelCommand = new RelayCommand(p => ExportToExcel());
            CreateRevitViewsCommand = new RelayCommand(p => CreateDraftingViews());
            PropagateAreasCommand = new RelayCommand(p => PropagateAreasFromTypicalGroups());
        }

        private void InitializeData()
        {
            try
            {
                // 1. Schemes
                List<string> schemes = _extractor.GetAreaSchemeNames();
                foreach (string s in schemes) AreaSchemes.Add(s);

                string grossMatch = schemes.FirstOrDefault(s => s.IndexOf("Gross", StringComparison.OrdinalIgnoreCase) >= 0);
                Config.GrossAreaSchemeName = grossMatch ?? (schemes.Count > 0 ? schemes[0] : string.Empty);

                string dedMatch = schemes.FirstOrDefault(s => s.IndexOf("Deduction", StringComparison.OrdinalIgnoreCase) >= 0 || s.IndexOf("Rentable", StringComparison.OrdinalIgnoreCase) >= 0);
                Config.DeductionAreaSchemeName = dedMatch ?? (schemes.Count > 1 ? schemes[1] : Config.GrossAreaSchemeName);

                // 2. Parameters
                List<string> paramsList = _extractor.GetAvailableAreaParameters();
                foreach (string p in paramsList) AvailableParameters.Add(p);

                string dedParam = paramsList.FirstOrDefault(p => p.IndexOf("Deduction", StringComparison.OrdinalIgnoreCase) >= 0);
                Config.DeductionTypeParameterName = dedParam ?? "Deduction";

                string bldgParam = paramsList.FirstOrDefault(p => p.IndexOf("Building", StringComparison.OrdinalIgnoreCase) >= 0);
                Config.BuildingParameterName = bldgParam ?? "Building";

                string usageParam = paramsList.FirstOrDefault(p => p.IndexOf("Usage", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("Category", StringComparison.OrdinalIgnoreCase) >= 0);
                Config.UsageCategoryParameterName = usageParam ?? "UsageCategory";

                // 3. Scope Boxes & View Parameters
                List<string> sBoxes = _viewGenService.GetAvailableScopeBoxes();
                foreach (string sb in sBoxes) AvailableScopeBoxes.Add(sb);

                List<string> vParams = _viewGenService.GetAvailableViewStringParameters();
                foreach (string vp in vParams) AvailableViewParameters.Add(vp);

                if (AvailableViewParameters.Contains("Building")) Config.ViewBuildingParameterName = "Building";
                else if (AvailableViewParameters.Contains("Comments")) Config.ViewBuildingParameterName = "Comments";

                // 4. Sheets & Titleblocks
                List<SheetItem> sheets = _sheetPlaceService.GetExistingSheets();
                foreach (SheetItem sh in sheets) AvailableSheets.Add(sh);
                if (AvailableSheets.Count > 0) SelectedSheet = AvailableSheets[0];

                AvailableTitleblocks = new ObservableCollection<TitleblockItem>();
                List<TitleblockItem> tblocks = _sheetPlaceService.GetAvailableTitleblocks();
                foreach (TitleblockItem tb in tblocks) AvailableTitleblocks.Add(tb);
                if (AvailableTitleblocks.Count > 0) SelectedTitleblock = AvailableTitleblocks[0];

                AvailableViewTemplates = new ObservableCollection<ViewTemplateItem>();
                AvailableViewTemplates.Add(new ViewTemplateItem { Name = "(None)", TemplateId = ElementId.InvalidElementId });
                List<ViewTemplateItem> vTemplates = _sheetPlaceService.GetAvailableViewTemplates();
                foreach (ViewTemplateItem vt in vTemplates) AvailableViewTemplates.Add(vt);

                PackageSettings = new ObservableCollection<PackageSetting>
                {
                    new PackageSetting(ViewPackageType.MasterOverall, "Master Overall Plans", "🌐", "M-", 101, SheetLayoutMode.Single1View, 192, "1/16\" = 1'-0\" (1:192)", ViewPlanKind.FloorPlan),
                    new PackageSetting(ViewPackageType.GrossArea, "Gross Area Plans", "📐", "Z-", 101, SheetLayoutMode.Quad4Views, 96, "1/8\" = 1'-0\" (1:96)", ViewPlanKind.AreaPlan, Config.GrossAreaSchemeName),
                    new PackageSetting(ViewPackageType.Deductions, "Deductions Plans", "✂️", "ZD-", 101, SheetLayoutMode.Quad4Views, 96, "1/8\" = 1'-0\" (1:96)", ViewPlanKind.AreaPlan, Config.DeductionAreaSchemeName),
                    new PackageSetting(ViewPackageType.EgressLifeSafety, "Life Safety Plans", "🚨", "LS-", 101, SheetLayoutMode.Dual2Views, 96, "1/8\" = 1'-0\" (1:96)", ViewPlanKind.FloorPlan),
                    new PackageSetting(ViewPackageType.CeilingPlanRCP, "Reflected Ceiling (RCP)", "💡", "RCP-", 101, SheetLayoutMode.Quad4Views, 96, "1/8\" = 1'-0\" (1:96)", ViewPlanKind.CeilingPlan),
                    new PackageSetting(ViewPackageType.Architectural, "Floor Plans", "🏛️", "A-", 101, SheetLayoutMode.Dual2Views, 96, "1/8\" = 1'-0\" (1:96)", ViewPlanKind.FloorPlan)
                };

                PlannedSheets = new ObservableCollection<PlannedSheet>();
                _selectedLayoutMode = SheetLayoutMode.Quad4Views;
                _selectedViewScale = 96;
                _onlyTypicalRanges = true;
                _repositionIfExists = true;

                // 5. Levels
                List<Level> levels = _duplicator.GetAllLevels();
                foreach (Level l in levels)
                {
                    AvailableLevels.Add(l.Name);
                }

                // 6. Load Multi-Buildings from Storage
                List<BuildingDefinition> loadedBldgs = _storageService.LoadBuildings(_doc);
                Buildings = new ObservableCollection<BuildingDefinition>(loadedBldgs);
                SelectedBuilding = Buildings.Count > 0 ? Buildings[0] : new BuildingDefinition("Building 1");

                RefreshTowerLevels();
                ComputePlannedSheets();
                StatusMessage = string.Format("Ready. {0} level(s) loaded across {1} building(s).", AvailableLevels.Count, Buildings.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = "Initialization Error: " + ex.Message;
            }
        }

        public void AddBuilding(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = string.Format("Building {0}", Buildings.Count + 1);
            }

            BuildingDefinition newBldg = new BuildingDefinition(name);
            Buildings.Add(newBldg);
            SelectedBuilding = newBldg;
            SaveTypicalGroups();
            StatusMessage = string.Format("Created '{0}'.", name);
            TriggerToast(string.Format("Building '{0}' created.", name), false);
        }

        public BuildingDefinition DuplicateBuilding(BuildingDefinition sourceBuilding, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                newName = string.Format("Building {0}", Buildings.Count + 1);
            }

            BuildingDefinition newBldg = new BuildingDefinition(newName);
            if (sourceBuilding != null)
            {
                newBldg.ScopeBoxName = sourceBuilding.ScopeBoxName;
                foreach (TypicalFloorGroup srcGroup in sourceBuilding.TypicalGroups)
                {
                    TypicalFloorGroup g = new TypicalFloorGroup();
                    g.Name = srcGroup.Name;
                    g.ColorHex = srcGroup.ColorHex;
                    g.IsSingleFloorOnly = srcGroup.IsSingleFloorOnly;
                    g.IsDuplexModule = srcGroup.IsDuplexModule;
                    g.SourceLevelName = srcGroup.SourceLevelName;
                    g.SourceLevelNameLower = srcGroup.SourceLevelNameLower;
                    g.SourceLevelNameUpper = srcGroup.SourceLevelNameUpper;
                    g.FromLevelName = srcGroup.FromLevelName;
                    g.ToLevelName = srcGroup.ToLevelName;
                    g.Order = srcGroup.Order;
                    newBldg.TypicalGroups.Add(g);
                }
            }

            Buildings.Add(newBldg);
            SelectedBuilding = newBldg;
            SaveTypicalGroups();
            RefreshTowerLevels();
            string msg = sourceBuilding != null ? string.Format("Created '{0}' by copying layout from '{1}'.", newName, sourceBuilding.Name) : string.Format("Created '{0}'.", newName);
            StatusMessage = msg;
            TriggerToast(msg, false);
            return newBldg;
        }

        public void CopyGroupsFromBuilding(BuildingDefinition targetBuilding, BuildingDefinition sourceBuilding)
        {
            if (targetBuilding == null || sourceBuilding == null || targetBuilding == sourceBuilding) return;

            targetBuilding.TypicalGroups.Clear();
            foreach (TypicalFloorGroup srcGroup in sourceBuilding.TypicalGroups)
            {
                TypicalFloorGroup g = new TypicalFloorGroup();
                g.Name = srcGroup.Name;
                g.ColorHex = srcGroup.ColorHex;
                g.IsSingleFloorOnly = srcGroup.IsSingleFloorOnly;
                g.IsDuplexModule = srcGroup.IsDuplexModule;
                g.SourceLevelName = srcGroup.SourceLevelName;
                g.SourceLevelNameLower = srcGroup.SourceLevelNameLower;
                g.SourceLevelNameUpper = srcGroup.SourceLevelNameUpper;
                g.FromLevelName = srcGroup.FromLevelName;
                g.ToLevelName = srcGroup.ToLevelName;
                g.Order = srcGroup.Order;
                targetBuilding.TypicalGroups.Add(g);
            }

            SaveTypicalGroups();
            RefreshTowerLevels();
            string msg = string.Format("Copied {0} typical group(s) from '{1}' to '{2}'.", targetBuilding.TypicalGroups.Count, sourceBuilding.Name, targetBuilding.Name);
            StatusMessage = msg;
            TriggerToast(msg, false);
        }

        public void AddCustomPackage(string name, string prefix, ViewPlanKind kind, string schemeName)
        {
            if (string.IsNullOrWhiteSpace(name)) name = string.Format("Custom Package {0}", PackageSettings.Count + 1);
            if (string.IsNullOrWhiteSpace(prefix)) prefix = "C-";

            string icon = (kind == ViewPlanKind.AreaPlan) ? "📐" : (kind == ViewPlanKind.CeilingPlan ? "💡" : "🏢");
            PackageSetting pkg = new PackageSetting(
                ViewPackageType.Custom,
                name.Trim(),
                icon,
                prefix.Trim().ToUpperInvariant(),
                101,
                SheetLayoutMode.Quad4Views,
                96,
                "1/8\" = 1'-0\" (1:96)",
                kind,
                schemeName);
            pkg.IsCustomPackage = true;
            PackageSettings.Add(pkg);
            ComputePlannedSheets();
            string msg = string.Format("Added package '{0}' ({1}).", pkg.DisplayName, kind);
            StatusMessage = msg;
            TriggerToast(msg, false);
        }

        public void RemovePackage(PackageSetting pkg)
        {
            if (pkg != null && PackageSettings.Contains(pkg))
            {
                PackageSettings.Remove(pkg);
                ComputePlannedSheets();
                string msg = string.Format("Removed package '{0}'.", pkg.DisplayName);
                StatusMessage = msg;
                TriggerToast(msg, false);
            }
        }

        public string GetNextLevelAbove(string levelName)
        {
            if (string.IsNullOrEmpty(levelName)) return null;
            List<Level> allLevels = _duplicator.GetAllLevels().OrderBy(l => l.Elevation).ToList();
            for (int i = 0; i < allLevels.Count; i++)
            {
                if (string.Equals(allLevels[i].Name, levelName, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < allLevels.Count)
                    {
                        return allLevels[i + 1].Name;
                    }
                    break;
                }
            }
            return null;
        }

        public void RemoveBuilding(BuildingDefinition bldg)
        {
            if (bldg != null && Buildings.Contains(bldg))
            {
                if (Buildings.Count <= 1)
                {
                    TriggerToast("Project must contain at least one building.", true);
                    return;
                }

                MessageBoxResult res = MessageBox.Show(string.Format("Are you sure you want to delete '{0}' and its typical floor groups?", bldg.Name),
                    "Confirm Delete Building", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                {
                    Buildings.Remove(bldg);
                    SelectedBuilding = Buildings[0];
                    SaveTypicalGroups();
                    StatusMessage = string.Format("Deleted '{0}'.", bldg.Name);
                    TriggerToast(string.Format("Deleted '{0}'.", bldg.Name), false);
                }
            }
        }

        public void AddTypicalGroup()
        {
            if (AvailableLevels.Count == 0 || SelectedBuilding == null) return;

            string[] defaultColors = new string[] { "#3B82F6", "#14B8A6", "#F59E0B", "#8B5CF6", "#F43F5E", "#22C55E", "#64748B", "#F97316" };
            int colorIdx = SelectedBuilding.TypicalGroups.Count % defaultColors.Length;

            // Find first unassigned level for this building
            HashSet<string> assigned = GetAssignedLevelsInBuilding(SelectedBuilding, null);
            string firstAvailableLvl = AvailableLevels.FirstOrDefault(l => !assigned.Contains(l));
            if (string.IsNullOrEmpty(firstAvailableLvl))
            {
                firstAvailableLvl = AvailableLevels[0];
            }

            TypicalFloorGroup newGroup = new TypicalFloorGroup();
            newGroup.Name = string.Format("Typical Floor {0}", SelectedBuilding.TypicalGroups.Count + 1);
            newGroup.ColorHex = defaultColors[colorIdx];
            newGroup.SourceLevelName = firstAvailableLvl;
            newGroup.SourceLevelNameLower = firstAvailableLvl;
            newGroup.SourceLevelNameUpper = firstAvailableLvl;
            newGroup.FromLevelName = firstAvailableLvl;
            newGroup.ToLevelName = firstAvailableLvl;
            newGroup.Order = SelectedBuilding.TypicalGroups.Count + 1;

            SelectedBuilding.TypicalGroups.Add(newGroup);
            RefreshTowerLevels();
            StatusMessage = "Added new Typical Floor group.";
            TriggerToast("Added new Typical Floor group.", false);
        }

        public void RemoveTypicalGroup(TypicalFloorGroup group)
        {
            if (group != null && SelectedBuilding != null && SelectedBuilding.TypicalGroups.Contains(group))
            {
                SelectedBuilding.TypicalGroups.Remove(group);
                RefreshTowerLevels();
                StatusMessage = string.Format("Removed group '{0}'.", group.Name);
                TriggerToast(string.Format("Removed group '{0}'.", group.Name), false);
            }
        }

        public HashSet<string> GetAssignedLevelsInBuilding(BuildingDefinition bldg, TypicalFloorGroup excludeGroup)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (bldg == null || bldg.TypicalGroups == null) return set;

            foreach (TypicalFloorGroup g in bldg.TypicalGroups)
            {
                if (excludeGroup != null && g == excludeGroup) continue;

                if (g.IsSingleLevel)
                {
                    if (!string.IsNullOrEmpty(g.SourceLevelName)) set.Add(g.SourceLevelName);
                }
                else
                {
                    List<string> inRange = _duplicator.GetLevelsInRange(g.FromLevelName, g.ToLevelName);
                    foreach (string lvl in inRange) set.Add(lvl);
                }
            }
            return set;
        }

        public List<LevelPickerItem> GetLevelPickerItemsForGroup(TypicalFloorGroup currentGroup)
        {
            List<LevelPickerItem> list = new List<LevelPickerItem>();
            if (SelectedBuilding == null) return list;

            Dictionary<string, string> occupiedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (TypicalFloorGroup g in SelectedBuilding.TypicalGroups)
            {
                if (g == currentGroup) continue;

                if (g.IsSingleLevel)
                {
                    if (!string.IsNullOrEmpty(g.SourceLevelName))
                        occupiedMap[g.SourceLevelName] = g.Name;
                }
                else
                {
                    List<string> range = _duplicator.GetLevelsInRange(g.FromLevelName, g.ToLevelName);
                    foreach (string lvl in range)
                        occupiedMap[lvl] = g.Name;
                }
            }

            foreach (string lvl in AvailableLevels)
            {
                bool isOccupied = occupiedMap.ContainsKey(lvl);
                list.Add(new LevelPickerItem
                {
                    LevelName = lvl,
                    IsAvailable = !isOccupied,
                    OccupiedByGroupName = isOccupied ? occupiedMap[lvl] : string.Empty
                });
            }

            return list;
        }

        public bool ValidateAndApplyRange(TypicalFloorGroup group, string fromLvl, string toLvl)
        {
            if (SelectedBuilding == null || group == null) return false;

            HashSet<string> assignedOthers = GetAssignedLevelsInBuilding(SelectedBuilding, group);
            List<string> candidateRange = _duplicator.GetLevelsInRange(fromLvl, toLvl);

            // Check overlap
            List<string> colliding = candidateRange.Where(l => assignedOthers.Contains(l)).ToList();
            if (colliding.Count > 0)
            {
                TriggerToast(string.Format("Overlap conflict: Level(s) [{0}] are already assigned in {1}.", string.Join(", ", colliding.ToArray()), SelectedBuilding.Name), true);
                return false;
            }

            group.FromLevelName = fromLvl;
            group.ToLevelName = toLvl;
            RefreshTowerLevels();
            return true;
        }

        public bool ShiftGroupRange(TypicalFloorGroup group, int delta)
        {
            if (group == null || SelectedBuilding == null || delta == 0) return false;

            List<Level> sortedLevels = _duplicator.GetAllLevels().OrderBy(l => l.Elevation).ToList();
            if (sortedLevels.Count == 0) return false;

            int fromIdx = -1;
            int toIdx = -1;

            string curFrom = group.FromLevelName;
            string curTo = group.ToLevelName;
            if (group.IsSingleLevel)
            {
                curFrom = group.SourceLevelName;
                curTo = group.SourceLevelName;
            }

            for (int i = 0; i < sortedLevels.Count; i++)
            {
                if (string.Equals(sortedLevels[i].Name, curFrom, StringComparison.OrdinalIgnoreCase)) fromIdx = i;
                if (string.Equals(sortedLevels[i].Name, curTo, StringComparison.OrdinalIgnoreCase)) toIdx = i;
            }

            if (fromIdx < 0 || toIdx < 0) return false;

            int newFromIdx = fromIdx + delta;
            int newToIdx = toIdx + delta;

            if (newFromIdx < 0)
            {
                TriggerToast("Cannot shift down: Already at the lowest level.", true);
                return false;
            }

            if (newToIdx >= sortedLevels.Count)
            {
                TriggerToast("Cannot shift up: Already at the top level.", true);
                return false;
            }

            string newFrom = sortedLevels[newFromIdx].Name;
            string newTo = sortedLevels[newToIdx].Name;

            HashSet<string> assignedOthers = GetAssignedLevelsInBuilding(SelectedBuilding, group);
            List<string> candidateRange = _duplicator.GetLevelsInRange(newFrom, newTo);
            List<string> colliding = candidateRange.Where(l => assignedOthers.Contains(l)).ToList();

            if (colliding.Count > 0)
            {
                TriggerToast(string.Format("Collision: Level(s) [{0}] are occupied in {1}.", string.Join(", ", colliding.ToArray()), SelectedBuilding.Name), true);
                return false;
            }

            group.FromLevelName = newFrom;
            group.ToLevelName = newTo;

            if (group.IsSingleLevel)
            {
                group.SourceLevelName = newFrom;
            }
            else if (group.IsDuplexModule)
            {
                int srcLowerIdx = -1;
                for (int i = 0; i < sortedLevels.Count; i++)
                {
                    if (string.Equals(sortedLevels[i].Name, group.SourceLevelNameLower, StringComparison.OrdinalIgnoreCase)) srcLowerIdx = i;
                }
                int newLowerIdx = (srcLowerIdx >= 0) ? srcLowerIdx + delta : newFromIdx;
                if (newLowerIdx >= newFromIdx && newLowerIdx <= newToIdx)
                {
                    group.SourceLevelNameLower = sortedLevels[newLowerIdx].Name;
                }
                else
                {
                    group.SourceLevelNameLower = newFrom;
                }
                string autoUpper = GetNextLevelAbove(group.SourceLevelNameLower);
                if (!string.IsNullOrEmpty(autoUpper)) group.SourceLevelNameUpper = autoUpper;
            }
            else
            {
                int srcIdx = -1;
                for (int i = 0; i < sortedLevels.Count; i++)
                {
                    if (string.Equals(sortedLevels[i].Name, group.SourceLevelName, StringComparison.OrdinalIgnoreCase)) srcIdx = i;
                }
                int newSrcIdx = (srcIdx >= 0) ? srcIdx + delta : newFromIdx;
                if (newSrcIdx >= newFromIdx && newSrcIdx <= newToIdx)
                {
                    group.SourceLevelName = sortedLevels[newSrcIdx].Name;
                }
                else
                {
                    group.SourceLevelName = newFrom;
                }
            }

            SaveTypicalGroups();
            RefreshTowerLevels();
            string msg = string.Format("Shifted '{0}' to {1} → {2}.", group.Name, newFrom, newTo);
            StatusMessage = msg;
            TriggerToast(msg, false);
            return true;
        }

        public bool ExpandOrContractGroup(TypicalFloorGroup group, int delta)
        {
            if (group == null || SelectedBuilding == null || delta == 0 || group.IsSingleLevel) return false;

            List<Level> sortedLevels = _duplicator.GetAllLevels().OrderBy(l => l.Elevation).ToList();
            if (sortedLevels.Count == 0) return false;

            int fromIdx = -1;
            int toIdx = -1;

            for (int i = 0; i < sortedLevels.Count; i++)
            {
                if (string.Equals(sortedLevels[i].Name, group.FromLevelName, StringComparison.OrdinalIgnoreCase)) fromIdx = i;
                if (string.Equals(sortedLevels[i].Name, group.ToLevelName, StringComparison.OrdinalIgnoreCase)) toIdx = i;
            }

            if (fromIdx < 0 || toIdx < 0) return false;

            int newToIdx = toIdx + delta;

            if (delta < 0)
            {
                if (newToIdx <= fromIdx)
                {
                    TriggerToast("Cannot shrink further: A typical range must contain at least 2 levels.", true);
                    return false;
                }
                string newTo = sortedLevels[newToIdx].Name;
                group.ToLevelName = newTo;
                SaveTypicalGroups();
                RefreshTowerLevels();
                string msg = string.Format("Contracted '{0}' to {1} → {2}.", group.Name, group.FromLevelName, newTo);
                StatusMessage = msg;
                TriggerToast(msg, false);
                return true;
            }
            else
            {
                if (newToIdx >= sortedLevels.Count)
                {
                    TriggerToast("Cannot expand: Top level reached.", true);
                    return false;
                }

                string newTopLvl = sortedLevels[newToIdx].Name;
                HashSet<string> assignedOthers = GetAssignedLevelsInBuilding(SelectedBuilding, group);
                if (assignedOthers.Contains(newTopLvl))
                {
                    TriggerToast(string.Format("Cannot expand: '{0}' is already occupied.", newTopLvl), true);
                    return false;
                }

                group.ToLevelName = newTopLvl;
                SaveTypicalGroups();
                RefreshTowerLevels();
                string msg = string.Format("Expanded '{0}' to {1} → {2}.", group.Name, group.FromLevelName, newTopLvl);
                StatusMessage = msg;
                TriggerToast(msg, false);
                return true;
            }
        }

        public List<string> GetUnassignedGaps()
        {
            List<string> gaps = new List<string>();
            if (SelectedBuilding == null) return gaps;

            HashSet<string> assigned = GetAssignedLevelsInBuilding(SelectedBuilding, null);
            foreach (string lvl in AvailableLevels)
            {
                if (!assigned.Contains(lvl))
                {
                    gaps.Add(lvl);
                }
            }
            return gaps;
        }

        public void RefreshTowerLevels()
        {
            TowerLevels.Clear();
            List<Level> allLevels = _duplicator.GetAllLevels().OrderByDescending(l => l.Elevation).ToList();

            foreach (Level lvl in allLevels)
            {
                LevelTowerItem item = new LevelTowerItem();
                item.LevelName = lvl.Name;
                item.Elevation = lvl.Elevation;
                item.ElevationDisplay = LevelCreatorService.FormatLength(_doc, lvl.Elevation);

                TypicalFloorGroup assignedGroup = null;
                bool isDuplexUpper = false;
                bool isDuplexLower = false;

                if (SelectedBuilding != null)
                {
                    foreach (TypicalFloorGroup g in SelectedBuilding.TypicalGroups)
                    {
                        if (g.IsSingleLevel)
                        {
                            if (string.Equals(g.SourceLevelName, lvl.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                assignedGroup = g;
                                break;
                            }
                        }
                        else
                        {
                            List<string> range = _duplicator.GetLevelsInRange(g.FromLevelName, g.ToLevelName);
                            int lvlIdx = range.FindIndex(r => string.Equals(r, lvl.Name, StringComparison.OrdinalIgnoreCase));
                            if (lvlIdx >= 0)
                            {
                                assignedGroup = g;
                                if (g.IsDuplexModule)
                                {
                                    if (lvlIdx % 2 == 0) isDuplexLower = true;
                                    else isDuplexUpper = true;
                                }
                                break;
                            }
                        }
                    }
                }

                if (assignedGroup != null)
                {
                    string label = assignedGroup.Name;
                    if (isDuplexLower) label += " (Lower)";
                    else if (isDuplexUpper) label += " (Upper)";

                    item.AssignedGroupName = label;
                    item.ColorHex = assignedGroup.ColorHex ?? "#3B82F6";
                    item.IsSingleFloor = assignedGroup.IsSingleLevel;
                }
                else
                {
                    item.AssignedGroupName = string.Empty;
                    item.ColorHex = "#CBD5E1"; // Subtle gray unassigned
                    item.IsSingleFloor = false;
                }

                TowerLevels.Add(item);
            }
        }

        public void TriggerToast(string message, bool isError)
        {
            if (OnToastNotification != null)
            {
                OnToastNotification(message, isError);
            }
        }

        public void ComputePlannedSheets()
        {
            if (PlannedSheets == null) PlannedSheets = new ObservableCollection<PlannedSheet>();
            PlannedSheets.Clear();
            if (Buildings == null || Buildings.Count == 0 || PackageSettings == null) return;

            List<BuildingDefinition> activeBldgs = Buildings.ToList();
            bool isMultiBuilding = activeBldgs.Count > 1;

            foreach (PackageSetting pkg in PackageSettings)
            {
                if (!pkg.IsEnabled) continue;
                if (pkg.PackageType == ViewPackageType.MasterOverall && !isMultiBuilding) continue;

                int maxPerSheet = (int)pkg.LayoutMode; // 1, 2, 3, 4, 6, 8
                int sheetNumberCounter = pkg.StartNumber;

                // Update Scale Recommendation for this package
                double refWidth = activeBldgs[0].FootprintWidthFt > 0 ? activeBldgs[0].FootprintWidthFt : 150.0;
                double refDepth = activeBldgs[0].FootprintDepthFt > 0 ? activeBldgs[0].FootprintDepthFt : 100.0;
                ScaleOption rec = _scaleAdvisor.RecommendScale(refWidth, refDepth, SelectedTitleblock, pkg.LayoutMode);
                pkg.RecommendedScaleDisplay = rec.DisplayName;

                // ── CASE A: Master Overall Campus Package ──
                if (pkg.PackageType == ViewPackageType.MasterOverall)
                {
                    List<PlannedViewport> queuedMaster = new List<PlannedViewport>();
                    HashSet<string> seenLevels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (BuildingDefinition b in activeBldgs)
                    {
                        foreach (TypicalFloorGroup g in b.TypicalGroups)
                        {
                            string srcLvl = g.IsDuplexModule ? g.SourceLevelNameLower : g.SourceLevelName;
                            if (string.IsNullOrEmpty(srcLvl) || seenLevels.Contains(srcLvl)) continue;
                            seenLevels.Add(srcLvl);

                            string rangeLabel = _viewGenService.GetGroupRangeLabel(g);
                            string kindSuffix = (pkg.ViewKind == ViewPlanKind.AreaPlan) ? "AREA PLAN" : "FLOOR PLAN";
                            queuedMaster.Add(new PlannedViewport
                            {
                                LevelName = srcLvl,
                                LevelRangeLabel = rangeLabel,
                                BuildingName = "Master",
                                ScopeBoxName = Config.MasterScopeBoxName,
                                ViewName = string.Format("FL. {0} - MASTER OVERALL {1}", rangeLabel, kindSuffix),
                                FormattedTitleOnSheet = string.Format("MASTER - {0} OVERALL {1}", rangeLabel.ToUpperInvariant(), kindSuffix),
                                PackageType = pkg.PackageType,
                                ViewKind = pkg.ViewKind,
                                AreaSchemeName = pkg.SelectedAreaSchemeName
                            });
                        }
                    }

                    for (int i = 0; i < queuedMaster.Count; i += maxPerSheet)
                    {
                        List<PlannedViewport> chunk = queuedMaster.Skip(i).Take(maxPerSheet).ToList();
                        for (int k = 0; k < chunk.Count; k++) chunk[k].GridIndex = k;

                        string sNum = string.Format("{0}{1}", pkg.SheetPrefix, sheetNumberCounter++);
                        string sName = chunk.Count == 1 ? string.Format("Master Overall - {0}", chunk[0].LevelName) : "Master Overall Campus Plans";

                        PlannedSheets.Add(new PlannedSheet
                        {
                            SheetNumber = sNum,
                            SheetName = sName,
                            BuildingName = "Master",
                            ScopeBoxName = Config.MasterScopeBoxName,
                            PackageType = pkg.PackageType,
                            LayoutMode = pkg.LayoutMode,
                            ScaleValue = pkg.ScaleValue,
                            ScaleDisplay = pkg.ScaleDisplay,
                            HasSummaryTable = pkg.IncludeSummaryTableOnSheet,
                            Viewports = chunk
                        });
                    }
                    continue;
                }

                // ── CASE B: Building-Specific Packages (Gross, Deductions, Life Safety, RCP, Floor Plans) ──
                foreach (BuildingDefinition bldg in activeBldgs)
                {
                    List<PlannedViewport> queuedViewports = new List<PlannedViewport>();

                    foreach (TypicalFloorGroup group in bldg.TypicalGroups)
                    {
                        string srcLevel = group.IsDuplexModule ? group.SourceLevelNameLower : group.SourceLevelName;
                        if (string.IsNullOrEmpty(srcLevel)) continue;

                        string rangeLabel = _viewGenService.GetGroupRangeLabel(group);
                        string bldgTag = bldg.Name.ToUpperInvariant();
                        string vName = "";
                        string titleOnSheet = "";

                        switch (pkg.PackageType)
                        {
                            case ViewPackageType.GrossArea:
                                vName = string.Format("FL. {0} - GROSS AREA PLAN ({1})", rangeLabel, bldgTag);
                                titleOnSheet = string.Format("{0} - {1} GROSS AREA PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                break;
                            case ViewPackageType.Deductions:
                                vName = string.Format("FL. {0} - DEDUCTIONS PLAN ({1})", rangeLabel, bldgTag);
                                titleOnSheet = string.Format("{0} - {1} DEDUCTIONS PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                break;
                            case ViewPackageType.Architectural:
                                if (pkg.ViewKind == ViewPlanKind.AreaPlan)
                                {
                                    string schName = !string.IsNullOrEmpty(pkg.SelectedAreaSchemeName) ? pkg.SelectedAreaSchemeName : "Area";
                                    vName = string.Format("FL. {0} - {1} PLAN ({2})", rangeLabel, schName.ToUpperInvariant(), bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} {2} PLAN", bldgTag, rangeLabel.ToUpperInvariant(), schName.ToUpperInvariant());
                                }
                                else
                                {
                                    vName = string.Format("FL. {0} - ARCHITECTURAL PLAN ({1})", rangeLabel, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} FLOOR PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                }
                                break;
                            case ViewPackageType.CeilingPlanRCP:
                                vName = string.Format("FL. {0} - CEILING PLAN RCP ({1})", rangeLabel, bldgTag);
                                titleOnSheet = string.Format("{0} - {1} REFLECTED CEILING PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                break;
                                case ViewPackageType.EgressLifeSafety:
                                if (pkg.ViewKind == ViewPlanKind.AreaPlan)
                                {
                                    vName = string.Format("FL. {0} - LIFE SAFETY AREA PLAN ({1})", rangeLabel, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} LIFE SAFETY AREA PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                }
                                else
                                {
                                    vName = string.Format("FL. {0} - LIFE SAFETY PLAN ({1})", rangeLabel, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} LIFE SAFETY PLAN", bldgTag, rangeLabel.ToUpperInvariant());
                                }
                                break;
                            case ViewPackageType.Custom:
                            default:
                                string pkgTitle = !string.IsNullOrEmpty(pkg.DisplayName) ? pkg.DisplayName.ToUpperInvariant() : "CUSTOM";
                                if (pkg.ViewKind == ViewPlanKind.AreaPlan)
                                {
                                    string sch = !string.IsNullOrEmpty(pkg.SelectedAreaSchemeName) ? pkg.SelectedAreaSchemeName.ToUpperInvariant() : "AREA";
                                    vName = string.Format("FL. {0} - {1} [{2}] ({3})", rangeLabel, pkgTitle, sch, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} {2}", bldgTag, rangeLabel.ToUpperInvariant(), pkgTitle);
                                }
                                else if (pkg.ViewKind == ViewPlanKind.CeilingPlan)
                                {
                                    vName = string.Format("FL. {0} - {1} RCP ({2})", rangeLabel, pkgTitle, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} {2}", bldgTag, rangeLabel.ToUpperInvariant(), pkgTitle);
                                }
                                else
                                {
                                    vName = string.Format("FL. {0} - {1} ({2})", rangeLabel, pkgTitle, bldgTag);
                                    titleOnSheet = string.Format("{0} - {1} {2}", bldgTag, rangeLabel.ToUpperInvariant(), pkgTitle);
                                }
                                break;
                        }

                        queuedViewports.Add(new PlannedViewport
                        {
                            LevelName = srcLevel,
                            LevelRangeLabel = rangeLabel,
                            BuildingName = bldg.Name,
                            ScopeBoxName = bldg.ScopeBoxName,
                            ViewName = vName,
                            FormattedTitleOnSheet = titleOnSheet,
                            PackageType = pkg.PackageType,
                            ViewKind = pkg.ViewKind,
                            AreaSchemeName = pkg.SelectedAreaSchemeName
                        });
                    }

                    for (int i = 0; i < queuedViewports.Count; i += maxPerSheet)
                    {
                        List<PlannedViewport> chunk = queuedViewports.Skip(i).Take(maxPerSheet).ToList();
                        for (int k = 0; k < chunk.Count; k++) chunk[k].GridIndex = k;

                        string sNum = string.Format("{0}{1}", pkg.SheetPrefix, sheetNumberCounter++);
                        string sName = chunk.Count == 1 ?
                            string.Format("{0} - {1} ({2})", bldg.Name, pkg.DisplayName, chunk[0].LevelName) :
                            string.Format("{0} - {1} (Typical Floors)", bldg.Name, pkg.DisplayName);

                        PlannedSheets.Add(new PlannedSheet
                        {
                            SheetNumber = sNum,
                            SheetName = sName,
                            BuildingName = bldg.Name,
                            ScopeBoxName = bldg.ScopeBoxName,
                            PackageType = pkg.PackageType,
                            LayoutMode = pkg.LayoutMode,
                            ScaleValue = pkg.ScaleValue,
                            ScaleDisplay = pkg.ScaleDisplay,
                            HasSummaryTable = pkg.IncludeSummaryTableOnSheet,
                            Viewports = chunk
                        });
                    }
                }
            }
        }

        public void ExecuteComposeSheets()
        {
            try
            {
                ComputePlannedSheets();
                if (PlannedSheets.Count == 0)
                {
                    TriggerToast("No sheets planned. Please enable at least one package and configure typical floors.", true);
                    return;
                }

                ElementId tbId = SelectedTitleblock != null ? SelectedTitleblock.FamilySymbolId : ElementId.InvalidElementId;

                // 1. Generate all views with scale, templates, and scope boxes
                Dictionary<string, ElementId> createdViews = _viewGenService.GeneratePackageViews(
                    Buildings.ToList(),
                    Config,
                    PackageSettings.ToList(),
                    SelectedViewScale,
                    OnlyTypicalRanges);

                // 2. Compose sheets and place viewports with Titleblock bounds
                int placedCount = _sheetPlaceService.ComposePlannedSheets(
                    PlannedSheets.ToList(),
                    tbId,
                    RepositionIfExists,
                    createdViews,
                    SelectedTitleblock);

                // Refresh project sheets
                AvailableSheets.Clear();
                foreach (SheetItem sh in _sheetPlaceService.GetExistingSheets()) AvailableSheets.Add(sh);

                string msg = string.Format("Successfully generated {0} view(s) and placed {1} viewport(s) across {2} sheet(s) in Revit.",
                    createdViews.Count, placedCount, PlannedSheets.Count);
                StatusMessage = msg;
                TriggerToast(msg, false);
            }
            catch (Exception ex)
            {
                StatusMessage = "Composition Error: " + ex.Message;
                TriggerToast("Error: " + ex.Message, true);
            }
        }

        public void GenerateProjectViews(bool createArch, bool createGross, bool createDed, bool typicalMasterOnly)
        {
            try
            {
                if (!createArch && !createGross && !createDed)
                {
                    TriggerToast("Please select at least one view type (Architectural, Gross, or Deductions).", true);
                    return;
                }

                LastGeneratedViews = _viewGenService.GenerateMasterAndDependentViews(
                    Buildings.ToList(),
                    Config,
                    createArch,
                    createGross,
                    createDed,
                    typicalMasterOnly);

                int masterCount = LastGeneratedViews.Count;
                int depCount = LastGeneratedViews.Sum(r => r.DependentViews.Count);

                string msg = string.Format("Created {0} Master View(s) and {1} Dependent View(s) in Project Browser.", masterCount, depCount);
                StatusMessage = msg;
                TriggerToast(msg, false);
            }
            catch (Exception ex)
            {
                StatusMessage = "View Generation Error: " + ex.Message;
                TriggerToast("Error creating views: " + ex.Message, true);
            }
        }

        public void PlaceViewsOnSelectedSheet()
        {
            try
            {
                if (SelectedSheet == null)
                {
                    TriggerToast("Please select a target Sheet first.", true);
                    return;
                }

                if (LastGeneratedViews == null || LastGeneratedViews.Count == 0)
                {
                    TriggerToast("No recently generated views found. Click 'Create Master & Dependent Views' first.", true);
                    return;
                }

                List<ElementId> viewIdsToPlace = new List<ElementId>();
                foreach (GeneratedViewResult r in LastGeneratedViews)
                {
                    if (r.MasterView != null) viewIdsToPlace.Add(r.MasterView.Id);
                    foreach (View dep in r.DependentViews)
                    {
                        if (dep != null) viewIdsToPlace.Add(dep.Id);
                    }
                }

                int placed = _sheetPlaceService.PlaceViewsOnSheet(SelectedSheet.SheetId, viewIdsToPlace);
                string msg = string.Format("Successfully placed {0} view(s) onto Sheet {1}.", placed, SelectedSheet.DisplayName);
                StatusMessage = msg;
                TriggerToast(msg, false);
            }
            catch (Exception ex)
            {
                StatusMessage = "Sheet Placement Error: " + ex.Message;
                TriggerToast("Error placing views on sheet: " + ex.Message, true);
            }
        }

        public void SaveTypicalGroups()
        {
            bool ok = _storageService.SaveBuildings(_doc, Buildings.ToList());
            if (ok)
            {
                StatusMessage = "All building typical floor definitions saved to Revit model.";
                TriggerToast("Typical floor definitions saved to model.", false);
            }
            else
            {
                StatusMessage = "Error saving definitions to Revit model.";
                TriggerToast("Error saving definitions to Revit model.", true);
            }
        }

        public string GetSourceLevelSummary(string levelName)
        {
            return _duplicator.GetLevelAreaSummary(levelName, Config.GrossAreaSchemeName, Config.DeductionAreaSchemeName);
        }

        public void RevertPropagatedAreas()
        {
            try
            {
                List<TypicalFloorGroup> allGroups = new List<TypicalFloorGroup>();
                foreach (BuildingDefinition b in Buildings)
                {
                    allGroups.AddRange(b.TypicalGroups);
                }

                if (allGroups.Count == 0)
                {
                    StatusMessage = "No typical floor groups defined.";
                    MessageBox.Show("Please define typical floor groups in Step 1 before clearing.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBoxResult confirm = MessageBox.Show(
                    "Are you sure you want to revert and clear all propagated areas and boundary lines across target levels for all buildings?\n\n• Source modeled levels will remain 100% untouched.\n• Revit view plans will NOT be deleted or modified.",
                    "Confirm Clear Propagated Areas",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirm != MessageBoxResult.Yes) return;

                StatusMessage = "Clearing propagated areas from target levels...";
                string msg = _duplicator.ClearPropagatedAreas(
                    allGroups,
                    Config,
                    PropagateGrossArea,
                    PropagateDeductionsArea
                );

                StatusMessage = msg;
                MessageBox.Show(msg, "BauTools — Clear Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                // Auto-refresh calculation table
                CalculateTable();
            }
            catch (Exception ex)
            {
                StatusMessage = "Clear Error: " + ex.Message;
                MessageBox.Show("Error clearing areas: " + ex.Message, "Clear Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void PropagateAreasFromTypicalGroups()
        {
            try
            {
                List<TypicalFloorGroup> allGroups = new List<TypicalFloorGroup>();
                foreach (BuildingDefinition b in Buildings)
                {
                    allGroups.AddRange(b.TypicalGroups);
                }

                if (allGroups.Count == 0)
                {
                    StatusMessage = "No typical floor groups defined to propagate.";
                    MessageBox.Show("Please add at least one Typical Floor group in Step 1.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Save groups first
                _storageService.SaveBuildings(_doc, Buildings.ToList());

                StatusMessage = "Propagating typical floor areas across model...";
                string msg = _duplicator.PropagateMultipleGroups(
                    allGroups,
                    Config,
                    PropagateGrossArea,
                    PropagateDeductionsArea
                );

                StatusMessage = msg;
                MessageBox.Show(msg, "BauTools — Propagation Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                // Auto-refresh calculations
                CalculateTable();
            }
            catch (Exception ex)
            {
                StatusMessage = "Propagation Error: " + ex.Message;
                MessageBox.Show("Error: " + ex.Message, "Propagation Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CalculateTable()
        {
            try
            {
                List<TypicalFloorGroup> allGroups = new List<TypicalFloorGroup>();
                foreach (BuildingDefinition b in Buildings)
                {
                    allGroups.AddRange(b.TypicalGroups);
                }

                List<AreaDataModel> rawAreas = _extractor.ExtractAreas(Config);
                ProjectResult = _calculator.ComputeProjectZoning(rawAreas, Config, allGroups);

                // Populate / sync building checkboxes
                SyncBuildingItems(ProjectResult.BuildingTables);

                // Filter displayed tables
                UpdateDisplayedTables();

                // Live Compliance Evaluation
                EvaluateCompliance();

                StatusMessage = string.Format("ZFA calculated: {0:N0} SF total across {1} building(s).", ProjectResult.TotalProjectZoningFloorArea, ProjectResult.BuildingTables.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = "Calculation Error: " + ex.Message;
                MessageBox.Show("Error calculating ZFA: " + ex.Message, "Calculation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void EvaluateCompliance()
        {
            if (ComplianceReport == null) ComplianceReport = new ZoningComplianceReport();
            if (LotData == null) LotData = new ZoningLotData();

            double allowable = LotData.TotalAllowableZfa;
            double proposed = ProjectResult != null ? ProjectResult.TotalProjectZoningFloorArea : 0.0;
            double remaining = allowable - proposed;
            double pct = allowable > 0 ? (proposed / allowable) * 100.0 : 0.0;
            bool isOver = proposed > allowable && allowable > 0;

            ComplianceReport.AllowableZfa = allowable;
            ComplianceReport.ProposedZfa = proposed;
            ComplianceReport.RemainingZfa = remaining;
            ComplianceReport.UtilizationPercent = pct;
            ComplianceReport.IsOverbuilt = isOver;

            if (allowable <= 0)
            {
                ComplianceReport.StatusSummary = "Please enter Lot Area and Allowable FAR to evaluate compliance.";
                ComplianceReport.ColorHex = "#64748B"; // Neutral Gray
            }
            else if (isOver)
            {
                ComplianceReport.StatusSummary = string.Format("⚠️ OVERBUILT: Exceeds allowable ZFA by {0:N0} SF ({1:N1}% of Cap)", Math.Abs(remaining), pct);
                ComplianceReport.ColorHex = "#EF4444"; // Red
            }
            else if (pct >= 95.0)
            {
                ComplianceReport.StatusSummary = string.Format("🟢 OPTIMAL: {0:N1}% Consumed ({1:N0} SF Unused Balance)", pct, remaining);
                ComplianceReport.ColorHex = "#10B981"; // Emerald Green
            }
            else
            {
                ComplianceReport.StatusSummary = string.Format("🔵 COMPLIANT: {0:N1}% Consumed ({1:N0} SF Unused Air Rights)", pct, remaining);
                ComplianceReport.ColorHex = "#3B82F6"; // Blue
            }
        }

        public void ImportZoningExcel(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    TriggerToast("Invalid file path.", true);
                    return;
                }

                ZoningLotData imported = _excelBridgeService.ImportZoningFromExcel(filePath);
                if (imported != null)
                {
                    LotData = imported;
                    EvaluateCompliance();
                    string msg = string.Format("Loaded Lot: {0:N0} SF, District: {1}, Total FAR: {2:N2}.",
                        imported.LotAreaSqFt, imported.ZoningDistrict, imported.TotalAllowableFar);
                    StatusMessage = msg;
                    TriggerToast("Excel Lot Data Imported Successfully!", false);
                }
                else
                {
                    TriggerToast("Could not parse Excel file. Check format.", true);
                }
            }
            catch (Exception ex)
            {
                TriggerToast("Import error: " + ex.Message, true);
            }
        }

        public void ExportZoningTemplateExcel(string filePath)
        {
            try
            {
                bool ok = _excelBridgeService.ExportZoningTemplate(filePath, LotData);
                if (ok)
                {
                    TriggerToast("Excel template saved successfully!", false);
                }
                else
                {
                    TriggerToast("Could not save Excel template.", true);
                }
            }
            catch (Exception ex)
            {
                TriggerToast("Export error: " + ex.Message, true);
            }
        }

        private void SyncBuildingItems(List<ZoningTableResult> tables)
        {
            List<string> newBldgNames = tables.Select(t => t.BuildingName).ToList();

            for (int i = BuildingItems.Count - 1; i >= 0; i--)
            {
                if (!newBldgNames.Contains(BuildingItems[i].Name))
                    BuildingItems.RemoveAt(i);
            }

            foreach (string bName in newBldgNames)
            {
                if (!BuildingItems.Any(item => item.Name == bName))
                {
                    BuildingFilterItem newItem = new BuildingFilterItem { Name = bName, IsSelected = true };
                    newItem.SelectionChanged = () => UpdateDisplayedTables();
                    BuildingItems.Add(newItem);
                }
            }
        }

        public void UpdateDisplayedTables()
        {
            DisplayedTables.Clear();
            if (ProjectResult == null) return;

            List<string> selectedNames = BuildingItems.Where(i => i.IsSelected).Select(i => i.Name).ToList();

            foreach (ZoningTableResult tbl in ProjectResult.BuildingTables)
            {
                if (selectedNames.Contains(tbl.BuildingName))
                {
                    DisplayedTables.Add(tbl);
                }
            }

            if (DisplayedTables.Count > 0)
            {
                SelectedTableResult = DisplayedTables[0];
            }
        }

        public void ExportToExcel()
        {
            try
            {
                if (ProjectResult == null || ProjectResult.BuildingTables.Count == 0)
                {
                    MessageBox.Show("Please calculate the ZFA matrix first in Step 3 before exporting.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Zoning Floor Area Matrix to Excel",
                    Filter = "Excel XML Spreadsheet (*.xls)|*.xls",
                    FileName = string.Format("BauTools_ZFA_Summary_{0:yyyyMMdd}.xls", DateTime.Now)
                };

                if (sfd.ShowDialog() == true)
                {
                    ExcelExporter.ExportProjectToExcelXml(ProjectResult, sfd.FileName);
                    StatusMessage = "Successfully exported to " + Path.GetFileName(sfd.FileName);
                    MessageBox.Show("Excel workbook generated successfully:\n" + sfd.FileName, "BauTools — Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Export Error: " + ex.Message;
                MessageBox.Show("Error generating Excel report: " + ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CreateDraftingViews()
        {
            try
            {
                if (ProjectResult == null || ProjectResult.BuildingTables.Count == 0)
                {
                    MessageBox.Show("Please calculate the ZFA matrix first in Step 3 before creating views.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                List<ZoningTableResult> tablesToDraw = DisplayedTables.Count > 0 ? DisplayedTables.ToList() : ProjectResult.BuildingTables;
                int count = 0;
                foreach (ZoningTableResult tbl in tablesToDraw)
                {
                    ViewDrafting vd = _sheetDrawer.CreateZoningTableDraftingView(tbl, "ZFA - " + tbl.BuildingName);
                    if (vd != null) count++;
                }

                StatusMessage = string.Format("Created {0} Revit drafting view(s) under Project Browser.", count);
                MessageBox.Show(string.Format("Successfully created {0} drafting view(s) with native vector tables in Revit.", count),
                    "BauTools — Drafting Views Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = "Drafting View Error: " + ex.Message;
                MessageBox.Show("Error creating drafting views: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void OnPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
    }
}

```

### `ZoningFloorArea\Views\BatchLevelGeneratorWindow.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;

namespace ZoningFloorArea.Views
{
    public class BatchLevelGeneratorWindow : Window
    {
        private readonly Document _doc;
        private readonly ObservableCollection<LevelCreationItem> _previewItems;

        // UI Controls
        private System.Windows.Controls.TextBox _txtFloorCount;
        private System.Windows.Controls.TextBox _txtTypicalHeight;
        private System.Windows.Controls.TextBox _txtBaseElevation;
        private System.Windows.Controls.TextBox _txtStartFloorNumber;

        private System.Windows.Controls.TextBox _txtCellarCount;
        private System.Windows.Controls.TextBox _txtCellarHeight;

        private System.Windows.Controls.CheckBox _chkIncludeRoof;
        private System.Windows.Controls.TextBox _txtRoofHeight;

        private System.Windows.Controls.CheckBox _chkIncludeBulkhead;
        private System.Windows.Controls.TextBox _txtBulkheadHeight;

        private System.Windows.Controls.CheckBox _chkTwoDigits;
        private System.Windows.Controls.CheckBox _chkCreateFloorPlans;
        private System.Windows.Controls.CheckBox _chkCreateCeilingPlans;

        private System.Windows.Controls.DataGrid _dataGrid;
        private System.Windows.Controls.TextBlock _statusSummary;

        // Color Palette matching BauTools
        private static readonly System.Windows.Media.Color COL_BG        = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#F1F5F9");
        private static readonly System.Windows.Media.Color COL_CARD      = System.Windows.Media.Colors.White;
        private static readonly System.Windows.Media.Color COL_DARK      = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0F172A");
        private static readonly System.Windows.Media.Color COL_ACCENT    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0071E3");
        private static readonly System.Windows.Media.Color COL_ACCENT2   = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0284C7");
        private static readonly System.Windows.Media.Color COL_MUTED     = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#64748B");
        private static readonly System.Windows.Media.Color COL_BORDER    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#CBD5E1");
        private static readonly System.Windows.Media.Color COL_HEADER_BG = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#1E293B");
        private static readonly System.Windows.Media.Color COL_SUCCESS   = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#16A34A");

        public BatchLevelGeneratorWindow(Document doc)
        {
            _doc = doc;
            _previewItems = new ObservableCollection<LevelCreationItem>();

            Title = "BauTools — Batch Level Generator (Multi-Story Buildings)";
            Height = 840;
            Width = 1100;
            MinHeight = 650;
            MinWidth = 850;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 13;

            BuildUI();
            RecalculateSchedule();
        }

        private void BuildUI()
        {
            SolidColorBrush cardBrush     = new SolidColorBrush(COL_CARD);
            SolidColorBrush darkBrush     = new SolidColorBrush(COL_DARK);
            SolidColorBrush accentBrush   = new SolidColorBrush(COL_ACCENT);
            SolidColorBrush accent2Brush  = new SolidColorBrush(COL_ACCENT2);
            SolidColorBrush mutedBrush    = new SolidColorBrush(COL_MUTED);
            SolidColorBrush borderBrush   = new SolidColorBrush(COL_BORDER);
            SolidColorBrush headerBgBrush = new SolidColorBrush(COL_HEADER_BG);

            System.Windows.Controls.Grid root = new System.Windows.Controls.Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Config Cards
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2: Preview Grid
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Footer

            // ══════════════════════════════════════════════════════════
            // 0. HEADER
            // ══════════════════════════════════════════════════════════
            Border headerBar = new Border
            {
                Background = headerBgBrush,
                Padding = new Thickness(24, 14, 24, 14)
            };

            System.Windows.Controls.Grid hGrid = new System.Windows.Controls.Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titlePanel = new StackPanel();
            StackPanel logoLine = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

            Border badge = new Border
            {
                Background = accent2Brush,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new TextBlock { Text = "BUILDING LEVELS", FontWeight = FontWeights.ExtraBold, FontSize = 12, Foreground = System.Windows.Media.Brushes.White };
            logoLine.Children.Add(badge);

            logoLine.Children.Add(new TextBlock
            {
                Text = "BauTools — Multi-Story Batch Level Generator",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            titlePanel.Children.Add(logoLine);

            titlePanel.Children.Add(new TextBlock
            {
                Text = "Batch generate typical floors, underground cellars, roof, and bulkhead levels with automatic elevations and view plans.",
                FontSize = 11,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#94A3B8")),
                Margin = new Thickness(0, 4, 0, 0)
            });
            hGrid.Children.Add(titlePanel);

            headerBar.Child = hGrid;
            System.Windows.Controls.Grid.SetRow(headerBar, 0);
            root.Children.Add(headerBar);

            // ══════════════════════════════════════════════════════════
            // 1. CONFIGURATION CARDS CONTAINER
            // ══════════════════════════════════════════════════════════
            Border configContainer = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 12, 16, 8),
                Padding = new Thickness(16, 12, 16, 12)
            };

            System.Windows.Controls.Grid cfgGrid = new System.Windows.Controls.Grid();
            cfgGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // Col 0: Typical Floors
            cfgGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }); // Col 1: Cellars
            cfgGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) }); // Col 2: Roof, Bulkhead & Views

            // ── Section 1: Typical Floors ──
            StackPanel sec1 = new StackPanel { Margin = new Thickness(0, 0, 14, 0) };
            sec1.Children.Add(new TextBlock { Text = "🏢 TYPICAL FLOORS (SUPERSTRUCTURE)", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 8) });

            // Floor count
            sec1.Children.Add(new TextBlock { Text = "Number of Typical Floors:", FontSize = 11, Foreground = mutedBrush });
            StackPanel countRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 6) };
            _txtFloorCount = new System.Windows.Controls.TextBox { Text = "15", Width = 60, Height = 28, TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, VerticalContentAlignment = VerticalAlignment.Center };
            _txtFloorCount.TextChanged += (s, e) => RecalculateSchedule();

            System.Windows.Controls.Button btnMinusFloors = new System.Windows.Controls.Button { Content = "−", Width = 28, Height = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(4, 0, 2, 0) };
            btnMinusFloors.Click += (s, e) => { int v; if (int.TryParse(_txtFloorCount.Text, out v) && v > 1) _txtFloorCount.Text = (v - 1).ToString(); };

            System.Windows.Controls.Button btnPlusFloors = new System.Windows.Controls.Button { Content = "+", Width = 28, Height = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(2, 0, 0, 0) };
            btnPlusFloors.Click += (s, e) => { int v; if (int.TryParse(_txtFloorCount.Text, out v)) _txtFloorCount.Text = (v + 1).ToString(); };

            countRow.Children.Add(_txtFloorCount);
            countRow.Children.Add(btnMinusFloors);
            countRow.Children.Add(btnPlusFloors);
            sec1.Children.Add(countRow);

            // Floor-to-floor height
            sec1.Children.Add(new TextBlock { Text = "Floor-to-Floor Height (Typical):", FontSize = 11, Foreground = mutedBrush });
            _txtTypicalHeight = new System.Windows.Controls.TextBox { Text = "10'-0\"", Height = 28, Margin = new Thickness(0, 2, 0, 6), VerticalContentAlignment = VerticalAlignment.Center };
            _txtTypicalHeight.TextChanged += (s, e) => RecalculateSchedule();
            sec1.Children.Add(_txtTypicalHeight);

            // Base elevation & Start Floor number
            System.Windows.Controls.Grid baseRow = new System.Windows.Controls.Grid { Margin = new Thickness(0, 0, 0, 0) };
            baseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            baseRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel baseCol1 = new StackPanel { Margin = new Thickness(0, 0, 4, 0) };
            baseCol1.Children.Add(new TextBlock { Text = "Base Elevation (Ground Floor):", FontSize = 11, Foreground = mutedBrush });
            _txtBaseElevation = new System.Windows.Controls.TextBox { Text = "0'-0\"", Height = 28, Margin = new Thickness(0, 2, 0, 0), VerticalContentAlignment = VerticalAlignment.Center };
            _txtBaseElevation.TextChanged += (s, e) => RecalculateSchedule();
            baseCol1.Children.Add(_txtBaseElevation);
            System.Windows.Controls.Grid.SetColumn(baseCol1, 0);
            baseRow.Children.Add(baseCol1);

            StackPanel baseCol2 = new StackPanel { Margin = new Thickness(4, 0, 0, 0) };
            baseCol2.Children.Add(new TextBlock { Text = "Start Floor #:", FontSize = 11, Foreground = mutedBrush });
            _txtStartFloorNumber = new System.Windows.Controls.TextBox { Text = "1", Height = 28, Margin = new Thickness(0, 2, 0, 0), VerticalContentAlignment = VerticalAlignment.Center };
            _txtStartFloorNumber.TextChanged += (s, e) => RecalculateSchedule();
            baseCol2.Children.Add(_txtStartFloorNumber);
            System.Windows.Controls.Grid.SetColumn(baseCol2, 1);
            baseRow.Children.Add(baseCol2);

            sec1.Children.Add(baseRow);
            System.Windows.Controls.Grid.SetColumn(sec1, 0);
            cfgGrid.Children.Add(sec1);

            // ── Section 2: Cellars ──
            StackPanel sec2 = new StackPanel { Margin = new Thickness(6, 0, 14, 0) };
            sec2.Children.Add(new TextBlock { Text = "🚗 CELLARS (SUB-GRADE LEVELS)", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 8) });

            sec2.Children.Add(new TextBlock { Text = "Number of Cellars:", FontSize = 11, Foreground = mutedBrush });
            StackPanel cellarCountRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 6) };
            _txtCellarCount = new System.Windows.Controls.TextBox { Text = "2", Width = 60, Height = 28, TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, VerticalContentAlignment = VerticalAlignment.Center };
            _txtCellarCount.TextChanged += (s, e) => RecalculateSchedule();

            System.Windows.Controls.Button btnMinusCellars = new System.Windows.Controls.Button { Content = "−", Width = 28, Height = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(4, 0, 2, 0) };
            btnMinusCellars.Click += (s, e) => { int v; if (int.TryParse(_txtCellarCount.Text, out v) && v > 0) _txtCellarCount.Text = (v - 1).ToString(); };

            System.Windows.Controls.Button btnPlusCellars = new System.Windows.Controls.Button { Content = "+", Width = 28, Height = 28, FontWeight = FontWeights.Bold, Margin = new Thickness(2, 0, 0, 0) };
            btnPlusCellars.Click += (s, e) => { int v; if (int.TryParse(_txtCellarCount.Text, out v)) _txtCellarCount.Text = (v + 1).ToString(); };

            cellarCountRow.Children.Add(_txtCellarCount);
            cellarCountRow.Children.Add(btnMinusCellars);
            cellarCountRow.Children.Add(btnPlusCellars);
            sec2.Children.Add(cellarCountRow);

            sec2.Children.Add(new TextBlock { Text = "Height per Cellar Level:", FontSize = 11, Foreground = mutedBrush });
            _txtCellarHeight = new System.Windows.Controls.TextBox { Text = "12'-0\"", Height = 28, Margin = new Thickness(0, 2, 0, 8), VerticalContentAlignment = VerticalAlignment.Center };
            _txtCellarHeight.TextChanged += (s, e) => RecalculateSchedule();
            sec2.Children.Add(_txtCellarHeight);

            _chkTwoDigits = new System.Windows.Controls.CheckBox { Content = "2-Digit Prefix (01 1ST FL., 00 CELLAR)", IsChecked = true, Margin = new Thickness(0, 4, 0, 0) };
            _chkTwoDigits.Checked += (s, e) => RecalculateSchedule();
            _chkTwoDigits.Unchecked += (s, e) => RecalculateSchedule();
            sec2.Children.Add(_chkTwoDigits);

            System.Windows.Controls.Grid.SetColumn(sec2, 1);
            cfgGrid.Children.Add(sec2);

            // ── Section 3: Roof, Bulkhead & Views ──
            StackPanel sec3 = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
            sec3.Children.Add(new TextBlock { Text = "🏗️ ROOF, BULKHEAD & VIEWS", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 8) });

            // Roof row
            StackPanel roofRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            _chkIncludeRoof = new System.Windows.Controls.CheckBox { Content = "Create ROOF level  |  Height:", IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Width = 180 };
            _chkIncludeRoof.Checked += (s, e) => RecalculateSchedule();
            _chkIncludeRoof.Unchecked += (s, e) => RecalculateSchedule();
            _txtRoofHeight = new System.Windows.Controls.TextBox { Text = "12'-0\"", Width = 75, Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
            _txtRoofHeight.TextChanged += (s, e) => RecalculateSchedule();
            roofRow.Children.Add(_chkIncludeRoof);
            roofRow.Children.Add(_txtRoofHeight);
            sec3.Children.Add(roofRow);

            // Bulkhead row
            StackPanel bhRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            _chkIncludeBulkhead = new System.Windows.Controls.CheckBox { Content = "Create BULKHEAD  |  Height:", IsChecked = true, VerticalAlignment = VerticalAlignment.Center, Width = 180 };
            _chkIncludeBulkhead.Checked += (s, e) => RecalculateSchedule();
            _chkIncludeBulkhead.Unchecked += (s, e) => RecalculateSchedule();
            _txtBulkheadHeight = new System.Windows.Controls.TextBox { Text = "10'-0\"", Width = 75, Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
            _txtBulkheadHeight.TextChanged += (s, e) => RecalculateSchedule();
            bhRow.Children.Add(_chkIncludeBulkhead);
            bhRow.Children.Add(_txtBulkheadHeight);
            sec3.Children.Add(bhRow);

            // View checkboxes
            _chkCreateFloorPlans = new System.Windows.Controls.CheckBox { Content = "Create associated Floor Plan Views", IsChecked = true, Margin = new Thickness(0, 2, 0, 3) };
            _chkCreateFloorPlans.Checked += (s, e) => UpdateViewsFlag(true);
            _chkCreateFloorPlans.Unchecked += (s, e) => UpdateViewsFlag(false);

            _chkCreateCeilingPlans = new System.Windows.Controls.CheckBox { Content = "Create associated Reflected Ceiling Plans (RCP)", IsChecked = true, Margin = new Thickness(0, 2, 0, 0) };
            _chkCreateCeilingPlans.Checked += (s, e) => UpdateCeilingViewsFlag(true);
            _chkCreateCeilingPlans.Unchecked += (s, e) => UpdateCeilingViewsFlag(false);

            sec3.Children.Add(_chkCreateFloorPlans);
            sec3.Children.Add(_chkCreateCeilingPlans);

            System.Windows.Controls.Grid.SetColumn(sec3, 2);
            cfgGrid.Children.Add(sec3);

            configContainer.Child = cfgGrid;
            System.Windows.Controls.Grid.SetRow(configContainer, 1);
            root.Children.Add(configContainer);

            // ══════════════════════════════════════════════════════════
            // 2. LIVE PREVIEW DATAGRID
            // ══════════════════════════════════════════════════════════
            Border gridCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 4, 16, 8),
                Padding = new Thickness(12)
            };

            System.Windows.Controls.Grid tableContainer = new System.Windows.Controls.Grid();
            tableContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            tableContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            TextBlock tableTitle = new TextBlock
            {
                Text = "LIVE PREVIEW — PLANNED LEVELS (Double-click any Level Name to edit before creation):",
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = mutedBrush,
                Margin = new Thickness(4, 0, 0, 8)
            };
            System.Windows.Controls.Grid.SetRow(tableTitle, 0);
            tableContainer.Children.Add(tableTitle);

            _dataGrid = new System.Windows.Controls.DataGrid
            {
                ItemsSource = _previewItems,
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserSortColumns = false,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E2E8F0")),
                RowHeight = 30,
                FontSize = 12.5,
                BorderThickness = new Thickness(1),
                BorderBrush = borderBrush,
                Background = System.Windows.Media.Brushes.White,
                AlternatingRowBackground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#F8FAFC"))
            };

            // Index
            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "#",
                Binding = new System.Windows.Data.Binding("Index"),
                IsReadOnly = true,
                Width = 45
            });

            // Category Type
            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Category",
                Binding = new System.Windows.Data.Binding("LevelType"),
                IsReadOnly = true,
                Width = 110
            });

            // Level Name (Editable)
            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Level Name ✏️ (Editable)",
                Binding = new System.Windows.Data.Binding("LevelName") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                IsReadOnly = false,
                Width = new DataGridLength(1.6, DataGridLengthUnitType.Star)
            });

            // Elevation
            _dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Elevation",
                Binding = new System.Windows.Data.Binding("ElevationDisplay"),
                IsReadOnly = true,
                Width = 120
            });

            // Create Floor Plan View Checkbox
            _dataGrid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Floor Plan [✔]",
                Binding = new System.Windows.Data.Binding("CreateFloorPlan") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                Width = 120
            });

            // Create Ceiling Plan (RCP) Checkbox
            _dataGrid.Columns.Add(new DataGridCheckBoxColumn
            {
                Header = "Ceiling Plan RCP [✔]",
                Binding = new System.Windows.Data.Binding("CreateCeilingPlan") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                Width = 140
            });

            System.Windows.Controls.Grid.SetRow(_dataGrid, 1);
            tableContainer.Children.Add(_dataGrid);

            gridCard.Child = tableContainer;
            System.Windows.Controls.Grid.SetRow(gridCard, 2);
            root.Children.Add(gridCard);

            // ══════════════════════════════════════════════════════════
            // 3. FOOTER ACTIONS
            // ══════════════════════════════════════════════════════════
            Border footer = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E2E8F0")),
                Padding = new Thickness(20, 12, 20, 12)
            };

            System.Windows.Controls.Grid footGrid = new System.Windows.Controls.Grid();
            footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusSummary = new TextBlock
            {
                Text = "Calculating...",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = darkBrush
            };
            footGrid.Children.Add(_statusSummary);

            StackPanel btnPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            System.Windows.Controls.Button btnReset = new System.Windows.Controls.Button
            {
                Content = "Reset Defaults",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnReset.Click += (s, e) => {
                _txtFloorCount.Text = "15";
                _txtTypicalHeight.Text = "10'-0\"";
                _txtBaseElevation.Text = "0'-0\"";
                _txtStartFloorNumber.Text = "1";
                _txtCellarCount.Text = "2";
                _txtCellarHeight.Text = "12'-0\"";
                _chkIncludeRoof.IsChecked = true;
                _txtRoofHeight.Text = "12'-0\"";
                _chkIncludeBulkhead.IsChecked = true;
                _txtBulkheadHeight.Text = "10'-0\"";
                _chkTwoDigits.IsChecked = true;
                _chkCreateFloorPlans.IsChecked = true;
                RecalculateSchedule();
            };
            btnPanel.Children.Add(btnReset);

            System.Windows.Controls.Button btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, e) => Close();
            btnPanel.Children.Add(btnCancel);

            System.Windows.Controls.Button btnCreate = new System.Windows.Controls.Button
            {
                Content = "⚡ Create Levels in Revit",
                Padding = new Thickness(20, 8, 20, 8),
                Background = accentBrush,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };
            btnCreate.Click += (s, e) => ExecuteCreation();
            btnPanel.Children.Add(btnCreate);

            System.Windows.Controls.Grid.SetColumn(btnPanel, 1);
            footGrid.Children.Add(btnPanel);

            footer.Child = footGrid;
            System.Windows.Controls.Grid.SetRow(footer, 3);
            root.Children.Add(footer);

            Content = root;
        }

        private void UpdateViewsFlag(bool create)
        {
            foreach (var item in _previewItems)
            {
                item.CreateFloorPlan = create;
            }
            if (_dataGrid != null) _dataGrid.Items.Refresh();
            UpdateStatusSummary();
        }

        private void UpdateCeilingViewsFlag(bool create)
        {
            foreach (var item in _previewItems)
            {
                item.CreateCeilingPlan = create;
            }
            if (_dataGrid != null) _dataGrid.Items.Refresh();
            UpdateStatusSummary();
        }

        private void UpdateStatusSummary()
        {
            if (_statusSummary == null) return;
            double topElev = _previewItems.Count > 0 ? _previewItems.Max(x => x.ElevationFeet) : 0;
            double lowestElev = _previewItems.Count > 0 ? _previewItems.Min(x => x.ElevationFeet) : 0;
            double totalHeight = topElev - lowestElev;

            int floorViewsCount = _previewItems.Count(x => x.CreateFloorPlan);
            int rcpViewsCount = _previewItems.Count(x => x.CreateCeilingPlan);

            _statusSummary.Text = string.Format("⚡ Ready to create {0} levels ({1} total height). Generates {2} Floor Plan(s) and {3} RCP View(s).",
                _previewItems.Count,
                LevelCreatorService.FormatLength(_doc, totalHeight),
                floorViewsCount,
                rcpViewsCount);
        }

        private void RecalculateSchedule()
        {
            if (_txtFloorCount == null) return;

            int fc, sf, cc;
            int floorCount = int.TryParse(_txtFloorCount.Text, out fc) ? Math.Max(0, fc) : 10;
            int startFloor = int.TryParse(_txtStartFloorNumber.Text, out sf) ? Math.Max(1, sf) : 1;
            int cellarCount = int.TryParse(_txtCellarCount.Text, out cc) ? Math.Max(0, cc) : 0;

            double baseElev, typicalHeight, cellarHeight, roofHeight, bulkheadHeight;
            LevelCreatorService.TryParseLength(_doc, _txtBaseElevation.Text, out baseElev);
            LevelCreatorService.TryParseLength(_doc, _txtTypicalHeight.Text, out typicalHeight);
            if (typicalHeight <= 0) typicalHeight = 10.0;

            LevelCreatorService.TryParseLength(_doc, _txtCellarHeight.Text, out cellarHeight);
            if (cellarHeight <= 0) cellarHeight = 12.0;

            bool roof = _chkIncludeRoof != null ? (_chkIncludeRoof.IsChecked == true) : true;
            string roofTxt = _txtRoofHeight != null ? _txtRoofHeight.Text : "12'";
            LevelCreatorService.TryParseLength(_doc, roofTxt, out roofHeight);
            if (roofHeight <= 0) roofHeight = 12.0;

            bool bulkhead = _chkIncludeBulkhead != null ? (_chkIncludeBulkhead.IsChecked == true) : true;
            string bulkTxt = _txtBulkheadHeight != null ? _txtBulkheadHeight.Text : "10'";
            LevelCreatorService.TryParseLength(_doc, bulkTxt, out bulkheadHeight);
            if (bulkheadHeight <= 0) bulkheadHeight = 10.0;

            bool twoDigits = _chkTwoDigits != null ? (_chkTwoDigits.IsChecked == true) : true;
            bool createFloorViews = _chkCreateFloorPlans != null ? (_chkCreateFloorPlans.IsChecked == true) : true;
            bool createCeilingViews = _chkCreateCeilingPlans != null ? (_chkCreateCeilingPlans.IsChecked == true) : true;

            var planned = LevelCreatorService.BuildPlannedLevels(
                _doc,
                baseElev,
                startFloor,
                floorCount,
                typicalHeight,
                cellarCount,
                cellarHeight,
                roof,
                roofHeight,
                bulkhead,
                bulkheadHeight,
                createFloorViews,
                createCeilingViews,
                twoDigits);

            _previewItems.Clear();
            foreach (var item in planned)
            {
                _previewItems.Add(item);
            }

            UpdateStatusSummary();
        }

        private void ExecuteCreation()
        {
            if (_previewItems.Count == 0)
            {
                MessageBox.Show("No levels configured to create.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int count = _previewItems.Count;
            int floorViewCount = _previewItems.Count(x => x.CreateFloorPlan);
            int rcpViewCount = _previewItems.Count(x => x.CreateCeilingPlan);
            int totalViews = floorViewCount + rcpViewCount;

            var confirm = MessageBox.Show(
                string.Format("Confirm creation of {0} new level(s), {1} Floor Plan(s), and {2} RCP Ceiling Plan(s) in Revit?", count, floorViewCount, rcpViewCount),
                "Confirm Batch Level Creation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            bool createCeilings = rcpViewCount > 0;

            Tuple<int, int, List<string>> createResult = LevelCreatorService.CreateLevelsInRevit(
                _doc,
                _previewItems.ToList(),
                createCeilings);

            int levelsCreated = createResult.Item1;
            int viewsCreated = createResult.Item2;
            List<string> errors = createResult.Item3;

            if (errors.Count > 0)
            {
                string msg = string.Format("Created {0} levels and {1} views with observations:\n\n{2}", levelsCreated, viewsCreated, string.Join("\n", errors.Take(5).ToArray()));
                if (errors.Count > 5) msg += string.Format("\n...and {0} more.", errors.Count - 5);

                MessageBox.Show(msg, "BauTools - Completed with Warnings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(string.Format("✅ Success!\n\n• Levels created: {0}\n• Plan & RCP views created: {1}", levelsCreated, viewsCreated),
                    "BauTools - Batch Level Generator", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            DialogResult = true;
            Close();
        }
    }
}

```

### `ZoningFloorArea\Views\BubbleHeadsWindow.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace ZoningFloorArea.Views
{
    public class BubbleHeadsWindow : Window
    {
        private readonly Document _doc;
        private readonly Autodesk.Revit.DB.View _activeView;
        private readonly List<Autodesk.Revit.DB.Grid> _grids;
        private readonly List<Level> _levels;

        // UI Controls
        private System.Windows.Controls.CheckBox _chkGrids;
        private System.Windows.Controls.CheckBox _chkLevels;

        // Radio buttons for End0
        private System.Windows.Controls.RadioButton _rbEnd0Show;
        private System.Windows.Controls.RadioButton _rbEnd0Hide;
        private System.Windows.Controls.RadioButton _rbEnd0Keep;

        // Radio buttons for End1
        private System.Windows.Controls.RadioButton _rbEnd1Show;
        private System.Windows.Controls.RadioButton _rbEnd1Hide;
        private System.Windows.Controls.RadioButton _rbEnd1Keep;

        private System.Windows.Controls.TextBlock _statusSummary;

        // Color Palette matching BauTools
        private static readonly System.Windows.Media.Color COL_BG        = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#F1F5F9");
        private static readonly System.Windows.Media.Color COL_CARD      = System.Windows.Media.Colors.White;
        private static readonly System.Windows.Media.Color COL_DARK      = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0F172A");
        private static readonly System.Windows.Media.Color COL_ACCENT    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0071E3");
        private static readonly System.Windows.Media.Color COL_ACCENT2   = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0284C7");
        private static readonly System.Windows.Media.Color COL_MUTED     = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#64748B");
        private static readonly System.Windows.Media.Color COL_BORDER    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#CBD5E1");
        private static readonly System.Windows.Media.Color COL_HEADER_BG = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#1E293B");
        private static readonly System.Windows.Media.Color COL_DANGER    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#DC2626");

        public BubbleHeadsWindow(Document doc, Autodesk.Revit.DB.View activeView)
        {
            _doc = doc;
            _activeView = activeView;

            // Collect elements in active view
            _grids = new FilteredElementCollector(_doc, _activeView.Id)
                .OfClass(typeof(Autodesk.Revit.DB.Grid))
                .Cast<Autodesk.Revit.DB.Grid>()
                .ToList();

            _levels = new FilteredElementCollector(_doc, _activeView.Id)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            Title = "BauTools — Bubble Heads & Datum Manager (Active View)";
            Height = 620;
            Width = 720;
            MinHeight = 560;
            MinWidth = 650;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 13;

            BuildUI();
            UpdateSummary();
        }

        private void BuildUI()
        {
            SolidColorBrush cardBrush     = new SolidColorBrush(COL_CARD);
            SolidColorBrush darkBrush     = new SolidColorBrush(COL_DARK);
            SolidColorBrush accentBrush   = new SolidColorBrush(COL_ACCENT);
            SolidColorBrush accent2Brush  = new SolidColorBrush(COL_ACCENT2);
            SolidColorBrush mutedBrush    = new SolidColorBrush(COL_MUTED);
            SolidColorBrush borderBrush   = new SolidColorBrush(COL_BORDER);
            SolidColorBrush headerBgBrush = new SolidColorBrush(COL_HEADER_BG);

            System.Windows.Controls.Grid root = new System.Windows.Controls.Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // View Info Bar
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Elements Target Card
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Presets Card
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Manual Options Card
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer Actions

            // 0. HEADER
            Border headerBar = new Border
            {
                Background = headerBgBrush,
                Padding = new Thickness(24, 14, 24, 14)
            };

            System.Windows.Controls.Grid hGrid = new System.Windows.Controls.Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titlePanel = new StackPanel();
            StackPanel logoLine = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

            Border badge = new Border
            {
                Background = accent2Brush,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new TextBlock { Text = "BUBBLES & DATUMS", FontWeight = FontWeights.ExtraBold, FontSize = 12, Foreground = System.Windows.Media.Brushes.White };
            logoLine.Children.Add(badge);

            logoLine.Children.Add(new TextBlock
            {
                Text = "BauTools — Bubble Heads & Datum Visibility",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            titlePanel.Children.Add(logoLine);

            titlePanel.Children.Add(new TextBlock
            {
                Text = "Show or Hide bubble heads for Grids and Levels exclusively in the active view.",
                FontSize = 11,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#94A3B8")),
                Margin = new Thickness(0, 4, 0, 0)
            });
            hGrid.Children.Add(titlePanel);

            headerBar.Child = hGrid;
            System.Windows.Controls.Grid.SetRow(headerBar, 0);
            root.Children.Add(headerBar);

            // 1. ACTIVE VIEW INFO BAR
            Border viewInfoBar = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E2E8F0")),
                Padding = new Thickness(20, 8, 20, 8)
            };

            StackPanel vPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            vPanel.Children.Add(new TextBlock { Text = "👁️ ACTIVE VIEW: ", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush });
            vPanel.Children.Add(new TextBlock { Text = string.Format("{0} ", _activeView.Name), FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = accentBrush });
            vPanel.Children.Add(new TextBlock { Text = string.Format("({0})", _activeView.ViewType), FontSize = 11.5, Foreground = mutedBrush });

            viewInfoBar.Child = vPanel;
            System.Windows.Controls.Grid.SetRow(viewInfoBar, 1);
            root.Children.Add(viewInfoBar);

            // 2. TARGET ELEMENTS CARD
            Border targetCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 12, 16, 6),
                Padding = new Thickness(16, 12, 16, 12)
            };

            StackPanel targetPanel = new StackPanel();
            targetPanel.Children.Add(new TextBlock { Text = "1. ELEMENTS TO MODIFY IN ACTIVE VIEW:", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 8) });

            StackPanel chkRow = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

            _chkGrids = new System.Windows.Controls.CheckBox
            {
                Content = string.Format("Grids ({0} in view)", _grids.Count),
                IsChecked = _grids.Count > 0,
                IsEnabled = _grids.Count > 0,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 24, 0)
            };
            _chkGrids.Checked += (s, e) => UpdateSummary();
            _chkGrids.Unchecked += (s, e) => UpdateSummary();

            _chkLevels = new System.Windows.Controls.CheckBox
            {
                Content = string.Format("Levels ({0} in view)", _levels.Count),
                IsChecked = _levels.Count > 0,
                IsEnabled = _levels.Count > 0,
                FontWeight = FontWeights.SemiBold
            };
            _chkLevels.Checked += (s, e) => UpdateSummary();
            _chkLevels.Unchecked += (s, e) => UpdateSummary();

            chkRow.Children.Add(_chkGrids);
            chkRow.Children.Add(_chkLevels);
            targetPanel.Children.Add(chkRow);

            targetCard.Child = targetPanel;
            System.Windows.Controls.Grid.SetRow(targetCard, 2);
            root.Children.Add(targetCard);

            // 3. QUICK PRESETS CARD
            Border presetCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 6, 16, 6),
                Padding = new Thickness(16, 12, 16, 12)
            };

            StackPanel presetPanel = new StackPanel();
            presetPanel.Children.Add(new TextBlock { Text = "2. QUICK PRESETS (1-CLICK CONFIGURATION):", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 8) });

            System.Windows.Controls.Grid pGrid = new System.Windows.Controls.Grid();
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Preset 1: Only Left
            System.Windows.Controls.Button btnPresetLeft = CreatePresetButton("⬅ Only Left / End 0\n(Show 0, Hide 1)", () => {
                _rbEnd0Show.IsChecked = true;
                _rbEnd1Hide.IsChecked = true;
            });
            System.Windows.Controls.Grid.SetColumn(btnPresetLeft, 0);
            pGrid.Children.Add(btnPresetLeft);

            // Preset 2: Only Right
            System.Windows.Controls.Button btnPresetRight = CreatePresetButton("➡ Only Right / End 1\n(Hide 0, Show 1)", () => {
                _rbEnd0Hide.IsChecked = true;
                _rbEnd1Show.IsChecked = true;
            });
            System.Windows.Controls.Grid.SetColumn(btnPresetRight, 1);
            pGrid.Children.Add(btnPresetRight);

            // Preset 3: Both Ends
            System.Windows.Controls.Button btnPresetBoth = CreatePresetButton("↔ Both Ends\n(Show 0 & 1)", () => {
                _rbEnd0Show.IsChecked = true;
                _rbEnd1Show.IsChecked = true;
            });
            System.Windows.Controls.Grid.SetColumn(btnPresetBoth, 2);
            pGrid.Children.Add(btnPresetBoth);

            // Preset 4: Turn OFF All
            System.Windows.Controls.Button btnPresetOff = CreatePresetButton("🚫 Turn OFF All\n(Hide 0 & 1)", () => {
                _rbEnd0Hide.IsChecked = true;
                _rbEnd1Hide.IsChecked = true;
            }, true);
            System.Windows.Controls.Grid.SetColumn(btnPresetOff, 3);
            pGrid.Children.Add(btnPresetOff);

            presetPanel.Children.Add(pGrid);
            presetCard.Child = presetPanel;
            System.Windows.Controls.Grid.SetRow(presetCard, 3);
            root.Children.Add(presetCard);

            // 4. MANUAL DETAILED OPTIONS CARD
            Border manualCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 6, 16, 12),
                Padding = new Thickness(16, 12, 16, 12)
            };

            StackPanel manualPanel = new StackPanel();
            manualPanel.Children.Add(new TextBlock { Text = "3. DETAILED END CONFIGURATION:", FontWeight = FontWeights.Bold, FontSize = 11.5, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 10) });

            System.Windows.Controls.Grid mGrid = new System.Windows.Controls.Grid();
            mGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Column 0: End 0
            StackPanel col0 = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            col0.Children.Add(new TextBlock { Text = "End 0 (Left / Bottom / Start):", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 6) });

            _rbEnd0Show = new System.Windows.Controls.RadioButton { Content = "🟢 Show Bubble", GroupName = "End0", IsChecked = true, Margin = new Thickness(0, 2, 0, 4) };
            _rbEnd0Hide = new System.Windows.Controls.RadioButton { Content = "🔴 Hide Bubble", GroupName = "End0", Margin = new Thickness(0, 2, 0, 4) };
            _rbEnd0Keep = new System.Windows.Controls.RadioButton { Content = "⚪ Keep unchanged", GroupName = "End0", Margin = new Thickness(0, 2, 0, 4) };

            _rbEnd0Show.Checked += (s, e) => UpdateSummary();
            _rbEnd0Hide.Checked += (s, e) => UpdateSummary();
            _rbEnd0Keep.Checked += (s, e) => UpdateSummary();

            col0.Children.Add(_rbEnd0Show);
            col0.Children.Add(_rbEnd0Hide);
            col0.Children.Add(_rbEnd0Keep);
            System.Windows.Controls.Grid.SetColumn(col0, 0);
            mGrid.Children.Add(col0);

            // Column 1: End 1
            StackPanel col1 = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
            col1.Children.Add(new TextBlock { Text = "End 1 (Right / Top / End):", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 6) });

            _rbEnd1Show = new System.Windows.Controls.RadioButton { Content = "🟢 Show Bubble", GroupName = "End1", Margin = new Thickness(0, 2, 0, 4) };
            _rbEnd1Hide = new System.Windows.Controls.RadioButton { Content = "🔴 Hide Bubble", GroupName = "End1", IsChecked = true, Margin = new Thickness(0, 2, 0, 4) };
            _rbEnd1Keep = new System.Windows.Controls.RadioButton { Content = "⚪ Keep unchanged", GroupName = "End1", Margin = new Thickness(0, 2, 0, 4) };

            _rbEnd1Show.Checked += (s, e) => UpdateSummary();
            _rbEnd1Hide.Checked += (s, e) => UpdateSummary();
            _rbEnd1Keep.Checked += (s, e) => UpdateSummary();

            col1.Children.Add(_rbEnd1Show);
            col1.Children.Add(_rbEnd1Hide);
            col1.Children.Add(_rbEnd1Keep);
            System.Windows.Controls.Grid.SetColumn(col1, 1);
            mGrid.Children.Add(col1);

            manualPanel.Children.Add(mGrid);
            manualCard.Child = manualPanel;
            System.Windows.Controls.Grid.SetRow(manualCard, 4);
            root.Children.Add(manualCard);

            // 5. FOOTER ACTIONS
            Border footer = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E2E8F0")),
                Padding = new Thickness(20, 12, 20, 12)
            };

            System.Windows.Controls.Grid footGrid = new System.Windows.Controls.Grid();
            footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusSummary = new TextBlock
            {
                Text = "Ready.",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = darkBrush
            };
            footGrid.Children.Add(_statusSummary);

            StackPanel btnPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            System.Windows.Controls.Button btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancel",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, e) => Close();
            btnPanel.Children.Add(btnCancel);

            System.Windows.Controls.Button btnApply = new System.Windows.Controls.Button
            {
                Content = "✔ Apply Changes",
                Padding = new Thickness(20, 8, 20, 8),
                Background = accentBrush,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };
            btnApply.Click += (s, e) => ApplyChanges();
            btnPanel.Children.Add(btnApply);

            System.Windows.Controls.Grid.SetColumn(btnPanel, 1);
            footGrid.Children.Add(btnPanel);

            footer.Child = footGrid;
            System.Windows.Controls.Grid.SetRow(footer, 5);
            root.Children.Add(footer);

            Content = root;
        }

        private System.Windows.Controls.Button CreatePresetButton(string text, Action onClick, bool isDanger = false)
        {
            System.Windows.Controls.Button btn = new System.Windows.Controls.Button
            {
                Content = text,
                Padding = new Thickness(8, 8, 8, 8),
                Margin = new Thickness(3, 0, 3, 0),
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = isDanger ? new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#FEE2E2")) : new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#EFF6FF")),
                BorderBrush = isDanger ? new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#FCA5A5")) : new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#BFDBFE")),
                BorderThickness = new Thickness(1)
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void UpdateSummary()
        {
            if (_statusSummary == null) return;

            int targetCount = 0;
            if (_chkGrids != null && _chkGrids.IsChecked == true) targetCount += _grids.Count;
            if (_chkLevels != null && _chkLevels.IsChecked == true) targetCount += _levels.Count;

            string end0Action = (_rbEnd0Show != null && _rbEnd0Show.IsChecked == true) ? "Show End 0" : ((_rbEnd0Hide != null && _rbEnd0Hide.IsChecked == true) ? "Hide End 0" : "Keep End 0");
            string end1Action = (_rbEnd1Show != null && _rbEnd1Show.IsChecked == true) ? "Show End 1" : ((_rbEnd1Hide != null && _rbEnd1Hide.IsChecked == true) ? "Hide End 1" : "Keep End 1");

            _statusSummary.Text = string.Format("⚡ {0} element(s) selected. Action: {1} | {2}.", targetCount, end0Action, end1Action);
        }

        private void ApplyChanges()
        {
            bool modifyGrids = _chkGrids.IsChecked == true;
            bool modifyLevels = _chkLevels.IsChecked == true;

            if (!modifyGrids && !modifyLevels)
            {
                MessageBox.Show("Please select at least one element category (Grids or Levels).", "BauTools", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int end0Mode = _rbEnd0Show.IsChecked == true ? 1 : (_rbEnd0Hide.IsChecked == true ? -1 : 0); // 1 = Show, -1 = Hide, 0 = Keep
            int end1Mode = _rbEnd1Show.IsChecked == true ? 1 : (_rbEnd1Hide.IsChecked == true ? -1 : 0);

            if (end0Mode == 0 && end1Mode == 0)
            {
                MessageBox.Show("Both ends are set to 'Keep unchanged'. No modifications to apply.", "BauTools", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int countModified = 0;

            using (Transaction tx = new Transaction(_doc, "BauTools: Toggle Bubble Heads"))
            {
                tx.Start();

                // Process Grids
                if (modifyGrids)
                {
                    foreach (var g in _grids)
                    {
                        try
                        {
                            if (end0Mode == 1) g.ShowBubbleInView(DatumEnds.End0, _activeView);
                            else if (end0Mode == -1) g.HideBubbleInView(DatumEnds.End0, _activeView);

                            if (end1Mode == 1) g.ShowBubbleInView(DatumEnds.End1, _activeView);
                            else if (end1Mode == -1) g.HideBubbleInView(DatumEnds.End1, _activeView);

                            countModified++;
                        }
                        catch
                        {
                        }
                    }
                }

                // Process Levels
                if (modifyLevels)
                {
                    foreach (Level l in _levels)
                    {
                        try
                        {
                            if (end0Mode == 1) l.ShowBubbleInView(DatumEnds.End0, _activeView);
                            else if (end0Mode == -1) l.HideBubbleInView(DatumEnds.End0, _activeView);

                            if (end1Mode == 1) l.ShowBubbleInView(DatumEnds.End1, _activeView);
                            else if (end1Mode == -1) l.HideBubbleInView(DatumEnds.End1, _activeView);

                            countModified++;
                        }
                        catch
                        {
                        }
                    }
                }

                tx.Commit();
            }

            TaskDialog.Show("BauTools - Bubble Heads",
                string.Format("✅ Updated bubbles on {0} element(s) in active view '{1}'.", countModified, _activeView.Name));

            DialogResult = true;
            Close();
        }
    }
}

```

### `ZoningFloorArea\Views\GenerativeZoningWindow.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;
using WpfGrid = System.Windows.Controls.Grid;
using WpfColor = System.Windows.Media.Color;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfSlider = System.Windows.Controls.Slider;
using WpfVisibility = System.Windows.Visibility;
using WpfColors = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfLine = System.Windows.Shapes.Line;

namespace ZoningFloorArea.Views
{
    public class GenerativeZoningWindow : Window
    {
        private readonly Document _doc;
        private readonly NeuralGenerativeSolver _solver;
        private readonly RevitMassingBakerService _bakerService;
        private readonly GenerativeInputParameters _inputs;

        private List<GenerativeScenario> _scenarios;
        private GenerativeScenario _activeScenario;

        // UI Controls
        private StackPanel _scenarioCardsContainer;
        private Viewport3D _viewport3D;
        private ModelVisual3D _modelVisual;
        private PerspectiveCamera _camera;
        private double _cameraDistance = 320.0;
        private double _cameraTheta = 45.0; // Azimuth angle
        private double _cameraPhi = 35.0;   // Elevation angle
        private System.Windows.Point _lastMousePos;
        private bool _isOrbiting = false;

        private WpfTextBlock _txtPreviewTitle;
        private WpfTextBlock _txtPreviewMetrics;
        private Border _badgeZfaStatus;
        private WpfTextBlock _txtZfaStatus;
        private Border _badgeHeightStatus;
        private WpfTextBlock _txtHeightStatus;
        private Border _badgeRevenueStatus;
        private WpfTextBlock _txtRevenueStatus;

        private WpfCheckBox _chkDesignOptions;
        private WpfCheckBox _chkCreateLevels;

        private static readonly WpfColor COL_BG = (WpfColor)ColorConverter.ConvertFromString("#F8FAFC");
        private static readonly WpfColor COL_SURFACE = (WpfColor)ColorConverter.ConvertFromString("#FFFFFF");
        private static readonly WpfColor COL_BORDER = (WpfColor)ColorConverter.ConvertFromString("#E2E8F0");
        private static readonly WpfColor COL_PRIMARY = (WpfColor)ColorConverter.ConvertFromString("#0071E3");
        private static readonly WpfColor COL_TEXT_MAIN = (WpfColor)ColorConverter.ConvertFromString("#0F172A");
        private static readonly WpfColor COL_TEXT_MUTED = (WpfColor)ColorConverter.ConvertFromString("#64748B");

        public GenerativeZoningWindow(Document doc)
        {
            _doc = doc;
            _solver = new NeuralGenerativeSolver();
            _bakerService = new RevitMassingBakerService(doc);
            _inputs = new GenerativeInputParameters();

            Title = "BauTools — Neural Generative Zoning & Real-Time Massing Morphing Engine";
            Width = 1320;
            Height = 880;
            MinWidth = 1100;
            MinHeight = 740;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12.5;

            BuildUI();
            RecalculateScenarios();
        }

        private void BuildUI()
        {
            WpfGrid root = new WpfGrid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Main Content
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // ── Row 0: Header ──
            Border header = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(24, 12, 24, 12)
            };
            StackPanel hStack = new StackPanel();
            hStack.Children.Add(new WpfTextBlock
            {
                Text = "⚡ Neural Generative Zoning & Real-Time Massing Morphing Engine",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            hStack.Children.Add(new WpfTextBlock
            {
                Text = "Live parametric synaptic sliders • Real-time volumetric morphing • Instant scenario clustering & Design Options baking.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 2, 0, 0)
            });
            header.Child = hStack;
            WpfGrid.SetRow(header, 0);
            root.Children.Add(header);

            // ── Row 1: 3-Column Workspace ──
            WpfGrid mainGrid = new WpfGrid { Margin = new Thickness(20, 14, 20, 14) };
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) }); // Left: Sliders
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // Center: Neural Synapses & Scenarios
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) }); // Right: 3D Preview

            mainGrid.Children.Add(CreateLiveSlidersPanel());
            mainGrid.Children.Add(CreateCenterScenariosPanel());
            mainGrid.Children.Add(CreateInteractive3DPanel());

            WpfGrid.SetRow(mainGrid, 1);
            root.Children.Add(mainGrid);

            // ── Row 2: Footer ──
            root.Children.Add(CreateFooterBar());

            Content = root;
        }

        private UIElement CreateLiveSlidersPanel()
        {
            Border card = CreateCard();
            WpfGrid.SetColumn(card, 0);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel sp = new StackPanel();

            sp.Children.Add(new WpfTextBlock
            {
                Text = "🎛️ LIVE SYNAPTIC ZONING SLIDERS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 0, 0, 10)
            });

            // 1. Regulatory Parameters
            AddSliderField(sp, "Lot Area (SF):", 3000, 60000, _inputs.LotAreaSqFt, true, "N0", v => { _inputs.LotAreaSqFt = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Base Allowable FAR:", 1.0, 18.0, _inputs.BaseFar, false, "N2", v => { _inputs.BaseFar = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Max Building Height (FT):", 60, 500, _inputs.MaxHeightFt, true, "N0", v => { _inputs.MaxHeightFt = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Street Front Setback (FT):", 0, 35, _inputs.SetbackFrontFt, true, "N0", v => { _inputs.SetbackFrontFt = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Rear Yard Setback (FT):", 10, 45, _inputs.SetbackRearFt, true, "N0", v => { _inputs.SetbackRearFt = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Side Yard Setbacks (FT):", 0, 25, _inputs.SetbackSidesFt, true, "N0", v => { _inputs.SetbackSidesFt = v; OnLiveParamChanged(); });

            Border sep1 = new Border { Height = 1, Background = new SolidColorBrush(COL_BORDER), Margin = new Thickness(0, 6, 0, 10) };
            sp.Children.Add(sep1);

            sp.Children.Add(new WpfTextBlock
            {
                Text = "🏛️ BASE, DORMERS & TOWER DRIVERS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 0, 0, 10)
            });

            AddSliderField(sp, "Base / Podium Floors:", 1, 6, _inputs.PodiumFloors, true, "N0", v => { _inputs.PodiumFloors = (int)v; OnLiveParamChanged(); });
            AddSliderField(sp, "Base Lot Coverage (%):", 40, 100, _inputs.PodiumCoveragePercent, true, "N0", v => { _inputs.PodiumCoveragePercent = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Dormer Transition Floors:", 0, 4, _inputs.DormerFloors, true, "N0", v => { _inputs.DormerFloors = (int)v; OnLiveParamChanged(); });
            AddSliderField(sp, "Dormer Setback Step (FT):", 4, 25, _inputs.DormerSetbackDepthFt, true, "N0", v => { _inputs.DormerSetbackDepthFt = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Tower Lot Coverage (%):", 20, 75, _inputs.TowerCoveragePercent, true, "N0", v => { _inputs.TowerCoveragePercent = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Floor-to-Floor Height (FT):", 9.5, 16.0, _inputs.FloorHeightTower, false, "N1", v => { _inputs.FloorHeightTower = v; OnLiveParamChanged(); });
            AddSliderField(sp, "Luxury Penthouse Floors:", 0, 4, _inputs.PenthouseFloors, true, "N0", v => { _inputs.PenthouseFloors = (int)v; OnLiveParamChanged(); });
            AddSliderField(sp, "Mandatory Housing (MIH %):", 0, 50, _inputs.MihPercent, true, "N0", v => { _inputs.MihPercent = v; OnLiveParamChanged(); });

            scroll.Content = sp;
            card.Child = scroll;
            return card;
        }

        private UIElement CreateCenterScenariosPanel()
        {
            Border card = CreateCard();
            WpfGrid.SetColumn(card, 2);

            WpfGrid cGrid = new WpfGrid();
            cGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            cGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Cards List

            StackPanel cHdr = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            cHdr.Children.Add(new WpfTextBlock
            {
                Text = "🧠 ACTIVE SCENARIOS & NEURAL CLUSTERS",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            });
            cHdr.Children.Add(new WpfTextBlock
            {
                Text = "Click any card to load its shape into the 3D visualizer, or use checkboxes to select masses to bake.",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            });
            WpfGrid.SetRow(cHdr, 0);
            cGrid.Children.Add(cHdr);

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _scenarioCardsContainer = new StackPanel();
            scroll.Content = _scenarioCardsContainer;
            WpfGrid.SetRow(scroll, 1);
            cGrid.Children.Add(scroll);

            card.Child = cGrid;
            return card;
        }

        private UIElement CreateInteractive3DPanel()
        {
            Border card = CreateCard();
            WpfGrid.SetColumn(card, 4);

            WpfGrid pGrid = new WpfGrid();
            pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title & View Cube Toolbar
            pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // HUD Badges
            pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 3D Viewport

            // Header & View Orientations
            WpfGrid topBar = new WpfGrid { Margin = new Thickness(0, 0, 0, 6) };
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel prevHdr = new StackPanel();
            _txtPreviewTitle = new WpfTextBlock
            {
                Text = "🏢 Interactive 3D Massing Viewport",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            };
            _txtPreviewMetrics = new WpfTextBlock
            {
                Text = "Drag left-mouse to orbit 360° • Scroll wheel to zoom.",
                FontSize = 10,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            };
            prevHdr.Children.Add(_txtPreviewTitle);
            prevHdr.Children.Add(_txtPreviewMetrics);
            WpfGrid.SetColumn(prevHdr, 0);
            topBar.Children.Add(prevHdr);

            // Orientation Preset Buttons
            StackPanel cubeBar = new StackPanel { Orientation = WpfOrientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            cubeBar.Children.Add(CreateOrientationButton("🏛️ Front", () => SetCameraOrientation(0.0, 15.0)));
            cubeBar.Children.Add(CreateOrientationButton("🏢 Rear", () => SetCameraOrientation(180.0, 15.0)));
            cubeBar.Children.Add(CreateOrientationButton("📐 3D Orbit", () => SetCameraOrientation(45.0, 35.0)));
            cubeBar.Children.Add(CreateOrientationButton("⬆️ Top", () => SetCameraOrientation(0.0, 89.0)));

            WpfGrid.SetColumn(cubeBar, 1);
            topBar.Children.Add(cubeBar);
            WpfGrid.SetRow(topBar, 0);
            pGrid.Children.Add(topBar);

            // HUD Badges Row
            WpfGrid hudGrid = new WpfGrid { Margin = new Thickness(0, 4, 0, 8) };
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _badgeZfaStatus = CreateHudBadge("ZFA CAP", out _txtZfaStatus);
            WpfGrid.SetColumn(_badgeZfaStatus, 0);
            hudGrid.Children.Add(_badgeZfaStatus);

            _badgeHeightStatus = CreateHudBadge("HEIGHT", out _txtHeightStatus);
            WpfGrid.SetColumn(_badgeHeightStatus, 2);
            hudGrid.Children.Add(_badgeHeightStatus);

            _badgeRevenueStatus = CreateHudBadge("EST. PROFORMA", out _txtRevenueStatus);
            WpfGrid.SetColumn(_badgeRevenueStatus, 4);
            hudGrid.Children.Add(_badgeRevenueStatus);

            WpfGrid.SetRow(hudGrid, 1);
            pGrid.Children.Add(hudGrid);

            // 3D Viewport Host
            Border viewportHost = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#0F172A")),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true
            };

            _viewport3D = new Viewport3D { ClipToBounds = true };
            _camera = new PerspectiveCamera
            {
                FieldOfView = 45,
                NearPlaneDistance = 1.0,
                FarPlaneDistance = 2000.0
            };
            _viewport3D.Camera = _camera;

            Model3DGroup lightsGroup = new Model3DGroup();
            lightsGroup.Children.Add(new AmbientLight(WpfColor.FromRgb(120, 130, 150)));
            lightsGroup.Children.Add(new DirectionalLight(WpfColor.FromRgb(255, 255, 255), new Vector3D(-1, -2, -3)));
            lightsGroup.Children.Add(new DirectionalLight(WpfColor.FromRgb(160, 180, 200), new Vector3D(2, 1, -1)));

            ModelVisual3D lightsVisual = new ModelVisual3D { Content = lightsGroup };
            _viewport3D.Children.Add(lightsVisual);

            _modelVisual = new ModelVisual3D();
            _viewport3D.Children.Add(_modelVisual);

            viewportHost.MouseLeftButtonDown += (s, e) =>
            {
                _isOrbiting = true;
                _lastMousePos = e.GetPosition(viewportHost);
                viewportHost.CaptureMouse();
            };

            viewportHost.MouseLeftButtonUp += (s, e) =>
            {
                _isOrbiting = false;
                viewportHost.ReleaseMouseCapture();
            };

            viewportHost.MouseMove += (s, e) =>
            {
                if (_isOrbiting)
                {
                    System.Windows.Point currentPos = e.GetPosition(viewportHost);
                    double dx = currentPos.X - _lastMousePos.X;
                    double dy = currentPos.Y - _lastMousePos.Y;

                    _cameraTheta -= dx * 0.6;
                    _cameraPhi = Math.Max(5.0, Math.Min(88.0, _cameraPhi + (dy * 0.5)));

                    _lastMousePos = currentPos;
                    UpdateCameraPosition();
                }
            };

            viewportHost.MouseWheel += (s, e) =>
            {
                double delta = e.Delta > 0 ? -25.0 : 25.0;
                _cameraDistance = Math.Max(80.0, Math.Min(700.0, _cameraDistance + delta));
                UpdateCameraPosition();
            };

            viewportHost.Child = _viewport3D;
            WpfGrid.SetRow(viewportHost, 2);
            pGrid.Children.Add(viewportHost);

            UpdateCameraPosition();
            card.Child = pGrid;
            return card;
        }

        private WpfButton CreateOrientationButton(string text, Action onClick)
        {
            WpfButton btn = new WpfButton
            {
                Content = text,
                Height = 24,
                FontSize = 9.5,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F1F5F9")),
                Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 4, 0),
                Padding = new Thickness(6, 0, 6, 0),
                Cursor = Cursors.Hand
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void SetCameraOrientation(double theta, double phi)
        {
            _cameraTheta = theta;
            _cameraPhi = phi;
            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            if (_camera == null) return;

            double radTheta = _cameraTheta * Math.PI / 180.0;
            double radPhi = _cameraPhi * Math.PI / 180.0;

            double x = _cameraDistance * Math.Cos(radPhi) * Math.Sin(radTheta);
            double y = -_cameraDistance * Math.Cos(radPhi) * Math.Cos(radTheta);
            double z = _cameraDistance * Math.Sin(radPhi);

            double targetZ = _activeScenario != null ? _activeScenario.TotalHeightFt * 0.45 : 70.0;

            _camera.Position = new Point3D(x, y, z + targetZ);
            _camera.LookDirection = new Vector3D(-x, -y, targetZ - (_camera.Position.Z));
            _camera.UpDirection = new Vector3D(0, 0, 1);
        }

        private Border CreateHudBadge(string label, out WpfTextBlock valueText)
        {
            Border b = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(6, 4, 6, 4)
            };

            StackPanel sp = new StackPanel();
            sp.Children.Add(new WpfTextBlock { Text = label, FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
            valueText = new WpfTextBlock { Text = "-", FontSize = 11, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_TEXT_MAIN) };
            sp.Children.Add(valueText);
            b.Child = sp;
            return b;
        }

        private UIElement CreateFooterBar()
        {
            Border footer = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 10, 24, 10)
            };

            WpfGrid fGrid = new WpfGrid();
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Options
            fGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Action Button

            StackPanel optStack = new StackPanel { Orientation = WpfOrientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            
            _chkDesignOptions = new WpfCheckBox
            {
                Content = "Assign to Revit Design Options",
                IsChecked = true,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            optStack.Children.Add(_chkDesignOptions);

            _chkCreateLevels = new WpfCheckBox
            {
                Content = "Auto-Generate Project Levels",
                IsChecked = true,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            optStack.Children.Add(_chkCreateLevels);

            WpfGrid.SetColumn(optStack, 0);
            fGrid.Children.Add(optStack);

            WpfButton btnBake = CreatePrimaryButton("🚀 Bake Selected Masses into Revit Project");
            btnBake.Height = 36;
            btnBake.Padding = new Thickness(20, 0, 20, 0);
            btnBake.Click += (s, e) => ExecuteBakeIntoRevit();
            WpfGrid.SetColumn(btnBake, 1);
            fGrid.Children.Add(btnBake);

            footer.Child = fGrid;
            WpfGrid.SetRow(footer, 2);
            return footer;
        }

        private void OnLiveParamChanged()
        {
            RecalculateScenarios();
        }

        private void RecalculateScenarios()
        {
            string previousActiveId = _activeScenario != null ? _activeScenario.Id : "scenario_interactive_custom";
            _scenarios = _solver.SolveScenarios(_inputs);

            _activeScenario = _scenarios.FirstOrDefault(s => s.Id == previousActiveId) ?? _scenarios[0];

            RefreshScenarioCardsUI();
            Render3DIsometricMassing();
            UpdateHudKpis();
            UpdateCameraPosition();
        }

        private void UpdateHudKpis()
        {
            if (_activeScenario == null) return;

            if (_txtZfaStatus != null)
            {
                _txtZfaStatus.Text = string.Format("{0:N0} SF ({1:N1}%)", _activeScenario.TotalZfa, _activeScenario.FarUtilizationPercent);
                _txtZfaStatus.Foreground = _activeScenario.FarUtilizationPercent > 100.0 ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#DC2626")) : new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#059669"));
            }

            if (_txtHeightStatus != null)
            {
                _txtHeightStatus.Text = string.Format("{0:N0} FT ({1} FL)", _activeScenario.TotalHeightFt, _activeScenario.TotalFloors);
                _txtHeightStatus.Foreground = _activeScenario.IsHeightExceeded ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#DC2626")) : new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#0284C7"));
            }

            if (_txtRevenueStatus != null)
            {
                _txtRevenueStatus.Text = string.Format("${0:N1}M", _activeScenario.EstimatedRevenueMillions);
                _txtRevenueStatus.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#059669"));
            }
        }

        private void RefreshScenarioCardsUI()
        {
            if (_scenarioCardsContainer == null) return;
            _scenarioCardsContainer.Children.Clear();

            foreach (GenerativeScenario s in _scenarios)
            {
                GenerativeScenario cur = s;
                bool isActive = (cur == _activeScenario);

                Border c = new Border
                {
                    Background = new SolidColorBrush(isActive ? (WpfColor)ColorConverter.ConvertFromString("#EFF6FF") : COL_SURFACE),
                    BorderBrush = new SolidColorBrush(isActive ? COL_PRIMARY : COL_BORDER),
                    BorderThickness = new Thickness(isActive ? 1.5 : 1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 6),
                    Cursor = Cursors.Hand
                };

                WpfGrid g = new WpfGrid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Checkbox
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Info
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Metrics

                WpfCheckBox chkBake = new WpfCheckBox
                {
                    IsChecked = cur.IsSelectedForBake,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                chkBake.Checked += (snd, ea) => cur.IsSelectedForBake = true;
                chkBake.Unchecked += (snd, ea) => cur.IsSelectedForBake = false;
                WpfGrid.SetColumn(chkBake, 0);
                g.Children.Add(chkBake);

                StackPanel tSp = new StackPanel();
                tSp.Children.Add(new WpfTextBlock
                {
                    Text = string.Format("{0} {1}", cur.Icon, cur.Title),
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(COL_TEXT_MAIN)
                });
                tSp.Children.Add(new WpfTextBlock
                {
                    Text = cur.Subtitle,
                    FontSize = 9.5,
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED)
                });
                WpfGrid.SetColumn(tSp, 1);
                g.Children.Add(tSp);

                StackPanel kSp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
                kSp.Children.Add(new WpfTextBlock
                {
                    Text = string.Format("{0:N0} SF", cur.TotalZfa),
                    FontWeight = FontWeights.Bold,
                    FontSize = 11.5,
                    Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(cur.ColorHex ?? "#2563EB")),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                kSp.Children.Add(new WpfTextBlock
                {
                    Text = string.Format("{0} FL • {1:N1}% FAR", cur.TotalFloors, cur.FarUtilizationPercent),
                    FontSize = 9,
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                WpfGrid.SetColumn(kSp, 2);
                g.Children.Add(kSp);

                c.Child = g;

                c.MouseLeftButtonDown += (snd, ea) =>
                {
                    _activeScenario = cur;
                    RefreshScenarioCardsUI();
                    Render3DIsometricMassing();
                    UpdateHudKpis();
                    UpdateCameraPosition();
                };

                _scenarioCardsContainer.Children.Add(c);
            }
        }

        private void Render3DIsometricMassing()
        {
            if (_modelVisual == null || _activeScenario == null) return;

            Model3DGroup buildingGroup = new Model3DGroup();

            // Draw Ground Site Polygon
            double siteW = _inputs.LotWidthFt;
            double siteD = _inputs.LotDepthFt;
            buildingGroup.Children.Add(CreateBox3D(0, 0, -1.0, siteW * 1.15, siteD * 1.15, 1.0, WpfColor.FromRgb(30, 41, 59)));

            // Draw Each Floor Slab
            foreach (MassingFloorBlock f in _activeScenario.Floors)
            {
                WpfColor fCol = (WpfColor)ColorConverter.ConvertFromString(f.ColorHex ?? "#3B82F6");
                double elevation = f.ElevationFt;
                double height = f.HeightFt;
                double width = f.WidthFt;
                double depth = f.DepthFt;
                double offsetX = f.OffsetXFt;
                double offsetY = f.OffsetYFt;

                GeometryModel3D floorModel = CreateBox3D(offsetX, offsetY, elevation, width, depth, height - 0.5, fCol);
                buildingGroup.Children.Add(floorModel);
            }

            _modelVisual.Content = buildingGroup;

            _txtPreviewTitle.Text = string.Format("{0} {1}", _activeScenario.Icon, _activeScenario.Title);
            _txtPreviewMetrics.Text = string.Format("Total ZFA: {0:N0} SF | {1} Floors ({2:N0} FT Total Height)\nBase: {3} FL | Dormers: {4} FL | Tower: {5} FL | Est. MIH: {6} Units",
                _activeScenario.TotalZfa, _activeScenario.TotalFloors, _activeScenario.TotalHeightFt,
                _activeScenario.PodiumFloors, _activeScenario.DormerFloors, _activeScenario.TowerFloors, _activeScenario.MihUnitsEstimate);
        }

        private GeometryModel3D CreateBox3D(double centerX, double centerY, double baseZ, double width, double depth, double height, WpfColor color)
        {
            double halfW = width / 2.0;
            double halfD = depth / 2.0;

            Point3D p0 = new Point3D(centerX - halfW, centerY - halfD, baseZ);
            Point3D p1 = new Point3D(centerX + halfW, centerY - halfD, baseZ);
            Point3D p2 = new Point3D(centerX + halfW, centerY + halfD, baseZ);
            Point3D p3 = new Point3D(centerX - halfW, centerY + halfD, baseZ);

            Point3D p4 = new Point3D(centerX - halfW, centerY - halfD, baseZ + height);
            Point3D p5 = new Point3D(centerX + halfW, centerY - halfD, baseZ + height);
            Point3D p6 = new Point3D(centerX + halfW, centerY + halfD, baseZ + height);
            Point3D p7 = new Point3D(centerX - halfW, centerY + halfD, baseZ + height);

            MeshGeometry3D mesh = new MeshGeometry3D();

            // Bottom
            AddQuad(mesh, p0, p3, p2, p1);
            // Top
            AddQuad(mesh, p4, p5, p6, p7);
            // Front (Street)
            AddQuad(mesh, p0, p1, p5, p4);
            // Back (Yard)
            AddQuad(mesh, p2, p3, p7, p6);
            // Right (Side)
            AddQuad(mesh, p1, p2, p6, p5);
            // Left (Side)
            AddQuad(mesh, p3, p0, p4, p7);

            MaterialGroup mat = new MaterialGroup();
            mat.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            mat.Children.Add(new SpecularMaterial(new SolidColorBrush(WpfColor.FromArgb(80, 255, 255, 255)), 20.0));

            return new GeometryModel3D(mesh, mat);
        }

        private void AddQuad(MeshGeometry3D mesh, Point3D p0, Point3D p1, Point3D p2, Point3D p3)
        {
            int baseIdx = mesh.Positions.Count;
            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 1);
            mesh.TriangleIndices.Add(baseIdx + 2);

            mesh.TriangleIndices.Add(baseIdx);
            mesh.TriangleIndices.Add(baseIdx + 2);
            mesh.TriangleIndices.Add(baseIdx + 3);
        }

        private void ExecuteBakeIntoRevit()
        {
            List<GenerativeScenario> toBake = _scenarios.Where(s => s.IsSelectedForBake).ToList();
            if (toBake.Count == 0)
            {
                MessageBox.Show("Please select at least one scenario checkbox to bake into Revit.", "BauTools Generative", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool createDO = _chkDesignOptions.IsChecked == true;
            bool createLevels = _chkCreateLevels.IsChecked == true;

            try
            {
                int shapes = _bakerService.BakeScenariosIntoDesignOptions(toBake, createDO, createLevels, "BauTools Generative Zoning");
                string msg = string.Format("Successfully baked {0} massing element(s) across {1} scenario(s) into Revit!", shapes, toBake.Count);
                MessageBox.Show(msg, "BauTools — Massing Bake Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error baking massing options: " + ex.Message, "Bake Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddSliderField(StackPanel parent, string label, double min, double max, double val, bool isInt, string fmt, Action<double> onVal)
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            
            WpfGrid hg = new WpfGrid();
            hg.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hg.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            WpfTextBlock lbl = new WpfTextBlock { Text = label, FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED) };
            WpfGrid.SetColumn(lbl, 0);
            hg.Children.Add(lbl);

            WpfTextBlock valBubble = new WpfTextBlock { Text = val.ToString(fmt), FontSize = 10, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_PRIMARY) };
            WpfGrid.SetColumn(valBubble, 1);
            hg.Children.Add(valBubble);

            sp.Children.Add(hg);

            WpfSlider sld = new WpfSlider
            {
                Minimum = min,
                Maximum = max,
                Value = val,
                IsSnapToTickEnabled = isInt,
                TickFrequency = isInt ? 1.0 : 0.25,
                Margin = new Thickness(0, 2, 0, 0)
            };

            sld.ValueChanged += (s, e) =>
            {
                double v = isInt ? Math.Round(sld.Value) : sld.Value;
                valBubble.Text = v.ToString(fmt);
                onVal(v);
            };

            sp.Children.Add(sld);
            parent.Children.Add(sp);
        }

        private Border CreateCard()
        {
            return new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14)
            };
        }

        private WpfButton CreatePrimaryButton(string text)
        {
            WpfButton btn = new WpfButton
            {
                Content = text,
                Background = new SolidColorBrush(COL_PRIMARY),
                Foreground = WpfBrushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            return btn;
        }
    }
}
```

### `ZoningFloorArea\Views\MainWindow.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;
using ZoningFloorArea.ViewModels;

// Aliases to avoid ambiguity between System.Windows and Autodesk.Revit.DB
using WpfGrid = System.Windows.Controls.Grid;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfVisibility = System.Windows.Visibility;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfBinding = System.Windows.Data.Binding;

namespace ZoningFloorArea.Views
{
    public class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        // Apple-style Neutral Palette
        private static readonly WpfColor COL_BG          = WpfColors.White;
        private static readonly WpfColor COL_SURFACE     = (WpfColor)ColorConverter.ConvertFromString("#F5F5F7");
        private static readonly WpfColor COL_TEXT_MAIN   = (WpfColor)ColorConverter.ConvertFromString("#1D1D1F");
        private static readonly WpfColor COL_TEXT_MUTED  = (WpfColor)ColorConverter.ConvertFromString("#86868B");
        private static readonly WpfColor COL_BORDER      = (WpfColor)ColorConverter.ConvertFromString("#D2D2D7");
        private static readonly WpfColor COL_BORDER_LIGHT= (WpfColor)ColorConverter.ConvertFromString("#E5E5EA");
        private static readonly WpfColor COL_PRIMARY     = (WpfColor)ColorConverter.ConvertFromString("#0071E3"); // Apple Blue
        private static readonly WpfColor COL_BTN_NEUTRAL = (WpfColor)ColorConverter.ConvertFromString("#E8E8ED");
        private static readonly WpfColor COL_BTN_HOVER   = (WpfColor)ColorConverter.ConvertFromString("#D1D1D6");
        private static readonly WpfColor COL_SUCCESS     = (WpfColor)ColorConverter.ConvertFromString("#34C759");
        private static readonly WpfColor COL_DANGER      = (WpfColor)ColorConverter.ConvertFromString("#FF3B30");

        // Color Palette for Popover
        private static readonly string[] COLOR_PALETTE = new string[]
        {
            "#0071E3", "#34C759", "#FF9500", "#AF52DE", "#FF2D55", "#5856D6", "#64748B", "#00C7BE"
        };

        // Step Panels
        private WpfButton[] _stepButtons;
        private Border[] _stepIndicatorBorders;
        private WpfGrid[] _stepPanels;
        private int _activeStepIndex = 0; // 0: Typical Floors, 1: Propagate, 2: Calculate, 3: Export

        // UI Controls for Dynamic Refresh
        private StackPanel _buildingTabBar;
        private StackPanel _typicalGroupsContainer;
        private StackPanel _towerContainer;
        private StackPanel _propagateSummaryContainer;
        private TabControl _tabControlBuildings;
        private StackPanel _step4PreviewContainer;
        private StackPanel _packagesContainer;
        private WpfTextBlock _step4SummaryBadge;
        private WpfTextBlock _txtStatus;

        // In-App Toast Container
        private Border _toastBorder;
        private WpfTextBlock _toastText;
        private DispatcherTimer _toastTimer;

        public MainWindow(Document doc) : this(new MainViewModel(doc))
        {
        }

        public MainWindow(MainViewModel vm)
        {
            _vm = vm;
            DataContext = _vm;

            Title = "BauTools — Zoning Floor Area Calculator";
            Width = 1260;
            Height = 860;
            MinWidth = 1050;
            MinHeight = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 13;

            _stepButtons = new WpfButton[4];
            _stepIndicatorBorders = new Border[4];
            _stepPanels = new WpfGrid[4];

            _vm.OnToastNotification = (msg, isError) => ShowToast(msg, isError);

            InitMinimalistStyles();
            BuildUI();
        }

        private void BuildUI()
        {
            WpfGrid rootGrid = new WpfGrid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Step Navigation Bar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2: Content Area
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Status & Navigation Footer

            // 0: Header
            UIElement header = CreateHeader();
            WpfGrid.SetRow(header, 0);
            rootGrid.Children.Add(header);

            // 1: Step Segmented Navigation
            UIElement stepper = CreateStepSegmentedBar();
            WpfGrid.SetRow(stepper, 1);
            rootGrid.Children.Add(stepper);

            // 2: Content Area (Houses all 4 step panels + Toast Host)
            WpfGrid contentHostGrid = new WpfGrid();

            Border contentHost = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(24, 16, 24, 16)
            };

            WpfGrid contentGrid = new WpfGrid();
            _stepPanels[0] = CreateStep1Panel(); // Typical Floors, Duplex & Buildings
            _stepPanels[1] = CreateStep2Panel(); // Propagate
            _stepPanels[2] = CreateStep3Panel(); // Calculate ZFA
            _stepPanels[3] = CreateStep4Panel(); // Master & Dependent Views, Sheets & Export

            for (int i = 0; i < 4; i++)
            {
                contentGrid.Children.Add(_stepPanels[i]);
            }

            contentHost.Child = contentGrid;
            contentHostGrid.Children.Add(contentHost);

            // Toast Floating Banner (Bottom-Right overlay)
            _toastBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 10, 16, 10),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 32, 24),
                Visibility = WpfVisibility.Collapsed,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = WpfColors.Black,
                    BlurRadius = 12,
                    Opacity = 0.15,
                    ShadowDepth = 3
                }
            };
            _toastText = new WpfTextBlock { FontWeight = FontWeights.SemiBold, FontSize = 12 };
            _toastBorder.Child = _toastText;
            contentHostGrid.Children.Add(_toastBorder);

            WpfGrid.SetRow(contentHostGrid, 2);
            rootGrid.Children.Add(contentHostGrid);

            // 3: Footer
            UIElement footer = CreateFooter();
            WpfGrid.SetRow(footer, 3);
            rootGrid.Children.Add(footer);

            Content = rootGrid;

            // Activate initial step
            SwitchToStep(0);
        }

        private void ShowToast(string message, bool isError)
        {
            if (_toastBorder == null || _toastText == null) return;

            Dispatcher.Invoke(() =>
            {
                _toastText.Text = (isError ? "⚠️ " : "✓ ") + message;
                _toastText.Foreground = isError ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#991B1B")) : new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#065F46"));
                _toastBorder.Background = isError ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#FEE2E2")) : new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#D1FAE5"));
                _toastBorder.BorderBrush = isError ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#FCA5A5")) : new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#6EE7B7"));
                _toastBorder.BorderThickness = new Thickness(1);
                _toastBorder.Visibility = WpfVisibility.Visible;

                if (_toastTimer != null) _toastTimer.Stop();
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
                _toastTimer.Tick += (s, e) =>
                {
                    _toastBorder.Visibility = WpfVisibility.Collapsed;
                    _toastTimer.Stop();
                };
                _toastTimer.Start();
            });
        }

        private UIElement CreateHeader()
        {
            Border header = new Border
            {
                Background = new SolidColorBrush(COL_BG),
                Padding = new Thickness(24, 14, 24, 12)
            };

            WpfGrid grid = new WpfGrid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Left: Title & Subtitle
            StackPanel titleStack = new StackPanel();
            
            StackPanel brandRow = new StackPanel { Orientation = WpfOrientation.Horizontal };
            brandRow.Children.Add(new WpfTextBlock
            {
                Text = "BauTools",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                VerticalAlignment = VerticalAlignment.Center
            });

            Border dot = new Border
            {
                Width = 4,
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            brandRow.Children.Add(dot);

            brandRow.Children.Add(new WpfTextBlock
            {
                Text = "Zoning Floor Area (ZFA) & Typical Floors Suite",
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            });
            titleStack.Children.Add(brandRow);

            WpfGrid.SetColumn(titleStack, 0);
            grid.Children.Add(titleStack);

            // Right: Developer Label
            WpfTextBlock devInfo = new WpfTextBlock
            {
                Text = "Arch Sergio Castro",
                FontSize = 12,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            };
            WpfGrid.SetColumn(devInfo, 1);
            grid.Children.Add(devInfo);

            header.Child = grid;
            return header;
        }

        private UIElement CreateStepSegmentedBar()
        {
            Border barContainer = new Border
            {
                Background = new SolidColorBrush(COL_BG),
                Padding = new Thickness(24, 0, 24, 12)
            };

            Border pill = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(3)
            };

            WpfGrid grid = new WpfGrid();
            for (int i = 0; i < 4; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            string[] stepTitles = new string[]
            {
                "1. Typical Floors & Buildings",
                "2. Propagate Areas",
                "3. Calculate ZFA Matrix",
                "4. Master Views, Sheets & Export"
            };

            for (int i = 0; i < 4; i++)
            {
                int stepIdx = i;
                Border stepBorder = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Background = WpfBrushes.Transparent,
                    Padding = new Thickness(0, 8, 0, 8)
                };

                WpfButton btn = new WpfButton
                {
                    Content = stepTitles[i],
                    Background = WpfBrushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                btn.Click += (s, e) => SwitchToStep(stepIdx);

                stepBorder.Child = btn;
                _stepIndicatorBorders[i] = stepBorder;
                _stepButtons[i] = btn;

                WpfGrid.SetColumn(stepBorder, i);
                grid.Children.Add(stepBorder);
            }

            pill.Child = grid;
            barContainer.Child = pill;
            return barContainer;
        }

        private void SwitchToStep(int stepIndex)
        {
            _activeStepIndex = stepIndex;
            _vm.CurrentStep = stepIndex + 1;

            for (int i = 0; i < 4; i++)
            {
                bool isActive = (i == stepIndex);
                _stepIndicatorBorders[i].Background = isActive ? WpfBrushes.White : WpfBrushes.Transparent;
                _stepButtons[i].Foreground = isActive ? new SolidColorBrush(COL_TEXT_MAIN) : new SolidColorBrush(COL_TEXT_MUTED);
                _stepButtons[i].FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Medium;
                _stepPanels[i].Visibility = isActive ? WpfVisibility.Visible : WpfVisibility.Collapsed;
            }

            // Trigger specific step refresh
            if (stepIndex == 0)
            {
                RefreshBuildingTabsUI();
                RefreshTypicalGroupsUI();
                RefreshTowerUI();
            }
            if (stepIndex == 1) RefreshPropagateReviewUI();
            if (stepIndex == 2) RefreshCalculateUI();
            if (stepIndex == 3) RefreshStep4PreviewUI();
        }

        // ══════════════════════════════════════════════════════════════
        // ── 3. STEP 1: TYPICAL FLOORS, DUPLEX & MULTI-BUILDINGS ──
        // ══════════════════════════════════════════════════════════════
        private WpfGrid CreateStep1Panel()
        {
            WpfGrid root = new WpfGrid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0: Building Selector Bar
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Row 1: Main 2-Column Area

            // ── Row 0: Building Selector Bar ──
            Border bldgBarHost = new Border
            {
                Background = WpfBrushes.White,
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 14)
            };

            WpfGrid bldgBarGrid = new WpfGrid();
            bldgBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Label
            bldgBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Tabs
            bldgBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Actions

            WpfTextBlock lblBldgs = new WpfTextBlock
            {
                Text = "PROJECT BUILDINGS:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            WpfGrid.SetColumn(lblBldgs, 0);
            bldgBarGrid.Children.Add(lblBldgs);

            _buildingTabBar = new StackPanel { Orientation = WpfOrientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            WpfGrid.SetColumn(_buildingTabBar, 1);
            bldgBarGrid.Children.Add(_buildingTabBar);

            // Add Building Button
            WpfButton btnAddBldg = CreateNeutralButton("＋ Add Building");
            btnAddBldg.Height = 28;
            btnAddBldg.Padding = new Thickness(12, 0, 12, 0);
            btnAddBldg.Click += (s, e) => ShowAddBuildingDialog();
            WpfGrid.SetColumn(btnAddBldg, 2);
            bldgBarGrid.Children.Add(btnAddBldg);

            bldgBarHost.Child = bldgBarGrid;
            WpfGrid.SetRow(bldgBarHost, 0);
            root.Children.Add(bldgBarHost);

            // ── Row 1: 2-Column Split (Left: Cards, Right: Visual Tower & Settings) ──
            WpfGrid cols = new WpfGrid();
            cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) }); // Left: Groups list
            cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });                    // Gap
            cols.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });   // Right: Tower Strip & Settings

            // ── Left Card: Typical Floor Groups ──
            Border leftCard = CreateCard();
            WpfGrid leftLayout = new WpfGrid();
            leftLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            leftLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Groups
            leftLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Actions

            // Group List Header
            WpfGrid headerRow = new WpfGrid { Margin = new Thickness(0, 0, 0, 14) };
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titleStack = new StackPanel();
            titleStack.Children.Add(new WpfTextBlock
            {
                Text = "Typical Floor Definitions",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            titleStack.Children.Add(new WpfTextBlock
            {
                Text = "Configure source floors (Single, Typical, or Duplex 2-Story modules). Overlaps are strictly prevented.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 2, 0, 0)
            });
            WpfGrid.SetColumn(titleStack, 0);
            headerRow.Children.Add(titleStack);

            StackPanel hdrBtnStack = new StackPanel { Orientation = WpfOrientation.Horizontal };

            WpfButton btnCopySetup = CreateNeutralButton("📑 Copy from...");
            btnCopySetup.Height = 32;
            btnCopySetup.Padding = new Thickness(12, 0, 12, 0);
            btnCopySetup.Margin = new Thickness(0, 0, 8, 0);
            btnCopySetup.ToolTip = "Copy typical floor groups from another building into this building";
            btnCopySetup.Click += (s, e) =>
            {
                if (_vm.Buildings.Count <= 1 || _vm.SelectedBuilding == null)
                {
                    _vm.TriggerToast("Add another building first to copy from.", true);
                    return;
                }
                ContextMenu cm = new ContextMenu();
                foreach (BuildingDefinition other in _vm.Buildings)
                {
                    if (other == _vm.SelectedBuilding) continue;
                    BuildingDefinition src = other;
                    MenuItem mi = new MenuItem { Header = string.Format("Copy from '{0}' ({1} groups)", src.Name, src.TypicalGroups.Count) };
                    mi.Click += (ms, me) =>
                    {
                        MessageBoxResult confirm = MessageBox.Show(
                            string.Format("Replace all typical floor groups in '{0}' with those from '{1}'?", _vm.SelectedBuilding.Name, src.Name),
                            "Confirm Copy Setup",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        if (confirm == MessageBoxResult.Yes)
                        {
                            _vm.CopyGroupsFromBuilding(_vm.SelectedBuilding, src);
                            RefreshTypicalGroupsUI();
                            RefreshTowerUI();
                        }
                    };
                    cm.Items.Add(mi);
                }
                cm.PlacementTarget = btnCopySetup;
                cm.IsOpen = true;
            };
            hdrBtnStack.Children.Add(btnCopySetup);

            WpfButton btnAdd = CreateNeutralButton("+ Add Typical Floor");
            btnAdd.Height = 32;
            btnAdd.Padding = new Thickness(14, 0, 14, 0);
            btnAdd.Click += (s, e) =>
            {
                _vm.AddTypicalGroup();
                RefreshTypicalGroupsUI();
                RefreshTowerUI();
            };
            hdrBtnStack.Children.Add(btnAdd);

            WpfGrid.SetColumn(hdrBtnStack, 1);
            headerRow.Children.Add(hdrBtnStack);

            WpfGrid.SetRow(headerRow, 0);
            leftLayout.Children.Add(headerRow);

            // Scrollable List of Group Cards
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _typicalGroupsContainer = new StackPanel();
            scroll.Content = _typicalGroupsContainer;
            WpfGrid.SetRow(scroll, 1);
            leftLayout.Children.Add(scroll);

            // Action Row
            WpfGrid actionRow = new WpfGrid { Margin = new Thickness(0, 14, 0, 0) };
            actionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            WpfButton btnSave = CreatePrimaryButton("Save to Revit Model");
            btnSave.Height = 36;
            btnSave.Padding = new Thickness(20, 0, 20, 0);
            btnSave.Click += (s, e) => _vm.SaveTypicalGroups();
            WpfGrid.SetColumn(btnSave, 1);
            actionRow.Children.Add(btnSave);

            WpfGrid.SetRow(actionRow, 2);
            leftLayout.Children.Add(actionRow);

            leftCard.Child = leftLayout;
            WpfGrid.SetColumn(leftCard, 0);
            cols.Children.Add(leftCard);

            // ── Right Card: Visual Tower Strip & Building Settings ──
            Border rightCard = CreateCard();
            WpfGrid rightLayout = new WpfGrid();
            rightLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Tower Header
            rightLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Tower Strip
            rightLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Building Scope Box & Scheme Mapping

            // Tower Header
            StackPanel towerHeader = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            towerHeader.Children.Add(new WpfTextBlock
            {
                Text = "Visual Building Tower",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            towerHeader.Children.Add(new WpfTextBlock
            {
                Text = "Live elevation diagram showing level assignments and duplex cycles top to bottom.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            });
            WpfGrid.SetRow(towerHeader, 0);
            rightLayout.Children.Add(towerHeader);

            // Scrollable Tower Strip
            ScrollViewer towerScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 0, 0, 12) };
            _towerContainer = new StackPanel();
            towerScroll.Content = _towerContainer;
            WpfGrid.SetRow(towerScroll, 1);
            rightLayout.Children.Add(towerScroll);

            // Building Scope Box & Scheme Settings Bottom Box
            Border schemeBox = new Border
            {
                Background = new SolidColorBrush(COL_SURFACE),
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10)
            };
            StackPanel schemeStack = new StackPanel();

            // Building Scope Box Row
            WpfGrid bldgScopeRow = new WpfGrid { Margin = new Thickness(0, 0, 0, 8) };
            bldgScopeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bldgScopeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel sbStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            sbStack.Children.Add(new WpfTextBlock { Text = "Building Scope Box (Crop):", FontSize = 10, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
            WpfComboBox cScope = new WpfComboBox { Height = 26, ItemsSource = _vm.AvailableScopeBoxes };
            if (_vm.SelectedBuilding != null) cScope.SelectedItem = _vm.SelectedBuilding.ScopeBoxName;
            cScope.SelectionChanged += (s, e) =>
            {
                if (cScope.SelectedItem != null && _vm.SelectedBuilding != null)
                {
                    _vm.SelectedBuilding.ScopeBoxName = cScope.SelectedItem.ToString();
                }
            };
            sbStack.Children.Add(cScope);
            WpfGrid.SetColumn(sbStack, 0);
            bldgScopeRow.Children.Add(sbStack);

            // Lot Area
            StackPanel sLot = new StackPanel();
            sLot.Children.Add(new WpfTextBlock { Text = "Lot Area (SF):", FontSize = 10, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
            WpfTextBox tLot = new WpfTextBox { Height = 26, VerticalContentAlignment = VerticalAlignment.Center };
            tLot.SetBinding(WpfTextBox.TextProperty, new WpfBinding("Config.LotArea") { Source = _vm, Mode = BindingMode.TwoWay });
            sLot.Children.Add(tLot);
            WpfGrid.SetColumn(sLot, 1);
            bldgScopeRow.Children.Add(sLot);

            schemeStack.Children.Add(bldgScopeRow);

            // Schemes Row
            WpfGrid schemeRow = new WpfGrid();
            schemeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            schemeRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel sGross = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
            sGross.Children.Add(new WpfTextBlock { Text = "Gross Scheme:", FontSize = 10, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
            WpfComboBox cGross = new WpfComboBox { Height = 26, ItemsSource = _vm.AreaSchemes };
            cGross.SetBinding(WpfComboBox.SelectedItemProperty, new WpfBinding("Config.GrossAreaSchemeName") { Source = _vm, Mode = BindingMode.TwoWay });
            sGross.Children.Add(cGross);
            WpfGrid.SetColumn(sGross, 0);
            schemeRow.Children.Add(sGross);

            StackPanel sDed = new StackPanel();
            sDed.Children.Add(new WpfTextBlock { Text = "Deduction Scheme:", FontSize = 10, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
            WpfComboBox cDed = new WpfComboBox { Height = 26, ItemsSource = _vm.AreaSchemes };
            cDed.SetBinding(WpfComboBox.SelectedItemProperty, new WpfBinding("Config.DeductionAreaSchemeName") { Source = _vm, Mode = BindingMode.TwoWay });
            sDed.Children.Add(cDed);
            WpfGrid.SetColumn(sDed, 1);
            schemeRow.Children.Add(sDed);

            schemeStack.Children.Add(schemeRow);
            schemeBox.Child = schemeStack;
            WpfGrid.SetRow(schemeBox, 2);
            rightLayout.Children.Add(schemeBox);

            rightCard.Child = rightLayout;
            WpfGrid.SetColumn(rightCard, 2);
            cols.Children.Add(rightCard);

            WpfGrid.SetRow(cols, 1);
            root.Children.Add(cols);

            return root;
        }

        private void RefreshBuildingTabsUI()
        {
            if (_buildingTabBar == null) return;
            _buildingTabBar.Children.Clear();

            foreach (BuildingDefinition bldg in _vm.Buildings)
            {
                BuildingDefinition currentBldg = bldg;
                bool isSelected = (_vm.SelectedBuilding == currentBldg);

                Border tabPill = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    Background = isSelected ? new SolidColorBrush(COL_PRIMARY) : new SolidColorBrush(COL_SURFACE),
                    BorderBrush = isSelected ? new SolidColorBrush(COL_PRIMARY) : new SolidColorBrush(COL_BORDER),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(12, 4, 12, 4),
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                StackPanel tabContent = new StackPanel { Orientation = WpfOrientation.Horizontal };

                // Building Name TextBlock (Double click to edit inline)
                WpfTextBlock txtBldgName = new WpfTextBlock
                {
                    Text = "🏢 " + currentBldg.Name,
                    FontWeight = isSelected ? FontWeights.Bold : FontWeights.Medium,
                    FontSize = 12,
                    Foreground = isSelected ? WpfBrushes.White : new SolidColorBrush(COL_TEXT_MAIN),
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = "Double-click to rename building"
                };

                // Inline rename textbox
                WpfTextBox txtEditName = new WpfTextBox
                {
                    Text = currentBldg.Name,
                    FontSize = 11.5,
                    Height = 22,
                    Padding = new Thickness(4, 0, 4, 0),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Visibility = WpfVisibility.Collapsed
                };

                Action finishRename = () =>
                {
                    if (!string.IsNullOrWhiteSpace(txtEditName.Text))
                    {
                        currentBldg.Name = txtEditName.Text.Trim();
                        txtBldgName.Text = "🏢 " + currentBldg.Name;
                    }
                    txtEditName.Visibility = WpfVisibility.Collapsed;
                    txtBldgName.Visibility = WpfVisibility.Visible;
                };

                txtEditName.LostFocus += (s, e) => finishRename();
                txtEditName.KeyDown += (s, e) =>
                {
                    if (e.Key == System.Windows.Input.Key.Enter) finishRename();
                    else if (e.Key == System.Windows.Input.Key.Escape)
                    {
                        txtEditName.Visibility = WpfVisibility.Collapsed;
                        txtBldgName.Visibility = WpfVisibility.Visible;
                    }
                };

                txtBldgName.MouseDown += (s, e) =>
                {
                    if (e.ClickCount == 2)
                    {
                        txtBldgName.Visibility = WpfVisibility.Collapsed;
                        txtEditName.Visibility = WpfVisibility.Visible;
                        txtEditName.Focus();
                        txtEditName.SelectAll();
                        e.Handled = true;
                    }
                };

                tabContent.Children.Add(txtBldgName);
                tabContent.Children.Add(txtEditName);

                // Small delete button if more than 1 building
                if (_vm.Buildings.Count > 1)
                {
                    WpfButton btnDelBldg = new WpfButton
                    {
                        Content = "✕",
                        Width = 16,
                        Height = 16,
                        Margin = new Thickness(8, 0, 0, 0),
                        FontSize = 9,
                        FontWeight = FontWeights.Bold,
                        Background = WpfBrushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Foreground = isSelected ? WpfBrushes.White : new SolidColorBrush(COL_TEXT_MUTED),
                        ToolTip = "Delete building"
                    };
                    btnDelBldg.Click += (s, e) =>
                    {
                        e.Handled = true;
                        _vm.RemoveBuilding(currentBldg);
                        RefreshBuildingTabsUI();
                        RefreshTypicalGroupsUI();
                        RefreshTowerUI();
                    };
                    tabContent.Children.Add(btnDelBldg);
                }

                tabPill.MouseLeftButtonDown += (s, e) =>
                {
                    if (txtEditName.Visibility == WpfVisibility.Visible) return;
                    _vm.SelectedBuilding = currentBldg;
                    RefreshBuildingTabsUI();
                    RefreshTypicalGroupsUI();
                    RefreshTowerUI();
                };

                tabPill.Child = tabContent;
                _buildingTabBar.Children.Add(tabPill);
            }
        }

        private void RefreshTypicalGroupsUI()
        {
            if (_typicalGroupsContainer == null) return;
            _typicalGroupsContainer.Children.Clear();

            if (_vm.SelectedBuilding == null || _vm.SelectedBuilding.TypicalGroups.Count == 0)
            {
                _typicalGroupsContainer.Children.Add(new WpfTextBlock
                {
                    Text = "No Typical Floor groups defined for this building yet.\nClick '+ Add Typical Floor' above to start.",
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    Margin = new Thickness(0, 20, 0, 0),
                    FontSize = 12
                });
                return;
            }

            foreach (TypicalFloorGroup group in _vm.SelectedBuilding.TypicalGroups)
            {
                TypicalFloorGroup currentGroup = group;
                Border card = new Border
                {
                    Background = new SolidColorBrush(COL_SURFACE),
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                StackPanel cardLayout = new StackPanel();

                // ── Row 1: Top Bar (Color Chip + Name + Single / Duplex Toggles + Delete) ──
                WpfGrid topBar = new WpfGrid { Margin = new Thickness(0, 0, 0, 8) };
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) }); // Color Popover Chip
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // Name
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Single Toggle
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Duplex Toggle
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Badge
                topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) }); // Delete

                // 1. Color Popover Chip
                Border colorBadge = new Border
                {
                    Width = 18,
                    Height = 18,
                    CornerRadius = new CornerRadius(9),
                    Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(currentGroup.ColorHex ?? "#0071E3")),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    ToolTip = "Click to pick color"
                };

                // Popover Popup for Color Picker
                Popup colorPopup = new Popup
                {
                    PlacementTarget = colorBadge,
                    Placement = PlacementMode.Bottom,
                    StaysOpen = false,
                    AllowsTransparency = true
                };

                Border popupBorder = new Border
                {
                    Background = WpfBrushes.White,
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6),
                    Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 8, Opacity = 0.15, ShadowDepth = 2 }
                };

                UniformGrid colorGrid = new UniformGrid { Columns = 4, Rows = 2 };
                foreach (string hex in COLOR_PALETTE)
                {
                    string currentHex = hex;
                    Border chip = new Border
                    {
                        Width = 20,
                        Height = 20,
                        CornerRadius = new CornerRadius(10),
                        Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(currentHex)),
                        Margin = new Thickness(3),
                        Cursor = System.Windows.Input.Cursors.Hand
                    };
                    chip.MouseLeftButtonDown += (s, e) =>
                    {
                        currentGroup.ColorHex = currentHex;
                        colorBadge.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(currentHex));
                        colorPopup.IsOpen = false;
                        RefreshTowerUI();
                    };
                    colorGrid.Children.Add(chip);
                }
                popupBorder.Child = colorGrid;
                colorPopup.Child = popupBorder;

                colorBadge.MouseLeftButtonDown += (s, e) => colorPopup.IsOpen = true;

                WpfGrid.SetColumn(colorBadge, 0);
                topBar.Children.Add(colorBadge);

                // 2. Group Name
                WpfTextBox txtName = new WpfTextBox
                {
                    Text = currentGroup.Name,
                    Height = 26,
                    BorderBrush = new SolidColorBrush(COL_BORDER),
                    Padding = new Thickness(6, 2, 6, 2),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                txtName.TextChanged += (s, e) =>
                {
                    currentGroup.Name = txtName.Text;
                    RefreshTowerUI();
                };
                WpfGrid.SetColumn(txtName, 1);
                topBar.Children.Add(txtName);

                // 3. Single Floor CheckBox
                WpfCheckBox chkSingle = new WpfCheckBox
                {
                    Content = "Single",
                    FontSize = 10.5,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0),
                    IsChecked = currentGroup.IsSingleFloorOnly
                };

                // 4. Duplex Module CheckBox
                WpfCheckBox chkDuplex = new WpfCheckBox
                {
                    Content = "Duplex (2-Story)",
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#4F46E5")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0),
                    IsChecked = currentGroup.IsDuplexModule
                };

                // 5. Status Badge Pill
                Border badgePill = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(7, 2, 7, 2),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                WpfTextBlock badgeText = new WpfTextBlock { FontSize = 9.5, FontWeight = FontWeights.Bold };
                badgePill.Child = badgeText;

                Action updateBadge = () =>
                {
                    if (currentGroup.IsSingleLevel)
                    {
                        badgePill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#E6F4EA"));
                        badgeText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#137333"));
                        badgeText.Text = "⭐ SINGLE";
                    }
                    else if (currentGroup.IsDuplexModule)
                    {
                        badgePill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#EEF2FF"));
                        badgeText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#4F46E5"));
                        badgeText.Text = "🏢 DUPLEX (2-STORY)";
                    }
                    else
                    {
                        badgePill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#E8F0FE"));
                        badgeText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1A73E8"));
                        badgeText.Text = "🔄 TYPICAL";
                    }
                    RefreshTowerUI();
                };
                updateBadge();

                WpfGrid.SetColumn(chkSingle, 2);
                topBar.Children.Add(chkSingle);

                WpfGrid.SetColumn(chkDuplex, 3);
                topBar.Children.Add(chkDuplex);

                WpfGrid.SetColumn(badgePill, 4);
                topBar.Children.Add(badgePill);

                // 6. Delete Button
                WpfButton btnDel = CreateDangerButton("✕");
                btnDel.Width = 22;
                btnDel.Height = 22;
                btnDel.Padding = new Thickness(0);
                btnDel.VerticalAlignment = VerticalAlignment.Center;
                btnDel.ToolTip = "Delete group";
                btnDel.Click += (s, e) =>
                {
                    _vm.RemoveTypicalGroup(currentGroup);
                    RefreshTypicalGroupsUI();
                    RefreshTowerUI();
                };
                WpfGrid.SetColumn(btnDel, 5);
                topBar.Children.Add(btnDel);

                cardLayout.Children.Add(topBar);

                // ── Row 2: Levels Selectors (Standard vs Duplex) ──
                WpfGrid levelsRow = new WpfGrid();
                levelsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }); // Source(s)
                levelsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // From
                levelsRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // To

                // Source Column (Standard single source OR Duplex Lower & Upper sources)
                StackPanel srcStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };

                // Standard Source Box
                StackPanel stdSrcBox = new StackPanel { Visibility = currentGroup.IsDuplexModule ? WpfVisibility.Collapsed : WpfVisibility.Visible };
                stdSrcBox.Children.Add(new WpfTextBlock { Text = "Source Level (Modeled):", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
                WpfComboBox comboSrc = new WpfComboBox { Height = 26 };
                WpfTextBlock srcStatus = new WpfTextBlock { FontSize = 9, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(2, 2, 0, 0), Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelName) };
                ConfigureLevelComboBox(comboSrc, currentGroup, currentGroup.SourceLevelName, (lvlName) =>
                {
                    currentGroup.SourceLevelName = lvlName;
                    srcStatus.Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelName);
                    updateBadge();
                    RefreshTypicalGroupsUI();
                });
                stdSrcBox.Children.Add(comboSrc);
                stdSrcBox.Children.Add(srcStatus);
                srcStack.Children.Add(stdSrcBox);

                // Duplex Lower & Upper Sources Box
                StackPanel duplexSrcBox = new StackPanel { Visibility = currentGroup.IsDuplexModule ? WpfVisibility.Visible : WpfVisibility.Collapsed };
                
                // Lower Level
                duplexSrcBox.Children.Add(new WpfTextBlock { Text = "Duplex Lower (Social/Access):", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
                WpfComboBox comboLower = new WpfComboBox { Height = 26 };
                WpfTextBlock lowerStatus = new WpfTextBlock { FontSize = 9, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(2, 1, 0, 4), Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelNameLower) };
                WpfTextBlock upperStatus = new WpfTextBlock { FontSize = 9, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(2, 1, 0, 0), Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelNameUpper) };

                ConfigureLevelComboBox(comboLower, currentGroup, currentGroup.SourceLevelNameLower, (lvlName) =>
                {
                    currentGroup.SourceLevelNameLower = lvlName;
                    string autoUpper = _vm.GetNextLevelAbove(lvlName);
                    if (!string.IsNullOrEmpty(autoUpper))
                    {
                        currentGroup.SourceLevelNameUpper = autoUpper;
                    }
                    lowerStatus.Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelNameLower);
                    upperStatus.Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelNameUpper);
                    updateBadge();
                    RefreshTypicalGroupsUI();
                });
                duplexSrcBox.Children.Add(comboLower);
                duplexSrcBox.Children.Add(lowerStatus);

                // Upper Level (Auto-paired)
                duplexSrcBox.Children.Add(new WpfTextBlock { Text = "Duplex Upper (Bedrooms/Void — Auto Paired):", FontSize = 9.5, Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#4F46E5")), Margin = new Thickness(0, 0, 0, 2) });
                WpfComboBox comboUpper = new WpfComboBox { Height = 26 };
                ConfigureLevelComboBox(comboUpper, currentGroup, currentGroup.SourceLevelNameUpper, (lvlName) =>
                {
                    currentGroup.SourceLevelNameUpper = lvlName;
                    upperStatus.Text = _vm.GetSourceLevelSummary(currentGroup.SourceLevelNameUpper);
                    updateBadge();
                    RefreshTypicalGroupsUI();
                });
                duplexSrcBox.Children.Add(comboUpper);
                duplexSrcBox.Children.Add(upperStatus);

                srcStack.Children.Add(duplexSrcBox);

                WpfGrid.SetColumn(srcStack, 0);
                levelsRow.Children.Add(srcStack);

                // From & To Dropdowns
                WpfComboBox comboFrom = new WpfComboBox { Height = 26, IsEnabled = !currentGroup.IsSingleFloorOnly };
                WpfComboBox comboTo = new WpfComboBox { Height = 26, IsEnabled = !currentGroup.IsSingleFloorOnly };

                // From Level Stack
                StackPanel fromStack = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
                fromStack.Children.Add(new WpfTextBlock { Text = "Range: From", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
                ConfigureLevelComboBox(comboFrom, currentGroup, currentGroup.FromLevelName, (fromVal) =>
                {
                    LevelPickerItem toItem = comboTo.SelectedItem as LevelPickerItem;
                    string toVal = toItem != null ? toItem.LevelName : currentGroup.ToLevelName;
                    bool valid = _vm.ValidateAndApplyRange(currentGroup, fromVal, toVal);
                    if (valid)
                    {
                        updateBadge();
                        RefreshTypicalGroupsUI();
                    }
                });
                fromStack.Children.Add(comboFrom);
                WpfGrid.SetColumn(fromStack, 1);
                levelsRow.Children.Add(fromStack);

                // To Level Stack
                StackPanel toStack = new StackPanel();
                toStack.Children.Add(new WpfTextBlock { Text = "Range: To", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
                ConfigureLevelComboBox(comboTo, currentGroup, currentGroup.ToLevelName, (toVal) =>
                {
                    LevelPickerItem fromItem = comboFrom.SelectedItem as LevelPickerItem;
                    string fromVal = fromItem != null ? fromItem.LevelName : currentGroup.FromLevelName;
                    bool valid = _vm.ValidateAndApplyRange(currentGroup, fromVal, toVal);
                    if (valid)
                    {
                        updateBadge();
                        RefreshTypicalGroupsUI();
                    }
                });
                toStack.Children.Add(comboTo);
                WpfGrid.SetColumn(toStack, 2);
                levelsRow.Children.Add(toStack);

                chkSingle.Checked += (s, e) =>
                {
                    currentGroup.IsSingleFloorOnly = true;
                    chkDuplex.IsChecked = false;
                    currentGroup.FromLevelName = currentGroup.SourceLevelName;
                    currentGroup.ToLevelName = currentGroup.SourceLevelName;
                    comboFrom.IsEnabled = false;
                    comboTo.IsEnabled = false;
                    updateBadge();
                    RefreshTypicalGroupsUI();
                };
                chkSingle.Unchecked += (s, e) =>
                {
                    currentGroup.IsSingleFloorOnly = false;
                    comboFrom.IsEnabled = true;
                    comboTo.IsEnabled = true;
                    updateBadge();
                    RefreshTypicalGroupsUI();
                };

                chkDuplex.Checked += (s, e) =>
                {
                    currentGroup.IsDuplexModule = true;
                    chkSingle.IsChecked = false;
                    string autoUpper = _vm.GetNextLevelAbove(currentGroup.SourceLevelNameLower);
                    if (!string.IsNullOrEmpty(autoUpper))
                    {
                        currentGroup.SourceLevelNameUpper = autoUpper;
                    }
                    stdSrcBox.Visibility = WpfVisibility.Collapsed;
                    duplexSrcBox.Visibility = WpfVisibility.Visible;
                    updateBadge();
                    RefreshTypicalGroupsUI();
                };
                chkDuplex.Unchecked += (s, e) =>
                {
                    currentGroup.IsDuplexModule = false;
                    stdSrcBox.Visibility = WpfVisibility.Visible;
                    duplexSrcBox.Visibility = WpfVisibility.Collapsed;
                    updateBadge();
                    RefreshTypicalGroupsUI();
                };

                cardLayout.Children.Add(levelsRow);

                // ── Row 3: Stacking Quick Action Toolbar (Shift Up/Down & Expand/Contract) ──
                if (!currentGroup.IsSingleFloorOnly)
                {
                    Border quickBar = new Border
                    {
                        Background = new SolidColorBrush(COL_BG),
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(8, 4, 8, 4),
                        Margin = new Thickness(0, 8, 0, 0)
                    };

                    WpfGrid qGrid = new WpfGrid();
                    qGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Label
                    qGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Spacer
                    qGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Shift buttons
                    qGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Expand buttons

                    WpfTextBlock lblStack = new WpfTextBlock
                    {
                        Text = "⚡ STACKING CONTROLS:",
                        FontSize = 9.5,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    WpfGrid.SetColumn(lblStack, 0);
                    qGrid.Children.Add(lblStack);

                    // Shift Group Up/Down
                    StackPanel shiftStack = new StackPanel { Orientation = WpfOrientation.Horizontal, Margin = new Thickness(0, 0, 10, 0) };
                    
                    WpfButton btnShiftDown = CreateMicroButton("▼ Shift Down");
                    btnShiftDown.Height = 22;
                    btnShiftDown.FontSize = 10;
                    btnShiftDown.Margin = new Thickness(0, 0, 4, 0);
                    btnShiftDown.ToolTip = "Shift entire group down by 1 level (maintains floor count and height)";
                    btnShiftDown.Click += (s, e) =>
                    {
                        bool ok = _vm.ShiftGroupRange(currentGroup, -1);
                        if (ok) RefreshTypicalGroupsUI();
                    };
                    shiftStack.Children.Add(btnShiftDown);

                    WpfButton btnShiftUp = CreateMicroButton("▲ Shift Up");
                    btnShiftUp.Height = 22;
                    btnShiftUp.FontSize = 10;
                    btnShiftUp.ToolTip = "Shift entire group up by 1 level (maintains floor count and height)";
                    btnShiftUp.Click += (s, e) =>
                    {
                        bool ok = _vm.ShiftGroupRange(currentGroup, +1);
                        if (ok) RefreshTypicalGroupsUI();
                    };
                    shiftStack.Children.Add(btnShiftUp);

                    WpfGrid.SetColumn(shiftStack, 2);
                    qGrid.Children.Add(shiftStack);

                    // Expand / Contract Top Level
                    StackPanel expStack = new StackPanel { Orientation = WpfOrientation.Horizontal };

                    WpfButton btnContract = CreateMicroButton("− 1 Floor");
                    btnContract.Height = 22;
                    btnContract.FontSize = 10;
                    btnContract.Margin = new Thickness(0, 0, 4, 0);
                    btnContract.ToolTip = "Contract group by removing the top floor (frees the level above)";
                    btnContract.Click += (s, e) =>
                    {
                        bool ok = _vm.ExpandOrContractGroup(currentGroup, -1);
                        if (ok) RefreshTypicalGroupsUI();
                    };
                    expStack.Children.Add(btnContract);

                    WpfButton btnExpand = CreateMicroButton("+ 1 Floor");
                    btnExpand.Height = 22;
                    btnExpand.FontSize = 10;
                    btnExpand.ToolTip = "Expand group by adding the next free floor at the top";
                    btnExpand.Click += (s, e) =>
                    {
                        bool ok = _vm.ExpandOrContractGroup(currentGroup, +1);
                        if (ok) RefreshTypicalGroupsUI();
                    };
                    expStack.Children.Add(btnExpand);

                    WpfGrid.SetColumn(expStack, 3);
                    qGrid.Children.Add(expStack);

                    quickBar.Child = qGrid;
                    cardLayout.Children.Add(quickBar);
                }

                card.Child = cardLayout;
                _typicalGroupsContainer.Children.Add(card);
            }
        }

        private void RefreshTowerUI()
        {
            if (_towerContainer == null) return;
            _towerContainer.Children.Clear();

            _vm.RefreshTowerLevels();

            // Live Gaps / Allocation Indicator
            List<string> gaps = _vm.GetUnassignedGaps();
            Border gapBanner = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 8)
            };
            if (gaps.Count == 0)
            {
                gapBanner.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#ECFDF5"));
                gapBanner.BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#A7F3D0"));
                gapBanner.BorderThickness = new Thickness(1);
                gapBanner.Child = new WpfTextBlock
                {
                    Text = "🟢 100% Floor Area Allocated (No Gaps)",
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#065F46"))
                };
            }
            else
            {
                gapBanner.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#FFFBEB"));
                gapBanner.BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#FDE68A"));
                gapBanner.BorderThickness = new Thickness(1);
                gapBanner.Child = new WpfTextBlock
                {
                    Text = string.Format("⚠️ {0} Unassigned Level(s): {1}", gaps.Count, string.Join(", ", gaps.Take(3).ToArray()) + (gaps.Count > 3 ? "..." : "")),
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#92400E")),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
            }
            _towerContainer.Children.Add(gapBanner);

            foreach (LevelTowerItem lvl in _vm.TowerLevels)
            {
                Border levelRow = new Border
                {
                    Background = WpfBrushes.White,
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                WpfGrid rowGrid = new WpfGrid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) }); // Elevation
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Level Name
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) }); // Assignment Badge

                // Elevation
                WpfTextBlock txtElev = new WpfTextBlock
                {
                    Text = lvl.ElevationDisplay,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    VerticalAlignment = VerticalAlignment.Center
                };
                WpfGrid.SetColumn(txtElev, 0);
                rowGrid.Children.Add(txtElev);

                // Level Name
                WpfTextBlock txtName = new WpfTextBlock
                {
                    Text = lvl.LevelName,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                    VerticalAlignment = VerticalAlignment.Center
                };
                WpfGrid.SetColumn(txtName, 1);
                rowGrid.Children.Add(txtName);

                // Assignment Pill
                Border assignPill = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 2, 8, 2),
                    VerticalAlignment = VerticalAlignment.Center
                };

                if (lvl.IsAssigned)
                {
                    assignPill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(lvl.ColorHex));
                    string badgeLabel = lvl.IsSingleFloor ? string.Format("⭐ {0}", lvl.AssignedGroupName) : string.Format("🔄 {0}", lvl.AssignedGroupName);
                    assignPill.Child = new WpfTextBlock
                    {
                        Text = badgeLabel,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = WpfBrushes.White,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                }
                else
                {
                    assignPill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F1F5F9"));
                    assignPill.Child = new WpfTextBlock
                    {
                        Text = "⚪ Unassigned",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(COL_TEXT_MUTED)
                    };
                }

                WpfGrid.SetColumn(assignPill, 2);
                rowGrid.Children.Add(assignPill);

                levelRow.Child = rowGrid;
                _towerContainer.Children.Add(levelRow);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // ── 4. STEP 2: PROPAGATE (Review & Execute) ──
        // ══════════════════════════════════════════════════════════════
        private WpfGrid CreateStep2Panel()
        {
            WpfGrid grid = new WpfGrid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Border card = CreateCard();
            WpfGrid cardLayout = new WpfGrid();
            cardLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title
            cardLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // View Preservation Alert
            cardLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Options
            cardLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Summary List

            // Title
            StackPanel titleStack = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
            titleStack.Children.Add(new WpfTextBlock
            {
                Text = "Area Propagation Preview (All Buildings)",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            titleStack.Children.Add(new WpfTextBlock
            {
                Text = "Duplicates Area Boundary Lines and Area calculation elements from source floors to target levels without altering view setups.",
                FontSize = 12,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 2, 0, 0)
            });
            WpfGrid.SetRow(titleStack, 0);
            cardLayout.Children.Add(titleStack);

            // View Preservation Alert
            Border alertBox = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#EFF6FF")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#BFDBFE")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 14)
            };
            WpfTextBlock alertText = new WpfTextBlock
            {
                Text = "🛡️ Non-Destructive Propagation: BauTools only copies Area Boundary Lines and Area calculations into existing Revit floor views. It never deletes or duplicates ViewPlans.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1E40AF")),
                TextWrapping = TextWrapping.Wrap
            };
            alertBox.Child = alertText;
            WpfGrid.SetRow(alertBox, 1);
            cardLayout.Children.Add(alertBox);

            // Scheme Checkboxes
            StackPanel optionsPanel = new StackPanel { Orientation = WpfOrientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            
            WpfCheckBox chkGross = new WpfCheckBox
            {
                Content = "Propagate Gross Building Areas",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 24, 0),
                IsChecked = _vm.PropagateGrossArea
            };
            chkGross.Checked += (s, e) => _vm.PropagateGrossArea = true;
            chkGross.Unchecked += (s, e) => _vm.PropagateGrossArea = false;
            optionsPanel.Children.Add(chkGross);

            WpfCheckBox chkDed = new WpfCheckBox
            {
                Content = "Propagate Rentable Deductions Areas",
                FontWeight = FontWeights.SemiBold,
                IsChecked = _vm.PropagateDeductionsArea
            };
            chkDed.Checked += (s, e) => _vm.PropagateDeductionsArea = true;
            chkDed.Unchecked += (s, e) => _vm.PropagateDeductionsArea = false;
            optionsPanel.Children.Add(chkDed);

            WpfGrid.SetRow(optionsPanel, 2);
            cardLayout.Children.Add(optionsPanel);

            // Scrollable Propagation Summary
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _propagateSummaryContainer = new StackPanel();
            scroll.Content = _propagateSummaryContainer;
            WpfGrid.SetRow(scroll, 3);
            cardLayout.Children.Add(scroll);

            card.Child = cardLayout;
            WpfGrid.SetRow(card, 0);
            grid.Children.Add(card);

            // Action Bar with Revert + Propagate
            Border actionBar = new Border { Margin = new Thickness(0, 16, 0, 0) };
            WpfGrid actGrid = new WpfGrid();
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Revert
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Spacer
            actGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Propagate

            WpfButton btnRevert = CreateDangerButton("↺ Revert / Clear Propagated Areas");
            btnRevert.Height = 38;
            btnRevert.Padding = new Thickness(16, 0, 16, 0);
            btnRevert.ToolTip = "Safely removes copied areas from target levels without modifying source floors or deleting views.";
            btnRevert.Click += (s, e) =>
            {
                _vm.RevertPropagatedAreas();
                RefreshPropagateReviewUI();
            };
            WpfGrid.SetColumn(btnRevert, 0);
            actGrid.Children.Add(btnRevert);

            WpfButton btnPropagate = CreatePrimaryButton("⚡ Propagate Areas in Revit Model");
            btnPropagate.Height = 38;
            btnPropagate.Padding = new Thickness(24, 0, 24, 0);
            btnPropagate.Click += (s, e) =>
            {
                _vm.PropagateAreasFromTypicalGroups();
                SwitchToStep(2); // Advance to Calculate step
            };
            WpfGrid.SetColumn(btnPropagate, 2);
            actGrid.Children.Add(btnPropagate);

            actionBar.Child = actGrid;
            WpfGrid.SetRow(actionBar, 1);
            grid.Children.Add(actionBar);

            return grid;
        }

        private void RefreshPropagateReviewUI()
        {
            if (_propagateSummaryContainer == null) return;
            _propagateSummaryContainer.Children.Clear();

            int totalGroupsCount = 0;
            foreach (BuildingDefinition bldg in _vm.Buildings)
            {
                totalGroupsCount += bldg.TypicalGroups.Count;
            }

            if (totalGroupsCount == 0)
            {
                _propagateSummaryContainer.Children.Add(new WpfTextBlock
                {
                    Text = "No Typical Floor groups defined yet. Please go to Step 1 to add typical floor groups.",
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            foreach (BuildingDefinition bldg in _vm.Buildings)
            {
                if (bldg.TypicalGroups.Count == 0) continue;

                WpfTextBlock bldgHeader = new WpfTextBlock
                {
                    Text = "🏢 " + bldg.Name.ToUpperInvariant(),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                    Margin = new Thickness(0, 8, 0, 6)
                };
                _propagateSummaryContainer.Children.Add(bldgHeader);

                foreach (TypicalFloorGroup g in bldg.TypicalGroups)
                {
                    Border b = new Border
                    {
                        Background = new SolidColorBrush(COL_SURFACE),
                        BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(14, 10, 14, 10),
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    WpfGrid gGrid = new WpfGrid();
                    gGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
                    gGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
                    gGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    gGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
                    gGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });

                    // Color
                    Border dot = new Border
                    {
                        Width = 14,
                        Height = 14,
                        CornerRadius = new CornerRadius(7),
                        Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(g.ColorHex ?? "#0071E3")),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    WpfGrid.SetColumn(dot, 0);
                    gGrid.Children.Add(dot);

                    // Group Name
                    WpfTextBlock txtName = new WpfTextBlock { Text = g.Name, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
                    WpfGrid.SetColumn(txtName, 1);
                    gGrid.Children.Add(txtName);

                    // Source
                    string srcLabel = g.IsDuplexModule ? string.Format("Lower: {0} | Upper: {1}", g.SourceLevelNameLower, g.SourceLevelNameUpper) : "Source: " + g.SourceLevelName;
                    WpfTextBlock txtSrc = new WpfTextBlock { Text = srcLabel, Foreground = new SolidColorBrush(COL_TEXT_MUTED), VerticalAlignment = VerticalAlignment.Center };
                    WpfGrid.SetColumn(txtSrc, 2);
                    gGrid.Children.Add(txtSrc);

                    // Range
                    string rangeStr = g.IsSingleLevel ? "Single Floor (" + g.SourceLevelName + ")" : "Range: " + g.FromLevelName + " → " + g.ToLevelName;
                    WpfTextBlock txtRange = new WpfTextBlock { Text = rangeStr, Foreground = new SolidColorBrush(COL_TEXT_MUTED), VerticalAlignment = VerticalAlignment.Center };
                    WpfGrid.SetColumn(txtRange, 3);
                    gGrid.Children.Add(txtRange);

                    // Status Badge
                    Border statusPill = new Border
                    {
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(8, 3, 8, 3),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    WpfTextBlock statusText = new WpfTextBlock { FontSize = 10.5, FontWeight = FontWeights.Bold };
                    statusPill.Child = statusText;

                    if (g.IsSingleLevel)
                    {
                        statusPill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#E6F4EA"));
                        statusText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#137333"));
                        statusText.Text = "⭐ Single Floor (Excluded)";
                    }
                    else if (g.IsDuplexModule)
                    {
                        statusPill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#EEF2FF"));
                        statusText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#4F46E5"));
                        statusText.Text = "🏢 Alternating Duplex Cycles";
                    }
                    else
                    {
                        statusPill.Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#E8F0FE"));
                        statusText.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1A73E8"));
                        statusText.Text = "🔄 Will Propagate Areas";
                    }

                    WpfGrid.SetColumn(statusPill, 4);
                    gGrid.Children.Add(statusPill);

                    b.Child = gGrid;
                    _propagateSummaryContainer.Children.Add(b);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        // ══════════════════════════════════════════════════════════════
        // ── 5. STEP 3: CALCULATE ZFA & ZONING COMPLIANCE HUD ──
        // ══════════════════════════════════════════════════════════════
        private WpfGrid CreateStep3Panel()
        {
            WpfGrid grid = new WpfGrid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0: Zoning Compliance HUD Banner
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 1: Building Selector Pills + Recalculate
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Row 2: DataGrid Matrix

            // ── Row 0: Zoning Envelope & Compliance HUD Banner ──
            _complianceBanner = new Border
            {
                Background = WpfBrushes.White,
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 12)
            };

            _complianceHudContainer = new StackPanel();
            _complianceBanner.Child = _complianceHudContainer;
            WpfGrid.SetRow(_complianceBanner, 0);
            grid.Children.Add(_complianceBanner);

            // ── Row 1: Building Selection & Recalculate ──
            WpfGrid topRow = new WpfGrid { Margin = new Thickness(0, 0, 0, 10) };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Building Checkbox Pills
            ItemsControl bldgItems = new ItemsControl
            {
                ItemsSource = _vm.BuildingItems,
                ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(WrapPanel)))
            };

            FrameworkElementFactory factory = new FrameworkElementFactory(typeof(WpfCheckBox));
            factory.SetBinding(WpfCheckBox.ContentProperty, new WpfBinding("Name"));
            factory.SetBinding(WpfCheckBox.IsCheckedProperty, new WpfBinding("IsSelected") { Mode = BindingMode.TwoWay });
            factory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 12, 0));
            factory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            bldgItems.ItemTemplate = new DataTemplate { VisualTree = factory };

            WpfGrid.SetColumn(bldgItems, 0);
            topRow.Children.Add(bldgItems);

            WpfButton btnRecalc = CreateNeutralButton("↻ Recalculate ZFA");
            btnRecalc.Height = 30;
            btnRecalc.Padding = new Thickness(14, 0, 14, 0);
            btnRecalc.Click += (s, e) =>
            {
                _vm.CalculateTable();
                RefreshCalculateUI();
            };
            WpfGrid.SetColumn(btnRecalc, 1);
            topRow.Children.Add(btnRecalc);

            WpfGrid.SetRow(topRow, 1);
            grid.Children.Add(topRow);

            // ── Row 2: TabControl for Buildings ──
            _tabControlBuildings = new TabControl
            {
                Background = WpfBrushes.White,
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(1)
            };

            WpfGrid.SetRow(_tabControlBuildings, 2);
            grid.Children.Add(_tabControlBuildings);

            return grid;
        }

        private Border _complianceBanner;
        private StackPanel _complianceHudContainer;

        private void RefreshCalculateUI()
        {
            RefreshComplianceHudUI();

            if (_tabControlBuildings == null) return;
            _tabControlBuildings.Items.Clear();

            foreach (ZoningTableResult tbl in _vm.DisplayedTables)
            {
                TabItem tab = new TabItem
                {
                    Header = tbl.BuildingName,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold
                };

                tab.Content = CreateBuildingDataGrid(tbl);
                _tabControlBuildings.Items.Add(tab);
            }
        }

        private void RefreshComplianceHudUI()
        {
            if (_complianceHudContainer == null) return;
            _complianceHudContainer.Children.Clear();

            _vm.EvaluateCompliance();
            ZoningLotData lot = _vm.LotData;
            ZoningComplianceReport rep = _vm.ComplianceReport;

            WpfGrid hudGrid = new WpfGrid();
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.15, GridUnitType.Star) }); // Left: Lot inputs & Excel buttons
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });                    // Gap
            hudGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.35, GridUnitType.Star) }); // Right: Compliance HUD & Gauge

            // ── Left Card: Lot & FAR Allowances ──
            StackPanel leftStack = new StackPanel();

            WpfGrid lHdrGrid = new WpfGrid { Margin = new Thickness(0, 0, 0, 6) };
            lHdrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            lHdrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            WpfTextBlock lTitle = new WpfTextBlock
            {
                Text = "ZONING ENVELOPE & LOT PARAMETERS:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            };
            WpfGrid.SetColumn(lTitle, 0);
            lHdrGrid.Children.Add(lTitle);

            // Action Buttons: Import & Template
            StackPanel btnStack = new StackPanel { Orientation = WpfOrientation.Horizontal };

            WpfButton btnImp = CreateNeutralButton("📥 Import Excel");
            btnImp.Height = 24;
            btnImp.FontSize = 10.5;
            btnImp.Padding = new Thickness(8, 0, 8, 0);
            btnImp.Margin = new Thickness(0, 0, 6, 0);
            btnImp.ToolTip = "Import Lot Area, Zoning District, and Allowable FARs from a standard Excel file.";
            btnImp.Click += (s, e) =>
            {
                Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Excel & CSV Files (*.xls;*.xml;*.csv)|*.xls;*.xml;*.csv|All Files (*.*)|*.*",
                    Title = "Import Zoning Lot Parameters"
                };
                if (dlg.ShowDialog() == true)
                {
                    _vm.ImportZoningExcel(dlg.FileName);
                    RefreshCalculateUI();
                }
            };
            btnStack.Children.Add(btnImp);

            WpfButton btnTpl = CreateNeutralButton("📄 Excel Template");
            btnTpl.Height = 24;
            btnTpl.FontSize = 10.5;
            btnTpl.Padding = new Thickness(8, 0, 8, 0);
            btnTpl.ToolTip = "Download a clean, pre-formatted Excel template to fill in project zoning data.";
            btnTpl.Click += (s, e) =>
            {
                Microsoft.Win32.SaveFileDialog saveDlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Spreadsheet (*.xls)|*.xls",
                    FileName = "BauTools_Zoning_Lot_Template.xls",
                    Title = "Export BauTools Zoning Excel Template"
                };
                if (saveDlg.ShowDialog() == true)
                {
                    _vm.ExportZoningTemplateExcel(saveDlg.FileName);
                }
            };
            btnStack.Children.Add(btnTpl);

            WpfGrid.SetColumn(btnStack, 1);
            lHdrGrid.Children.Add(btnStack);
            leftStack.Children.Add(lHdrGrid);

            // Lot Form Fields Row
            WpfGrid formGrid = new WpfGrid();
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Lot Area
            StackPanel sArea = new StackPanel();
            sArea.Children.Add(new WpfTextBlock { Text = "Lot Area (SF):", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            WpfTextBox txtArea = new WpfTextBox { Text = lot.LotAreaSqFt.ToString("N0"), Height = 26, FontSize = 11, VerticalContentAlignment = VerticalAlignment.Center };
            txtArea.TextChanged += (s, e) =>
            {
                double v;
                if (double.TryParse(txtArea.Text.Replace(",", ""), out v)) { lot.LotAreaSqFt = v; _vm.EvaluateCompliance(); RefreshComplianceHudUI(); }
            };
            sArea.Children.Add(txtArea);
            WpfGrid.SetColumn(sArea, 0);
            formGrid.Children.Add(sArea);

            // District
            StackPanel sDist = new StackPanel();
            sDist.Children.Add(new WpfTextBlock { Text = "District:", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            WpfTextBox txtDist = new WpfTextBox { Text = lot.ZoningDistrict, Height = 26, FontSize = 11, VerticalContentAlignment = VerticalAlignment.Center };
            txtDist.TextChanged += (s, e) => { lot.ZoningDistrict = txtDist.Text; };
            sDist.Children.Add(txtDist);
            WpfGrid.SetColumn(sDist, 2);
            formGrid.Children.Add(sDist);

            // Allowable FAR
            StackPanel sFar = new StackPanel();
            sFar.Children.Add(new WpfTextBlock { Text = "Allowable FAR:", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            WpfTextBox txtFar = new WpfTextBox { Text = lot.BaseResidentialFar.ToString("N2"), Height = 26, FontSize = 11, VerticalContentAlignment = VerticalAlignment.Center };
            txtFar.TextChanged += (s, e) =>
            {
                double v;
                if (double.TryParse(txtFar.Text, out v)) { lot.BaseResidentialFar = v; _vm.EvaluateCompliance(); RefreshComplianceHudUI(); }
            };
            sFar.Children.Add(txtFar);
            WpfGrid.SetColumn(sFar, 4);
            formGrid.Children.Add(sFar);

            leftStack.Children.Add(formGrid);
            WpfGrid.SetColumn(leftStack, 0);
            hudGrid.Children.Add(leftStack);

            // ── Right Card: Compliance HUD & Gauge ──
            StackPanel rightStack = new StackPanel();

            WpfGrid rHdr = new WpfGrid { Margin = new Thickness(0, 0, 0, 6) };
            rHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            WpfTextBlock rTitle = new WpfTextBlock
            {
                Text = "ZONING COMPLIANCE & CAPACITY:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            };
            WpfGrid.SetColumn(rTitle, 0);
            rHdr.Children.Add(rTitle);

            // Status Pill
            WpfColor statCol = (WpfColor)ColorConverter.ConvertFromString(rep.ColorHex ?? "#10B981");
            Border statusPill = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromArgb(35, statCol.R, statCol.G, statCol.B)),
                BorderBrush = new SolidColorBrush(statCol),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2)
            };
            statusPill.Child = new WpfTextBlock
            {
                Text = rep.StatusSummary,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(statCol)
            };
            WpfGrid.SetColumn(statusPill, 1);
            rHdr.Children.Add(statusPill);
            rightStack.Children.Add(rHdr);

            // KPI 3-Pill Row
            WpfGrid kpiGrid = new WpfGrid { Margin = new Thickness(0, 0, 0, 6) };
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            kpiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Allowable
            Border bAllow = CreateKpiPill("Max Cap", string.Format("{0:N0} SF", rep.AllowableZfa), "#475569");
            WpfGrid.SetColumn(bAllow, 0);
            kpiGrid.Children.Add(bAllow);

            // Proposed
            Border bProp = CreateKpiPill("Proposed", string.Format("{0:N0} SF", rep.ProposedZfa), "#1E40AF");
            WpfGrid.SetColumn(bProp, 2);
            kpiGrid.Children.Add(bProp);

            // Balance
            string balPrefix = rep.RemainingZfa >= 0 ? "+" : "";
            Border bBal = CreateKpiPill("Balance", string.Format("{0}{1:N0} SF", balPrefix, rep.RemainingZfa), rep.ColorHex);
            WpfGrid.SetColumn(bBal, 4);
            kpiGrid.Children.Add(bBal);

            rightStack.Children.Add(kpiGrid);

            // Battery / Progress Bar
            Border gaugeBg = new Border
            {
                Height = 8,
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F1F5F9")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#CBD5E1")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                ClipToBounds = true
            };

            double clampedPct = Math.Min(100.0, Math.Max(0.0, rep.UtilizationPercent));
            WpfGrid barGrid = new WpfGrid();
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(clampedPct, GridUnitType.Star) });
            barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(Math.Max(0.01, 100.0 - clampedPct), GridUnitType.Star) });

            Border fillBar = new Border { Background = new SolidColorBrush(statCol), CornerRadius = new CornerRadius(3) };
            WpfGrid.SetColumn(fillBar, 0);
            barGrid.Children.Add(fillBar);

            gaugeBg.Child = barGrid;
            rightStack.Children.Add(gaugeBg);

            WpfGrid.SetColumn(rightStack, 2);
            hudGrid.Children.Add(rightStack);

            _complianceHudContainer.Children.Add(hudGrid);
        }

        private Border CreateKpiPill(string label, string val, string hex)
        {
            WpfColor c = (WpfColor)ColorConverter.ConvertFromString(hex ?? "#475569");
            Border b = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F8FAFC")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#E2E8F0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 3, 6, 3)
            };

            StackPanel sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            sp.Children.Add(new WpfTextBlock { Text = label.ToUpperInvariant(), FontSize = 8.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_TEXT_MUTED), HorizontalAlignment = HorizontalAlignment.Center });
            sp.Children.Add(new WpfTextBlock { Text = val, FontSize = 11.5, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(c), HorizontalAlignment = HorizontalAlignment.Center });

            b.Child = sp;
            return b;
        }

        private UIElement CreateBuildingDataGrid(ZoningTableResult tableResult)
        {
            Border host = new Border { Background = WpfBrushes.White, Padding = new Thickness(12) };

            ScrollViewer scroll = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            DataGrid grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                IsReadOnly = true,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                Background = WpfBrushes.White,
                RowBackground = WpfBrushes.White,
                AlternatingRowBackground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#FAFAFA")),
                BorderThickness = new Thickness(0),
                ColumnHeaderHeight = 36,
                RowHeight = 30,
                FontSize = 12
            };

            // 1. Group Indicator Column
            DataGridTextColumn colGroup = new DataGridTextColumn
            {
                Header = "Group",
                Binding = new WpfBinding("GroupName"),
                Width = new DataGridLength(110)
            };
            grid.Columns.Add(colGroup);

            // 2. Level Column
            DataGridTextColumn colLevel = new DataGridTextColumn
            {
                Header = "Level",
                Binding = new WpfBinding("LevelName"),
                FontWeight = FontWeights.SemiBold,
                Width = new DataGridLength(120)
            };
            grid.Columns.Add(colLevel);

            // 3. Gross Floor Area
            DataGridTextColumn colGross = new DataGridTextColumn
            {
                Header = "Gross Floor Area",
                Binding = new WpfBinding("GrossFloorArea") { StringFormat = "{0:N2}" },
                Width = new DataGridLength(120)
            };
            grid.Columns.Add(colGross);

            // 4. Dynamic Deduction Columns
            foreach (string cat in tableResult.DeductionCategories)
            {
                DataGridTextColumn colDed = new DataGridTextColumn
                {
                    Header = cat,
                    Binding = new WpfBinding("Deductions[" + cat + "]") { StringFormat = "{0:N2}" },
                    Width = new DataGridLength(100)
                };
                grid.Columns.Add(colDed);
            }

            // 5. Total Deductions
            DataGridTextColumn colTotDed = new DataGridTextColumn
            {
                Header = "Total Deductions",
                Binding = new WpfBinding("TotalDeductions") { StringFormat = "{0:N2}" },
                Width = new DataGridLength(115)
            };
            grid.Columns.Add(colTotDed);

            // 6. Net Area
            DataGridTextColumn colNet = new DataGridTextColumn
            {
                Header = "Net Area",
                Binding = new WpfBinding("NetArea") { StringFormat = "{0:N2}" },
                Width = new DataGridLength(110)
            };
            grid.Columns.Add(colNet);

            // 7. 5% ULEB
            DataGridTextColumn colUleb = new DataGridTextColumn
            {
                Header = "5% ULEB",
                Binding = new WpfBinding("UlebAmount") { StringFormat = "{0:N2}" },
                Width = new DataGridLength(90)
            };
            grid.Columns.Add(colUleb);

            // 8. Zoning Floor Area
            DataGridTextColumn colZfa = new DataGridTextColumn
            {
                Header = "Zoning Floor Area",
                Binding = new WpfBinding("ZoningFloorArea") { StringFormat = "{0:N2}" },
                FontWeight = FontWeights.Bold,
                Width = new DataGridLength(130)
            };
            grid.Columns.Add(colZfa);

            // 9. FAR
            DataGridTextColumn colFar = new DataGridTextColumn
            {
                Header = "FAR",
                Binding = new WpfBinding("Far") { StringFormat = "{0:N2}" },
                Width = new DataGridLength(80)
            };
            grid.Columns.Add(colFar);

            // Build Items Source
            List<LevelZoningRow> displayList = new List<LevelZoningRow>();
            if (tableResult.ResidentialRows != null) displayList.AddRange(tableResult.ResidentialRows);
            if (tableResult.ResidentialSubtotal != null) displayList.Add(tableResult.ResidentialSubtotal);
            if (tableResult.CommercialRows != null) displayList.AddRange(tableResult.CommercialRows);
            if (tableResult.CommercialSubtotal != null) displayList.Add(tableResult.CommercialSubtotal);
            if (tableResult.GrandTotal != null) displayList.Add(tableResult.GrandTotal);

            grid.ItemsSource = displayList;
            scroll.Content = grid;
            host.Child = scroll;
            return host;
        }

        // ══════════════════════════════════════════════════════════════
        // ── 6. STEP 4: SMART SHEET DIAGRAMMER & VIEW COMPOSER ──
        // ══════════════════════════════════════════════════════════════
        private WpfGrid CreateStep4Panel()
        {
            WpfGrid grid = new WpfGrid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) }); // Left: Sheet Composer Settings & Packages
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });                    // Gap
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });   // Right: Live Visual Preview & Actions

            // ── Left Card: Sheet Composer & Package Selector ──
            Border cardConfig = CreateCard();
            WpfGrid cfgGrid = new WpfGrid();
            cfgGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            cfgGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Settings

            // Header
            StackPanel hdr = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            hdr.Children.Add(new WpfTextBlock
            {
                Text = "Sheet Diagrammer & Package Composer",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            hdr.Children.Add(new WpfTextBlock
            {
                Text = "Compose typical floors, ZFA deductions, ceilings (RCP), and egress plans into multi-viewport sheets.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                Margin = new Thickness(0, 2, 0, 0)
            });
            WpfGrid.SetRow(hdr, 0);
            cfgGrid.Children.Add(hdr);

            // Scrollable Content
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel formStack = new StackPanel();

            // 1. Titleblock Selection & Workspace Bar
            Border tbBox = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#EFF6FF")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#BFDBFE")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            StackPanel tbStack = new StackPanel();
            tbStack.Children.Add(new WpfTextBlock
            {
                Text = "📐 PROJECT TITLEBLOCK & DRAWING WORKSPACE:",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1E40AF")),
                Margin = new Thickness(0, 0, 0, 4)
            });

            WpfComboBox comboTb = new WpfComboBox
            {
                Height = 28,
                ItemsSource = _vm.AvailableTitleblocks,
                DisplayMemberPath = "Name",
                SelectedItem = _vm.SelectedTitleblock
            };
            comboTb.SelectionChanged += (s, e) =>
            {
                _vm.SelectedTitleblock = comboTb.SelectedItem as TitleblockItem;
                RefreshStep4PreviewUI();
            };
            tbStack.Children.Add(comboTb);
            tbBox.Child = tbStack;
            formStack.Children.Add(tbBox);

            // 2. View Packages Section with 1 to 8 Matrix Grid
            WpfGrid pkgHdrGrid = new WpfGrid { Margin = new Thickness(0, 4, 0, 8) };
            pkgHdrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pkgHdrGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            pkgHdrGrid.Children.Add(new WpfTextBlock
            {
                Text = "CONFIGURACION INDEPENDIENTE POR PAQUETE DE PLANOS:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            });

            WpfButton btnAddPkg = CreateNeutralButton("➕ Agregar Paquete");
            btnAddPkg.Height = 26;
            btnAddPkg.FontSize = 10.5;
            btnAddPkg.Padding = new Thickness(10, 0, 10, 0);
            btnAddPkg.Click += (s, e) =>
            {
                string newPkgName = string.Format("Paquete {0}", _vm.PackageSettings.Count + 1);
                string defaultScheme = _vm.AreaSchemes.Count > 0 ? _vm.AreaSchemes[0] : "";
                _vm.AddCustomPackage(newPkgName, "P-", ViewPlanKind.AreaPlan, defaultScheme);
                RefreshPackageListUI();
                RefreshStep4PreviewUI();
            };
            WpfGrid.SetColumn(btnAddPkg, 1);
            pkgHdrGrid.Children.Add(btnAddPkg);

            formStack.Children.Add(pkgHdrGrid);

            _packagesContainer = new StackPanel();
            formStack.Children.Add(_packagesContainer);
            RefreshPackageListUI();

            // Scope Box & Parameters Section
            WpfGrid scopeGrid = new WpfGrid { Margin = new Thickness(0, 6, 0, 10) };
            scopeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            scopeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            scopeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            StackPanel msStack = new StackPanel();
            msStack.Children.Add(new WpfTextBlock { Text = "Master Scope Box (Overall):", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            WpfComboBox cMasterScope = new WpfComboBox { Height = 28, ItemsSource = _vm.AvailableScopeBoxes, SelectedItem = _vm.Config.MasterScopeBoxName };
            cMasterScope.SelectionChanged += (s, e) => { if (cMasterScope.SelectedItem != null) _vm.Config.MasterScopeBoxName = cMasterScope.SelectedItem.ToString(); };
            msStack.Children.Add(cMasterScope);
            WpfGrid.SetColumn(msStack, 0);
            scopeGrid.Children.Add(msStack);

            StackPanel vpStack = new StackPanel();
            vpStack.Children.Add(new WpfTextBlock { Text = "Building View Parameter:", FontSize = 9.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            WpfComboBox cViewParam = new WpfComboBox { Height = 28, ItemsSource = _vm.AvailableViewParameters, SelectedItem = _vm.Config.ViewBuildingParameterName };
            cViewParam.SelectionChanged += (s, e) => { if (cViewParam.SelectedItem != null) _vm.Config.ViewBuildingParameterName = cViewParam.SelectedItem.ToString(); };
            vpStack.Children.Add(cViewParam);
            WpfGrid.SetColumn(vpStack, 2);
            scopeGrid.Children.Add(vpStack);

            formStack.Children.Add(scopeGrid);

            // Checkbox: Reposition
            WpfCheckBox chkRepo = new WpfCheckBox
            {
                Content = "Reposition & update viewports if views already exist on sheets",
                IsChecked = _vm.RepositionIfExists,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 4)
            };
            chkRepo.Checked += (s, e) => _vm.RepositionIfExists = true;
            chkRepo.Unchecked += (s, e) => _vm.RepositionIfExists = false;
            formStack.Children.Add(chkRepo);

            scroll.Content = formStack;
            WpfGrid.SetRow(scroll, 1);
            cfgGrid.Children.Add(scroll);

            cardConfig.Child = cfgGrid;
            WpfGrid.SetColumn(cardConfig, 0);
            grid.Children.Add(cardConfig);

            // ── Right Card: Live Visual Sheet Preview & Action Bar ──
            Border cardPreview = CreateCard();
            WpfGrid prevLayout = new WpfGrid();
            prevLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            prevLayout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Scrollable Sheet Previews
            prevLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Action Bar

            // Header
            StackPanel prevHdr = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            prevHdr.Children.Add(new WpfTextBlock
            {
                Text = "Live Sheet & Matrix Canvas Visualizer",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_TEXT_MAIN)
            });
            prevHdr.Children.Add(new WpfTextBlock
            {
                Text = "Simulated drawing workspace displaying real viewport matrix slots, building Scope Boxes, and Title on Sheet badges.",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED)
            });
            WpfGrid.SetRow(prevHdr, 0);
            prevLayout.Children.Add(prevHdr);

            // Scrollable Preview Container
            ScrollViewer prevScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            _step4PreviewContainer = new StackPanel();
            prevScroll.Content = _step4PreviewContainer;
            WpfGrid.SetRow(prevScroll, 1);
            prevLayout.Children.Add(prevScroll);

            // Action Bar
            Border actBox = new Border { Margin = new Thickness(0, 14, 0, 0) };
            WpfGrid actGrid = new WpfGrid();
            actGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Summary badge
            actGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            _step4SummaryBadge = new WpfTextBlock
            {
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1D4ED8")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            WpfGrid.SetRow(_step4SummaryBadge, 0);
            actGrid.Children.Add(_step4SummaryBadge);

            WpfGrid btnGrid = new WpfGrid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Excel
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Spacer
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Compose

            WpfButton btnExcel = CreateNeutralButton("📊 Export Excel (.xls)");
            btnExcel.Height = 36;
            btnExcel.Padding = new Thickness(14, 0, 14, 0);
            btnExcel.Click += (s, e) => _vm.ExportExcelCommand.Execute(null);
            WpfGrid.SetColumn(btnExcel, 0);
            btnGrid.Children.Add(btnExcel);

            WpfButton btnCompose = CreatePrimaryButton("🚀 Generate Views & Compose Sheets in Revit");
            btnCompose.Height = 38;
            btnCompose.Padding = new Thickness(22, 0, 22, 0);
            btnCompose.Click += (s, e) =>
            {
                _vm.ExecuteComposeSheets();
                RefreshStep4PreviewUI();
            };
            WpfGrid.SetColumn(btnCompose, 2);
            btnGrid.Children.Add(btnCompose);

            WpfGrid.SetRow(btnGrid, 1);
            actGrid.Children.Add(btnGrid);

            actBox.Child = actGrid;
            WpfGrid.SetRow(actBox, 2);
            prevLayout.Children.Add(actBox);

            cardPreview.Child = prevLayout;
            WpfGrid.SetColumn(cardPreview, 2);
            grid.Children.Add(cardPreview);

            return grid;
        }

        private void RefreshPackageListUI()
        {
            if (_packagesContainer == null) return;
            _packagesContainer.Children.Clear();

            foreach (PackageSetting pkg in _vm.PackageSettings)
            {
                PackageSetting currentPkg = pkg;

                // Hide Master package if only 1 building
                if (currentPkg.PackageType == ViewPackageType.MasterOverall && _vm.Buildings.Count <= 1)
                    continue;

                Border pBox = new Border
                {
                    Background = new SolidColorBrush(COL_SURFACE),
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                StackPanel pCardStack = new StackPanel();

                // Row 1: Checkbox & Name + Prefix + Delete Button (if custom)
                WpfGrid r1Grid = new WpfGrid { Margin = new Thickness(0, 0, 0, 6) };
                r1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                r1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
                if (currentPkg.IsCustomPackage)
                {
                    r1Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
                }

                WpfCheckBox chk = new WpfCheckBox
                {
                    Content = string.Format("{0} {1}", currentPkg.Icon, currentPkg.DisplayName),
                    IsChecked = currentPkg.IsEnabled,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                chk.Checked += (s, e) => { currentPkg.IsEnabled = true; RefreshStep4PreviewUI(); };
                chk.Unchecked += (s, e) => { currentPkg.IsEnabled = false; RefreshStep4PreviewUI(); };
                WpfGrid.SetColumn(chk, 0);
                r1Grid.Children.Add(chk);

                WpfTextBox txtPfx = new WpfTextBox
                {
                    Text = currentPkg.SheetPrefix,
                    Height = 24,
                    FontSize = 11,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    ToolTip = "Sheet Number Prefix (e.g. Z-, ZD-, LS-, RCP-, M-)"
                };
                txtPfx.TextChanged += (s, e) => { currentPkg.SheetPrefix = txtPfx.Text; RefreshStep4PreviewUI(); };
                WpfGrid.SetColumn(txtPfx, 1);
                r1Grid.Children.Add(txtPfx);

                if (currentPkg.IsCustomPackage)
                {
                    WpfButton btnDelPkg = new WpfButton
                    {
                        Content = "✕",
                        Width = 20,
                        Height = 20,
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Background = WpfBrushes.Transparent,
                        BorderThickness = new Thickness(0),
                        Foreground = new SolidColorBrush(COL_DANGER),
                        ToolTip = "Eliminar este paquete de planos"
                    };
                    btnDelPkg.Click += (s, e) =>
                    {
                        _vm.RemovePackage(currentPkg);
                        RefreshPackageListUI();
                        RefreshStep4PreviewUI();
                    };
                    WpfGrid.SetColumn(btnDelPkg, 2);
                    r1Grid.Children.Add(btnDelPkg);
                }

                pCardStack.Children.Add(r1Grid);

                // Row 1.5: View Plan Kind (Tipo de Vista) + Revit Area Scheme Dropdown
                WpfGrid rKindGrid = new WpfGrid { Margin = new Thickness(0, 0, 0, 4) };
                rKindGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // View Kind
                rKindGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                rKindGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.7, GridUnitType.Star) }); // Area Scheme

                // View Kind Dropdown
                StackPanel vkStack = new StackPanel();
                vkStack.Children.Add(new WpfTextBlock { Text = "Tipo de Vista a Generar:", FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(COL_TEXT_MAIN) });
                WpfComboBox comboVk = new WpfComboBox { Height = 25, FontSize = 10 };
                comboVk.Items.Add("🏢 Floor Plan (Arquitectura)");
                comboVk.Items.Add("📐 Area Plan (Planta de Áreas)");
                comboVk.Items.Add("💡 Reflected Ceiling (RCP)");

                switch (currentPkg.ViewKind)
                {
                    case ViewPlanKind.FloorPlan: comboVk.SelectedIndex = 0; break;
                    case ViewPlanKind.AreaPlan: comboVk.SelectedIndex = 1; break;
                    case ViewPlanKind.CeilingPlan: comboVk.SelectedIndex = 2; break;
                    default: comboVk.SelectedIndex = 0; break;
                }

                // Area Scheme Dropdown
                StackPanel asStack = new StackPanel();
                asStack.Children.Add(new WpfTextBlock { Text = "Esquema de Área (Revit AreaScheme):", FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(COL_TEXT_MAIN) });
                WpfComboBox comboAs = new WpfComboBox { Height = 25, FontSize = 10, ItemsSource = _vm.AreaSchemes };

                if (!string.IsNullOrEmpty(currentPkg.SelectedAreaSchemeName))
                {
                    comboAs.SelectedItem = currentPkg.SelectedAreaSchemeName;
                }
                else if (_vm.AreaSchemes.Count > 0)
                {
                    if (currentPkg.PackageType == ViewPackageType.GrossArea && !string.IsNullOrEmpty(_vm.Config.GrossAreaSchemeName))
                        comboAs.SelectedItem = _vm.Config.GrossAreaSchemeName;
                    else if (currentPkg.PackageType == ViewPackageType.Deductions && !string.IsNullOrEmpty(_vm.Config.DeductionAreaSchemeName))
                        comboAs.SelectedItem = _vm.Config.DeductionAreaSchemeName;
                    else
                        comboAs.SelectedIndex = 0;
                }

                comboAs.IsEnabled = (currentPkg.ViewKind == ViewPlanKind.AreaPlan);

                // Informative Status Text
                WpfTextBlock txtSchemeDesc = new WpfTextBlock
                {
                    FontSize = 8.5,
                    Margin = new Thickness(0, 2, 0, 4),
                    FontWeight = FontWeights.Medium
                };

                Action updateDesc = () =>
                {
                    if (currentPkg.ViewKind == ViewPlanKind.AreaPlan)
                    {
                        string sName = comboAs.SelectedItem != null ? comboAs.SelectedItem.ToString() : (currentPkg.SelectedAreaSchemeName ?? "No seleccionado");
                        txtSchemeDesc.Text = string.Format("✓ Revit generará vistas tipo 'Area Plan' asociadas al esquema: \"{0}\"", sName);
                        txtSchemeDesc.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#7C3AED")); // Purple
                    }
                    else if (currentPkg.ViewKind == ViewPlanKind.CeilingPlan)
                    {
                        txtSchemeDesc.Text = "✓ Revit generará vistas tipo 'Reflected Ceiling Plan (RCP)'";
                        txtSchemeDesc.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#D97706")); // Amber
                    }
                    else
                    {
                        txtSchemeDesc.Text = "✓ Revit generará plantas de piso estándar ('Floor Plan')";
                        txtSchemeDesc.Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#2563EB")); // Blue
                    }
                };
                updateDesc();

                comboVk.SelectionChanged += (s, e) =>
                {
                    switch (comboVk.SelectedIndex)
                    {
                        case 0: currentPkg.ViewKind = ViewPlanKind.FloorPlan; break;
                        case 1: currentPkg.ViewKind = ViewPlanKind.AreaPlan; break;
                        case 2: currentPkg.ViewKind = ViewPlanKind.CeilingPlan; break;
                    }
                    comboAs.IsEnabled = (currentPkg.ViewKind == ViewPlanKind.AreaPlan);
                    if (currentPkg.ViewKind == ViewPlanKind.AreaPlan && comboAs.SelectedItem != null)
                    {
                        currentPkg.SelectedAreaSchemeName = comboAs.SelectedItem.ToString();
                    }
                    updateDesc();
                    RefreshStep4PreviewUI();
                };

                comboAs.SelectionChanged += (s, e) =>
                {
                    if (comboAs.SelectedItem != null)
                    {
                        currentPkg.SelectedAreaSchemeName = comboAs.SelectedItem.ToString();
                        updateDesc();
                        RefreshStep4PreviewUI();
                    }
                };

                vkStack.Children.Add(comboVk);
                WpfGrid.SetColumn(vkStack, 0);
                rKindGrid.Children.Add(vkStack);

                asStack.Children.Add(comboAs);
                WpfGrid.SetColumn(asStack, 2);
                rKindGrid.Children.Add(asStack);

                pCardStack.Children.Add(rKindGrid);
                pCardStack.Children.Add(txtSchemeDesc);

                // Row 2: Matrix Layout (1 to 8) + View Template + Scale
                WpfGrid r2Grid = new WpfGrid();
                r2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }); // Matrix
                r2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                r2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) }); // Template
                r2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                r2Grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }); // Scale

                // Matrix Combo (1 to 8 plans per sheet)
                StackPanel mxStack = new StackPanel();
                mxStack.Children.Add(new WpfTextBlock { Text = "Grid Matrix:", FontSize = 8.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
                WpfComboBox comboMatrix = new WpfComboBox { Height = 24, FontSize = 10 };
                comboMatrix.Items.Add("1 Plan / Sheet (1x1)");
                comboMatrix.Items.Add("2 Plans (1x2)");
                comboMatrix.Items.Add("3 Plans (1x3)");
                comboMatrix.Items.Add("4 Plans (2x2 Matrix)");
                comboMatrix.Items.Add("6 Plans (2x3 Matrix)");
                comboMatrix.Items.Add("8 Plans (2x4 Matrix)");

                switch (currentPkg.LayoutMode)
                {
                    case SheetLayoutMode.Single1View: comboMatrix.SelectedIndex = 0; break;
                    case SheetLayoutMode.Dual2Views: comboMatrix.SelectedIndex = 1; break;
                    case SheetLayoutMode.Triple3Views: comboMatrix.SelectedIndex = 2; break;
                    case SheetLayoutMode.Quad4Views: comboMatrix.SelectedIndex = 3; break;
                    case SheetLayoutMode.Hex6Views: comboMatrix.SelectedIndex = 4; break;
                    case SheetLayoutMode.Octo8Views: comboMatrix.SelectedIndex = 5; break;
                    default: comboMatrix.SelectedIndex = 3; break;
                }

                comboMatrix.SelectionChanged += (s, e) =>
                {
                    switch (comboMatrix.SelectedIndex)
                    {
                        case 0: currentPkg.LayoutMode = SheetLayoutMode.Single1View; break;
                        case 1: currentPkg.LayoutMode = SheetLayoutMode.Dual2Views; break;
                        case 2: currentPkg.LayoutMode = SheetLayoutMode.Triple3Views; break;
                        case 3: currentPkg.LayoutMode = SheetLayoutMode.Quad4Views; break;
                        case 4: currentPkg.LayoutMode = SheetLayoutMode.Hex6Views; break;
                        case 5: currentPkg.LayoutMode = SheetLayoutMode.Octo8Views; break;
                    }
                    RefreshStep4PreviewUI();
                };
                mxStack.Children.Add(comboMatrix);
                WpfGrid.SetColumn(mxStack, 0);
                r2Grid.Children.Add(mxStack);

                // View Template Dropdown
                StackPanel vtStack = new StackPanel();
                vtStack.Children.Add(new WpfTextBlock { Text = "View Template:", FontSize = 8.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
                WpfComboBox comboVt = new WpfComboBox
                {
                    Height = 24,
                    FontSize = 10,
                    ItemsSource = _vm.AvailableViewTemplates,
                    DisplayMemberPath = "Name",
                    SelectedIndex = 0
                };
                comboVt.SelectionChanged += (s, e) =>
                {
                    ViewTemplateItem sel = comboVt.SelectedItem as ViewTemplateItem;
                    if (sel != null) currentPkg.SelectedTemplateId = sel.TemplateId;
                };
                vtStack.Children.Add(comboVt);
                WpfGrid.SetColumn(vtStack, 2);
                r2Grid.Children.Add(vtStack);

                // Scale Dropdown
                StackPanel scStack = new StackPanel();
                scStack.Children.Add(new WpfTextBlock { Text = "Escala:", FontSize = 8.5, Foreground = new SolidColorBrush(COL_TEXT_MUTED) });
                WpfComboBox comboSc = new WpfComboBox { Height = 24, FontSize = 10 };
                comboSc.Items.Add("1/4\" (1:48)");
                comboSc.Items.Add("3/16\" (1:64)");
                comboSc.Items.Add("1/8\" (1:96)");
                comboSc.Items.Add("3/32\" (1:128)");
                comboSc.Items.Add("1/16\" (1:192)");
                comboSc.Items.Add("1:50 Metric");
                comboSc.Items.Add("1:100 Metric");
                comboSc.Items.Add("1:200 Metric");

                if (currentPkg.ScaleValue == 48) comboSc.SelectedIndex = 0;
                else if (currentPkg.ScaleValue == 64) comboSc.SelectedIndex = 1;
                else if (currentPkg.ScaleValue == 96) comboSc.SelectedIndex = 2;
                else if (currentPkg.ScaleValue == 128) comboSc.SelectedIndex = 3;
                else if (currentPkg.ScaleValue == 192) comboSc.SelectedIndex = 4;
                else if (currentPkg.ScaleValue == 50) comboSc.SelectedIndex = 5;
                else if (currentPkg.ScaleValue == 100) comboSc.SelectedIndex = 6;
                else if (currentPkg.ScaleValue == 200) comboSc.SelectedIndex = 7;
                else comboSc.SelectedIndex = 2;

                comboSc.SelectionChanged += (s, e) =>
                {
                    switch (comboSc.SelectedIndex)
                    {
                        case 0: currentPkg.ScaleValue = 48; currentPkg.ScaleDisplay = "1/4\" = 1'-0\""; break;
                        case 1: currentPkg.ScaleValue = 64; currentPkg.ScaleDisplay = "3/16\" = 1'-0\""; break;
                        case 2: currentPkg.ScaleValue = 96; currentPkg.ScaleDisplay = "1/8\" = 1'-0\""; break;
                        case 3: currentPkg.ScaleValue = 128; currentPkg.ScaleDisplay = "3/32\" = 1'-0\""; break;
                        case 4: currentPkg.ScaleValue = 192; currentPkg.ScaleDisplay = "1/16\" = 1'-0\""; break;
                        case 5: currentPkg.ScaleValue = 50; currentPkg.ScaleDisplay = "1:50 Metric"; break;
                        case 6: currentPkg.ScaleValue = 100; currentPkg.ScaleDisplay = "1:100 Metric"; break;
                        case 7: currentPkg.ScaleValue = 200; currentPkg.ScaleDisplay = "1:200 Metric"; break;
                    }
                    RefreshStep4PreviewUI();
                };
                scStack.Children.Add(comboSc);
                WpfGrid.SetColumn(scStack, 4);
                r2Grid.Children.Add(scStack);

                pCardStack.Children.Add(r2Grid);
                pBox.Child = pCardStack;
                _packagesContainer.Children.Add(pBox);
            }
        }

        private void RefreshStep4PreviewUI()
        {
            if (_step4PreviewContainer == null) return;
            _step4PreviewContainer.Children.Clear();

            _vm.ComputePlannedSheets();

            int totalViews = _vm.PlannedSheets.Sum(s => s.Viewports.Count);
            if (_step4SummaryBadge != null)
            {
                _step4SummaryBadge.Text = string.Format("⚡ Ready to generate {0} view(s) across {1} planned sheet(s) in Revit.", totalViews, _vm.PlannedSheets.Count);
            }

            if (_vm.PlannedSheets.Count == 0)
            {
                _step4PreviewContainer.Children.Add(new WpfTextBlock
                {
                    Text = "No sheets planned. Please enable at least one package on the left and configure typical floor groups in Step 1.",
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            foreach (PlannedSheet ps in _vm.PlannedSheets)
            {
                Border sheetCard = new Border
                {
                    Background = WpfBrushes.White,
                    BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#93C5FD")),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                StackPanel sStack = new StackPanel();

                // Sheet Header Bar
                WpfGrid shHdr = new WpfGrid { Margin = new Thickness(0, 0, 0, 8) };
                shHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                shHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                shHdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Border numPill = new Border
                {
                    Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#DBEAFE")),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 2, 8, 2),
                    Margin = new Thickness(0, 0, 8, 0)
                };
                numPill.Child = new WpfTextBlock
                {
                    Text = ps.SheetNumber,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1E40AF")),
                    FontSize = 11.5
                };
                WpfGrid.SetColumn(numPill, 0);
                shHdr.Children.Add(numPill);

                WpfTextBlock txtShName = new WpfTextBlock
                {
                    Text = ps.SheetName,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(COL_TEXT_MAIN)
                };
                WpfGrid.SetColumn(txtShName, 1);
                shHdr.Children.Add(txtShName);

                // Scale badge
                Border scBadge = new Border
                {
                    Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F1F5F9")),
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2)
                };
                scBadge.Child = new WpfTextBlock
                {
                    Text = "📐 " + ps.ScaleDisplay,
                    FontSize = 9.5,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(COL_TEXT_MUTED)
                };
                WpfGrid.SetColumn(scBadge, 2);
                shHdr.Children.Add(scBadge);

                sStack.Children.Add(shHdr);

                // Simulated Viewport Layout Canvas (Matrix 1 to 8)
                int rows = 1;
                int cols = 1;
                switch (ps.LayoutMode)
                {
                    case SheetLayoutMode.Single1View: rows = 1; cols = 1; break;
                    case SheetLayoutMode.Dual2Views: rows = 1; cols = 2; break;
                    case SheetLayoutMode.Triple3Views: rows = 1; cols = 3; break;
                    case SheetLayoutMode.Quad4Views: rows = 2; cols = 2; break;
                    case SheetLayoutMode.Hex6Views: rows = 2; cols = 3; break;
                    case SheetLayoutMode.Octo8Views: rows = 2; cols = 4; break;
                }

                Border canvas = new Border
                {
                    Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F8FAFC")),
                    BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6),
                    Height = rows > 1 ? 120 : 75
                };

                WpfGrid vpGrid = new WpfGrid();
                for (int c = 0; c < cols; c++)
                {
                    if (c > 0) vpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
                    vpGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
                for (int r = 0; r < rows; r++)
                {
                    if (r > 0) vpGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(6) });
                    vpGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                }

                for (int vIdx = 0; vIdx < ps.Viewports.Count; vIdx++)
                {
                    PlannedViewport vp = ps.Viewports[vIdx];
                    Border vpBox = new Border
                    {
                        Background = WpfBrushes.White,
                        BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#CBD5E1")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(4)
                    };

                    StackPanel vpContent = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                    vpContent.Children.Add(new WpfTextBlock
                    {
                        Text = !string.IsNullOrEmpty(vp.FormattedTitleOnSheet) ? vp.FormattedTitleOnSheet : ("📐 " + vp.LevelName),
                        FontWeight = FontWeights.Bold,
                        FontSize = 9.5,
                        Foreground = new SolidColorBrush(COL_TEXT_MAIN),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextWrapping = TextWrapping.NoWrap,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    });

                    // View Kind & Area Scheme Badge
                    string kindBadgeText = "";
                    string kindBadgeCol = "#2563EB";
                    if (vp.ViewKind == ViewPlanKind.AreaPlan)
                    {
                        string sName = !string.IsNullOrEmpty(vp.AreaSchemeName) ? vp.AreaSchemeName : "Area";
                        kindBadgeText = string.Format("📐 Area: {0}", sName);
                        kindBadgeCol = "#7C3AED"; // Purple
                    }
                    else if (vp.ViewKind == ViewPlanKind.CeilingPlan)
                    {
                        kindBadgeText = "💡 RCP Ceiling";
                        kindBadgeCol = "#D97706"; // Amber
                    }
                    else
                    {
                        kindBadgeText = "🏢 Floor Plan";
                        kindBadgeCol = "#2563EB"; // Blue
                    }

                    vpContent.Children.Add(new WpfTextBlock
                    {
                        Text = kindBadgeText,
                        FontSize = 8,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString(kindBadgeCol)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 1, 0, 1)
                    });

                    string scopeBoxLabel = !string.IsNullOrEmpty(vp.ScopeBoxName) && vp.ScopeBoxName != "(None)" ?
                        string.Format("🟢 Scope: {0}", vp.ScopeBoxName) : "⚪ No Scope Box";

                    vpContent.Children.Add(new WpfTextBlock
                    {
                        Text = scopeBoxLabel,
                        FontSize = 7.5,
                        Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#10B981")),
                        HorizontalAlignment = HorizontalAlignment.Center
                    });

                    vpBox.Child = vpContent;

                    int colIdx = (vIdx % cols) * 2;
                    int rowIdx = (vIdx / cols) * 2;

                    WpfGrid.SetColumn(vpBox, colIdx);
                    WpfGrid.SetRow(vpBox, rowIdx);
                    vpGrid.Children.Add(vpBox);
                }

                canvas.Child = vpGrid;
                sStack.Children.Add(canvas);

                sheetCard.Child = sStack;
                _step4PreviewContainer.Children.Add(sheetCard);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // ── 7. FOOTER (Status & Step Navigation) ──
        // ══════════════════════════════════════════════════════════════
        private UIElement CreateFooter()
        {
            Border footer = new Border
            {
                Background = new SolidColorBrush(COL_BG),
                Padding = new Thickness(24, 14, 24, 16)
            };

            WpfGrid grid = new WpfGrid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Status
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Next / Back Buttons

            // Status Message
            _txtStatus = new WpfTextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(COL_TEXT_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            };
            _txtStatus.SetBinding(WpfTextBlock.TextProperty, new WpfBinding("StatusMessage"));
            WpfGrid.SetColumn(_txtStatus, 0);
            grid.Children.Add(_txtStatus);

            // Back & Next Buttons
            StackPanel navStack = new StackPanel { Orientation = WpfOrientation.Horizontal };

            WpfButton btnBack = CreateNeutralButton("Back");
            btnBack.Height = 34;
            btnBack.Padding = new Thickness(16, 0, 16, 0);
            btnBack.Margin = new Thickness(0, 0, 10, 0);
            btnBack.Click += (s, e) =>
            {
                if (_activeStepIndex > 0) SwitchToStep(_activeStepIndex - 1);
            };
            navStack.Children.Add(btnBack);

            WpfButton btnNext = CreatePrimaryButton("Next Step →");
            btnNext.Height = 34;
            btnNext.Padding = new Thickness(18, 0, 18, 0);
            btnNext.Click += (s, e) =>
            {
                if (_activeStepIndex < 3) SwitchToStep(_activeStepIndex + 1);
            };
            navStack.Children.Add(btnNext);

            WpfGrid.SetColumn(navStack, 1);
            grid.Children.Add(navStack);

            footer.Child = grid;
            return footer;
        }

        private void ConfigureLevelComboBox(WpfComboBox combo, TypicalFloorGroup currentGroup, string selectedLevelName, Action<string> onLevelSelected)
        {
            List<LevelPickerItem> items = _vm.GetLevelPickerItemsForGroup(currentGroup);
            combo.ItemsSource = items;
            combo.DisplayMemberPath = "DisplayText";

            LevelPickerItem currentSel = items.FirstOrDefault(i => string.Equals(i.LevelName, selectedLevelName, StringComparison.OrdinalIgnoreCase));
            if (currentSel != null)
            {
                combo.SelectedItem = currentSel;
            }

            Style itemStyle = new Style(typeof(ComboBoxItem));
            DataTrigger trigDisabled = new DataTrigger { Binding = new WpfBinding("IsAvailable"), Value = false };
            trigDisabled.Setters.Add(new Setter(ComboBoxItem.IsEnabledProperty, false));
            trigDisabled.Setters.Add(new Setter(ComboBoxItem.ForegroundProperty, new SolidColorBrush(COL_TEXT_MUTED)));
            itemStyle.Triggers.Add(trigDisabled);
            combo.ItemContainerStyle = itemStyle;

            combo.SelectionChanged += (s, e) =>
            {
                LevelPickerItem sel = combo.SelectedItem as LevelPickerItem;
                if (sel != null)
                {
                    if (!sel.IsAvailable)
                    {
                        _vm.TriggerToast(string.Format("'{0}' is already occupied by '{1}'.", sel.LevelName, sel.OccupiedByGroupName), true);
                        LevelPickerItem prev = items.FirstOrDefault(i => string.Equals(i.LevelName, selectedLevelName, StringComparison.OrdinalIgnoreCase));
                        combo.SelectedItem = prev;
                        return;
                    }
                    onLevelSelected(sel.LevelName);
                }
            };
        }

        private void ShowAddBuildingDialog()
        {
            if (_vm.Buildings.Count == 0)
            {
                _vm.AddBuilding("Building 1");
                RefreshBuildingTabsUI();
                RefreshTypicalGroupsUI();
                RefreshTowerUI();
                return;
            }

            Window dlg = new Window
            {
                Title = "Add New Building",
                Width = 490,
                SizeToContent = SizeToContent.Height,
                MinHeight = 360,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(COL_BG),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12.5
            };

            WpfGrid g = new WpfGrid { Margin = new Thickness(24, 20, 24, 24) };
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Title
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Name input
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Options
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Buttons

            // Title
            StackPanel tStack = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            tStack.Children.Add(new WpfTextBlock { Text = "🏢 Add New Building", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_TEXT_MAIN) });
            tStack.Children.Add(new WpfTextBlock { Text = "Specify the building name and optionally copy typical floor groups from an existing building.", FontSize = 11, Foreground = new SolidColorBrush(COL_TEXT_MUTED), TextWrapping = TextWrapping.Wrap });
            WpfGrid.SetRow(tStack, 0);
            g.Children.Add(tStack);

            // Name
            StackPanel nStack = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            nStack.Children.Add(new WpfTextBlock { Text = "Building Name:", FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
            WpfTextBox txtName = new WpfTextBox { Text = string.Format("Building {0}", _vm.Buildings.Count + 1), Height = 28, Padding = new Thickness(6, 2, 6, 2), VerticalContentAlignment = VerticalAlignment.Center };
            nStack.Children.Add(txtName);
            WpfGrid.SetRow(nStack, 1);
            g.Children.Add(nStack);

            // Options
            Border optBox = new Border { Background = new SolidColorBrush(COL_SURFACE), BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 18) };
            StackPanel optStack = new StackPanel();

            WpfRadioButton rbCopy = new WpfRadioButton { Content = "📋 Copy typical floor setup from existing building:", IsChecked = true, FontWeight = FontWeights.Medium, Margin = new Thickness(0, 0, 0, 6) };
            WpfComboBox cSourceBldg = new WpfComboBox { Height = 26, ItemsSource = _vm.Buildings, DisplayMemberPath = "Name", SelectedItem = _vm.SelectedBuilding ?? _vm.Buildings[0], Margin = new Thickness(20, 0, 0, 8) };

            WpfRadioButton rbBlank = new WpfRadioButton { Content = "⚪ Start with blank configuration (No typical floors)", FontWeight = FontWeights.Medium };

            rbCopy.Checked += (s, e) => cSourceBldg.IsEnabled = true;
            rbBlank.Checked += (s, e) => cSourceBldg.IsEnabled = false;

            optStack.Children.Add(rbCopy);
            optStack.Children.Add(cSourceBldg);
            optStack.Children.Add(rbBlank);
            optBox.Child = optStack;
            WpfGrid.SetRow(optBox, 2);
            g.Children.Add(optBox);

            // Action Execute Helper
            Action doCreate = () =>
            {
                string bName = string.IsNullOrWhiteSpace(txtName.Text) ? string.Format("Building {0}", _vm.Buildings.Count + 1) : txtName.Text.Trim();
                BuildingDefinition srcBldg = cSourceBldg.SelectedItem as BuildingDefinition;
                if (rbCopy.IsChecked == true && srcBldg != null)
                {
                    _vm.DuplicateBuilding(srcBldg, bName);
                }
                else
                {
                    _vm.AddBuilding(bName);
                }

                RefreshBuildingTabsUI();
                RefreshTypicalGroupsUI();
                RefreshTowerUI();
                dlg.Close();
            };

            // Buttons
            StackPanel btnRow = new StackPanel { Orientation = WpfOrientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            WpfButton btnCancel = CreateNeutralButton("Cancel");
            btnCancel.Height = 34;
            btnCancel.Padding = new Thickness(16, 0, 16, 0);
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => dlg.Close();
            btnRow.Children.Add(btnCancel);

            WpfButton btnOk = CreatePrimaryButton("＋ Create Building");
            btnOk.Height = 34;
            btnOk.Padding = new Thickness(20, 0, 20, 0);
            btnOk.Click += (s, e) => doCreate();
            btnRow.Children.Add(btnOk);

            WpfGrid.SetRow(btnRow, 3);
            g.Children.Add(btnRow);

            // Key triggers
            dlg.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter) doCreate();
                else if (e.Key == System.Windows.Input.Key.Escape) dlg.Close();
            };

            dlg.Content = g;
            dlg.ShowDialog();
        }

        private Border CreateCard()
        {
            return new Border
            {
                Background = WpfBrushes.White,
                BorderBrush = new SolidColorBrush(COL_BORDER_LIGHT),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20)
            };
        }

        private Style _primaryBtnStyle;
        private Style _neutralBtnStyle;
        private Style _dangerBtnStyle;
        private Style _microBtnStyle;

        private void InitMinimalistStyles()
        {
            _primaryBtnStyle = CreateMinimalistButtonStyle(
                COL_PRIMARY,
                (WpfColor)ColorConverter.ConvertFromString("#0077ED"),
                (WpfColor)ColorConverter.ConvertFromString("#005BB5"),
                WpfColors.White,
                (WpfColor)ColorConverter.ConvertFromString("#0064C8"),
                6,
                FontWeights.SemiBold);

            _neutralBtnStyle = CreateMinimalistButtonStyle(
                WpfColors.White,
                (WpfColor)ColorConverter.ConvertFromString("#F8FAFC"),
                (WpfColor)ColorConverter.ConvertFromString("#F1F5F9"),
                (WpfColor)ColorConverter.ConvertFromString("#1E293B"),
                (WpfColor)ColorConverter.ConvertFromString("#CBD5E1"),
                6,
                FontWeights.Medium);

            _dangerBtnStyle = CreateMinimalistButtonStyle(
                (WpfColor)ColorConverter.ConvertFromString("#FEF2F2"),
                (WpfColor)ColorConverter.ConvertFromString("#FEE2E2"),
                (WpfColor)ColorConverter.ConvertFromString("#FECACA"),
                (WpfColor)ColorConverter.ConvertFromString("#DC2626"),
                (WpfColor)ColorConverter.ConvertFromString("#FCA5A5"),
                6,
                FontWeights.SemiBold);

            _microBtnStyle = CreateMinimalistButtonStyle(
                (WpfColor)ColorConverter.ConvertFromString("#F8FAFC"),
                (WpfColor)ColorConverter.ConvertFromString("#E2E8F0"),
                (WpfColor)ColorConverter.ConvertFromString("#CBD5E1"),
                (WpfColor)ColorConverter.ConvertFromString("#334155"),
                (WpfColor)ColorConverter.ConvertFromString("#E2E8F0"),
                4,
                FontWeights.SemiBold);
        }

        private static Style CreateMinimalistButtonStyle(
            WpfColor defaultBg,
            WpfColor hoverBg,
            WpfColor pressedBg,
            WpfColor textCol,
            WpfColor borderCol,
            int cornerRadius,
            FontWeight fontWeight)
        {
            Style style = new Style(typeof(WpfButton));
            style.Setters.Add(new Setter(WpfButton.ForegroundProperty, new SolidColorBrush(textCol)));
            style.Setters.Add(new Setter(WpfButton.FontWeightProperty, fontWeight));
            style.Setters.Add(new Setter(WpfButton.CursorProperty, System.Windows.Input.Cursors.Hand));
            style.Setters.Add(new Setter(WpfButton.SnapsToDevicePixelsProperty, true));

            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border), "btnBorder");
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(defaultBg));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(borderCol));
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));

            FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.MarginProperty, new Thickness(6, 0, 6, 0));
            contentPresenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);

            borderFactory.AppendChild(contentPresenter);

            ControlTemplate template = new ControlTemplate(typeof(WpfButton));
            template.VisualTree = borderFactory;

            // Hover trigger
            Trigger hoverTrigger = new Trigger { Property = WpfButton.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(hoverBg), "btnBorder"));
            template.Triggers.Add(hoverTrigger);

            // Pressed trigger
            Trigger pressedTrigger = new Trigger { Property = WpfButton.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(pressedBg), "btnBorder"));
            template.Triggers.Add(pressedTrigger);

            // Disabled trigger
            Trigger disabledTrigger = new Trigger { Property = WpfButton.IsEnabledProperty, Value = false };
            disabledTrigger.Setters.Add(new Setter(Border.OpacityProperty, 0.45, "btnBorder"));
            template.Triggers.Add(disabledTrigger);

            style.Setters.Add(new Setter(WpfButton.TemplateProperty, template));
            return style;
        }

        private WpfButton CreatePrimaryButton(string text)
        {
            if (_primaryBtnStyle == null) InitMinimalistStyles();
            return new WpfButton
            {
                Content = text,
                Style = _primaryBtnStyle
            };
        }

        private WpfButton CreateNeutralButton(string text)
        {
            if (_neutralBtnStyle == null) InitMinimalistStyles();
            return new WpfButton
            {
                Content = text,
                Style = _neutralBtnStyle
            };
        }

        private WpfButton CreateDangerButton(string text)
        {
            if (_dangerBtnStyle == null) InitMinimalistStyles();
            return new WpfButton
            {
                Content = text,
                Style = _dangerBtnStyle
            };
        }

        private WpfButton CreateMicroButton(string text)
        {
            if (_microBtnStyle == null) InitMinimalistStyles();
            return new WpfButton
            {
                Content = text,
                Style = _microBtnStyle
            };
        }
    }
}

```

### `ZoningFloorArea\Views\NycLotWindow.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;

// Aliases to avoid ambiguity between System.Windows and Autodesk.Revit.DB
using WpfGrid = System.Windows.Controls.Grid;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfProgressBar = System.Windows.Controls.ProgressBar;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfVisibility = System.Windows.Visibility;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using WpfBrushes = System.Windows.Media.Brushes;

namespace ZoningFloorArea.Views
{
    public class NycLotWindow : Window
    {
        private readonly Document _doc;
        private readonly NycPlutoService _plutoService;
        private readonly RevitLotDrawerService _drawerService;
        private readonly List<Level> _levels;
        private readonly List<string> _availableLineStyles;

        private NycLotInfo _currentLot;
        private NycBlockContext _currentBlockContext;
        private readonly ObservableCollection<NycSearchResult> _searchResults;

        // UI Controls - Search
        private WpfRadioButton _rbSearchAddress = null;
        private WpfRadioButton _rbSearchBbl = null;
        private StackPanel _panelAddressSearch = null;
        private StackPanel _panelBblSearch = null;
        private WpfTextBox _txtAddressQuery = null;
        private WpfListBox _listSearchResults = null;
        private WpfComboBox _comboBorough = null;
        private WpfTextBox _txtBlock = null;
        private WpfTextBox _txtLot = null;
        private WpfButton _btnSearch = null;
        private WpfProgressBar _progressBar = null;

        // UI Controls - Drawing, Level & Grouping Options
        private WpfComboBox _comboElementType = null;
        private WpfComboBox _comboAnchorCorner = null;
        private WpfCheckBox _chkAlignPbp = null;
        private WpfCheckBox _chkCreatePropLineLvl1 = null;
        private WpfComboBox _comboLevels = null;

        // Proposal C: Grouping Mode Selectors
        private WpfRadioButton _rbGroupSingle = null;
        private WpfRadioButton _rbGroupSplit = null;
        private WpfRadioButton _rbGroupNone = null;
        private WpfCheckBox _chkPinGroup = null;

        // Proposal B: Zoning Drafting View Table
        private WpfCheckBox _chkGenerateZoningTable = null;

        // UI Controls - Granular Line Style Selectors
        private WpfCheckBox _chkDrawSubjectLot = null;
        private WpfComboBox _comboSubjectLineStyle = null;
        private WpfCheckBox _chkDrawAdjacentLots = null;
        private WpfComboBox _comboAdjacentLineStyle = null;
        private WpfCheckBox _chkDrawBlockContext = null;
        private WpfComboBox _comboBlockContextLineStyle = null;
        private WpfCheckBox _chkDrawSidewalk = null;
        private WpfComboBox _comboSidewalkLineStyle = null;
        private WpfCheckBox _chkPlaceStreetNotes = null;

        // UI Controls - 3D Building Masses
        private WpfCheckBox _chkCreate3DBuildingMasses = null;
        private WpfCheckBox _chkExtrudeSubjectLotBuilding = null;

        // UI Controls - Info Card
        private Border _infoCardContainer = null;
        private WpfTextBlock _txtPlaceholderInfo = null;
        private StackPanel _panelLotDetails = null;
        private WpfTextBlock _lblLotAddress = null;
        private WpfTextBlock _lblLotBbl = null;
        private WpfTextBlock _lblBlockContextSummary = null;
        private WpfTextBlock _lblZoningSummary = null;
        private WpfTextBlock _lblLotArea = null;
        private WpfTextBlock _lblBldgArea = null;
        private WpfTextBlock _lblResFar = null;
        private WpfTextBlock _lblCommFar = null;
        private WpfTextBlock _lblFacilFar = null;
        private WpfTextBlock _lblBuiltFar = null;
        private WpfTextBlock _lblDimensions = null;
        private WpfTextBlock _lblExtraDetails = null;

        // Action Buttons
        private WpfButton _btnDrawInRevit = null;
        private WpfTextBlock _txtStatusMsg = null;

        // Theme Colors matching BauTools
        private static readonly WpfColor COL_BG        = (WpfColor)ColorConverter.ConvertFromString("#F8FAFC");
        private static readonly WpfColor COL_CARD      = WpfColors.White;
        private static readonly WpfColor COL_DARK      = (WpfColor)ColorConverter.ConvertFromString("#0F172A");
        private static readonly WpfColor COL_ACCENT    = (WpfColor)ColorConverter.ConvertFromString("#2563EB");
        private static readonly WpfColor COL_ACCENT2   = (WpfColor)ColorConverter.ConvertFromString("#0284C7");
        private static readonly WpfColor COL_MUTED     = (WpfColor)ColorConverter.ConvertFromString("#64748B");
        private static readonly WpfColor COL_BORDER    = (WpfColor)ColorConverter.ConvertFromString("#E2E8F0");
        private static readonly WpfColor COL_HEADER_BG = (WpfColor)ColorConverter.ConvertFromString("#1E293B");

        public NycLotWindow(Document doc)
        {
            _doc = doc;
            _plutoService = new NycPlutoService();
            _drawerService = new RevitLotDrawerService(doc);
            _searchResults = new ObservableCollection<NycSearchResult>();

            _levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            _availableLineStyles = _drawerService.GetAvailableLineStyles();

            Title = "BauTools — NYC Lot Boundary, 3D Context Masses & Zoning Table";
            Height = 910;
            Width = 1180;
            MinHeight = 780;
            MinWidth = 1000;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 13;

            BuildUI();
        }

        private void BuildUI()
        {
            var mainGrid = new WpfGrid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // ── 1. HEADER ──
            var header = CreateHeader();
            WpfGrid.SetRow(header, 0);
            mainGrid.Children.Add(header);

            // ── 2. CONTENT (2 Columns) ──
            var contentGrid = new WpfGrid { Margin = new Thickness(24, 14, 24, 14) };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(490) }); // Left: Search & Options
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });  // Gap
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Right: Preview Card

            var leftPanel = CreateLeftPanel();
            WpfGrid.SetColumn(leftPanel, 0);
            contentGrid.Children.Add(leftPanel);

            var rightPanel = CreateRightPanel();
            WpfGrid.SetColumn(rightPanel, 2);
            contentGrid.Children.Add(rightPanel);

            WpfGrid.SetRow(contentGrid, 1);
            mainGrid.Children.Add(contentGrid);

            // ── 3. FOOTER ──
            var footer = CreateFooter();
            WpfGrid.SetRow(footer, 2);
            mainGrid.Children.Add(footer);

            Content = mainGrid;
        }

        private UIElement CreateHeader()
        {
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(COL_HEADER_BG),
                Padding = new Thickness(24, 14, 24, 14)
            };

            var stack = new StackPanel();

            var titleRow = new StackPanel { Orientation = WpfOrientation.Horizontal };
            var badge = new Border
            {
                Background = new SolidColorBrush(COL_ACCENT),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new WpfTextBlock
            {
                Text = "NYC GIS 3D",
                Foreground = WpfBrushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 11
            };
            titleRow.Children.Add(badge);

            titleRow.Children.Add(new WpfTextBlock
            {
                Text = "NYC Lot Boundary, 3D Context Masses & Zoning Schedule",
                Foreground = WpfBrushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 17,
                VerticalAlignment = VerticalAlignment.Center
            });
            stack.Children.Add(titleRow);

            stack.Children.Add(new WpfTextBlock
            {
                Text = "Official NYC MapPLUTO & Building Footprints. Named Model Groups, Level 1 boundaries, 3D building masses & Native Revit Zoning Drafting View.",
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#94A3B8")),
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });

            headerBorder.Child = stack;
            return headerBorder;
        }

        private UIElement CreateLeftPanel()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(0, 0, 8, 0) };
            var stack = new StackPanel();

            // ── Card 1: Search Lot ──
            var searchCard = CreateCard("1. Search NYC Tax Lot");
            var searchContent = new StackPanel();

            var modePanel = new StackPanel { Orientation = WpfOrientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            _rbSearchAddress = new WpfRadioButton
            {
                Content = "By Address",
                IsChecked = true,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 16, 0)
            };
            _rbSearchAddress.Checked += (s, e) => ToggleSearchMode(true);

            _rbSearchBbl = new WpfRadioButton
            {
                Content = "By BBL (Boro-Block-Lot)",
                FontWeight = FontWeights.SemiBold
            };
            _rbSearchBbl.Checked += (s, e) => ToggleSearchMode(false);

            modePanel.Children.Add(_rbSearchAddress);
            modePanel.Children.Add(_rbSearchBbl);
            searchContent.Children.Add(modePanel);

            // Address Search
            _panelAddressSearch = new StackPanel();
            _panelAddressSearch.Children.Add(new WpfTextBlock
            {
                Text = "Street Address or Building Name:",
                FontSize = 11,
                Foreground = new SolidColorBrush(COL_MUTED),
                Margin = new Thickness(0, 0, 0, 4)
            });

            var searchRow = new WpfGrid();
            searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            searchRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtAddressQuery = new WpfTextBox
            {
                Height = 30,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 12,
                VerticalContentAlignment = VerticalAlignment.Center,
                BorderBrush = new SolidColorBrush(COL_BORDER)
            };
            _txtAddressQuery.KeyDown += async (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                {
                    await PerformAddressSearchAsync();
                }
            };
            WpfGrid.SetColumn(_txtAddressQuery, 0);
            searchRow.Children.Add(_txtAddressQuery);

            _btnSearch = CreateStyledButton("Search", COL_ACCENT, WpfBrushes.White);
            _btnSearch.Height = 30;
            _btnSearch.Padding = new Thickness(14, 0, 14, 0);
            _btnSearch.Click += async (s, e) => await PerformAddressSearchAsync();
            WpfGrid.SetColumn(_btnSearch, 2);
            searchRow.Children.Add(_btnSearch);

            _panelAddressSearch.Children.Add(searchRow);

            _listSearchResults = new WpfListBox
            {
                Height = 85,
                Margin = new Thickness(0, 6, 0, 0),
                ItemsSource = _searchResults,
                DisplayMemberPath = "Label",
                BorderBrush = new SolidColorBrush(COL_BORDER),
                Visibility = WpfVisibility.Collapsed
            };
            _listSearchResults.SelectionChanged += async (s, e) =>
            {
                NycSearchResult selected = _listSearchResults.SelectedItem as NycSearchResult;
                if (selected != null && !string.IsNullOrEmpty(selected.Bbl))
                {
                    await LoadLotByBblAsync(selected.Bbl);
                }
            };
            _panelAddressSearch.Children.Add(_listSearchResults);

            searchContent.Children.Add(_panelAddressSearch);

            // BBL Search
            _panelBblSearch = new StackPanel { Visibility = WpfVisibility.Collapsed };

            var bblGrid = new WpfGrid();
            bblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            bblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            bblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            bblGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var boroStack = new StackPanel();
            boroStack.Children.Add(new WpfTextBlock { Text = "Borough:", FontSize = 11, Foreground = new SolidColorBrush(COL_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            _comboBorough = new WpfComboBox { Height = 28 };
            _comboBorough.Items.Add("1 - Manhattan");
            _comboBorough.Items.Add("2 - Bronx");
            _comboBorough.Items.Add("3 - Brooklyn");
            _comboBorough.Items.Add("4 - Queens");
            _comboBorough.Items.Add("5 - Staten Island");
            _comboBorough.SelectedIndex = 0;
            boroStack.Children.Add(_comboBorough);
            WpfGrid.SetColumn(boroStack, 0);
            bblGrid.Children.Add(boroStack);

            var blockStack = new StackPanel();
            blockStack.Children.Add(new WpfTextBlock { Text = "Block:", FontSize = 11, Foreground = new SolidColorBrush(COL_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            _txtBlock = new WpfTextBox { Height = 28, Padding = new Thickness(4), VerticalContentAlignment = VerticalAlignment.Center, BorderBrush = new SolidColorBrush(COL_BORDER) };
            blockStack.Children.Add(_txtBlock);
            WpfGrid.SetColumn(blockStack, 2);
            bblGrid.Children.Add(blockStack);

            var lotStack = new StackPanel();
            lotStack.Children.Add(new WpfTextBlock { Text = "Lot:", FontSize = 11, Foreground = new SolidColorBrush(COL_MUTED), Margin = new Thickness(0, 0, 0, 2) });
            _txtLot = new WpfTextBox { Height = 28, Padding = new Thickness(4), VerticalContentAlignment = VerticalAlignment.Center, BorderBrush = new SolidColorBrush(COL_BORDER) };
            lotStack.Children.Add(_txtLot);
            WpfGrid.SetColumn(lotStack, 4);
            bblGrid.Children.Add(lotStack);

            _panelBblSearch.Children.Add(bblGrid);

            var btnLookupBbl = CreateStyledButton("Lookup BBL", COL_ACCENT, WpfBrushes.White);
            btnLookupBbl.Height = 28;
            btnLookupBbl.Margin = new Thickness(0, 6, 0, 0);
            btnLookupBbl.Click += async (s, e) => await PerformBblSearchAsync();
            _panelBblSearch.Children.Add(btnLookupBbl);

            searchContent.Children.Add(_panelBblSearch);

            _progressBar = new WpfProgressBar
            {
                Height = 3,
                IsIndeterminate = true,
                Margin = new Thickness(0, 6, 0, 0),
                Visibility = WpfVisibility.Collapsed
            };
            searchContent.Children.Add(_progressBar);

            searchCard.Child = searchContent;
            stack.Children.Add(searchCard);

            // ── Card 2: Level, Base Placement & Grouping (Proposal C) ──
            var baseCard = CreateCard("2. Placement, Grouping & Zoning Table");
            var baseContent = new StackPanel();

            var lvlRow = new WpfGrid { Margin = new Thickness(0, 0, 0, 8) };
            lvlRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });
            lvlRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            lvlRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) });

            var lvlStack = new StackPanel();
            lvlStack.Children.Add(new WpfTextBlock { Text = "Target Level (Level 1):", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(COL_DARK), Margin = new Thickness(0, 0, 0, 2) });
            _comboLevels = new WpfComboBox { Height = 28 };
            int defaultLevelIdx = 0;
            var lvl1 = _drawerService.GetLevel1();
            for (int i = 0; i < _levels.Count; i++)
            {
                _comboLevels.Items.Add(_levels[i].Name);
                if (lvl1 != null && _levels[i].Id == lvl1.Id) defaultLevelIdx = i;
            }
            if (_comboLevels.Items.Count > 0) _comboLevels.SelectedIndex = defaultLevelIdx;
            lvlStack.Children.Add(_comboLevels);
            WpfGrid.SetColumn(lvlStack, 0);
            lvlRow.Children.Add(lvlStack);

            var elemStack = new StackPanel();
            elemStack.Children.Add(new WpfTextBlock { Text = "Element Type:", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(COL_DARK), Margin = new Thickness(0, 0, 0, 2) });
            _comboElementType = new WpfComboBox { Height = 28 };
            _comboElementType.Items.Add("Model Curves (3D on Level)");
            _comboElementType.Items.Add("Detail Curves (2D Active View)");
            _comboElementType.Items.Add("Area Boundary Lines (Area Plan)");
            _comboElementType.SelectedIndex = 0;
            elemStack.Children.Add(_comboElementType);
            WpfGrid.SetColumn(elemStack, 2);
            lvlRow.Children.Add(elemStack);

            baseContent.Children.Add(lvlRow);

            _chkCreatePropLineLvl1 = new WpfCheckBox
            {
                Content = "🔒 Ensure Lot Boundaries are placed at Level 1",
                IsChecked = true,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#15803D")),
                Margin = new Thickness(0, 0, 0, 4)
            };
            baseContent.Children.Add(_chkCreatePropLineLvl1);

            _chkAlignPbp = new WpfCheckBox
            {
                Content = "Align Lot Anchor with Project Base Point (PBP)",
                IsChecked = true,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 4)
            };
            baseContent.Children.Add(_chkAlignPbp);

            _comboAnchorCorner = new WpfComboBox { Height = 26, Margin = new Thickness(0, 0, 0, 8) };
            _comboAnchorCorner.Items.Add("Southwest Corner (Min X, Min Y) — Default");
            _comboAnchorCorner.Items.Add("Northwest Corner (Min X, Max Y)");
            _comboAnchorCorner.Items.Add("Southeast Corner (Max X, Min Y)");
            _comboAnchorCorner.Items.Add("Northeast Corner (Max X, Max Y)");
            _comboAnchorCorner.Items.Add("Geometric Center (Center of Bounding Box)");
            _comboAnchorCorner.SelectedIndex = 0;
            baseContent.Children.Add(_comboAnchorCorner);

            // Grouping options (Proposal C)
            var groupBorder = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F0FDF4")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#BBF7D0")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 2, 0, 6)
            };
            var groupStack = new StackPanel();

            groupStack.Children.Add(new WpfTextBlock
            {
                Text = "📦 Model Grouping Mode:",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#166534")),
                Margin = new Thickness(0, 0, 0, 4)
            });

            _rbGroupSingle = new WpfRadioButton
            {
                Content = "Single Group: [Address] (All elements grouped together)",
                IsChecked = true,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 2)
            };
            groupStack.Children.Add(_rbGroupSingle);

            _rbGroupSplit = new WpfRadioButton
            {
                Content = "Split in 2 Groups: [NYC Lot - Address] & [NYC Context - Block]",
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 2)
            };
            groupStack.Children.Add(_rbGroupSplit);

            _rbGroupNone = new WpfRadioButton
            {
                Content = "Do not group elements",
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 4)
            };
            groupStack.Children.Add(_rbGroupNone);

            _chkPinGroup = new WpfCheckBox
            {
                Content = "📌 Pin / Lock Groups in Revit (prevent accidental movement)",
                IsChecked = false,
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(COL_DARK),
                Margin = new Thickness(18, 0, 0, 0)
            };
            groupStack.Children.Add(_chkPinGroup);

            groupBorder.Child = groupStack;
            baseContent.Children.Add(groupBorder);

            // Proposal B: Zoning Schedule Drafting Table
            _chkGenerateZoningTable = new WpfCheckBox
            {
                Content = "📊 Generate NYC Zoning Summary Drafting View / Table",
                IsChecked = true,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#0369A1")),
                Margin = new Thickness(0, 2, 0, 2)
            };
            baseContent.Children.Add(_chkGenerateZoningTable);

            baseCard.Child = baseContent;
            stack.Children.Add(baseCard);

            // ── Card 3: Granular Line Style Selectors ──
            var stylesCard = CreateCard("3. Line Style Selectors (Per Lot Type)");
            var stylesContent = new StackPanel();

            stylesContent.Children.Add(CreateLineStyleRow(
                out _chkDrawSubjectLot, "🔴 Development Lot (Lote en cuestión):", true,
                out _comboSubjectLineStyle, RevitLotDrawerService.STYLE_SUBJECT_RED));

            stylesContent.Children.Add(CreateLineStyleRow(
                out _chkDrawAdjacentLots, "🟠 Adjacent Lots (Lotes Circundantes / Vecinos):", true,
                out _comboAdjacentLineStyle, RevitLotDrawerService.STYLE_ADJACENT_ORANGE));

            stylesContent.Children.Add(CreateLineStyleRow(
                out _chkDrawBlockContext, "🏙️ Block Context (Resto de la Manzana):", true,
                out _comboBlockContextLineStyle, RevitLotDrawerService.STYLE_CONTEXT_GRAY));

            stylesContent.Children.Add(CreateLineStyleRow(
                out _chkDrawSidewalk, "🚶 Sidewalk Curbs (Aceras / Bordillos 12ft):", true,
                out _comboSidewalkLineStyle, RevitLotDrawerService.STYLE_SIDEWALK_BLUE));

            _chkPlaceStreetNotes = new WpfCheckBox
            {
                Content = "🔤 Place Surrounding Street Titles as Text Notes",
                IsChecked = true,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0, 4, 0, 2)
            };
            stylesContent.Children.Add(_chkPlaceStreetNotes);

            stylesCard.Child = stylesContent;
            stack.Children.Add(stylesCard);

            // ── Card 4: 3D Context Building Masses ──
            var massCard = CreateCard("4. 🏢 3D Building Masses (Real NYC Heights)");
            var massContent = new StackPanel();

            _chkCreate3DBuildingMasses = new WpfCheckBox
            {
                Content = "🏢 Create 3D Context Masses with Real Heights (HEIGHT_ROO)",
                IsChecked = true,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#1E40AF")),
                Margin = new Thickness(0, 0, 0, 4)
            };
            massContent.Children.Add(_chkCreate3DBuildingMasses);

            _chkExtrudeSubjectLotBuilding = new WpfCheckBox
            {
                Content = "Extrude existing building on Development Lot (Lote propio)",
                IsChecked = false,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(20, 0, 0, 6)
            };
            massContent.Children.Add(_chkExtrudeSubjectLotBuilding);

            var noteSubcat = new WpfTextBlock
            {
                Text = "• Subcategory: Generic Models > NYC Context Building\n• Material: NYC - Urban Context (Auto-created, no duplicates)\n• Courtyards & Interior Holes are automatically extruded.",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(COL_MUTED),
                Margin = new Thickness(20, 0, 0, 2)
            };
            massContent.Children.Add(noteSubcat);

            _chkCreate3DBuildingMasses.Checked += (s, e) => _chkExtrudeSubjectLotBuilding.IsEnabled = true;
            _chkCreate3DBuildingMasses.Unchecked += (s, e) => _chkExtrudeSubjectLotBuilding.IsEnabled = false;

            massCard.Child = massContent;
            stack.Children.Add(massCard);

            scroll.Content = stack;
            return scroll;
        }

        private UIElement CreateLineStyleRow(out WpfCheckBox chk, string label, bool defaultChecked, out WpfComboBox combo, string defaultStyleName)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

            chk = new WpfCheckBox
            {
                Content = label,
                IsChecked = defaultChecked,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 2)
            };
            panel.Children.Add(chk);

            combo = new WpfComboBox { Height = 26, Margin = new Thickness(20, 0, 0, 0) };
            int selectedIdx = 0;
            for (int i = 0; i < _availableLineStyles.Count; i++)
            {
                combo.Items.Add(_availableLineStyles[i]);
                if (string.Equals(_availableLineStyles[i], defaultStyleName, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIdx = i;
                }
            }
            combo.SelectedIndex = selectedIdx;

            var targetCombo = combo;
            chk.Checked += (s, e) => targetCombo.IsEnabled = true;
            chk.Unchecked += (s, e) => targetCombo.IsEnabled = false;

            panel.Children.Add(combo);
            return panel;
        }

        private UIElement CreateRightPanel()
        {
            _infoCardContainer = new Border
            {
                Background = new SolidColorBrush(COL_CARD),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Effect = new DropShadowEffect
                {
                    Color = WpfColors.Black,
                    Direction = 270,
                    ShadowDepth = 1,
                    Opacity = 0.05,
                    BlurRadius = 8
                }
            };

            var rootStack = new StackPanel();

            _txtPlaceholderInfo = new WpfTextBlock
            {
                Text = "🔍 Search for an address or BBL on the left to preview the tax lot geometry, zoning districts, FAR limits, 3D building heights, and surrounding streets.",
                FontSize = 13,
                Foreground = new SolidColorBrush(COL_MUTED),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20, 100, 20, 100)
            };
            rootStack.Children.Add(_txtPlaceholderInfo);

            _panelLotDetails = new StackPanel { Visibility = WpfVisibility.Collapsed };

            // 1. Lot Header Banner
            var banner = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#EFF6FF")),
                BorderBrush = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#BFDBFE")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var bannerStack = new StackPanel();
            _lblLotAddress = new WpfTextBlock
            {
                Text = "350 5TH AVENUE",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_DARK)
            };
            _lblLotBbl = new WpfTextBlock
            {
                Text = "BBL: 1008350041 | Manhattan | Block: 835 | Lot: 41",
                FontSize = 12,
                Foreground = new SolidColorBrush(COL_ACCENT),
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 2, 0, 0)
            };
            _lblBlockContextSummary = new WpfTextBlock
            {
                Text = "🏙️ Block Context: 11 Lots | 3D Buildings Loaded | Streets: W 33RD ST, W 34TH ST, 5TH AVE",
                FontSize = 11,
                Foreground = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#0369A1")),
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 3, 0, 0)
            };
            bannerStack.Children.Add(_lblLotAddress);
            bannerStack.Children.Add(_lblLotBbl);
            bannerStack.Children.Add(_lblBlockContextSummary);
            banner.Child = bannerStack;
            _panelLotDetails.Children.Add(banner);

            // 2. Zoning Section
            var zoningBox = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F8FAFC")),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var zStack = new StackPanel();
            zStack.Children.Add(new WpfTextBlock
            {
                Text = "ZONING & URBAN PLANNING",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_MUTED),
                Margin = new Thickness(0, 0, 0, 4)
            });
            _lblZoningSummary = new WpfTextBlock
            {
                Text = "C5-3 / Special: MID (Midtown)",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(COL_DARK)
            };
            _lblExtraDetails = new WpfTextBlock
            {
                Text = "Owner: EMPIRE STATE BLDG | Land Use: Commercial | Year Built: 1931",
                FontSize = 11,
                Foreground = new SolidColorBrush(COL_MUTED),
                Margin = new Thickness(0, 4, 0, 0)
            };
            zStack.Children.Add(_lblZoningSummary);
            zStack.Children.Add(_lblExtraDetails);
            zoningBox.Child = zStack;
            _panelLotDetails.Children.Add(zoningBox);

            // 3. FAR & Areas Grid
            var metricsGrid = new WpfGrid { Margin = new Thickness(0, 0, 0, 12) };
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _lblResFar = new WpfTextBlock { Text = "0.00", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };
            _lblCommFar = new WpfTextBlock { Text = "15.00", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };
            _lblFacilFar = new WpfTextBlock { Text = "15.00", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };
            _lblBuiltFar = new WpfTextBlock { Text = "28.12", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_ACCENT2) };

            var c1 = CreateMetricMiniCard("Resid. FAR", _lblResFar);
            var c2 = CreateMetricMiniCard("Comm. FAR", _lblCommFar);
            var c3 = CreateMetricMiniCard("Facil. FAR", _lblFacilFar);
            var c4 = CreateMetricMiniCard("Built FAR", _lblBuiltFar);

            WpfGrid.SetColumn(c1, 0); metricsGrid.Children.Add(c1);
            WpfGrid.SetColumn(c2, 2); metricsGrid.Children.Add(c2);
            WpfGrid.SetColumn(c3, 4); metricsGrid.Children.Add(c3);
            WpfGrid.SetColumn(c4, 6); metricsGrid.Children.Add(c4);

            _panelLotDetails.Children.Add(metricsGrid);

            // 4. Lot Area & Dimensions Box
            var dimGrid = new WpfGrid { Margin = new Thickness(0, 0, 0, 10) };
            dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });

            _lblLotArea = new WpfTextBlock { Text = "91,351 SF", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };
            _lblBldgArea = new WpfTextBlock { Text = "2,568,970 SF", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };
            _lblDimensions = new WpfTextBlock { Text = "197.5 ft × 425.0 ft", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(COL_DARK) };

            var d1 = CreateMetricMiniCard("Lot Area (PLUTO)", _lblLotArea);
            var d2 = CreateMetricMiniCard("Bldg Gross Area", _lblBldgArea);
            var d3 = CreateMetricMiniCard("Subject Lot W × D", _lblDimensions);

            WpfGrid.SetColumn(d1, 0); dimGrid.Children.Add(d1);
            WpfGrid.SetColumn(d2, 2); dimGrid.Children.Add(d2);
            WpfGrid.SetColumn(d3, 4); dimGrid.Children.Add(d3);

            _panelLotDetails.Children.Add(dimGrid);

            rootStack.Children.Add(_panelLotDetails);
            _infoCardContainer.Child = rootStack;

            return _infoCardContainer;
        }

        private UIElement CreateFooter()
        {
            var footerBorder = new Border
            {
                Background = WpfBrushes.White,
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(24, 14, 24, 14)
            };

            var footerGrid = new WpfGrid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtStatusMsg = new WpfTextBlock
            {
                Text = "Ready to search.",
                FontSize = 12,
                Foreground = new SolidColorBrush(COL_MUTED),
                VerticalAlignment = VerticalAlignment.Center
            };
            WpfGrid.SetColumn(_txtStatusMsg, 0);
            footerGrid.Children.Add(_txtStatusMsg);

            var btnStack = new StackPanel { Orientation = WpfOrientation.Horizontal };

            var btnCancel = CreateStyledButton("Close", (WpfColor)ColorConverter.ConvertFromString("#E2E8F0"), new SolidColorBrush(COL_DARK));
            btnCancel.Width = 90;
            btnCancel.Height = 34;
            btnCancel.Margin = new Thickness(0, 0, 10, 0);
            btnCancel.Click += (s, e) => Close();
            btnStack.Children.Add(btnCancel);

            _btnDrawInRevit = CreateStyledButton("Draw in Revit", COL_ACCENT, WpfBrushes.White);
            _btnDrawInRevit.Width = 140;
            _btnDrawInRevit.Height = 34;
            _btnDrawInRevit.FontWeight = FontWeights.Bold;
            _btnDrawInRevit.IsEnabled = false;
            _btnDrawInRevit.Click += (s, e) => ExecuteDrawLot();
            btnStack.Children.Add(_btnDrawInRevit);

            WpfGrid.SetColumn(btnStack, 1);
            footerGrid.Children.Add(btnStack);

            footerBorder.Child = footerGrid;
            return footerBorder;
        }

        // ── Helper UI Builders ──
        private Border CreateCard(string title)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(COL_CARD),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 10),
                Effect = new DropShadowEffect
                {
                    Color = WpfColors.Black,
                    Direction = 270,
                    ShadowDepth = 1,
                    Opacity = 0.04,
                    BlurRadius = 4
                }
            };

            var stack = new StackPanel();
            stack.Children.Add(new WpfTextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(COL_DARK),
                Margin = new Thickness(0, 0, 0, 8)
            });

            return card;
        }

        private Border CreateMetricMiniCard(string label, WpfTextBlock valueBlock)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#F8FAFC")),
                BorderBrush = new SolidColorBrush(COL_BORDER),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8)
            };
            var stack = new StackPanel();
            stack.Children.Add(new WpfTextBlock
            {
                Text = label,
                FontSize = 10,
                Foreground = new SolidColorBrush(COL_MUTED),
                Margin = new Thickness(0, 0, 0, 2)
            });
            stack.Children.Add(valueBlock);
            border.Child = stack;
            return border;
        }

        private WpfButton CreateStyledButton(string text, WpfColor bgColor, System.Windows.Media.Brush fgBrush)
        {
            var btn = new WpfButton
            {
                Content = text,
                Background = new SolidColorBrush(bgColor),
                Foreground = fgBrush,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            return btn;
        }

        // ── Search & Logic Handlers ──
        private void ToggleSearchMode(bool addressMode)
        {
            _panelAddressSearch.Visibility = addressMode ? WpfVisibility.Visible : WpfVisibility.Collapsed;
            _panelBblSearch.Visibility = addressMode ? WpfVisibility.Collapsed : WpfVisibility.Visible;
        }

        private async Task PerformAddressSearchAsync()
        {
            string query = _txtAddressQuery.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                SetStatus("Please enter a NYC address to search.", true);
                return;
            }

            SetLoading(true, string.Format("Searching '{0}' in NYC Planning GeoSearch...", query));
            _searchResults.Clear();
            _listSearchResults.Visibility = WpfVisibility.Collapsed;

            try
            {
                var results = await _plutoService.SearchAddressAsync(query);
                if (results.Count == 0)
                {
                    SetStatus("No NYC addresses found matching your search.", true);
                }
                else
                {
                    foreach (var res in results)
                    {
                        _searchResults.Add(res);
                    }
                    _listSearchResults.Visibility = WpfVisibility.Visible;
                    _listSearchResults.SelectedIndex = 0;
                    SetStatus(string.Format("Found {0} address matches. Select one to load geometry.", results.Count));
                }
            }
            catch (Exception ex)
            {
                SetStatus(string.Format("Search error: {0}", ex.Message), true);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task PerformBblSearchAsync()
        {
            int boroughCode = _comboBorough.SelectedIndex + 1;
            int block;
            if (!int.TryParse(_txtBlock.Text.Trim(), out block) || block <= 0)
            {
                SetStatus("Please enter a valid Block number.", true);
                return;
            }
            int lot;
            if (!int.TryParse(_txtLot.Text.Trim(), out lot) || lot <= 0)
            {
                SetStatus("Please enter a valid Lot number.", true);
                return;
            }

            string bbl = string.Format("{0}{1:D5}{2:D4}", boroughCode, block, lot);
            await LoadLotByBblAsync(bbl);
        }

        private async Task LoadLotByBblAsync(string bbl)
        {
            SetLoading(true, string.Format("Querying MapPLUTO & 3D Building Footprints for BBL {0}...", bbl));

            try
            {
                var lotInfo = await _plutoService.GetLotByBblAsync(bbl);
                if (lotInfo == null)
                {
                    SetStatus(string.Format("Could not find MapPLUTO data for BBL {0}.", bbl), true);
                    _currentLot = null;
                    _currentBlockContext = null;
                    _btnDrawInRevit.IsEnabled = false;
                    _panelLotDetails.Visibility = WpfVisibility.Collapsed;
                    _txtPlaceholderInfo.Visibility = WpfVisibility.Visible;
                }
                else
                {
                    _currentLot = lotInfo;

                    // Fetch full block context and 3D building footprints
                    _currentBlockContext = await _plutoService.GetBlockContextAsync(lotInfo);

                    DisplayLotInfo(lotInfo, _currentBlockContext);
                    _btnDrawInRevit.IsEnabled = true;
                    int bldgCount = _currentBlockContext.Buildings.Count;
                    int totalCount = _currentBlockContext.AllLots.Count;
                    SetStatus(string.Format("Loaded NYC Lot {0} ({1} lots in block, {2} 3D buildings). Ready to draw on Level 1 as Group.", lotInfo.Bbl, totalCount, bldgCount));
                }
            }
            catch (Exception ex)
            {
                SetStatus(string.Format("Error loading BBL: {0}", ex.Message), true);
                _btnDrawInRevit.IsEnabled = false;
            }
            finally
            {
                SetLoading(false);
            }
        }

        private void DisplayLotInfo(NycLotInfo lot, NycBlockContext blockContext)
        {
            _txtPlaceholderInfo.Visibility = WpfVisibility.Collapsed;
            _panelLotDetails.Visibility = WpfVisibility.Visible;

            _lblLotAddress.Text = string.IsNullOrWhiteSpace(lot.Address) ? string.Format("LOT {0}, BLOCK {1}", lot.Lot, lot.Block) : lot.Address.ToUpperInvariant();
            _lblLotBbl.Text = string.Format("BBL: {0} | Borough: {1} | Block: {2} | Lot: {3} | ZIP: {4}", lot.Bbl, lot.Borough, lot.Block, lot.Lot, lot.ZipCode);

            var streets = blockContext.GetSurroundingStreetNames();
            string streetSummary = streets.Count > 0 ? string.Join(", ", streets.Values) : "N/A";
            int bldgCount = blockContext.Buildings.Count;
            _lblBlockContextSummary.Text = string.Format("Block {0}: {1} Lots | {2} 3D Buildings | Streets: {3}", lot.Block, blockContext.AllLots.Count, bldgCount, streetSummary);

            _lblZoningSummary.Text = lot.GetZoningSummary();
            _lblExtraDetails.Text = string.Format("Owner: {0} | Class: {1} | Built: {2} | Floors: {3}", string.IsNullOrEmpty(lot.OwnerName) ? "N/A" : lot.OwnerName, lot.BuildingClass, lot.YearBuilt > 0 ? lot.YearBuilt.ToString() : "N/A", lot.NumFloors);

            _lblResFar.Text = lot.ResFar.ToString("F2");
            _lblCommFar.Text = lot.CommFar.ToString("F2");
            _lblFacilFar.Text = lot.FacilFar.ToString("F2");
            _lblBuiltFar.Text = lot.BuiltFar.ToString("F2");

            _lblLotArea.Text = string.Format("{0:N0} SF", lot.LotAreaSqFt);
            _lblBldgArea.Text = string.Format("{0:N0} SF", lot.TotalBldgAreaSqFt);
            _lblDimensions.Text = string.Format("{0:F1} ft x {1:F1} ft", lot.WidthFt, lot.DepthFt);
        }

        private void ExecuteDrawLot()
        {
            if (_currentLot == null || _currentBlockContext == null)
            {
                SetStatus("No lot selected to draw.", true);
                return;
            }

            LotGroupingMode grpMode = LotGroupingMode.SingleGroup;
            if (_rbGroupSplit.IsChecked == true) grpMode = LotGroupingMode.SplitSubjectAndContext;
            else if (_rbGroupNone.IsChecked == true) grpMode = LotGroupingMode.NoGrouping;

            string subjStyle = _comboSubjectLineStyle.SelectedItem != null ? _comboSubjectLineStyle.SelectedItem.ToString() : RevitLotDrawerService.STYLE_SUBJECT_RED;
            string adjStyle = _comboAdjacentLineStyle.SelectedItem != null ? _comboAdjacentLineStyle.SelectedItem.ToString() : RevitLotDrawerService.STYLE_ADJACENT_ORANGE;
            string ctxStyle = _comboBlockContextLineStyle.SelectedItem != null ? _comboBlockContextLineStyle.SelectedItem.ToString() : RevitLotDrawerService.STYLE_CONTEXT_GRAY;
            string swStyle = _comboSidewalkLineStyle.SelectedItem != null ? _comboSidewalkLineStyle.SelectedItem.ToString() : RevitLotDrawerService.STYLE_SIDEWALK_BLUE;

            var options = new LotDrawOptions
            {
                ElementType = (LotElementType)_comboElementType.SelectedIndex,
                AnchorCorner = (LotAnchorCorner)_comboAnchorCorner.SelectedIndex,
                AlignWithPbp = _chkAlignPbp.IsChecked == true,
                EnsureLevel1Placement = _chkCreatePropLineLvl1.IsChecked == true,
                GroupingMode = grpMode,
                PinCreatedGroup = _chkPinGroup.IsChecked == true,
                GenerateZoningDraftingTable = _chkGenerateZoningTable.IsChecked == true,
                DrawSubjectLot = _chkDrawSubjectLot.IsChecked == true,
                SubjectLineStyle = subjStyle,
                DrawAdjacentLots = _chkDrawAdjacentLots.IsChecked == true,
                AdjacentLineStyle = adjStyle,
                DrawRemainingBlockLots = _chkDrawBlockContext.IsChecked == true,
                BlockContextLineStyle = ctxStyle,
                DrawSidewalks = _chkDrawSidewalk.IsChecked == true,
                SidewalkLineStyle = swStyle,
                PlaceStreetTextNotes = _chkPlaceStreetNotes.IsChecked == true,
                Create3DBuildingMasses = _chkCreate3DBuildingMasses.IsChecked == true,
                ExtrudeSubjectLotBuilding = _chkExtrudeSubjectLotBuilding.IsChecked == true
            };

            if (_comboLevels.SelectedIndex >= 0 && _comboLevels.SelectedIndex < _levels.Count)
            {
                options.TargetLevel = _levels[_comboLevels.SelectedIndex];
            }

            var result = _drawerService.DrawLotWithContext(_currentBlockContext, options);

            if (result.Success)
            {
                string targetLevelName = options.TargetLevel != null ? options.TargetLevel.Name : "Level 1";
                MessageBox.Show(
                    string.Format("{0}\n\nLevel: {1}\nLot: {2}\nBBL: {3}\nZoning: {4}\nArea: {5:N0} SF", result.Message, targetLevelName, _currentLot != null ? _currentLot.Address : "", _currentLot != null ? _currentLot.Bbl : "", _currentLot != null ? _currentLot.GetZoningSummary() : "", _currentLot != null ? _currentLot.LotAreaSqFt : 0.0),
                    "BauTools — NYC Lot & Urban Context Created",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Close();
            }
            else
            {
                MessageBox.Show(
                    result.Message,
                    "BauTools — Draw Lot Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                SetStatus(result.Message, true);
            }
        }

        private void SetLoading(bool isLoading, string statusText = "")
        {
            _progressBar.Visibility = isLoading ? WpfVisibility.Visible : WpfVisibility.Collapsed;
            _btnSearch.IsEnabled = !isLoading;
            if (!string.IsNullOrEmpty(statusText))
            {
                SetStatus(statusText);
            }
        }

        private void SetStatus(string msg, bool isError = false)
        {
            _txtStatusMsg.Text = msg;
            _txtStatusMsg.Foreground = isError
                ? new SolidColorBrush((WpfColor)ColorConverter.ConvertFromString("#DC2626"))
                : new SolidColorBrush(COL_MUTED);
        }
    }
}

```

### `ZoningFloorArea\Views\RenameLevelsWindow.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;
using ZoningFloorArea.Services;

namespace ZoningFloorArea.Views
{
    public class RenameLevelsWindow : Window
    {
        private readonly Document _doc;
        private readonly List<LevelRenameItem> _allItems;
        private readonly ObservableCollection<LevelRenameItem> _displayItems;

        // UI Controls
        private System.Windows.Controls.ComboBox _baseLevelCombo;
        private System.Windows.Controls.TextBox _floorCountTxt;
        private System.Windows.Controls.CheckBox _chkIncludeRoof;
        private System.Windows.Controls.CheckBox _chkIncludeBulkhead;
        private System.Windows.Controls.CheckBox _chkTwoDigits;
        private System.Windows.Controls.DataGrid _dataGrid;
        private System.Windows.Controls.TextBlock _statusSummary;

        // Color Palette matching BauTools
        private static readonly System.Windows.Media.Color COL_BG        = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#F1F5F9");
        private static readonly System.Windows.Media.Color COL_CARD      = System.Windows.Media.Colors.White;
        private static readonly System.Windows.Media.Color COL_DARK      = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0F172A");
        private static readonly System.Windows.Media.Color COL_ACCENT    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#2563EB");
        private static readonly System.Windows.Media.Color COL_ACCENT2   = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#0284C7");
        private static readonly System.Windows.Media.Color COL_MUTED     = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#64748B");
        private static readonly System.Windows.Media.Color COL_BORDER    = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#CBD5E1");
        private static readonly System.Windows.Media.Color COL_HEADER_BG = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#1E293B");
        private static readonly System.Windows.Media.Color COL_SUCCESS   = (System.Windows.Media.Color)ColorConverter.ConvertFromString("#16A34A");

        public RenameLevelsWindow(Document doc)
        {
            _doc = doc;
            _allItems = new List<LevelRenameItem>();
            _displayItems = new ObservableCollection<LevelRenameItem>();

            Title = "BauTools — Rename Levels (Ordinal & Cellar/Roof)";
            Height = 720;
            Width = 980;
            MinHeight = 550;
            MinWidth = 750;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(COL_BG);
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            FontSize = 13;

            LoadLevelsFromDocument();
            BuildUI();
            RecalculateNames();
        }

        private void LoadLevelsFromDocument()
        {
            var levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            foreach (var lvl in levels)
            {
                // Format elevation nicely (imperial ft-in or metric depending on unit)
                string elevStr = FormatElevation(lvl.Elevation);
                var item = new LevelRenameItem(lvl, elevStr);
                _allItems.Add(item);
                _displayItems.Add(item);
            }
        }

        private string FormatElevation(double rawFeet)
        {
            try
            {
                #if REVIT2021_OR_GREATER || true
                return UnitFormatUtils.Format(_doc.GetUnits(), SpecTypeId.Length, rawFeet, false);
                #else
                return string.Format("{0:F2} ft", rawFeet);
                #endif
            }
            catch
            {
                return string.Format("{0:F2}'", rawFeet);
            }
        }

        private void BuildUI()
        {
            SolidColorBrush cardBrush     = new SolidColorBrush(COL_CARD);
            SolidColorBrush darkBrush     = new SolidColorBrush(COL_DARK);
            SolidColorBrush accentBrush   = new SolidColorBrush(COL_ACCENT);
            SolidColorBrush accent2Brush  = new SolidColorBrush(COL_ACCENT2);
            SolidColorBrush mutedBrush    = new SolidColorBrush(COL_MUTED);
            SolidColorBrush borderBrush   = new SolidColorBrush(COL_BORDER);
            SolidColorBrush headerBgBrush = new SolidColorBrush(COL_HEADER_BG);
            SolidColorBrush successBrush  = new SolidColorBrush(COL_SUCCESS);

            System.Windows.Controls.Grid root = new System.Windows.Controls.Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Config Options Card
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // DataGrid Card
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer Actions

            // ══════════════════════════════════════════════════════════
            // 0. HEADER
            // ══════════════════════════════════════════════════════════
            Border headerBar = new Border
            {
                Background = headerBgBrush,
                Padding = new Thickness(24, 14, 24, 14)
            };

            System.Windows.Controls.Grid hGrid = new System.Windows.Controls.Grid();
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel titlePanel = new StackPanel();
            StackPanel logoLine = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

            Border badge = new Border
            {
                Background = accent2Brush,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new TextBlock { Text = "LEVELS", FontWeight = FontWeights.ExtraBold, FontSize = 12, Foreground = System.Windows.Media.Brushes.White };
            logoLine.Children.Add(badge);

            logoLine.Children.Add(new TextBlock
            {
                Text = "BauTools — Automatic Level Renamer",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            });
            titlePanel.Children.Add(logoLine);

            titlePanel.Children.Add(new TextBlock
            {
                Text = "Renombra niveles con nomenclatura ordinal (01 1ST FL., 02 2ND FL.), Cellar bajo 0, Roof y Bulkhead.",
                FontSize = 11,
                Foreground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#94A3B8")),
                Margin = new Thickness(0, 4, 0, 0)
            });
            hGrid.Children.Add(titlePanel);

            headerBar.Child = hGrid;
            System.Windows.Controls.Grid.SetRow(headerBar, 0);
            root.Children.Add(headerBar);

            // ══════════════════════════════════════════════════════════
            // 1. CONFIGURATION CARD
            // ══════════════════════════════════════════════════════════
            Border configCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 14, 16, 8),
                Padding = new Thickness(18, 14, 18, 14)
            };

            System.Windows.Controls.Grid configGrid = new System.Windows.Controls.Grid();
            configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) });
            configGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.8, GridUnitType.Star) });

            // Column 0: Base Level (Ground / 1st floor)
            StackPanel col0 = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };
            col0.Children.Add(new TextBlock { Text = "NIVEL BASE (PLANTA BAJA / 01 1ST FL.):", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 6) });

            _baseLevelCombo = new System.Windows.Controls.ComboBox
            {
                ItemsSource = _allItems,
                DisplayMemberPath = "CurrentName",
                Height = 32,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            // Select default: first level >= 0 or index 0
            int defaultBaseIdx = _allItems.FindIndex(x => x.RawElevation >= -0.001);
            _baseLevelCombo.SelectedIndex = defaultBaseIdx >= 0 ? defaultBaseIdx : 0;
            _baseLevelCombo.SelectionChanged += (s, e) => RecalculateNames();
            col0.Children.Add(_baseLevelCombo);
            System.Windows.Controls.Grid.SetColumn(col0, 0);
            configGrid.Children.Add(col0);

            // Column 1: Number of floors
            StackPanel col1 = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };
            col1.Children.Add(new TextBlock { Text = "CANTIDAD DE PISOS:", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 6) });

            StackPanel floorCountPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            _floorCountTxt = new System.Windows.Controls.TextBox
            {
                Width = 60,
                Height = 32,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            
            // Calculate initial sensible default for floor count
            int initialFloors = Math.Max(1, _allItems.Count - Math.Max(0, defaultBaseIdx) - 2);
            _floorCountTxt.Text = initialFloors.ToString();
            _floorCountTxt.TextChanged += (s, e) => RecalculateNames();

            System.Windows.Controls.Button btnMinus = new System.Windows.Controls.Button { Content = "−", Width = 30, Height = 32, FontWeight = FontWeights.Bold, Margin = new Thickness(6, 0, 2, 0) };
            btnMinus.Click += (s, e) => {
                int val;
                if (int.TryParse(_floorCountTxt.Text, out val) && val > 1) {
                    _floorCountTxt.Text = (val - 1).ToString();
                }
            };

            System.Windows.Controls.Button btnPlus = new System.Windows.Controls.Button { Content = "+", Width = 30, Height = 32, FontWeight = FontWeights.Bold, Margin = new Thickness(2, 0, 0, 0) };
            btnPlus.Click += (s, e) => {
                int val;
                if (int.TryParse(_floorCountTxt.Text, out val)) {
                    _floorCountTxt.Text = (val + 1).ToString();
                }
            };

            floorCountPanel.Children.Add(_floorCountTxt);
            floorCountPanel.Children.Add(btnMinus);
            floorCountPanel.Children.Add(btnPlus);
            col1.Children.Add(floorCountPanel);
            System.Windows.Controls.Grid.SetColumn(col1, 1);
            configGrid.Children.Add(col1);

            // Column 2: Checkboxes
            StackPanel col2 = new StackPanel();
            col2.Children.Add(new TextBlock { Text = "OPCIONES DE REMATE Y FORMATO:", FontWeight = FontWeights.SemiBold, FontSize = 11, Foreground = darkBrush, Margin = new Thickness(0, 0, 0, 6) });

            _chkIncludeRoof = new System.Windows.Controls.CheckBox { Content = "Incluir ROOF (sobre el último piso)", IsChecked = true, Margin = new Thickness(0, 2, 0, 4) };
            _chkIncludeRoof.Checked += (s, e) => RecalculateNames();
            _chkIncludeRoof.Unchecked += (s, e) => RecalculateNames();

            _chkIncludeBulkhead = new System.Windows.Controls.CheckBox { Content = "Incluir BULKHEAD (sobre Roof)", IsChecked = true, Margin = new Thickness(0, 2, 0, 4) };
            _chkIncludeBulkhead.Checked += (s, e) => RecalculateNames();
            _chkIncludeBulkhead.Unchecked += (s, e) => RecalculateNames();

            _chkTwoDigits = new System.Windows.Controls.CheckBox { Content = "Prefijo 2 dígitos (01 1ST FL., 00 CELLAR)", IsChecked = true, Margin = new Thickness(0, 2, 0, 0) };
            _chkTwoDigits.Checked += (s, e) => RecalculateNames();
            _chkTwoDigits.Unchecked += (s, e) => RecalculateNames();

            col2.Children.Add(_chkIncludeRoof);
            col2.Children.Add(_chkIncludeBulkhead);
            col2.Children.Add(_chkTwoDigits);
            System.Windows.Controls.Grid.SetColumn(col2, 2);
            configGrid.Children.Add(col2);

            configCard.Child = configGrid;
            System.Windows.Controls.Grid.SetRow(configCard, 1);
            root.Children.Add(configCard);

            // ══════════════════════════════════════════════════════════
            // 2. LIVE PREVIEW DATAGRID CARD
            // ══════════════════════════════════════════════════════════
            Border gridCard = new Border
            {
                Background = cardBrush,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(16, 4, 16, 10),
                Padding = new Thickness(12)
            };

            System.Windows.Controls.Grid tableContainer = new System.Windows.Controls.Grid();
            tableContainer.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            tableContainer.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            TextBlock tableTitle = new TextBlock
            {
                Text = "VISTA PREVIA EN VIVO (Puedes hacer doble clic en 'Nombre Propuesto' para editarlo manualmente):",
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Foreground = mutedBrush,
                Margin = new Thickness(4, 0, 0, 8)
            };
            System.Windows.Controls.Grid.SetRow(tableTitle, 0);
            tableContainer.Children.Add(tableTitle);

            _dataGrid = new System.Windows.Controls.DataGrid
            {
                ItemsSource = _displayItems,
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserSortColumns = false,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E2E8F0")),
                RowHeight = 32,
                FontSize = 12.5,
                BorderThickness = new Thickness(1),
                BorderBrush = borderBrush,
                Background = System.Windows.Media.Brushes.White,
                AlternatingRowBackground = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#F8FAFC"))
            };

            // Checkbox Column
            var chkCol = new DataGridCheckBoxColumn
            {
                Header = "Aplicar",
                Binding = new System.Windows.Data.Binding("IsSelected") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                Width = 65
            };
            _dataGrid.Columns.Add(chkCol);

            // Elevation Column
            var elevCol = new DataGridTextColumn
            {
                Header = "Elevación",
                Binding = new System.Windows.Data.Binding("ElevationDisplay"),
                IsReadOnly = true,
                Width = 110
            };
            _dataGrid.Columns.Add(elevCol);

            // Current Name Column
            var currCol = new DataGridTextColumn
            {
                Header = "Nombre Actual",
                Binding = new System.Windows.Data.Binding("CurrentName"),
                IsReadOnly = true,
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            };
            _dataGrid.Columns.Add(currCol);

            // Proposed Name Column (Editable)
            var propCol = new DataGridTextColumn
            {
                Header = "Nombre Propuesto ✏️ (Editable)",
                Binding = new System.Windows.Data.Binding("ProposedName") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
                IsReadOnly = false,
                Width = new DataGridLength(1.3, DataGridLengthUnitType.Star)
            };
            _dataGrid.Columns.Add(propCol);

            System.Windows.Controls.Grid.SetRow(_dataGrid, 1);
            tableContainer.Children.Add(_dataGrid);

            gridCard.Child = tableContainer;
            System.Windows.Controls.Grid.SetRow(gridCard, 2);
            root.Children.Add(gridCard);

            // ══════════════════════════════════════════════════════════
            // 3. FOOTER ACTIONS
            // ══════════════════════════════════════════════════════════
            Border footer = new Border
            {
                Background = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#E2E8F0")),
                Padding = new Thickness(20, 12, 20, 12)
            };

            System.Windows.Controls.Grid footGrid = new System.Windows.Controls.Grid();
            footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusSummary = new TextBlock
            {
                Text = string.Format("{0} niveles detectados.", _allItems.Count),
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = darkBrush
            };
            footGrid.Children.Add(_statusSummary);

            StackPanel btnPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };

            System.Windows.Controls.Button btnReset = new System.Windows.Controls.Button
            {
                Content = "Restablecer",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnReset.Click += (s, e) => RecalculateNames();
            btnPanel.Children.Add(btnReset);

            System.Windows.Controls.Button btnCancel = new System.Windows.Controls.Button
            {
                Content = "Cancelar",
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, e) => Close();
            btnPanel.Children.Add(btnCancel);

            System.Windows.Controls.Button btnApply = new System.Windows.Controls.Button
            {
                Content = "✔ Aplicar Renombrado",
                Padding = new Thickness(18, 8, 18, 8),
                Background = accentBrush,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };
            btnApply.Click += (s, e) => ApplyRenaming();
            btnPanel.Children.Add(btnApply);

            System.Windows.Controls.Grid.SetColumn(btnPanel, 1);
            footGrid.Children.Add(btnPanel);

            footer.Child = footGrid;
            System.Windows.Controls.Grid.SetRow(footer, 3);
            root.Children.Add(footer);

            Content = root;
        }

        private void RecalculateNames()
        {
            if (_baseLevelCombo == null || _allItems.Count == 0) return;

            var baseItem = _baseLevelCombo.SelectedItem as LevelRenameItem ?? _allItems[0];
            int f;
            int floors = (_floorCountTxt != null && int.TryParse(_floorCountTxt.Text, out f)) ? Math.Max(1, f) : 1;
            bool roof = (_chkIncludeRoof != null ? _chkIncludeRoof.IsChecked : true) ?? true;
            bool bulkhead = (_chkIncludeBulkhead != null ? _chkIncludeBulkhead.IsChecked : true) ?? true;
            bool twoDigits = (_chkTwoDigits != null ? _chkTwoDigits.IsChecked : true) ?? true;

            LevelRenamerService.CalculateProposedNames(
                _allItems,
                baseItem,
                floors,
                roof,
                bulkhead,
                twoDigits);

            // Update UI status
            int changeCount = _allItems.Count(x => x.IsSelected && x.IsChanged);
            if (_statusSummary != null)
            {
                _statusSummary.Text = string.Format("⚡ {0} de {1} nivel(es) cambiarán de nombre.", changeCount, _allItems.Count);
            }

            if (_dataGrid != null)
            {
                _dataGrid.Items.Refresh();
            }
        }

        private void ApplyRenaming()
        {
            int toChange = _allItems.Count(x => x.IsSelected && x.IsChanged);
            if (toChange == 0)
            {
                MessageBox.Show("No hay cambios pendientes de renombrado para aplicar.",
                    "BauTools - Rename Levels", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                string.Format("¿Estás seguro de que deseas renombrar {0} nivel(es)?\n\n" +
                "Revit actualizará los nombres de los niveles seleccionados.", toChange),
                "Confirmar Renombrado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            var result = LevelRenamerService.ApplyRenaming(_doc, _allItems);
            int renamedCount = result.Item1;
            List<string> errors = result.Item2;

            if (errors.Count > 0)
            {
                string msg = string.Format("Se renombraron {0} nivel(es) con algunas advertencias:\n\n{1}",
                             renamedCount,
                             string.Join("\n", errors.Take(5)));
                if (errors.Count > 5)
                {
                    msg += string.Format("\n...y {0} más.", errors.Count - 5);
                }

                MessageBox.Show(msg, "BauTools - Resultado", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(string.Format("✅ ¡Éxito! Se renombraron correctamente {0} nivel(es).", renamedCount),
                    "BauTools - Rename Levels", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            DialogResult = true;
            Close();
        }
    }
}

```

### `ZoningFloorArea/ZoningFloorArea.addin`
```xml
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Zoning Floor Area Calculator</Name>
    <Assembly>C:\Users\MSI\AppData\Roaming\Autodesk\Revit\Addins\2026\ZoningFloorArea\ZoningFloorArea.dll</Assembly>
    <FullClassName>ZoningFloorArea.App</FullClassName>
    <ClientId>a7e492b1-5821-4f18-a621-8f9f7438c821</ClientId>
    <VendorId>BauTools</VendorId>
    <VendorDescription>BauTools by Arch Sergio Castro - Revit Productivity Add-ins</VendorDescription>
  </AddIn>
</RevitAddIns>

```

### `CLAUDE_CODE_DEV_GUIDE.md`
```markdown
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

```

