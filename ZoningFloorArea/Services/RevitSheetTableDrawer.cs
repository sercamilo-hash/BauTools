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
                double colWidthResGross = 1.2;
                double colWidthComGross = 1.2;
                double colWidthDed = 1.0;
                double colWidthResZfa = 1.1;
                double colWidthComZfa = 1.1;
                double colWidthTotZfa = 1.2;
                double colWidthResFar = 0.8;
                double colWidthComFar = 0.8;
                double colWidthTotFar = 0.9;

                int dedCount = table.DeductionCategories.Count;

                double propWidth = colWidthResGross + colWidthComGross;
                double dedsSpanWidth = dedCount * colWidthDed;
                double zfaWidth = colWidthResZfa + colWidthComZfa + colWidthTotZfa;
                double farWidth = colWidthResFar + colWidthComFar + colWidthTotFar;
                double totalWidth = colWidthLevel + propWidth + dedsSpanWidth + zfaWidth + farWidth;

                double rowHeight = 0.3;
                double headerHeight = 0.4;
                double startX = 0.0;
                double currentY = 0.0;

                ElementId textTypeId = _doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);

                // Row 1: Title Header
                DrawRectangle(_doc, draftingView, startX, currentY - headerHeight, totalWidth, headerHeight);
                CreateCellText(_doc, draftingView, startX, currentY - headerHeight, totalWidth, headerHeight,
                    "FLOOR AREA CALCULATIONS", textTypeId, HorizontalTextAlignment.Center, true);

                currentY -= headerHeight;

                // Row 2: Top Grouping Headers (PROPOSED, DEDUCTIONS, ZFA, FAR)
                double xHdr = startX;
                DrawRectangle(_doc, draftingView, xHdr, currentY - headerHeight, colWidthLevel, headerHeight);
                xHdr += colWidthLevel;

                DrawRectangle(_doc, draftingView, xHdr, currentY - headerHeight, propWidth, headerHeight);
                CreateCellText(_doc, draftingView, xHdr, currentY - headerHeight, propWidth, headerHeight, "PROPOSED", textTypeId, HorizontalTextAlignment.Center, true);
                xHdr += propWidth;

                DrawRectangle(_doc, draftingView, xHdr, currentY - headerHeight, dedsSpanWidth, headerHeight);
                CreateCellText(_doc, draftingView, xHdr, currentY - headerHeight, dedsSpanWidth, headerHeight, "DEDUCTIONS", textTypeId, HorizontalTextAlignment.Center, true);
                xHdr += dedsSpanWidth;

                DrawRectangle(_doc, draftingView, xHdr, currentY - headerHeight, zfaWidth, headerHeight);
                CreateCellText(_doc, draftingView, xHdr, currentY - headerHeight, zfaWidth, headerHeight, "ZFA", textTypeId, HorizontalTextAlignment.Center, true);
                xHdr += zfaWidth;

                DrawRectangle(_doc, draftingView, xHdr, currentY - headerHeight, farWidth, headerHeight);
                CreateCellText(_doc, draftingView, xHdr, currentY - headerHeight, farWidth, headerHeight, "FAR", textTypeId, HorizontalTextAlignment.Center, true);

                currentY -= headerHeight;

                // Row 3: Sub-headers
                double xCursor = startX;

                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthLevel, headerHeight, "LEVEL", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthResGross, headerHeight, "RESIDENTIAL\nGFA", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthComGross, headerHeight, "COMMERCIAL\nGFA", textTypeId);

                foreach (string cat in table.DeductionCategories)
                {
                    xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthDed, headerHeight, cat, textTypeId);
                }

                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthResZfa, headerHeight, "RESIDENTIAL", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthComZfa, headerHeight, "COMMERCIAL", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthTotZfa, headerHeight, "TOTAL ZFA", textTypeId);

                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthResFar, headerHeight, "RESIDENTIAL", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthComFar, headerHeight, "COMMERCIAL", textTypeId);
                xCursor = DrawColumnHeader(_doc, draftingView, xCursor, currentY, colWidthTotFar, headerHeight, "TOTAL FAR", textTypeId);

                currentY -= headerHeight;

                // Data Rows (1 per level)
                foreach (LevelZoningRow r in table.Rows)
                {
                    xCursor = startX;

                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthLevel, rowHeight, r.LevelName, textTypeId, HorizontalTextAlignment.Center, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthResGross, rowHeight, FormatNum(r.ResidentialGrossFloorArea), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthComGross, rowHeight, FormatNum(r.CommercialGrossFloorArea), textTypeId, HorizontalTextAlignment.Right, false);

                    foreach (string cat in table.DeductionCategories)
                    {
                        double val = r.GetDeduction(cat);
                        xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthDed, rowHeight, FormatNum(val), textTypeId, HorizontalTextAlignment.Right, false);
                    }

                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthResZfa, rowHeight, FormatNum(r.ResidentialZfa), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthComZfa, rowHeight, FormatNum(r.CommercialZfa), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthTotZfa, rowHeight, FormatNum(r.TotalZfa), textTypeId, HorizontalTextAlignment.Right, true);

                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthResFar, rowHeight, FormatNum(r.ResidentialFar), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthComFar, rowHeight, FormatNum(r.CommercialFar), textTypeId, HorizontalTextAlignment.Right, false);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthTotFar, rowHeight, FormatNum(r.TotalFar), textTypeId, HorizontalTextAlignment.Right, true);

                    currentY -= rowHeight;
                }

                // Bottom Row: TOTALS
                LevelZoningRow tot = table.TotalsRow ?? new LevelZoningRow { LevelName = "TOTALS" };
                xCursor = startX;

                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthLevel, rowHeight, "TOTALS", textTypeId, HorizontalTextAlignment.Center, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthResGross, rowHeight, FormatNum(tot.ResidentialGrossFloorArea), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthComGross, rowHeight, FormatNum(tot.CommercialGrossFloorArea), textTypeId, HorizontalTextAlignment.Right, true);

                foreach (string cat in table.DeductionCategories)
                {
                    double val = tot.GetDeduction(cat);
                    xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthDed, rowHeight, FormatNum(val), textTypeId, HorizontalTextAlignment.Right, true);
                }

                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthResZfa, rowHeight, FormatNum(tot.ResidentialZfa), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthComZfa, rowHeight, FormatNum(tot.CommercialZfa), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthTotZfa, rowHeight, FormatNum(tot.TotalZfa), textTypeId, HorizontalTextAlignment.Right, true);

                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthResFar, rowHeight, FormatNum(tot.ResidentialFar), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthComFar, rowHeight, FormatNum(tot.CommercialFar), textTypeId, HorizontalTextAlignment.Right, true);
                xCursor = DrawCell(_doc, draftingView, xCursor, currentY - rowHeight, colWidthTotFar, rowHeight, FormatNum(tot.TotalFar), textTypeId, HorizontalTextAlignment.Right, true);

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
