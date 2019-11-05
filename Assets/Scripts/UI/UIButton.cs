using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter)),
 RequireComponent(typeof(MeshRenderer)),
 RequireComponent(typeof(BoxCollider))]
public class UIButton : UIElement
{
    [SpaceHeader("Panel Shape Parmeters", 6, 0.8f, 0.8f, 0.8f)]
    public float width = 1.5f;
    public float height = 0.5f;
    public float margin = 0.05f;
    public float thickness = 0.05f;

    [SpaceHeader("Subdivision Parameters", 6, 0.8f, 0.8f, 0.8f)]
    public int nbSubdivCornerFixed = 3;
    public int nbSubdivCornerPerUnit = 3;

    [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
    public UnityEvent onHoverEvent = null;
    public UnityEvent onClickEvent = null;
    public UnityEvent onReleaseEvent = null;

    public float Width { get { return width; } set { width = value; RebuildMesh(); } }
    public float Height { get { return height; } set { height = value; RebuildMesh(); } }

    private bool needRebuild = false;

    void Start()
    {
        // test
        onClickEvent.AddListener(onPushButton);
        onReleaseEvent.AddListener(onReleaseButton);
    }

    private void OnValidate()
    {
        const float min_width = 0.01f;
        const float min_height = 0.01f;
        const float min_thickness = 0.01f;
        const int min_nbSubdivCornerFixed = 1;
        const int min_nbSubdivCornerPerUnit = 1;

        if (width < min_width)
            width = min_width;
        if (height < min_height)
            height = min_height;
        if (thickness < min_thickness)
            thickness = min_thickness;
        if (margin > width / 2.0f || margin > height / 2.0f)
            margin = Mathf.Min(width / 2.0f, height / 2.0f);
        if (nbSubdivCornerFixed < min_nbSubdivCornerFixed)
            nbSubdivCornerFixed = min_nbSubdivCornerFixed;
        if (nbSubdivCornerPerUnit < min_nbSubdivCornerPerUnit)
            nbSubdivCornerPerUnit = min_nbSubdivCornerPerUnit;

        needRebuild = true;
    }

    private void Update()
    {
        if (needRebuild)
        {
            RebuildMesh();
            needRebuild = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 labelPosition = transform.TransformPoint(new Vector3(-width / 4.0f, 0.0f, -thickness / 2.0f - 0.01f));
        Vector3 posTopLeft = transform.TransformPoint(new Vector3(-width / 2.0f + margin, +height / 2.0f - margin, -thickness / 2.0f - 0.01f));
        Vector3 posTopRight = transform.TransformPoint(new Vector3(+width / 2.0f - margin, +height / 2.0f - margin, -thickness / 2.0f - 0.01f));
        Vector3 posBottomLeft = transform.TransformPoint(new Vector3(-width / 2.0f + margin, -height / 2.0f + margin, -thickness / 2.0f - 0.01f));
        Vector3 posBottomRight = transform.TransformPoint(new Vector3(+width / 2.0f - margin, -height / 2.0f + margin, -thickness / 2.0f - 0.01f));

        Gizmos.color = Color.white;
        Gizmos.DrawLine(posTopLeft, posTopRight);
        Gizmos.DrawLine(posTopRight, posBottomRight);
        Gizmos.DrawLine(posBottomRight, posBottomLeft);
        Gizmos.DrawLine(posBottomLeft, posTopLeft);
        UnityEditor.Handles.Label(labelPosition, gameObject.name);
    }

    public void RebuildMesh()
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        Mesh theNewMesh = UIButton.BuildRoundedRectEx(width, height, margin, thickness, nbSubdivCornerFixed, nbSubdivCornerPerUnit);
        theNewMesh.name = "UIButton_GeneratedMesh";
        meshFilter.sharedMesh = theNewMesh;
        UpdateColliderDimensions();
    }

    private void UpdateColliderDimensions()
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        BoxCollider coll = gameObject.GetComponent<BoxCollider>();
        if (meshFilter != null && coll != null)
        {
            coll.center = meshFilter.sharedMesh.bounds.center;
            coll.size = meshFilter.sharedMesh.bounds.size;
        }
    }

