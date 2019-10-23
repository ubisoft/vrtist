using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter)),
 RequireComponent(typeof(MeshRenderer))]
public class UIPanel : MonoBehaviour
{
    [Header("Panel Shape Parmeters")]
    public float width = 4.0f;
    public float height = 6.0f;
    public float margin = 0.2f;
    public float radius = 0.1f;

    [Header("Subdivision Parameters")]
    public int circleSubdiv = 8;
    public int nbSubdivPerUnit = 1;
    public int nbSubdivCornerFixed = 3;
    public int nbSubdivCornerPerUnit = 3;

    private bool needRebuild = false;

    private void Start()
    {
        
    }

    private void OnValidate()
    {
        const float min_width = 0.01f;
        const float min_height = 0.01f;
        const float min_radius = 0.01f;
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
            needRebuild = false;
        }
    }

    public void RebuildMesh()
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        Mesh theNewMesh = UIPanel.BuildRoundedRectEx(
            width, height, margin, radius, 
            circleSubdiv, nbSubdivPerUnit, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
        theNewMesh.name = "UIPanel_GeneratedMesh";
        meshFilter.sharedMesh = theNewMesh;
    }

    public static Mesh BuildRoundedRect(float width, float height, float margin, float radius)
    {
        const int default_circleSubdiv = 8;
        const int default_nbSubdivPerUnit = 1;
        const int default_nbSubdivCornerFixed = 3;
        const int default_nbSubdivCornerPerUnit = 3;

        return BuildRoundedRectEx(
            width, height, margin, radius,
            default_circleSubdiv, 
            default_nbSubdivPerUnit, 
            default_nbSubdivCornerFixed, 
            default_nbSubdivCornerPerUnit
        );
    }

    public static Mesh BuildRoundedRectEx(
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
}
