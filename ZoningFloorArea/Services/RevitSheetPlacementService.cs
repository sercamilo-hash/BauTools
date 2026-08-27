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