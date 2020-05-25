using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    public class GoToUtils
    {
        [MenuItem("VRtist/GOTO Palette")]
        static void GoToPalette()
        {
            GameObject palette = GameObject.Find("Camera Rig/Pivot/LeftHandle/PaletteHandle/Palette");
            UnityEditor.Selection.activeTransform = palette.transform;
        }

        [MenuItem("VRtist/GOTO Panels")]
        static void GoToPanels()
        {
            GameObject panels = GameObject.Find("Camera Rig/Pivot/LeftHandle/PaletteHandle/Palette/MainPanel/ToolsPanelGroup");
            UnityEditor.Selection.activeTransform = panels.transform;
        }

        [MenuItem("VRtist/GOTO Tools")]
        static void GoToTools()
        {
            GameObject tools = GameObject.Find("Camera Rig/Pivot/RightHandle/Tools");
            UnityEditor.Selection.activeTransform = tools.transform;
        }
    }
}
