using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public static class LevelRenamerService
    {
        public static string GetOrdinal(int number)
        {
            if (number <= 0) return number.ToString();

            int rem100 = number % 100;
            if (rem100 >= 11 && rem100 <= 13)
            {
                return string.Format("{0}TH", number);
            }

            switch (number % 10)
            {
                case 1: return string.Format("{0}ST", number);
                case 2: return string.Format("{0}ND", number);
                case 3: return string.Format("{0}RD", number);
                default: return string.Format("{0}TH", number);
            }
        }

        public static void CalculateProposedNames(
            List<LevelRenameItem> items,
            LevelRenameItem baseLevelItem,
            int numberOfFloors,
            bool includeRoof,
            bool includeBulkhead,
            bool useTwoDigitPrefix)
        {
            if (items == null || items.Count == 0) return;

            // Sort all items strictly by elevation
            List<LevelRenameItem> sorted = items.OrderBy(x => x.RawElevation).ToList();

            int baseIndex = sorted.IndexOf(baseLevelItem);
            if (baseIndex < 0)
            {
                // Default to first level >= 0.0 or index 0
                baseIndex = sorted.FindIndex(x => x.RawElevation >= -0.001);
                if (baseIndex < 0) baseIndex = 0;
            }

            // 1. Process underground levels (below baseIndex)
            // sorted[baseIndex - 1] is immediately below ground (CELLAR)
            // sorted[baseIndex - 2] is deeper (CELLAR 1 / SUB-CELLAR)
            int cellarCount = baseIndex;
            for (int i = 0; i < cellarCount; i++)
            {
                int depthFromGround = baseIndex - i; // 1 for immediately below, 2 for lower...
                
                string cellarName;
                if (depthFromGround == 1)
                {
                    cellarName = useTwoDigitPrefix ? "00 CELLAR" : "CELLAR";
                }
                else
                {
                    int cellarNum = depthFromGround - 1;
                    cellarName = string.Format("CELLAR {0}", cellarNum);
                }

                sorted[i].ProposedName = cellarName;
            }

            // 2. Process floors from baseLevel upwards
            int aboveCount = sorted.Count - baseIndex;
            int floorNumber = 1;

            for (int i = baseIndex; i < sorted.Count; i++)
            {
                int aboveIndex = i - baseIndex; // 0, 1, 2...

                if (aboveIndex < numberOfFloors)
                {
                    // Regular floor
                    string prefix = useTwoDigitPrefix ? string.Format("{0:D2} ", floorNumber) : string.Format("{0} ", floorNumber);
                    string ordinal = GetOrdinal(floorNumber);
                    sorted[i].ProposedName = string.Format("{0}{1} FL.", prefix, ordinal);
                    floorNumber++;
                }
                else
                {
                    // Upper levels (Roof, Bulkhead, etc.)
                    int extraIndex = aboveIndex - numberOfFloors; // 0 for first extra, 1 for second...

                    if (extraIndex == 0 && includeRoof)
                    {
                        string prefix = useTwoDigitPrefix ? string.Format("{0:D2} ", floorNumber) : "";
                        sorted[i].ProposedName = string.Format("{0}ROOF", prefix);
                        floorNumber++;
                    }
                    else if ((extraIndex == 1 && includeRoof && includeBulkhead) ||
                             (extraIndex == 0 && !includeRoof && includeBulkhead))
                    {
                        string prefix = useTwoDigitPrefix ? string.Format("{0:D2} ", floorNumber) : "";
                        sorted[i].ProposedName = string.Format("{0}BULKHEAD", prefix);
                        floorNumber++;
                    }
                    else
                    {
                        string prefix = useTwoDigitPrefix ? string.Format("{0:D2} ", floorNumber) : "";
                        sorted[i].ProposedName = string.Format("{0}UPPER LEVEL {1}", prefix, extraIndex + 1);
                        floorNumber++;
                    }
                }
            }
        }

        public static Tuple<int, List<string>> ApplyRenaming(
            Document doc,
            List<LevelRenameItem> items)
        {
            int renamed = 0;
            List<string> errors = new List<string>();

            List<LevelRenameItem> toRename = items.Where(x => x.IsSelected && x.IsChanged).ToList();
            if (toRename.Count == 0) return Tuple.Create(0, errors);

            using (Transaction tx = new Transaction(doc, "BauTools: Rename Levels"))
            {
                tx.Start();

                // Phase 1: Temporary unique names to avoid collisions in Revit
                foreach (LevelRenameItem item in toRename)
                {
                    try
                    {
                        item.LevelElement.Name = string.Format("_BAU_TEMP_{0}", Guid.NewGuid().ToString("N").Substring(0, 8));
                    }
                    catch (Exception ex)
                    {
                        errors.Add(string.Format("Error temporal en '{0}': {1}", item.CurrentName, ex.Message));
                    }
                }

                // Phase 2: Assign final proposed names
                foreach (LevelRenameItem item in toRename)
                {
                    try
                    {
                        item.LevelElement.Name = item.ProposedName;
                        renamed++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add(string.Format("Error al asignar '{0}' a '{1}': {2}", item.ProposedName, item.CurrentName, ex.Message));
                    }
                }

                tx.Commit();
            }

            return Tuple.Create(renamed, errors);
        }
    }
}
