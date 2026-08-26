# -*- coding: utf-8 -*-
"""Bubble Heads — BauTools Suite"""
import clr
import os
from pyrevit import revit, DB, UI

ext_dir = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(__file__))))
dll_path = os.path.join(ext_dir, "bin", "ZoningFloorArea.dll")

if not os.path.exists(dll_path):
    UI.TaskDialog.Show("BauTools Error", "Could not locate ZoningFloorArea.dll at:\n" + dll_path)
else:
    clr.AddReferenceToFileAndPath(dll_path)
    from ZoningFloorArea.Views import BubbleHeadsWindow
    
    doc = revit.doc
    if doc is None:
        UI.TaskDialog.Show("BauTools", "Please open a Revit project before launching Bubble Heads.")
    else:
        active_view = doc.ActiveView
        if active_view is None:
            UI.TaskDialog.Show("BauTools", "No active view is currently open.")
        else:
            win = BubbleHeadsWindow(doc, active_view)
            win.ShowDialog()