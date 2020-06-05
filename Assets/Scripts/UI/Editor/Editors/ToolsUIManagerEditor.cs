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
                if (GUILayout.Button("Add Element To List", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    List<GameObject> list = new List<GameObject>();
                    list.Add(Resources.Load("Prefabs/UI/DEBUG/DynListQuad") as GameObject);
                    list.Add(Resources.Load("Prefabs/UI/DEBUG/DynListSphere") as GameObject);
                    list.Add(Resources.Load("Prefabs/UI/DEBUG/DynListCapsule") as GameObject);
                    GameObject obj = GameObject.Instantiate(list[Random.Range(0, list.Count)]);
                    obj.GetComponentInChildren<MeshRenderer>().material.SetColor("_UnlitColor", Random.ColorHSV());
                    uiManager.DEBUG_AddItemToList(obj.transform);
                }
                if (GUILayout.Button("Remove Last Element From List", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    uiManager.DEBUG_RemoveLastItemFromList();
                }
                if (GUILayout.Button("Reset DynList state", GUILayout.Width(200), GUILayout.Height(30)))
                {
                    uiManager.DEBUG_ResetList();
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            Handles.EndGUI();
        }
    }
}
