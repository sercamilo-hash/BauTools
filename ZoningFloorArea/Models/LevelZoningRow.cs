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
