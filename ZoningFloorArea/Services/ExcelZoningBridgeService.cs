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