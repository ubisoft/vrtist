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

    private static readonly float default_width = 4.0f;
    private static readonly float default_height = 6.0f;
    private static readonly float default_margin = 0.2f;
    private static readonly float default_radius = 0.1f;

    [MenuItem("VRtist/Create UI Panel")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<WizardCreatePanel>("Create UI Panel", "Create");//, "OtherButton");
    }

    [MenuItem("GameObject/VRtist/UIPanel", false, 49)]
    public static void OnCreateFromHierarchy()
    {
        Transform parent = null;
        Transform T = Selection.activeTransform;
        if (T != null)// && T.GetComponent<UIPanel>() != null)
        {
            parent = T;
        }

        CreateUIPanel("Panel", parent, default_width, default_height, default_margin, default_radius, LoadDefaultUIMaterial(), UIElement.default_color);
    }

    private void OnWizardUpdate()
    {
        helpString = "Create a new UIPanel";

        if (uiMaterial == null)
        {
            uiMaterial = LoadDefaultUIMaterial();
        }
    }

    static Material LoadDefaultUIMaterial()
    {
        string[] uiMaterialAssetPath = AssetDatabase.FindAssets("UIPanel", new[] { "Assets/Resources/Materials" });
        if (uiMaterialAssetPath.Length == 1)
        {
            return AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(uiMaterialAssetPath[0]), typeof(Material)) as Material;
        }
        else
        {
            return null;
        }
    }

    private void OnWizardCreate()
    {
        CreateUIPanel(panelName, parentPanel ? parentPanel.transform : null, width, height, margin, radius, uiMaterial, color);
    }

    private static void CreateUIPanel(
        string panelName, 
        Transform parent,
        float width,
        float height,
        float margin,
        float radius,
        Material material,
        Color color)
    {
        GameObject go = new GameObject(panelName);

        // NOTE: also creates a MeshFilter and MeshRenderer
        UIPanel uiPanel = go.AddComponent<UIPanel>();
        uiPanel.transform.parent = parent;
        uiPanel.transform.localPosition = Vector3.zero;
        uiPanel.transform.localRotation = Quaternion.identity;
        uiPanel.transform.localScale = Vector3.one;
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
        if (meshRenderer != null && material != null)
        {
            // TODO: see if we need to Instantiate(uiMaterial), or modify the instance created when calling meshRenderer.material
            //       to make the error disappear;

            // Get an instance of the same material
            // NOTE: sends an warning about leaking instances, because meshRenderer.material create instances while we are in EditorMode.
            //meshRenderer.sharedMaterial = uiMaterial;
            //Material material = meshRenderer.material; // instance of the sharedMaterial

            // Clone the material.
            meshRenderer.sharedMaterial = Instantiate(material);
            Material sharedMaterial = meshRenderer.sharedMaterial;

            sharedMaterial.SetColor("_BaseColor", color);
        }
    }

    //private void OnWizardOtherButton()
    //{
    //}
}
