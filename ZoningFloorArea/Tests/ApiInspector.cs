using System;
using System.IO;
using System.Reflection;

namespace ZoningFloorArea.Tests
{
    public class ApiInspector
    {
        public static void Inspect()
        {
            string dllPath = @"g:\Other computers\My Laptop\ENT\REVIT DEVADDINS\ZoningFloorArea\lib\RevitAPI.dll";
            if (!File.Exists(dllPath))
            {
                Console.WriteLine("DLL not found: " + dllPath);
                return;
            }

            try
            {
                Assembly asm = Assembly.LoadFrom(dllPath);
                Console.WriteLine("Assembly Loaded: " + asm.FullName);

                foreach (Type t in asm.GetTypes())
                {
                    if (t.Name.Contains("PropertyLine", StringComparison.OrdinalIgnoreCase) || 
                        t.Name.Contains("SiteProperty", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("=== TYPE: " + t.FullName + " ===");
                        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                        {
                            Console.WriteLine("  Method: " + m.Name + " (" + string.Join(", ", Array.ConvertAll(m.GetParameters(), p => p.ParameterType.Name + " " + p.Name)) + ")");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }
    }
}
