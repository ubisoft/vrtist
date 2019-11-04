using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class WizardCreateUIButton : ScriptableWizard
{
    public UIPanel parentPanel = null;
    public string panelName = "Button";
    public float width = 1.5f;
    public float height = 0.5f;
    public float margin = 0.05f;
    public float thickness = 0.05f;
    public Material uiMaterial = null;
    public Color color = Color.white;

    private static readonly float default_width = 1.5f;
    private static readonly float default_height = 0.5f;
    private static readonly float default_margin = 0.05f;
    private static readonly float default_thickness = 0.05f;

    [MenuItem("VRtist/Create UI Button")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<WizardCreateUIButton>("Create UI Button", "Create");//, "OtherButton");
    }

    [MenuItem("GameObject/VRtist/UIButton", false, 49)]
    public static void OnCreateFromHierarchy()
    {
        Transform parent = null;
        Transform T = Selection.activeTransform;
        if (T != null)// && T.GetComponent<UIPanel>() != null)
        {
            parent = T;
        }

        CreateUIButton("Button", parent, default_width, default_height, default_margin, default_thickness, LoadDefaultUIMaterial(), UIElement.default_color);
    }

    private void OnWizardUpdate()
    {
        helpString = "Create a new UIButton";

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
        CreateUIButton(panelName, parentPanel ? parentPanel.transform : null, width, height, margin, thickness, uiMaterial, color);
    }

    private static void CreateUIButton(
        string panelName,
        Transform parent,
        float width,
        float height,
        float margin,
        float thickness,
        Material material,
        Color color)
    {
        GameObject go = new GameObject(panelName);

        // NOTE: also creates a MeshFilter and MeshRenderer
        UIButton uiButton = go.AddComponent<UIButton>();
        uiButton.transform.parent = parent;
        uiButton.transform.localPosition = Vector3.zero;
        uiButton.transform.localRotation = Quaternion.identity;
        uiButton.transform.localScale = Vector3.one;
        uiButton.width = width;
        uiButton.height = height;
        uiButton.margin = margin;
        uiButton.thickness = thickness;

        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.mesh = UIButton.BuildRoundedRect(width, height, margin);
            BoxCollider coll = go.GetComponent<BoxCollider>();
            if (coll != null)
            {
                coll.center = meshFilter.sharedMesh.bounds.center;
                coll.size = meshFilter.sharedMesh.bounds.size;
                coll.isTrigger = true;
            }
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