    public static Mesh BuildRoundedRect(float width, float height, float margin)
    {
        const int default_nbSubdivCornerFixed = 3;
        const int default_nbSubdivCornerPerUnit = 3;
        const float default_thickness = 0.05f;

        return BuildRoundedRectEx(
            width, height, margin, default_thickness,
            default_nbSubdivCornerFixed, default_nbSubdivCornerPerUnit);
    }

    public static Mesh BuildRoundedRectEx(
        float width,
        float height,
        float margin,
        float thickness,
        int nbSubdivCornerFixed,
        int nbSubdivCornerPerUnit)
    {
        List<Vector3> vertices = new List<Vector3>(); // TODO: use Vector3[] vertices = new Vector3[computed_size];
        List<Vector3> normals = new List<Vector3>(); // TODO: use Vector3[] normals = new Vector3[computed_size];
        List<int> indices = new List<int>(); // TODO: use int[] indices = new int[computed_size];

        Vector3 innerTopLeft_front     = new Vector3(-width / 2.0f + margin, +height / 2.0f - margin, -thickness / 2.0f);
        Vector3 innerTopRight_front    = new Vector3(+width / 2.0f - margin, +height / 2.0f - margin, -thickness / 2.0f);
        Vector3 innerBottomRight_front = new Vector3(+width / 2.0f - margin, -height / 2.0f + margin, -thickness / 2.0f);
        Vector3 innerBottomLeft_front  = new Vector3(-width / 2.0f + margin, -height / 2.0f + margin, -thickness / 2.0f);

        Vector3 innerTopLeft_back      = new Vector3(-width / 2.0f + margin, +height / 2.0f - margin, +thickness / 2.0f);
        Vector3 innerTopRight_back     = new Vector3(+width / 2.0f - margin, +height / 2.0f - margin, +thickness / 2.0f);
        Vector3 innerBottomRight_back  = new Vector3(+width / 2.0f - margin, -height / 2.0f + margin, +thickness / 2.0f);
        Vector3 innerBottomLeft_back   = new Vector3(-width / 2.0f + margin, -height / 2.0f + margin, +thickness / 2.0f);

        float cornerLength = margin * Mathf.PI / 2.0f;
        int nbSubdivOnCorner = Mathf.FloorToInt(nbSubdivCornerFixed + cornerLength * nbSubdivCornerPerUnit);
        int nbTrianglesOnCorner = 1 + nbSubdivOnCorner;
        int nbVerticesOnCorner = 2 + nbSubdivOnCorner;

        int indexOffset = 0;

        #region front-rect-face

        // FRONT
        vertices.Add(innerTopLeft_front);
        vertices.Add(innerTopRight_front);
        vertices.Add(innerBottomLeft_front);
        vertices.Add(innerBottomRight_front);

        vertices.Add(innerTopLeft_front + new Vector3(0, margin, 0));
        vertices.Add(innerTopRight_front + new Vector3(0, margin, 0));
        vertices.Add(innerTopLeft_front + new Vector3(-margin, 0, 0));
        vertices.Add(innerTopRight_front + new Vector3(margin, 0, 0));
        vertices.Add(innerBottomLeft_front + new Vector3(-margin, 0, 0));
        vertices.Add(innerBottomRight_front + new Vector3(margin, 0, 0));
        vertices.Add(innerBottomLeft_front + new Vector3(0, -margin, 0));
        vertices.Add(innerBottomRight_front + new Vector3(0, -margin, 0));

        for(int i = 0; i < 12; ++i)
        {
            normals.Add(-Vector3.forward);
        }
        
        int[] front_middle_indices = new int[] 
        {
            4,5,1,
            4,1,0,
            6,0,2,
            6,2,8,
            0,1,3,
            0,3,2,
            1,7,9,
            1,9,3,
            2,3,11,
            2,11,10
        };

        for (int i = 0; i < front_middle_indices.Length; ++i)
        {
            indices.Add(indexOffset + front_middle_indices[i]);
        }

        indexOffset = vertices.Count;

        #endregion

        #region back-rect-face

        // BACK
        vertices.Add(innerTopLeft_back);
        vertices.Add(innerTopRight_back);
        vertices.Add(innerBottomLeft_back);
        vertices.Add(innerBottomRight_back);

        vertices.Add(innerTopLeft_back + new Vector3(0, margin, 0));
        vertices.Add(innerTopRight_back + new Vector3(0, margin, 0));
        vertices.Add(innerTopLeft_back + new Vector3(-margin, 0, 0));
        vertices.Add(innerTopRight_back + new Vector3(margin, 0, 0));
        vertices.Add(innerBottomLeft_back + new Vector3(-margin, 0, 0));
        vertices.Add(innerBottomRight_back + new Vector3(margin, 0, 0));
        vertices.Add(innerBottomLeft_back + new Vector3(0, -margin, 0));
        vertices.Add(innerBottomRight_back + new Vector3(0, -margin, 0));

        for (int i = 0; i < 12; ++i)
        {
            normals.Add(Vector3.forward);
        }

        int[] back_middle_indices = new int[]
        {
            4,1,5,
            4,0,1,
            6,2,0,
            6,8,2,
            0,3,1,
            0,2,3,
            1,9,7,
            1,3,9,
            2,11,3,
            2,10,11
        };

        for (int i = 0; i < back_middle_indices.Length; ++i)
        {
            indices.Add(indexOffset + back_middle_indices[i]);
        }

        indexOffset = vertices.Count;

        #endregion

        #region side-rect-4-faces

        vertices.Add(innerTopLeft_front + new Vector3(0, margin, 0));
        vertices.Add(innerTopRight_front + new Vector3(0, margin, 0));
        vertices.Add(innerTopLeft_back + new Vector3(0, margin, 0));
        vertices.Add(innerTopRight_back + new Vector3(0, margin, 0));

        vertices.Add(innerTopLeft_front + new Vector3(-margin, 0, 0));
        vertices.Add(innerBottomLeft_front + new Vector3(-margin, 0, 0));
        vertices.Add(innerTopLeft_back + new Vector3(-margin, 0, 0));
        vertices.Add(innerBottomLeft_back + new Vector3(-margin, 0, 0));

        vertices.Add(innerTopRight_front + new Vector3(margin, 0, 0));
        vertices.Add(innerBottomRight_front + new Vector3(margin, 0, 0));
        vertices.Add(innerTopRight_back + new Vector3(margin, 0, 0));
        vertices.Add(innerBottomRight_back + new Vector3(margin, 0, 0));

        vertices.Add(innerBottomLeft_front + new Vector3(0, -margin, 0));
        vertices.Add(innerBottomRight_front + new Vector3(0, -margin, 0));
        vertices.Add(innerBottomLeft_back + new Vector3(0, -margin, 0));
        vertices.Add(innerBottomRight_back + new Vector3(0, -margin, 0));

        for (int i = 0; i < 4; ++i) normals.Add(Vector3.up);
        for (int i = 0; i < 4; ++i) normals.Add(Vector3.left);
        for (int i = 0; i < 4; ++i) normals.Add(Vector3.right);
        for (int i = 0; i < 4; ++i) normals.Add(Vector3.down);

        int[] side_faces_indices = new int[]
        {
            0,2,1,
            2,3,1,
            5,7,4,
            7,6,4,
            8,10,9,
            10,11,9,
            13,15,12,
            15,14,12
        };

        for (int i = 0; i < side_faces_indices.Length; ++i)
        {
            indices.Add(indexOffset + side_faces_indices[i]);
        }

        indexOffset = vertices.Count;

        #endregion



        #region front-top-left-corner

        vertices.Add(innerTopLeft_front);

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
            Vector3 outerCirclePos = new Vector3(
                -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerTopLeft_front + outerCirclePos);
        }

