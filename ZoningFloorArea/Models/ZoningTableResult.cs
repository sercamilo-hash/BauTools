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
