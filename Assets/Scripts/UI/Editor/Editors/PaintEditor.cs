using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

namespace VRtist
{
    [CustomEditor(typeof(Paint))]
    public class PaintEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        private void OnSceneGUI()
        {
            Paint paint = target as Paint;

            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(10, 10, Screen.width, Screen.height));
            GUILayout.BeginVertical();
            {
                if (GUILayout.Button("Generate Random BrushStroke", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    paint.GenerateRandomBrushStroke();
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            Handles.EndGUI();
        }
    }
}
