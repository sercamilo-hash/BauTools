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
