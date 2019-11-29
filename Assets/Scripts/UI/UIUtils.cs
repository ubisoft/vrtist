using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VRtist
{
    public class UIUtils
    {
        #region Mesh Building

        public static Mesh BuildBox(float width, float height)
        {
            const float default_thickness = 0.001f;

            return BuildBoxEx(width, height, default_thickness);
        }

        public static Mesh BuildBoxEx(float width, float height, float thickness)
        {
            List<Vector3> vertices = new List<Vector3>(); // TODO: use Vector3[] vertices = new Vector3[computed_size];
            List<Vector3> normals = new List<Vector3>(); // TODO: use Vector3[] normals = new Vector3[computed_size];
            List<Vector2> uvs = new List<Vector2>();
            List<int> indices = new List<int>(); // TODO: use int[] indices = new int[computed_size];

            Vector3 innerTopLeft_front = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 innerTopRight_front = new Vector3(width, 0.0f, 0.0f);
            Vector3 innerBottomRight_front = new Vector3(width, -height, 0.0f);
            Vector3 innerBottomLeft_front = new Vector3(0.0f, -height, 0.0f);

            Vector3 innerTopLeft_back = new Vector3(0.0f, 0.0f, thickness);
            Vector3 innerTopRight_back = new Vector3(width, 0.0f, thickness);
            Vector3 innerBottomRight_back = new Vector3(width, -height, thickness);
            Vector3 innerBottomLeft_back = new Vector3(0.0f, -height, thickness);

            Vector2 uv00 = new Vector2(0,0);
            Vector2 uv01 = new Vector2(0, 1);
            Vector2 uv11 = new Vector2(1, 1);
            Vector2 uv10 = new Vector2(1, 0);

            // TOP
            vertices.Add(innerTopLeft_front);
            vertices.Add(innerTopRight_front);
            vertices.Add(innerTopLeft_back);
            vertices.Add(innerTopRight_back);

            uvs.Add(uv00);
            uvs.Add(uv10);
            uvs.Add(uv00);
            uvs.Add(uv10);

            // LEFT
            vertices.Add(innerTopLeft_front);
            vertices.Add(innerBottomLeft_front);
            vertices.Add(innerTopLeft_back);
            vertices.Add(innerBottomLeft_back);

            uvs.Add(uv00);
            uvs.Add(uv01);
            uvs.Add(uv00);
            uvs.Add(uv01);

            // RIGHT
            vertices.Add(innerTopRight_front);
            vertices.Add(innerBottomRight_front);
            vertices.Add(innerTopRight_back);
            vertices.Add(innerBottomRight_back);

            uvs.Add(uv10);
            uvs.Add(uv11);
            uvs.Add(uv10);
            uvs.Add(uv11);

            // BOTTOM
            vertices.Add(innerBottomLeft_front);
            vertices.Add(innerBottomRight_front);
            vertices.Add(innerBottomLeft_back);
            vertices.Add(innerBottomRight_back);

            uvs.Add(uv01);
            uvs.Add(uv11);
            uvs.Add(uv01);
            uvs.Add(uv11);

            // FRONT
            vertices.Add(innerTopLeft_front);
            vertices.Add(innerTopRight_front);
            vertices.Add(innerBottomLeft_front);
            vertices.Add(innerBottomRight_front);

            uvs.Add(uv00);
            uvs.Add(uv10);
            uvs.Add(uv01);
            uvs.Add(uv11);

            // BACK
            vertices.Add(innerTopLeft_back);
            vertices.Add(innerTopRight_back);
            vertices.Add(innerBottomLeft_back);
            vertices.Add(innerBottomRight_back);

            uvs.Add(uv00);
            uvs.Add(uv10);
            uvs.Add(uv01);
            uvs.Add(uv11);

            for (int i = 0; i < 4; ++i) normals.Add(Vector3.up);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.left);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.right);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.down);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.back);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.forward);

            int[] face_indices = new int[]
            {
            0,2,1,
            2,3,1,
            5,7,4,
            7,6,4,
            8,10,9,
            10,11,9,
            13,15,12,
            15,14,12,
            16,17,19,
            16,19,18,
            20,23,21,
            20,22,23
            };

            for (int i = 0; i < face_indices.Length; ++i)
            {
                indices.Add(face_indices[i]);
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            return mesh;
        }

        public static Mesh BuildRoundedBox(float width, float height, float margin, float thickness)
        {
            const int default_nbSubdivCornerFixed = 3;
            const int default_nbSubdivCornerPerUnit = 3;
            
            return BuildRoundedBoxEx(
                width, height, margin, thickness,
                default_nbSubdivCornerFixed, default_nbSubdivCornerPerUnit);
        }

        public static Mesh BuildRoundedBoxEx(float width, float height, float margin, float thickness, int nbSubdivCornerFixed, int nbSubdivCornerPerUnit)
        {
            List<Vector3> vertices = new List<Vector3>(); // TODO: use Vector3[] vertices = new Vector3[computed_size];
            List<Vector3> normals = new List<Vector3>(); // TODO: use Vector3[] normals = new Vector3[computed_size];
            List<int> indices = new List<int>(); // TODO: use int[] indices = new int[computed_size];

            Vector3 innerTopLeft_front = new Vector3(margin, -margin, 0.0f);
            Vector3 innerTopRight_front = new Vector3(width - margin, -margin, 0.0f);
            Vector3 innerBottomRight_front = new Vector3(width - margin, -height + margin, 0.0f);
            Vector3 innerBottomLeft_front = new Vector3(margin, -height + margin, 0.0f);

            Vector3 innerTopLeft_back = new Vector3(margin, -margin, thickness);
            Vector3 innerTopRight_back = new Vector3(width - margin, -margin, thickness);
            Vector3 innerBottomRight_back = new Vector3(width - margin, -height + margin, thickness);
            Vector3 innerBottomLeft_back = new Vector3(margin, -height + margin, thickness);

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

            for (int i = 0; i < 12; ++i)
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
                indices.Add(indexOffset + 2 * (i + 1) + 0);
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

        public static Mesh BuildHollowCube(float width, float height)
        {
            const float default_margin = 0.005f;
            const float default_thickness = 0.005f;

            return BuildHollowCubeEx(width, height, default_margin, default_thickness);
        }

        public static Mesh BuildHollowCubeEx(float width, float height, float margin, float thickness)
        {
            int currentIndex = 0;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> indices = new List<int>();

            float h2 = height / 2.0f;
            float m = margin;

            Vector3 front_outer_top_left = new Vector3(0.0f, h2, -thickness);
            Vector3 front_outer_top_right = new Vector3(width, h2, -thickness);
            Vector3 front_outer_bottom_left = new Vector3(0.0f, -h2, -thickness);
            Vector3 front_outer_bottom_right = new Vector3(width, -h2, -thickness);

            Vector3 front_inner_top_left = new Vector3(m, h2-m, -thickness);
            Vector3 front_inner_top_right = new Vector3(width-m, h2-m, -thickness);
            Vector3 front_inner_bottom_left = new Vector3(m, -h2+m, -thickness);
            Vector3 front_inner_bottom_right = new Vector3(width-m, -h2+m, -thickness);

            Vector3 back_outer_top_left = new Vector3(0.0f, h2, 0.0f);
            Vector3 back_outer_top_right = new Vector3(width, h2, 0.0f);
            Vector3 back_outer_bottom_left = new Vector3(0.0f, -h2, 0.0f);
            Vector3 back_outer_bottom_right = new Vector3(width, -h2, 0.0f);

            Vector3 back_inner_top_left = new Vector3(m, h2-m, 0.0f);
            Vector3 back_inner_top_right = new Vector3(width-m, h2-m, 0.0f);
            Vector3 back_inner_bottom_left = new Vector3(m, -h2+m, 0.0f);
            Vector3 back_inner_bottom_right = new Vector3(width-m, -h2+m, 0.0f);


            #region front-hollow-face

            vertices.Add(front_outer_top_left);
            vertices.Add(front_outer_top_right);
            vertices.Add(front_outer_bottom_left);
            vertices.Add(front_outer_bottom_right);
            vertices.Add(front_inner_top_left);
            vertices.Add(front_inner_top_right);
            vertices.Add(front_inner_bottom_left);
            vertices.Add(front_inner_bottom_right);

            for (int i = 0; i < 8; ++i) normals.Add(Vector3.up);

            int[] outer_front_hollow_face_indices = new int[]
            {
            0,1,4,
            4,1,5,
            5,1,7,
            7,1,3,
            7,3,6,
            6,3,2,
            6,2,4,
            4,2,0
            };

            for (int i = 0; i < outer_front_hollow_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + outer_front_hollow_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion


            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            return mesh;
        }

        #endregion

        #region Resource Loading

        public static Material LoadMaterial(string materialName)
        {
            string[] pathList = AssetDatabase.FindAssets(materialName, new[] { "Assets/Resources/Materials/UI" });
            if (pathList.Length > 0)
            {
                foreach (string path in pathList)
                {
                    var obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(path), typeof(Material));
                    if (obj is Material)
                        return obj as Material;
                }
            }
            return null;
        }

        public static Sprite LoadIcon(string iconName)
        {
            // TODO: Doesn't work, find out why.
            //Sprite sprite = Resources.Load("Textures/UI/paint") as Sprite;
            //return sprite;

            string[] pathList = AssetDatabase.FindAssets(iconName, new[] { "Assets/Resources/Textures/UI" });
            if (pathList.Length > 0)
            {
                foreach (string path in pathList)
                {
                    var obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(path), typeof(Sprite));
                    if (obj is Sprite)
                        return obj as Sprite;
                }
            }
            return null;
        }

        #endregion


    }
}
