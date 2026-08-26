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
