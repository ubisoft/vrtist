using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter)),
 RequireComponent(typeof(MeshRenderer))]
public class UIPanel : UIElement
{
    [SpaceHeader("Panel Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
    public float margin = 0.02f;
    public float radius = 0.01f;

    [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
    public int circleSubdiv = 8;
    public int nbSubdivPerUnit = 1;
    public int nbSubdivCornerFixed = 3;
    public int nbSubdivCornerPerUnit = 3;

    private bool needRebuild = false;

    private void OnValidate()
    {
        const float min_width = 0.01f;
        const float min_height = 0.01f;
        const float min_radius = 0.001f;
        const int min_circleSubdiv = 3;
        const int min_nbSubdivPerUnit = 1;
        const int min_nbSubdivCornerFixed = 1;
        const int min_nbSubdivCornerPerUnit = 1;

        if (width < min_width)
            width = min_width;
        if (height < min_height)
            height = min_height;
        if (radius < min_radius)
            radius = min_radius;
        if (margin > width / 2.0f || margin > height / 2.0f)
            margin = Mathf.Min(width / 2.0f, height / 2.0f);
        if (radius > width / 2.0f || radius > height / 2.0f)
            radius = Mathf.Min(width / 2.0f, height / 2.0f);
        if (margin < radius)
            margin = radius;
        if (circleSubdiv < min_circleSubdiv)
            circleSubdiv = min_circleSubdiv;
        if (nbSubdivPerUnit < min_nbSubdivPerUnit)
            nbSubdivPerUnit = min_nbSubdivPerUnit;
        if (nbSubdivCornerFixed < min_nbSubdivCornerFixed)
            nbSubdivCornerFixed = min_nbSubdivCornerFixed;
        if (nbSubdivCornerPerUnit < min_nbSubdivCornerPerUnit)
            nbSubdivCornerPerUnit = min_nbSubdivCornerPerUnit;

        needRebuild = true;

        // NOTE: RebuildMesh() cannot be called in OnValidate().
    }

    private void Update()
    {
        if (needRebuild)
        {
            RebuildMesh();
            UpdateLocalPosition();
            UpdateAnchor();
            UpdateChildren();
            SetColor(baseColor);
            needRebuild = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 labelPosition  = transform.TransformPoint(new Vector3(-width / 2.0f + margin + radius, height / 2.0f - margin - radius, 0.0f));
        Vector3 posTopLeft     = transform.TransformPoint(new Vector3(-width / 2.0f + margin, +height / 2.0f - margin, 0));
        Vector3 posTopRight    = transform.TransformPoint(new Vector3(+width / 2.0f - margin, +height / 2.0f - margin, 0));
        Vector3 posBottomLeft  = transform.TransformPoint(new Vector3(-width / 2.0f + margin, -height / 2.0f + margin, 0));
        Vector3 posBottomRight = transform.TransformPoint(new Vector3(+width / 2.0f - margin, -height / 2.0f + margin, 0));

        Gizmos.color = Color.white;
        Gizmos.DrawLine(posTopLeft, posTopRight);
        Gizmos.DrawLine(posTopRight, posBottomRight);
        Gizmos.DrawLine(posBottomRight, posBottomLeft);
        Gizmos.DrawLine(posBottomLeft, posTopLeft);
        UnityEditor.Handles.Label(labelPosition, gameObject.name);

        //Gizmos.color = Color.blue;
        //Gizmos.DrawSphere(transform.TransformPoint(anchor), 1.1f * radius);
    }

    public override void RebuildMesh()
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        Mesh theNewMesh = UIPanel.BuildRoundedRectTubeEx(
            width, height, margin, radius, 
            circleSubdiv, nbSubdivPerUnit, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
        theNewMesh.name = "UIPanel_GeneratedMesh";
        meshFilter.sharedMesh = theNewMesh;
    }

    public static Mesh BuildRoundedRectTube(float width, float height, float margin, float radius)
    {
        const int default_circleSubdiv = 8;
        const int default_nbSubdivPerUnit = 1;
        const int default_nbSubdivCornerFixed = 3;
        const int default_nbSubdivCornerPerUnit = 3;

        return BuildRoundedRectTubeEx(
            width, height, margin, radius,
            default_circleSubdiv, 
            default_nbSubdivPerUnit, 
            default_nbSubdivCornerFixed, 
            default_nbSubdivCornerPerUnit
        );
    }

    public static Mesh BuildRoundedRectTubeEx(
        float width, 
        float height, 
        float margin, 
        float radius, 
        int circleSubdiv, 
        int nbSubdivPerUnit,
        int nbSubdivCornerFixed, 
        int nbSubdivCornerPerUnit)
    {
        List<Vector3> vertices = new List<Vector3>(); // TODO: use Vector3[] vertices = new Vector3[computed_size];
        List<Vector3> normals = new List<Vector3>(); // TODO: use Vector3[] normals = new Vector3[computed_size];
        List<int> indices = new List<int>(); // TODO: use int[] indices = new int[computed_size];

        float hTubeWidth = width - 2.0f * margin;
        float vTubeHeight = height - 2.0f * margin;
        float cornerLength = margin * Mathf.PI / 2.0f;
        int nbSubdivOnWidth = Mathf.FloorToInt(hTubeWidth * nbSubdivPerUnit);
        int nbSectionsOnWidth = 1 + nbSubdivOnWidth;
        int nbCirclesOnWidth = 2 + nbSubdivOnWidth;
        int nbSubdivOnHeight = Mathf.FloorToInt(vTubeHeight * nbSubdivPerUnit);
        int nbSectionsOnHeight = 1 + nbSubdivOnHeight;
        int nbCirclesOnHeight = 2 + nbSubdivOnHeight;
        int nbSubdivOnCorner = Mathf.FloorToInt(nbSubdivCornerFixed + cornerLength * nbSubdivCornerPerUnit);
        int nbSectionsOnCorner = 1 + nbSubdivOnCorner;
        int nbCirclesOnCorner = 2 + nbSubdivOnCorner;
        float sectionWidth = hTubeWidth / (float)(nbSubdivOnWidth + 1);
        float sectionHeight = vTubeHeight / (float)(nbSubdivOnHeight + 1);

        Vector3 panelTopLeft = new Vector3(-width / 2.0f, height / 2.0f, 0.0f);
        Vector3 panelTopRight = new Vector3(width / 2.0f, height / 2.0f, 0.0f);
        Vector3 panelBottomLeft = new Vector3(-width / 2.0f, -height / 2.0f, 0.0f);
        Vector3 panelBottomRight = new Vector3(width / 2.0f, -height / 2.0f, 0.0f);

        int indexOffset = 0;

        #region top-horizontal-bar: left to right, circle right axis = +Z
        {
            Vector3 sectionOffset = panelTopLeft + new Vector3(margin, 0, 0);
            for (int cs = 0; cs < nbCirclesOnWidth; ++cs)
            {
                for (int i = 0; i < circleSubdiv; ++i)
                {
                    float fi = (float)i / (float)circleSubdiv;
                    vertices.Add(new Vector3(
                        sectionOffset.x,
                        sectionOffset.y + radius * Mathf.Sin(2.0f * Mathf.PI * fi),
                        sectionOffset.z + radius * Mathf.Cos(2.0f * Mathf.PI * fi)
                    ));
                    normals.Add(new Vector3(
                        0,
                        Mathf.Sin(2.0f * Mathf.PI * fi),
                        Mathf.Cos(2.0f * Mathf.PI * fi)
                    ));
                }
                sectionOffset.x += sectionWidth;
            }
            FillCircleSectionsWithTriangles(indices, indexOffset, circleSubdiv, nbSectionsOnWidth);
            indexOffset += nbCirclesOnWidth * circleSubdiv;
        }
        #endregion

        #region bottom-horizontal-bar: left to right, circle right axis = +Z
        {
            Vector3 sectionOffset = panelBottomLeft + new Vector3(margin, 0, 0);
            for (int cs = 0; cs < nbCirclesOnWidth; ++cs)
            {
                for (int i = 0; i < circleSubdiv; ++i)
                {
                    float fi = (float)i / (float)circleSubdiv;
                    vertices.Add(new Vector3(
                        sectionOffset.x,
                        sectionOffset.y + radius * Mathf.Sin(2.0f * Mathf.PI * fi),
                        sectionOffset.z + radius * Mathf.Cos(2.0f * Mathf.PI * fi)
                    ));
                    normals.Add(new Vector3(
                        0,
                        Mathf.Sin(2.0f * Mathf.PI * fi),
                        Mathf.Cos(2.0f * Mathf.PI * fi)
                    ));
                }
                sectionOffset.x += sectionWidth;
            }
            FillCircleSectionsWithTriangles(indices, indexOffset, circleSubdiv, nbSectionsOnWidth);
            indexOffset += nbCirclesOnWidth * circleSubdiv;
        }
        #endregion

        #region left-vertical-bar: bottom to top, circle right axis = +Z
        {
            Vector3 sectionOffset = panelBottomLeft + new Vector3(0, margin, 0);
            for (int cs = 0; cs < nbCirclesOnHeight; ++cs)
            {
                for (int i = 0; i < circleSubdiv; ++i)
                {
                    float fi = (float)i / (float)circleSubdiv;
                    vertices.Add(new Vector3(
                        sectionOffset.x - radius * Mathf.Sin(2.0f * Mathf.PI * fi),
                        sectionOffset.y,
                        sectionOffset.z + radius * Mathf.Cos(2.0f * Mathf.PI * fi)
                    ));
                    normals.Add(new Vector3(
                        -Mathf.Sin(2.0f * Mathf.PI * fi),
                        0,
                        Mathf.Cos(2.0f * Mathf.PI * fi)
                    ));
                }
                sectionOffset.y += sectionHeight;
            }
            FillCircleSectionsWithTriangles(indices, indexOffset, circleSubdiv, nbSectionsOnHeight);
            indexOffset += nbCirclesOnHeight * circleSubdiv;
        }
        #endregion

        #region right-vertical-bar: bottom to top, circle right axis = +Z
        {
            Vector3 sectionOffset = panelBottomRight + new Vector3(0, margin, 0);
            for (int cs = 0; cs < nbCirclesOnHeight; ++cs)
            {
                for (int i = 0; i < circleSubdiv; ++i)
                {
                    float fi = (float)i / (float)circleSubdiv;
                    vertices.Add(new Vector3(
                        sectionOffset.x - radius * Mathf.Sin(2.0f * Mathf.PI * fi),
                        sectionOffset.y,
                        sectionOffset.z + radius * Mathf.Cos(2.0f * Mathf.PI * fi)
                    ));
                    normals.Add(new Vector3(
                        -Mathf.Sin(2.0f * Mathf.PI * fi),
                        0,
                        Mathf.Cos(2.0f * Mathf.PI * fi)
                    ));
                }
                sectionOffset.y += sectionHeight;
            }
            FillCircleSectionsWithTriangles(indices, indexOffset, circleSubdiv, nbSectionsOnHeight);
            indexOffset += nbCirclesOnHeight * circleSubdiv;
        }
        #endregion

        #region top-left-corner: outer-circle from -X(left) to Y(top), circle right axis = +Z
        {
            Vector3 cornerCenter = panelTopLeft + new Vector3(margin, -margin, 0);
            Vector3 unitRight = new Vector3(0, 0, 1);
            Vector3 unitUp = new Vector3(0, 1, 0);
            for (int cs = 0; cs < nbCirclesOnCorner; ++cs)
            {
                float fQuarterPercent = 0.25f * (float)cs / (float)(nbCirclesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
                Vector3 outerCirclePos = new Vector3(
                    -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                    +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                    0);
                Vector3 innerCircleCenter = cornerCenter + outerCirclePos;
                unitUp = outerCirclePos.normalized;
                for (int i = 0; i < circleSubdiv; ++i)
                {
                    float fi = (float)i / (float)circleSubdiv;

                    Vector3 innerCirclePos =  unitRight * radius * Mathf.Cos(2.0f * Mathf.PI * fi)
                                            + unitUp * radius * Mathf.Sin(2.0f * Mathf.PI * fi);
                    vertices.Add(innerCircleCenter + innerCirclePos);

                    normals.Add(innerCirclePos.normalized);
                }
            }
            FillCircleSectionsWithTriangles(indices, indexOffset, circleSubdiv, nbSectionsOnCorner);
            indexOffset += nbCirclesOnCorner * circleSubdiv;
        }
        #endregion

        #region top-right-corner: outer-circle from Y(top) to X(right), circle right axis = +Z
        {
            Vector3 cornerCenter = panelTopRight + new Vector3(-margin, -margin, 0);
            Vector3 unitRight = new Vector3(0, 0, 1);
            Vector3 unitUp = new Vector3(0, 1, 0);
            for (int cs = 0; cs < nbCirclesOnCorner; ++cs)
            {
                float fQuarterPercent = 0.25f * (float)cs / (float)(nbCirclesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
                Vector3 outerCirclePos = new Vector3(
                    +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                    +margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                    0);
                Vector3 innerCircleCenter = cornerCenter + outerCirclePos;
                unitUp = outerCirclePos.normalized;
                for (int i = 0; i < circleSubdiv; ++i)
                {
                    float fi = (float)i / (float)circleSubdiv;

                    Vector3 innerCirclePos = unitRight * radius * Mathf.Cos(2.0f * Mathf.PI * fi)
                                            + unitUp * radius * Mathf.Sin(2.0f * Mathf.PI * fi);
                    vertices.Add(innerCircleCenter + innerCirclePos);

                    normals.Add(innerCirclePos.normalized);
                }
            }
            FillCircleSectionsWithTriangles(indices, indexOffset, circleSubdiv, nbSectionsOnCorner);
            indexOffset += nbCirclesOnCorner * circleSubdiv;
        }
        #endregion

        #region bottom-right-corner: outer-circle from -Y(bottom) to X(right), circle right axis = +Z
        {
            Vector3 cornerCenter = panelBottomRight + new Vector3(-margin, +margin, 0);
            Vector3 unitRight = new Vector3(0, 0, 1);
            Vector3 unitUp = new Vector3(0, 1, 0);
            for (int cs = 0; cs < nbCirclesOnCorner; ++cs)
            {
                float fQuarterPercent = 0.25f * (float)cs / (float)(nbCirclesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
                Vector3 outerCirclePos = new Vector3(
                    +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                    -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                    0);
                Vector3 innerCircleCenter = cornerCenter + outerCirclePos;
                unitUp = -outerCirclePos.normalized;
                for (int i = 0; i < circleSubdiv; ++i)
                {
                    float fi = (float)i / (float)circleSubdiv;

                    Vector3 innerCirclePos = unitRight * radius * Mathf.Cos(2.0f * Mathf.PI * fi)
                                            + unitUp * radius * Mathf.Sin(2.0f * Mathf.PI * fi);
                    vertices.Add(innerCircleCenter + innerCirclePos);

                    normals.Add(innerCirclePos.normalized);
                }
            }
            FillCircleSectionsWithTriangles(indices, indexOffset, circleSubdiv, nbSectionsOnCorner);
            indexOffset += nbCirclesOnCorner * circleSubdiv;
        }
        #endregion

        #region bottom-left-corner: outer-circle from -X(left) to -Y(bottom), circle right axis = +Z
        {
            Vector3 cornerCenter = panelBottomLeft + new Vector3(+margin, +margin, 0);
            Vector3 unitRight = new Vector3(0, 0, 1);
            Vector3 unitUp = new Vector3(0, 1, 0);
            for (int cs = 0; cs < nbCirclesOnCorner; ++cs)
            {
                float fQuarterPercent = 0.25f * (float)cs / (float)(nbCirclesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
                Vector3 outerCirclePos = new Vector3(
                    -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                    -margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                    0);
                Vector3 innerCircleCenter = cornerCenter + outerCirclePos;
                unitUp = -outerCirclePos.normalized;
                for (int i = 0; i < circleSubdiv; ++i)
                {
                    float fi = (float)i / (float)circleSubdiv;

                    Vector3 innerCirclePos = unitRight * radius * Mathf.Cos(2.0f * Mathf.PI * fi)
                                            + unitUp * radius * Mathf.Sin(2.0f * Mathf.PI * fi);
                    vertices.Add(innerCircleCenter + innerCirclePos);

                    normals.Add(innerCirclePos.normalized);
                }
            }
            FillCircleSectionsWithTriangles(indices, indexOffset, circleSubdiv, nbSectionsOnCorner);
            indexOffset += nbCirclesOnCorner * circleSubdiv;
        }
        #endregion

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        return mesh;
    }

    private static void FillCircleSectionsWithTriangles(List<int> indices, int indexOffset, int circleSubdiv, int nbSections)
    {
        for (int cs = 0; cs < nbSections; ++cs)
        {
            for (int i = 0; i < circleSubdiv - 1; ++i)
            {
                int idx0 = cs * circleSubdiv + i;
                int idx1 = (cs + 1) * circleSubdiv + i;
                int idx2 = (cs + 1) * circleSubdiv + i + 1;
                int idx3 = cs * circleSubdiv + i + 1;
                indices.Add(indexOffset + idx0);
                indices.Add(indexOffset + idx1);
                indices.Add(indexOffset + idx3);
                indices.Add(indexOffset + idx3);
                indices.Add(indexOffset + idx1);
                indices.Add(indexOffset + idx2);
            }
            {   // loop to first vertices in circles
                int idx0 = (cs * circleSubdiv + (circleSubdiv - 1));
                int idx1 = ((cs + 1) * circleSubdiv + (circleSubdiv - 1));
                int idx2 = ((cs + 1) * circleSubdiv + 0);
                int idx3 = (cs * circleSubdiv + 0);
                indices.Add(indexOffset + idx0);
                indices.Add(indexOffset + idx1);
                indices.Add(indexOffset + idx3);
                indices.Add(indexOffset + idx3);
                indices.Add(indexOffset + idx1);
                indices.Add(indexOffset + idx2);
            }
        }
    }

    public static void CreateUIPanel(
        string panelName,
        Transform parent,
        Vector3 relativeLocation,
        float width,
        float height,
        float margin,
        float radius,
        Material material,
        Color color)
    {
        GameObject go = new GameObject(panelName);

        // Find the anchor of the parent if it is a UIElement
        Vector3 parentAnchor = Vector3.zero;
        if (parent)
        {
            UIElement elem = parent.gameObject.GetComponent<UIElement>();
            if (elem)
            {
                parentAnchor = elem.anchor;
            }
        }

        UIPanel uiPanel = go.AddComponent<UIPanel>(); // NOTE: also creates the MeshFilter, MeshRenderer and Collider components
        uiPanel.relativeLocation = relativeLocation;
        uiPanel.transform.parent = parent;
        uiPanel.transform.localPosition = parentAnchor + relativeLocation;
        uiPanel.transform.localRotation = Quaternion.identity;
        uiPanel.transform.localScale = Vector3.one;
        uiPanel.width = width;
        uiPanel.height = height;
        uiPanel.margin = margin;
        uiPanel.radius = radius;

        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            meshFilter.sharedMesh = UIPanel.BuildRoundedRectTube(width, height, margin, radius);
            // TODO: make the Build* functions return the anchor point.
            uiPanel.anchor = new Vector3(-width / 2.0f, height / 2.0f, 0.0f);
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
}
