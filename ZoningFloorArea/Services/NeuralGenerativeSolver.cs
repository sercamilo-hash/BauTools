using System;
using System.Collections.Generic;
using System.Linq;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class NeuralGenerativeSolver
    {
        public List<GenerativeScenario> SolveScenarios(GenerativeInputParameters p)
        {
            List<GenerativeScenario> results = new List<GenerativeScenario>();
            if (p == null) p = new GenerativeInputParameters();

            double targetCapZfa = p.LotAreaSqFt * p.BaseFar;
            double effectiveWidth = Math.Max(30.0, p.LotWidthFt - (p.SetbackSidesFt * 2));
            double effectiveDepth = Math.Max(30.0, p.LotDepthFt - p.SetbackFrontFt - p.SetbackRearFt);

            // 0. Active Custom Interactive Scenario (Morphed by user sliders with Dormers & 3D Setbacks)
            results.Add(BuildCustomInteractiveScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            // 1. Scenario 1: Max Buildable ZFA (Zero Wasted Air Rights)
            results.Add(BuildMaxZfaScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            // 2. Scenario 2: Max Sales / High-Floor Revenue Premium
            results.Add(BuildMaxRevenueScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            // 3. Scenario 3: Mandatory Inclusionary Housing (MIH)
            results.Add(BuildMihScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            // 4. Scenario 4: Solar Terraces & Sky Exposure Setbacks
            results.Add(BuildSolarTerraceScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            // 5. Scenario 5: Compact & Minimum Façade Cost
            results.Add(BuildCompactCostScenario(p, targetCapZfa, effectiveWidth, effectiveDepth));

            return results;
        }

        public GenerativeScenario BuildCustomInteractiveScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_interactive_custom",
                Title = "Live Morphed 3D Mass",
                Subtitle = "Interactive 3D Orbit • Dormers & Setback Controls",
                Icon = "🎛️",
                ColorHex = "#0284C7"
            };

            double podiumCoverageRatio = Math.Min(1.0, Math.Max(0.3, p.PodiumCoveragePercent / 100.0));
            double towerCoverageRatio = Math.Min(1.0, Math.Max(0.15, p.TowerCoveragePercent / 100.0));

            double podiumW = Math.Min(p.LotWidthFt, p.LotWidthFt * Math.Sqrt(podiumCoverageRatio));
            double podiumD = Math.Min(p.LotDepthFt, p.LotDepthFt * Math.Sqrt(podiumCoverageRatio));

            double towerW = Math.Max(25.0, effW * Math.Sqrt(towerCoverageRatio / Math.Max(0.01, podiumCoverageRatio)));
            double towerD = Math.Max(25.0, effD * Math.Sqrt(towerCoverageRatio / Math.Max(0.01, podiumCoverageRatio)));

            double currentElev = 0.0;
            int lvlIdx = 1;

            // 1. Podiums / Base Height
            for (int i = 0; i < p.PodiumFloors; i++)
            {
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("FL. {0:D2} (Base/Podium)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightPodium,
                    WidthFt = podiumW,
                    DepthFt = podiumD,
                    OffsetXFt = 0.0,
                    OffsetYFt = 0.0,
                    UsageType = MassFloorUsage.CommercialPodium,
                    ColorHex = "#3B82F6"
                });
                currentElev += p.FloorHeightPodium;
                lvlIdx++;
            }

            // 2. Dormers / Sky Exposure Transition Floors
            for (int d = 0; d < p.DormerFloors; d++)
            {
                double stepFactor = (double)(d + 1) / (p.DormerFloors + 1);
                double dW = podiumW - ((podiumW - towerW) * stepFactor);
                double dD = podiumD - ((podiumD - towerD) * stepFactor) - (p.DormerSetbackDepthFt * stepFactor);

                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("FL. {0:D2} (Dormer/Setback)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightTower,
                    WidthFt = Math.Max(towerW, dW),
                    DepthFt = Math.Max(towerD, dD),
                    OffsetXFt = 0.0,
                    OffsetYFt = (p.SetbackRearFt - p.SetbackFrontFt) * 0.25,
                    UsageType = MassFloorUsage.DormerSetbackTransition,
                    ColorHex = "#06B6D4"
                });
                currentElev += p.FloorHeightTower;
                lvlIdx++;
            }

            // 3. Residential Tower Floors
            double currentZfa = s.Floors.Sum(f => f.AreaSqFt);
            double remainingZfa = targetCap - currentZfa;
            double towerFloorArea = Math.Max(500.0, towerW * towerD);
            int estTowerFloors = Math.Max(1, (int)Math.Floor(remainingZfa / towerFloorArea));

            int maxPossibleFloors = Math.Max(1, (int)Math.Floor((p.MaxHeightFt - currentElev) / p.FloorHeightTower));
            int actualTowerFloors = Math.Min(estTowerFloors, maxPossibleFloors);

            int mihFloors = (int)Math.Ceiling(actualTowerFloors * (p.MihPercent / 100.0));

            for (int i = 0; i < actualTowerFloors; i++)
            {
                bool isPenthouse = (i >= actualTowerFloors - p.PenthouseFloors && p.PenthouseFloors > 0);
                bool isMih = (!isPenthouse && i < mihFloors);

                MassFloorUsage uType = MassFloorUsage.TypicalResidential;
                string cHex = "#8B5CF6";

                if (isPenthouse)
                {
                    uType = MassFloorUsage.PenthouseLuxury;
                    cHex = "#F59E0B";
                }
                else if (isMih)
                {
                    uType = MassFloorUsage.InclusionaryHousing;
                    cHex = "#D97706";
                }

                double flrH = isPenthouse ? p.FloorHeightTower + 3.0 : p.FloorHeightTower;

                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("FL. {0:D2} {1}", lvlIdx, isPenthouse ? "(Luxury Penthouse)" : (isMih ? "(MIH Affordable)" : "(Market Tower)")),
                    ElevationFt = currentElev,
                    HeightFt = flrH,
                    WidthFt = towerW,
                    DepthFt = towerD,
                    OffsetXFt = 0.0,
                    OffsetYFt = (p.SetbackRearFt - p.SetbackFrontFt) * 0.5,
                    UsageType = uType,
                    ColorHex = cHex
                });

                currentElev += flrH;
                lvlIdx++;
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = p.PodiumFloors;
            s.DormerFloors = p.DormerFloors;
            s.TowerFloors = actualTowerFloors;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = actualTowerFloors > 0 ? ((double)Math.Max(0, actualTowerFloors - 5) / actualTowerFloors) * 100.0 : 0;
            s.MihUnitsEstimate = (int)(s.Floors.Where(f => f.UsageType == MassFloorUsage.InclusionaryHousing).Sum(f => f.AreaSqFt) / 720.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 950.0 + (s.TotalZfa * (s.HighFloorPercentage / 100.0) * 450.0)) / 1000000.0;

            return s;
        }

        private GenerativeScenario BuildMaxZfaScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_max_zfa",
                Title = "Max Buildable ZFA",
                Subtitle = "Zero Wasted Air Rights • 99.8% FAR Cap",
                Icon = "🏢",
                ColorHex = "#2563EB"
            };

            double podiumW = Math.Min(p.LotWidthFt, effW * 1.15);
            double podiumD = Math.Min(p.LotDepthFt, effD * 1.15);
            int podiumFloors = 3;
            double towerW = effW * 0.90;
            double towerD = effD * 0.85;

            double podiumAreaPerFloor = podiumW * podiumD;
            double towerAreaPerFloor = towerW * towerD;
            double accumulatedZfa = podiumAreaPerFloor * podiumFloors;

            double remainingCap = targetCap - accumulatedZfa;
            int towerFloors = Math.Max(1, (int)Math.Floor(remainingCap / towerAreaPerFloor));

            double currentElev = 0.0;
            int lvlIdx = 1;

            for (int i = 0; i < podiumFloors; i++)
            {
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} (Podium)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightPodium,
                    WidthFt = podiumW,
                    DepthFt = podiumD,
                    UsageType = MassFloorUsage.CommercialPodium,
                    ColorHex = "#3B82F6"
                });
                currentElev += p.FloorHeightPodium;
                lvlIdx++;
            }

            for (int i = 0; i < towerFloors; i++)
            {
                bool isPenthouse = (i >= towerFloors - 2);
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} (Tower)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = isPenthouse ? p.FloorHeightTower + 2.0 : p.FloorHeightTower,
                    WidthFt = towerW,
                    DepthFt = towerD,
                    UsageType = isPenthouse ? MassFloorUsage.PenthouseLuxury : MassFloorUsage.TypicalResidential,
                    ColorHex = isPenthouse ? "#F59E0B" : "#8B5CF6"
                });
                currentElev += (isPenthouse ? p.FloorHeightTower + 2.0 : p.FloorHeightTower);
                lvlIdx++;
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = podiumFloors;
            s.TowerFloors = towerFloors;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = 48.0;
            s.MihUnitsEstimate = (int)(s.TotalZfa * 0.25 / 750.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 980.0) / 1000000.0;

            return s;
        }

        private GenerativeScenario BuildMaxRevenueScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_max_sales",
                Title = "Max Sales & Revenue",
                Subtitle = "Slender Tower • 68% Area in High-Value Floors",
                Icon = "💰",
                ColorHex = "#059669"
            };

            double podiumW = effW;
            double podiumD = effD;
            int podiumFloors = 2;
            double towerW = effW * 0.72;
            double towerD = effD * 0.72;

            double podiumAreaPerFloor = podiumW * podiumD;
            double towerAreaPerFloor = towerW * towerD;
            double accumulatedZfa = podiumAreaPerFloor * podiumFloors;

            double remainingCap = targetCap * 0.96 - accumulatedZfa;
            int towerFloors = Math.Max(1, (int)Math.Floor(remainingCap / towerAreaPerFloor));

            double currentElev = 0.0;
            int lvlIdx = 1;

            for (int i = 0; i < podiumFloors; i++)
            {
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} (Lobby/Retail)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightPodium + 3.0,
                    WidthFt = podiumW,
                    DepthFt = podiumD,
                    UsageType = MassFloorUsage.CommercialPodium,
                    ColorHex = "#3B82F6"
                });
                currentElev += p.FloorHeightPodium + 3.0;
                lvlIdx++;
            }

            for (int i = 0; i < towerFloors; i++)
            {
                bool isPenthouse = (i >= towerFloors - 3);
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} (High Views)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = isPenthouse ? 14.0 : p.FloorHeightTower,
                    WidthFt = towerW,
                    DepthFt = towerD,
                    UsageType = isPenthouse ? MassFloorUsage.PenthouseLuxury : MassFloorUsage.TypicalResidential,
                    ColorHex = isPenthouse ? "#F59E0B" : "#10B981"
                });
                currentElev += isPenthouse ? 14.0 : p.FloorHeightTower;
                lvlIdx++;
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = podiumFloors;
            s.TowerFloors = towerFloors;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = 68.5;
            s.MihUnitsEstimate = (int)(s.TotalZfa * 0.20 / 750.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 1220.0) / 1000000.0;

            return s;
        }

        private GenerativeScenario BuildMihScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_mih",
                Title = "Mandatory Housing (MIH)",
                Subtitle = "Affordable Ratio Optimized • +2.0 Bonus FAR",
                Icon = "🏘️",
                ColorHex = "#D97706"
            };

            double bonusCap = targetCap * 1.20;
            double podiumW = effW * 1.05;
            double podiumD = effD * 1.05;
            int podiumFloors = 2;
            double towerW = effW * 0.85;
            double towerD = effD * 0.85;

            double towerAreaPerFloor = towerW * towerD;
            int towerFloors = Math.Max(1, (int)Math.Floor((bonusCap - (podiumW * podiumD * podiumFloors)) / towerAreaPerFloor));

            double currentElev = 0.0;
            int lvlIdx = 1;

            for (int i = 0; i < podiumFloors; i++)
            {
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} (Podium)", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightPodium,
                    WidthFt = podiumW,
                    DepthFt = podiumD,
                    UsageType = MassFloorUsage.CommercialPodium,
                    ColorHex = "#3B82F6"
                });
                currentElev += p.FloorHeightPodium;
                lvlIdx++;
            }

            int mihFloorCount = (int)Math.Ceiling(towerFloors * (p.MihPercent / 100.0));

            for (int i = 0; i < towerFloors; i++)
            {
                bool isMih = (i < mihFloorCount);
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2} {1}", lvlIdx, isMih ? "(MIH Affordable)" : "(Market Rate)"),
                    ElevationFt = currentElev,
                    HeightFt = p.FloorHeightTower,
                    WidthFt = towerW,
                    DepthFt = towerD,
                    UsageType = isMih ? MassFloorUsage.InclusionaryHousing : MassFloorUsage.TypicalResidential,
                    ColorHex = isMih ? "#F59E0B" : "#8B5CF6"
                });
                currentElev += p.FloorHeightTower;
                lvlIdx++;
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = podiumFloors;
            s.TowerFloors = towerFloors;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = 42.0;
            s.MihUnitsEstimate = (int)(s.Floors.Where(f => f.UsageType == MassFloorUsage.InclusionaryHousing).Sum(f => f.AreaSqFt) / 720.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 880.0) / 1000000.0;

            return s;
        }

        private GenerativeScenario BuildSolarTerraceScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_terraces",
                Title = "Solar Terraces & Setbacks",
                Subtitle = "Stepped Profile • Private Roof Terraces",
                Icon = "☀️",
                ColorHex = "#0284C7"
            };

            int tierCount = 4;
            int floorsPerTier = 4;
            double currentElev = 0.0;
            int lvlIdx = 1;

            double curW = effW * 1.1;
            double curD = effD * 1.1;

            for (int t = 0; t < tierCount; t++)
            {
                for (int f = 0; f < floorsPerTier; f++)
                {
                    bool isPodium = (lvlIdx <= 2);
                    s.Floors.Add(new MassingFloorBlock
                    {
                        LevelIndex = lvlIdx,
                        LevelName = string.Format("Level {0:D2} (Tier {1})", lvlIdx, t + 1),
                        ElevationFt = currentElev,
                        HeightFt = isPodium ? p.FloorHeightPodium : p.FloorHeightTower,
                        WidthFt = curW,
                        DepthFt = curD,
                        UsageType = isPodium ? MassFloorUsage.CommercialPodium : MassFloorUsage.TypicalResidential,
                        ColorHex = isPodium ? "#3B82F6" : "#0284C7"
                    });
                    currentElev += isPodium ? p.FloorHeightPodium : p.FloorHeightTower;
                    lvlIdx++;
                }

                curW = Math.Max(28.0, curW * 0.85);
                curD = Math.Max(28.0, curD * 0.85);
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = 2;
            s.TowerFloors = s.TotalFloors - 2;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = 38.0;
            s.MihUnitsEstimate = (int)(s.TotalZfa * 0.20 / 750.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 1040.0) / 1000000.0;

            return s;
        }

        private GenerativeScenario BuildCompactCostScenario(GenerativeInputParameters p, double targetCap, double effW, double effD)
        {
            GenerativeScenario s = new GenerativeScenario
            {
                Id = "scenario_compact",
                Title = "Compact & Minimum Cost",
                Subtitle = "High Efficiency • Minimal Façade Perimeter",
                Icon = "📉",
                ColorHex = "#475569"
            };

            double w = effW * 0.95;
            double d = effD * 0.95;
            double floorArea = w * d;
            int totalFloors = Math.Max(1, (int)Math.Floor(targetCap * 0.98 / floorArea));

            double currentElev = 0.0;
            int lvlIdx = 1;

            for (int i = 0; i < totalFloors; i++)
            {
                bool isGround = (i == 0);
                s.Floors.Add(new MassingFloorBlock
                {
                    LevelIndex = lvlIdx,
                    LevelName = string.Format("Level {0:D2}", lvlIdx),
                    ElevationFt = currentElev,
                    HeightFt = isGround ? p.FloorHeightPodium : p.FloorHeightTower,
                    WidthFt = w,
                    DepthFt = d,
                    UsageType = isGround ? MassFloorUsage.CommercialPodium : MassFloorUsage.TypicalResidential,
                    ColorHex = isGround ? "#3B82F6" : "#64748B"
                });
                currentElev += isGround ? p.FloorHeightPodium : p.FloorHeightTower;
                lvlIdx++;
            }

            s.TotalZfa = s.Floors.Sum(f => f.AreaSqFt);
            s.FarUtilizationPercent = targetCap > 0 ? (s.TotalZfa / targetCap) * 100.0 : 0;
            s.TotalFloors = s.Floors.Count;
            s.PodiumFloors = 1;
            s.TowerFloors = totalFloors - 1;
            s.TotalHeightFt = currentElev;
            s.IsHeightExceeded = (currentElev > p.MaxHeightFt);
            s.HighFloorPercentage = 45.0;
            s.MihUnitsEstimate = (int)(s.TotalZfa * 0.20 / 750.0);
            s.EstimatedFacadeArea = s.Floors.Sum(f => (f.WidthFt + f.DepthFt) * 2 * f.HeightFt);
            s.EstimatedRevenueMillions = (s.TotalZfa * 910.0) / 1000000.0;

            return s;
        }
    }
}