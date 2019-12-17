using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VRtist
{
    [CustomEditor(typeof(Lighting))]
    public class LightingEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        private void OnSceneGUI()
        {
            Lighting lighting = target as Lighting;

            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(10, 10, Screen.width, Screen.height));
            GUILayout.BeginVertical();
            {
                if (GUILayout.Button("Create POINT", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    lighting.CreateLight("Point");
                }
                if (GUILayout.Button("Create SPOT", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    lighting.CreateLight("Spot");
                }
                if (GUILayout.Button("Create SUN", GUILayout.Width(100), GUILayout.Height(30)))
                {
                    lighting.CreateLight("Sun");
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            Handles.EndGUI();
        }
    }
}
