using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class ToolsWindow : EditorWindow
    {
        [MenuItem("VRtist/Tools Window")]
        public static void ShowWindow()
        {
            ToolsWindow w = GetWindow<ToolsWindow>("VRtist Tools");
            w.minSize = new Vector2(1, 1);
            w.Show();
        }

        private void OnGUI()
        {
            float labelWidth = 1.0f;

            minSize = new Vector2(1, 1);

            GUIStyle textStyle = new GUIStyle();
            textStyle.alignment = TextAnchor.MiddleLeft;
            textStyle.normal.textColor = Color.black;

            GUILayout.BeginArea(new Rect(5, 5, position.width - 10, position.height - 10));
            { 
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(new GUIContent("GOTO:"), textStyle, GUILayout.Width(60), GUILayout.Height(20));
                    if (GUILayout.Button("Palette", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                    {
                        GoToPalette();
                    }

                    if (GUILayout.Button("Panels", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                    {
                        GoToPanels();
                    }

                    if (GUILayout.Button("Tools", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                    {
                        GoToTools();
                    }
                }
                GUILayout.EndHorizontal();

                //DebugUI debug = GameObject.FindObjectOfType<DebugUI>();
                GameObject singleton = GameObject.Find("Singleton");
                DebugUI debug = (singleton != null) ? singleton.GetComponent<DebugUI>() : null;
                if (debug != null)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(new GUIContent("UI Colors:"), textStyle, GUILayout.Width(60), GUILayout.Height(20));
                        labelWidth =
                            EditorStyles.label.CalcSize(new GUIContent("Refresh")).x
                            + EditorStyles.label.padding.left + EditorStyles.label.border.left
                            + EditorStyles.label.padding.right + EditorStyles.label.border.right;

                        //GUILayout.Width(labelWidth)

                        if (GUILayout.Button("Refresh", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                        {
                            debug.UIOPTIONS_Refresh();
                        }

                        if (GUILayout.Button("Relink Colors", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                        {
                            debug.UIOPTIONS_ResetAllColors();
                        }

                        if (GUILayout.Button("TMPro text -> (ui)", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                        {
                            debug.Replace_TextMeshPro_By_TextMeshProUGUI();
                        }

                        //if (GUILayout.Button("Relink Thickness", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                        //{
                        //    debug.UIOPTIONS_ResetAllThickness();
                        //}

                        //if (GUILayout.Button("Link HOVERED Col", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                        //{
                        //    debug.UIOPTIONS_HoveredColor();
                        //}
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(new GUIContent("Other:"), textStyle, GUILayout.Width(60), GUILayout.Height(20));
                        //if (GUILayout.Button("+Coll in Panel", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                        //{
                        //    debug.AddCollidersToUIPanels();
                        //}
                        if (GUILayout.Button("Relink/Fix Mats", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                        {
                            debug.MATERIALS_RelinkAndFix();
                        }
                        if (GUILayout.Button("Checkbox sorting orger", GUILayout.ExpandWidth(false), GUILayout.Height(20)))
                        {
                            debug.CheckBox_SortingOrder();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndArea();
            }
        }


        [MenuItem("VRtist/GOTO/Palette")]
        static void GoToPalette()
        {
            GameObject palette = GameObject.Find("Camera Rig/Pivot/PaletteController/PaletteHandle/Palette");
            UnityEditor.Selection.activeTransform = palette.transform;
        }

        [MenuItem("VRtist/GOTO/Panels")]
        static void GoToPanels()
        {
            GameObject panels = GameObject.Find("Camera Rig/Pivot/PaletteController/PaletteHandle/Palette/MainPanel/ToolsPanelGroup");
            UnityEditor.Selection.activeTransform = panels.transform;
        }

        [MenuItem("VRtist/GOTO/Tools")]
        static void GoToTools()
        {
            GameObject tools = GameObject.Find("Camera Rig/Pivot/ToolsController/Tools");
            UnityEditor.Selection.activeTransform = tools.transform;
        }
    }
}
