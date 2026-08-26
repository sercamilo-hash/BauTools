namespace ZoningFloorArea.Models
{
    public enum UnitDisplayMode
    {
        SquareFeet,
        SquareMeters
    }

    public class MappingConfig
    {
        public string GrossAreaSchemeName { get; set; }
        public string DeductionAreaSchemeName { get; set; }
        public string DeductionTypeParameterName { get; set; }
        public string UsageCategoryParameterName { get; set; }
        public string BuildingParameterName { get; set; }
        public string MasterScopeBoxName { get; set; }
        public string ViewBuildingParameterName { get; set; }

        public string BuildingName { get; set; }
        public double LotArea { get; set; }
        public double UlebPercent { get; set; }

        public UnitDisplayMode DisplayUnit { get; set; }

        public MappingConfig()
        {
            GrossAreaSchemeName = "Gross Building";
            DeductionAreaSchemeName = "Rentable";
            DeductionTypeParameterName = "Deduction";
            UsageCategoryParameterName = "Comments";
            BuildingParameterName = "Building";
            MasterScopeBoxName = "";
            ViewBuildingParameterName = "Building";

            BuildingName = "BUILDING C";
            LotArea = 34500.0;
            UlebPercent = 0.05;
            DisplayUnit = UnitDisplayMode.SquareFeet;
        }
    }
}
