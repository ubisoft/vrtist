using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class WizardCreatePanel : ScriptableWizard
{
    public UIPanel parentPanel = null;
    public string panelName = "Panel";
    public float width = 4.0f;
    public float height = 6.0f;
    public float margin = 0.2f;
    public float radius = 0.1f;
    public Material uiMaterial = null;
    public Color color = Color.white;

    [MenuItem("VRtist/Create UI Panel")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<WizardCreatePanel>("Create UI Panel", "Create");//, "OtherButton");
    }

    private void OnWizardUpdate()
    {
        helpString = "Create a new UIPanel";

        if (uiMaterial == null)
        {
            string[] uiMaterialAssetPath = AssetDatabase.FindAssets("UIPanel", new[] { "Assets/Materials" });
            if (uiMaterialAssetPath.Length == 1)
            {
                uiMaterial = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(uiMaterialAssetPath[0]), typeof(Material)) as Material;
            }
        }
    }

    private void OnWizardCreate()
    {
        GameObject go = new GameObject(panelName);

        // NOTE: also creates a MeshFilter and MeshRenderer
        UIPanel uiPanel = go.AddComponent<UIPanel>();
        uiPanel.transform.parent = parentPanel ? parentPanel.transform : null;
        uiPanel.width = width;
        uiPanel.height = height;
        uiPanel.margin = margin;
        uiPanel.radius = radius;

        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh = UIPanel.BuildRoundedRect(width, height, margin, radius);
        }

        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        if (meshRenderer != null && uiMaterial != null)
        {
            // TODO: see if we need to Instantiate(uiMaterial), or modify the instance created when calling meshRenderer.material
            //       to make the error disappear;

            // Get an instance of the same material
            // NOTE: sends an warning about leaking instances, because meshRenderer.material create instances while we are in EditorMode.
            //meshRenderer.sharedMaterial = uiMaterial;
            //Material material = meshRenderer.material; // instance of the sharedMaterial

            // Clone the material.
            meshRenderer.sharedMaterial = Instantiate(uiMaterial);
            Material material = meshRenderer.sharedMaterial;

            material.SetColor("_BaseColor", color);
        }
    }

    //private void OnWizardOtherButton()
    //{
    //}
}
