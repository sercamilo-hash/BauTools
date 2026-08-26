using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using ZoningFloorArea.Models;

namespace ZoningFloorArea.Services
{
    public class TypicalFloorStorageService
    {
        private static readonly Guid SCHEMA_GUID = new Guid("A4D59E72-8C1B-4E33-9F52-D9A3B5C7E120");
        private const string SCHEMA_NAME = "BauToolsTypicalFloorsSchema";
        private const string FIELD_NAME = "TypicalFloorsJson";
        private const string FIELD_BUILDINGS = "BuildingsJson";

        private static Schema GetOrCreateSchema()
        {
            Schema existing = Schema.Lookup(SCHEMA_GUID);
            if (existing != null)
            {
                return existing;
            }

            SchemaBuilder builder = new SchemaBuilder(SCHEMA_GUID);
            builder.SetSchemaName(SCHEMA_NAME);
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.SetVendorId("BauTools");
            builder.SetApplicationGUID(new Guid("F3B1A2C4-D5E6-4F7A-8B9C-0D1E2F3A4B5C"));

            FieldBuilder field = builder.AddSimpleField(FIELD_NAME, typeof(string));
            field.SetDocumentation("JSON serialized list of TypicalFloorGroup definitions for BauTools ZFA.");

            return builder.Finish();
        }

        public List<BuildingDefinition> LoadBuildings(Document doc)
        {
            List<BuildingDefinition> result = new List<BuildingDefinition>();
            if (doc == null) return result;

            try
            {
                Schema schema = GetOrCreateSchema();
                if (schema == null) return result;

                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(DataStorage));

                foreach (Element elem in collector)
                {
                    DataStorage storage = elem as DataStorage;
                    if (storage != null)
                    {
                        Entity entity = storage.GetEntity(schema);
                        if (entity != null && entity.IsValid())
                        {
                            string json = entity.Get<string>(schema.GetField(FIELD_NAME));
                            if (!string.IsNullOrEmpty(json))
                            {
                                // First check if json is List<BuildingDefinition>
                                try
                                {
                                    List<BuildingDefinition> bldgs = JsonSerializer.Deserialize<List<BuildingDefinition>>(json);
                                    if (bldgs != null && bldgs.Count > 0 && bldgs[0].TypicalGroups != null)
                                    {
                                        return bldgs;
                                    }
                                }
                                catch
                                {
                                    // Fallback: legacy flat List<TypicalFloorGroup>
                                    List<TypicalFloorGroup> legacyGroups = JsonSerializer.Deserialize<List<TypicalFloorGroup>>(json);
                                    if (legacyGroups != null && legacyGroups.Count > 0)
                                    {
                                        BuildingDefinition defaultBldg = new BuildingDefinition("Building 1");
                                        defaultBldg.TypicalGroups = new ObservableCollection<TypicalFloorGroup>(legacyGroups);
                                        result.Add(defaultBldg);
                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            if (result.Count == 0)
            {
                result.Add(new BuildingDefinition("Building 1"));
            }

            return result;
        }

        public bool SaveBuildings(Document doc, List<BuildingDefinition> buildings)
        {
            if (doc == null || buildings == null) return false;

            try
            {
                Schema schema = GetOrCreateSchema();
                if (schema == null) return false;

                string json = JsonSerializer.Serialize(buildings);

                using (Transaction t = new Transaction(doc, "BauTools - Save Multi-Building Definitions"))
                {
                    t.Start();

                    DataStorage targetStorage = null;
                    FilteredElementCollector collector = new FilteredElementCollector(doc)
                        .OfClass(typeof(DataStorage));

                    foreach (Element elem in collector)
                    {
                        DataStorage storage = elem as DataStorage;
                        if (storage != null)
                        {
                            Entity entity = storage.GetEntity(schema);
                            if (entity != null && entity.IsValid())
                            {
                                targetStorage = storage;
                                break;
                            }
                        }
                    }

                    if (targetStorage == null)
                    {
                        targetStorage = DataStorage.Create(doc);
                    }

                    Entity newEntity = new Entity(schema);
                    newEntity.Set(schema.GetField(FIELD_NAME), json);
                    targetStorage.SetEntity(newEntity);

                    t.Commit();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
