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
