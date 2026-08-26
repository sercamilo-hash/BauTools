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
                        list.Add(new TitleblockItem
                        {
                            Name = disp,
                            FamilySymbolId = sym.Id
                        });
                    }
                }
            }
            catch
            {
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

        public XYZ GetViewportCenter(SheetLayoutMode mode, int gridIndex)
        {
            // Standard Sheet coordinate math (in feet on titleblock)
            switch (mode)
            {
                case SheetLayoutMode.SingleView:
                    return new XYZ(1.6, 1.3, 0);

                case SheetLayoutMode.SideBySide2Views:
                    if (gridIndex == 0) return new XYZ(0.85, 1.3, 0); // Left plan
                    return new XYZ(2.35, 1.3, 0); // Right plan

                case SheetLayoutMode.Matrix4Views:
                    if (gridIndex == 0) return new XYZ(0.85, 1.9, 0); // Top-Left
                    if (gridIndex == 1) return new XYZ(2.35, 1.9, 0); // Top-Right
                    if (gridIndex == 2) return new XYZ(0.85, 0.8, 0); // Bottom-Left
                    return new XYZ(2.35, 0.8, 0); // Bottom-Right

                default:
                    return new XYZ(1.5, 1.5, 0);
            }
        }

        public int ComposePlannedSheets(
            List<PlannedSheet> plannedSheets,
            ElementId titleblockId,
            bool repositionIfExists,
            Dictionary<string, ElementId> createdViewsByName)
        {
            if (plannedSheets == null || plannedSheets.Count == 0) return 0;

            int placedViewCount = 0;

            using (Transaction tx = new Transaction(_doc, "BauTools: Compose Sheets & Viewports"))
            {
                tx.Start();

                foreach (PlannedSheet ps in plannedSheets)
                {
                    ViewSheet sheet = FindExistingSheetByNumber(ps.SheetNumber);
                    if (sheet == null)
                    {
                        sheet = ViewSheet.Create(_doc, titleblockId != ElementId.InvalidElementId ? titleblockId : ElementId.InvalidElementId);
                        sheet.SheetNumber = ps.SheetNumber;
                        sheet.Name = ps.SheetName;
                    }

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
                        else
                        {
                            View found = FindViewByName(vp.ViewName);
                            if (found != null) viewId = found.Id;
                        }

                        if (viewId == ElementId.InvalidElementId) continue;

                        XYZ pos = GetViewportCenter(ps.LayoutMode, vp.GridIndex);

                        try
                        {
                            if (Viewport.CanAddViewToSheet(_doc, sheet.Id, viewId))
                            {
                                Viewport newVp = Viewport.Create(_doc, sheet.Id, viewId, pos);
                                if (newVp != null) placedViewCount++;
                            }
                            else if (repositionIfExists)
                            {
                                // Check if viewport already exists on this sheet
                                foreach (ElementId existingVpId in sheet.GetAllViewports())
                                {
                                    Viewport exVp = _doc.GetElement(existingVpId) as Viewport;
                                    if (exVp != null && exVp.ViewId == viewId)
                                    {
                                        exVp.SetBoxCenter(pos);
                                        placedViewCount++;
                                        break;
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                tx.Commit();
            }

            return placedViewCount;
        }

        public int PlaceViewsOnSheet(ElementId sheetId, List<ElementId> viewIds)
        {
            if (sheetId == ElementId.InvalidElementId || viewIds == null || viewIds.Count == 0)
                return 0;

            ViewSheet sheet = _doc.GetElement(sheetId) as ViewSheet;
            if (sheet == null) return 0;

            int placedCount = 0;

            using (Transaction tx = new Transaction(_doc, "BauTools: Place Views on Sheet"))
            {
                tx.Start();

                double startX = 1.2;
                double startY = 1.2;
                double spacingX = 1.2;
                int col = 0;

                foreach (ElementId vId in viewIds)
                {
                    try
                    {
                        if (Viewport.CanAddViewToSheet(_doc, sheet.Id, vId))
                        {
                            XYZ location = new XYZ(startX + (col * spacingX), startY, 0);
                            Viewport vp = Viewport.Create(_doc, sheet.Id, vId, location);
                            if (vp != null)
                            {
                                placedCount++;
                                col++;
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                tx.Commit();
            }

            return placedCount;
        }

        private ViewSheet FindExistingSheetByNumber(string sheetNumber)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(ViewSheet));
            foreach (ViewSheet vs in collector.Cast<ViewSheet>())
            {
                if (string.Equals(vs.SheetNumber, sheetNumber, StringComparison.OrdinalIgnoreCase))
                    return vs;
            }
            return null;
        }

        private View FindViewByName(string viewName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc).OfClass(typeof(View));
            foreach (View v in collector.Cast<View>())
            {
                if (string.Equals(v.Name, viewName, StringComparison.OrdinalIgnoreCase))
                    return v;
            }
            return null;
        }
    }
}