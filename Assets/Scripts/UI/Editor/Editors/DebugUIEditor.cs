using UnityEditor;
using UnityEngine;

namespace VRtist
{
    [CustomEditor(typeof(DebugUI))]
    public class DebugUIEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        private void OnSceneGUI()
        {
            DebugUI debug = target as DebugUI;

            Handles.BeginGUI();

            GUILayout.BeginArea(new Rect(10, 10, Screen.width, Screen.height));
            GUILayout.BeginVertical();
            {
                GUIStyle textStyle = new GUIStyle();
                textStyle.normal.textColor = Color.white;

                //
                // SHOT LIST
                //

                GUILayout.Label(new GUIContent("Debug ShotList"), textStyle, GUILayout.Height(30));

                if (GUILayout.Button("Add Element To List", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    int start = Random.Range(1, 100);
                    int end = start + Random.Range(10, 30);
                    Shot shot = new Shot { camera = null, enabled = true, start = start, end = end, name = $"sn_{Random.Range(1, 1000)}" };
                    ShotItem shotItem = ShotItem.GenerateShotItem(shot);
                    debug.SHOTLIST_AddItemToList(shotItem.transform);
                }
                if (GUILayout.Button("Reset DynList state", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    debug.SHOTLIST_ClearList();
                }

                GUILayout.Space(50);

                //
                // UI OPTIONS
                //

                GUILayout.Label(new GUIContent("Debug UIOptions"), textStyle, GUILayout.Height(30));

                if (GUILayout.Button("Refresh", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    debug.UIOPTIONS_Refresh();
                }

                if (GUILayout.Button("Relink Widgets <-> Colors", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    debug.UIOPTIONS_ResetAllColors();
                }

                if (GUILayout.Button("Random Change Colors", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    debug.UIOPTIONS_RandomChangeColors();
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            Handles.EndGUI();
        }
    }
}