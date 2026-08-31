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

        // Proposed GFA
        public double ResidentialGrossFloorArea { get; set; }
        public double CommercialGrossFloorArea { get; set; }
        public double TotalGrossFloorArea
        {
            get { return ResidentialGrossFloorArea + CommercialGrossFloorArea; }
        }

        // Deductions
        public Dictionary<string, double> Deductions { get; set; }
        public Dictionary<string, double> ResidentialDeductions { get; set; }
        public Dictionary<string, double> CommercialDeductions { get; set; }

        public double TotalDeductions { get; set; }
        public double TotalResidentialDeductions { get; set; }
        public double TotalCommercialDeductions { get; set; }

        public double UlebPercent { get; set; }
        public double LotArea { get; set; }

        // ZFA Calculations
        public double ResidentialZfa
        {
            get
            {
                double net = Math.Max(0, ResidentialGrossFloorArea - TotalResidentialDeductions);
                double uleb = net * UlebPercent;
                return Math.Max(0, net - uleb);
            }
        }

        public double CommercialZfa
        {
            get
            {
                double net = Math.Max(0, CommercialGrossFloorArea - TotalCommercialDeductions);
                double uleb = net * UlebPercent;
                return Math.Max(0, net - uleb);
            }
        }

        public double TotalZfa
        {
            get { return ResidentialZfa + CommercialZfa; }
        }

        // FAR Calculations
        public double ResidentialFar
        {
            get { return LotArea > 0 ? ResidentialZfa / LotArea : 0; }
        }

        public double CommercialFar
        {
            get { return LotArea > 0 ? CommercialZfa / LotArea : 0; }
        }

        public double TotalFar
        {
            get { return LotArea > 0 ? TotalZfa / LotArea : 0; }
        }

        // Backward compatibility
        public double GrossFloorArea
        {
            get { return TotalGrossFloorArea; }
            set { ResidentialGrossFloorArea = value; }
        }

        public double NetArea
        {
            get { return Math.Max(0, TotalGrossFloorArea - TotalDeductions); }
        }

        public double UlebAmount
        {
            get { return NetArea * UlebPercent; }
        }

        public double ZoningFloorArea
        {
            get { return TotalZfa; }
        }

        public double Far
        {
            get { return TotalFar; }
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
            ResidentialDeductions = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            CommercialDeductions = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            UlebPercent = 0.0;
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

        public void SetResidentialDeduction(string categoryName, double val)
        {
            ResidentialDeductions[categoryName] = val;
            double comVal = 0.0;
            CommercialDeductions.TryGetValue(categoryName, out comVal);
            Deductions[categoryName] = val + comVal;
            RecalculateTotalDeductions();
        }

        public void SetCommercialDeduction(string categoryName, double val)
        {
            CommercialDeductions[categoryName] = val;
            double resVal = 0.0;
            ResidentialDeductions.TryGetValue(categoryName, out resVal);
            Deductions[categoryName] = val + resVal;
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

            double resSum = 0;
            foreach (KeyValuePair<string, double> kvp in ResidentialDeductions)
            {
                resSum += kvp.Value;
            }
            TotalResidentialDeductions = resSum;

            double comSum = 0;
            foreach (KeyValuePair<string, double> kvp in CommercialDeductions)
            {
                comSum += kvp.Value;
            }
            TotalCommercialDeductions = comSum;
        }
    }
}
