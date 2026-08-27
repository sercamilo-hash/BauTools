using System;
using System.Collections.Generic;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class ScaleOption
    {
        public int ScaleValue { get; set; }
        public string DisplayName { get; set; }
        public double Factor { get; set; } // e.g. 1/96 = 0.0104167

        public ScaleOption(int val, string name)
        {
            ScaleValue = val;
            DisplayName = name;
            Factor = 1.0 / val;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class SmartScaleAdvisorService
    {
        public static readonly List<ScaleOption> StandardScales = new List<ScaleOption>
        {
            new ScaleOption(24, "1/2\" = 1'-0\" (1:24)"),
            new ScaleOption(32, "3/8\" = 1'-0\" (1:32)"),
            new ScaleOption(48, "1/4\" = 1'-0\" (1:48)"),
            new ScaleOption(64, "3/16\" = 1'-0\" (1:64)"),
            new ScaleOption(96, "1/8\" = 1'-0\" (1:96)"),
            new ScaleOption(128, "3/32\" = 1'-0\" (1:128)"),
            new ScaleOption(192, "1/16\" = 1'-0\" (1:192)"),
            new ScaleOption(50, "1:50 Metric"),
            new ScaleOption(100, "1:100 Metric"),
            new ScaleOption(200, "1:200 Metric")
        };

        public ScaleOption RecommendScale(
            double buildingWidthFt,
            double buildingDepthFt,
            TitleblockItem titleblock,
            SheetLayoutMode layoutMode)
        {
            if (buildingWidthFt <= 0) buildingWidthFt = 150.0;
            if (buildingDepthFt <= 0) buildingDepthFt = 100.0;
            if (titleblock == null) titleblock = new TitleblockItem();

            int rows = 1;
            int cols = 1;

            switch (layoutMode)
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

            // Usable slot in inches on paper (with margin for annotations/titles)
            double slotW_in = (titleblock.UsableWidthInches / cols) - 1.5;
            double slotH_in = (titleblock.UsableHeightInches / rows) - 2.0;

            if (slotW_in <= 2.0) slotW_in = 2.0;
            if (slotH_in <= 2.0) slotH_in = 2.0;

            // Iterate scales from largest to smallest to find the optimal fit
            ScaleOption bestFit = StandardScales[4]; // Default 1/8"

            foreach (ScaleOption s in StandardScales)
            {
                // Converted to paper inches
                double drawnW_in = (buildingWidthFt * 12.0) / s.ScaleValue;
                double drawnH_in = (buildingDepthFt * 12.0) / s.ScaleValue;

                if (drawnW_in <= slotW_in && drawnH_in <= slotH_in)
                {
                    return s;
                }
            }

            return StandardScales[6]; // Fallback to 1/16"
        }
    }
}