        for (int i = 0; i < nbVerticesOnCorner + 1; ++i)
        {
            normals.Add(-Vector3.forward);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 0);
            indices.Add(indexOffset + i + 1);
            indices.Add(indexOffset + i + 2);
        }

        indexOffset = vertices.Count;

        #endregion

        #region back-top-left-corner

        vertices.Add(innerTopLeft_back);

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
            Vector3 outerCirclePos = new Vector3(
                -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerTopLeft_back + outerCirclePos);
        }

        for (int i = 0; i < nbVerticesOnCorner + 1; ++i)
        {
            normals.Add(Vector3.forward);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 0);
            indices.Add(indexOffset + i + 2);
            indices.Add(indexOffset + i + 1);
        }

        indexOffset = vertices.Count;

        #endregion

        #region side-top-left-corner

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
            Vector3 outerCirclePos = new Vector3(
                -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerTopLeft_front + outerCirclePos);
            vertices.Add(innerTopLeft_back + outerCirclePos);
            normals.Add(outerCirclePos.normalized);
            normals.Add(outerCirclePos.normalized);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 2 * i + 0);
            indices.Add(indexOffset + 2 * i + 1);
            indices.Add(indexOffset + 2 * ( i + 1) + 0);
            indices.Add(indexOffset + 2 * i + 1);
            indices.Add(indexOffset + 2 * (i + 1) + 1);
            indices.Add(indexOffset + 2 * (i + 1) + 0);
        }

        indexOffset = vertices.Count;

        #endregion



        #region front-top-right-corner

        vertices.Add(innerTopRight_front);

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
            Vector3 outerCirclePos = new Vector3(
                +margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerTopRight_front + outerCirclePos);
        }

        for (int i = 0; i < nbVerticesOnCorner + 1; ++i)
        {
            normals.Add(-Vector3.forward);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 0);
            indices.Add(indexOffset + i + 2);
            indices.Add(indexOffset + i + 1);
        }

        indexOffset = vertices.Count;

        #endregion

        #region back-top-right-corner

        vertices.Add(innerTopRight_back);

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
            Vector3 outerCirclePos = new Vector3(
                +margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerTopRight_back + outerCirclePos);
        }

        for (int i = 0; i < nbVerticesOnCorner + 1; ++i)
        {
            normals.Add(Vector3.forward);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 0);
            indices.Add(indexOffset + i + 1);
            indices.Add(indexOffset + i + 2);
        }

        indexOffset = vertices.Count;

        #endregion

        #region side-top-right-corner

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], over a quarter of circle
            Vector3 outerCirclePos = new Vector3(
                +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                +margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerTopRight_front + outerCirclePos);
            vertices.Add(innerTopRight_back + outerCirclePos);
            normals.Add(outerCirclePos.normalized);
            normals.Add(outerCirclePos.normalized);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 2 * i + 0);
            indices.Add(indexOffset + 2 * i + 1);
            indices.Add(indexOffset + 2 * (i + 1) + 0);
            indices.Add(indexOffset + 2 * i + 1);
            indices.Add(indexOffset + 2 * (i + 1) + 1);
            indices.Add(indexOffset + 2 * (i + 1) + 0);
        }

        indexOffset = vertices.Count;

        #endregion



        #region front-bottom-left-corner

        vertices.Add(innerBottomLeft_front);

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
            Vector3 outerCirclePos = new Vector3(
                -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                -margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerBottomLeft_front + outerCirclePos);
        }

        for (int i = 0; i < nbVerticesOnCorner + 1; ++i)
        {
            normals.Add(-Vector3.forward);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 0);
            indices.Add(indexOffset + i + 2);
            indices.Add(indexOffset + i + 1);
        }

        indexOffset = vertices.Count;

        #endregion

        #region back-bottom-left-corner

        vertices.Add(innerBottomLeft_back);

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
            Vector3 outerCirclePos = new Vector3(
                -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                -margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerBottomLeft_back + outerCirclePos);
        }

        for (int i = 0; i < nbVerticesOnCorner + 1; ++i)
        {
            normals.Add(Vector3.forward);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 0);
            indices.Add(indexOffset + i + 1);
            indices.Add(indexOffset + i + 2);
        }

        indexOffset = vertices.Count;

        #endregion

        #region side-bottom-left-corner

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], over a quarter of circle
            Vector3 outerCirclePos = new Vector3(
                -margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerBottomLeft_front + outerCirclePos);
            vertices.Add(innerBottomLeft_back + outerCirclePos);
            normals.Add(outerCirclePos.normalized);
            normals.Add(outerCirclePos.normalized);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 2 * i + 0);
            indices.Add(indexOffset + 2 * i + 1);
            indices.Add(indexOffset + 2 * (i + 1) + 0);
            indices.Add(indexOffset + 2 * i + 1);
            indices.Add(indexOffset + 2 * (i + 1) + 1);
            indices.Add(indexOffset + 2 * (i + 1) + 0);
        }

        indexOffset = vertices.Count;

        #endregion


        #region front-bottom-right-corner

        vertices.Add(innerBottomRight_front);

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
            Vector3 outerCirclePos = new Vector3(
                +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerBottomRight_front + outerCirclePos);
        }

        for (int i = 0; i < nbVerticesOnCorner + 1; ++i)
        {
            normals.Add(-Vector3.forward);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 0);
            indices.Add(indexOffset + i + 2);
            indices.Add(indexOffset + i + 1);
        }

        indexOffset = vertices.Count;

        #endregion

        #region back-bottom-right-corner

        vertices.Add(innerBottomRight_back);

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
            Vector3 outerCirclePos = new Vector3(
                +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerBottomRight_back + outerCirclePos);
        }

        for (int i = 0; i < nbVerticesOnCorner + 1; ++i)
        {
            normals.Add(Vector3.forward);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 0);
            indices.Add(indexOffset + i + 1);
            indices.Add(indexOffset + i + 2);
        }

        indexOffset = vertices.Count;

        #endregion

        #region side-bottom-right-corner

        for (int cs = 0; cs < nbVerticesOnCorner; ++cs)
        {
            float fQuarterPercent = 0.25f * (float)cs / (float)(nbVerticesOnCorner - 1); // [0..1], over a quarter of circle
            Vector3 outerCirclePos = new Vector3(
                +margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                -margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                0);
            vertices.Add(innerBottomRight_front + outerCirclePos);
            vertices.Add(innerBottomRight_back + outerCirclePos);
            normals.Add(outerCirclePos.normalized);
            normals.Add(outerCirclePos.normalized);
        }

        for (int i = 0; i < nbTrianglesOnCorner; ++i)
        {
            indices.Add(indexOffset + 2 * i + 0);
            indices.Add(indexOffset + 2 * i + 1);
            indices.Add(indexOffset + 2 * (i + 1) + 0);
            indices.Add(indexOffset + 2 * i + 1);
            indices.Add(indexOffset + 2 * (i + 1) + 1);
            indices.Add(indexOffset + 2 * (i + 1) + 0);
        }

        indexOffset = vertices.Count;

        #endregion


        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        return mesh;
    }

    private void OnTriggerEnter(Collider otherCollider)
    {
        Debug.Log("Trigger Collision with " + otherCollider.gameObject.name);
        onClickEvent.Invoke();
    }

    private void OnTriggerExit(Collider otherCollider)
    {
        Debug.Log("Trigger EXIT Collision with " + otherCollider.gameObject.name);
        onReleaseEvent.Invoke();
    }

    private void OnTriggerStay(Collider otherCollider)
    {
        Debug.Log("Trigger STAY Collision with " + otherCollider.gameObject.name);
        onHoverEvent.Invoke();
    }



    // test
    private Color savedColor = Color.white;

    public void onPushButton()
    {
        Debug.Log("ON PUSH BUTTON");

        Material sharedMaterial = GetComponent<MeshRenderer>().material;
        savedColor = sharedMaterial.GetColor("_BaseColor");
        sharedMaterial.SetColor("_BaseColor", Color.red);
    }

    public void onReleaseButton()
    {
        Debug.Log("ON RELEASE BUTTON");

        Material sharedMaterial = GetComponent<MeshRenderer>().material;
        sharedMaterial.SetColor("_BaseColor", savedColor);
    }
}
