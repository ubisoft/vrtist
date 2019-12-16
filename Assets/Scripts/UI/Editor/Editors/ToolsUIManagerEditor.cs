using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    [CustomEditor(typeof(ToolsUIManager))]
    public class ToolsUIManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        private void OnSceneGUI()
        {
            ToolsUIManager uiManager = target as ToolsUIManager;

            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(10, 10, Screen.width, Screen.height));
            GUILayout.BeginVertical();
            {
                if (GUILayout.Button("Toggle Palette", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    Debug.Log("Pushed on Toggle Palette");
                    uiManager.TogglePalette();
                }
                if (GUILayout.Button("Selector", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    Debug.Log("Pushed on Selector Panel");
                    uiManager.TogglePanel("Selector");
                }
                if (GUILayout.Button("Paint", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    Debug.Log("Pushed on Paint Panel");
                    uiManager.TogglePanel("Paint");
                }
                if (GUILayout.Button("Lighting", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    Debug.Log("Pushed on Toggle Lighting Panel");
                    uiManager.TogglePanel("Lighting");
                }
                if (GUILayout.Button("Camera", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    Debug.Log("Pushed on Toggle Camera Panel");
                    uiManager.TogglePanel("Camera");
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            Handles.EndGUI();
        }
    }
}
