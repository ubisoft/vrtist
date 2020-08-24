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
                // UI OPTIONS
                //

                GUILayout.Space(20);

                GUILayout.Label(new GUIContent("Debug UIOptions"), textStyle, GUILayout.Height(30));

                if (GUILayout.Button("Refresh", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    debug.UIOPTIONS_Refresh();
                }

                if (GUILayout.Button("Relink Widgets <-> Colors", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    debug.UIOPTIONS_ResetAllColors();
                }

                //if (GUILayout.Button("Link Widgets HOVERED Color", GUILayout.Width(200), GUILayout.Height(30)))
                //{
                //    debug.UIOPTIONS_HoveredColor();
                //}

                //if (GUILayout.Button("Add Collider to UIPanel", GUILayout.Width(200), GUILayout.Height(30)))
                //{
                //    debug.AddCollidersToUIPanels();
                //}

                //
                // Asset Bank
                //

                //GUILayout.Space(20);

                //GUILayout.Label(new GUIContent("Asset Bank"), textStyle, GUILayout.Height(30));

                //if (GUILayout.Button("Reorder", GUILayout.Width(200), GUILayout.Height(30)))
                //{
                //    debug.AssetBank_Reorder();
                //}

                //
                // Checkable icons.
                //

                //GUILayout.Space(20);

                //GUILayout.Label(new GUIContent("Checkable Buttons"), textStyle, GUILayout.Height(30));

                //if (GUILayout.Button("SetBaseSprite", GUILayout.Width(200), GUILayout.Height(30)))
                //{
                //    debug.Checkable_SetBaseSprite();
                //}

                //
                //
                //
                GUILayout.Space(20);

                GUILayout.Label(new GUIContent("Materials"), textStyle, GUILayout.Height(30));

                if (GUILayout.Button("Relink/Fix Widgets Materials", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    debug.MATERIALS_RelinkAndFix();
                }

                //GUILayout.Space(20);

                //GUILayout.Label(new GUIContent("TextMeshPro"), textStyle, GUILayout.Height(30));

                //if (GUILayout.Button("Replace Text <-> TexMeshPro", GUILayout.Width(200), GUILayout.Height(30)))
                //{
                //    debug.Replace_Text_By_TextMeshPro();
                //}
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            Handles.EndGUI();
        }
    }
}