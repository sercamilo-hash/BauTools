$sourceDll = "g:\Other computers\My Laptop\ENT\REVIT DEVADDINS\ZoningFloorArea\ZoningFloorArea.dll"
$userAddinDir = "C:\Users\MSI\AppData\Roaming\Autodesk\Revit\Addins\2026"
$userTargetFolder = "$userAddinDir\ZoningFloorArea"
$resourcesFolder = "$userTargetFolder\Resources"

if (!(Test-Path $userTargetFolder)) { New-Item -ItemType Directory -Force -Path $userTargetFolder | Out-Null }
if (!(Test-Path $resourcesFolder)) { New-Item -ItemType Directory -Force -Path $resourcesFolder | Out-Null }

# Copy resources
Copy-Item "g:\Other computers\My Laptop\ENT\REVIT DEVADDINS\ZoningFloorArea\Resources\*" $resourcesFolder -Force -Recurse -ErrorAction SilentlyContinue

# Copy Addin manifest
Copy-Item "g:\Other computers\My Laptop\ENT\REVIT DEVADDINS\ZoningFloorArea\ZoningFloorArea.addin" "$userAddinDir\ZoningFloorArea.addin" -Force -ErrorAction SilentlyContinue

try {
    Copy-Item $sourceDll "$userTargetFolder\ZoningFloorArea.dll" -Force
    Write-Output "✅ Successfully deployed ZoningFloorArea.dll and resources to $userTargetFolder"
} catch {
    Write-Output "⚠️ Note: Revit 2026 is currently open and locking the DLL. Close Revit to finish applying the new version."
}
