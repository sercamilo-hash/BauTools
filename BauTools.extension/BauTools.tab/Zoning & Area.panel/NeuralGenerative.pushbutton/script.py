# -*- coding: utf-8 -*-
"""Neural Generative Zoning — BauTools Suite"""
import clr
import os
from pyrevit import revit, DB, UI

ext_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(__file__))))
dll_path = os.path.join(ext_dir, "bin", "ZoningFloorArea.dll")

if not os.path.exists(dll_path):
    UI.TaskDialog.Show("BauTools Error", "Could not locate ZoningFloorArea.dll at:\n" + dll_path)
else:
    clr.AddReferenceToFileAndPath(dll_path)
    from ZoningFloorArea.Views import GenerativeZoningWindow
    
    doc = revit.doc
    if doc is None:
        UI.TaskDialog.Show("BauTools", "Please open a Revit project before launching Neural Generative.")
    else:
        win = GenerativeZoningWindow(doc)
        win.ShowDialog()