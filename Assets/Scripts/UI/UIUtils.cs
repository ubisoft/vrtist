using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;

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

            Vector2 uv00 = new Vector2(0, 0);
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], over a quarter of circle
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], over a quarter of circle
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
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
                float fQuarterPercent = 0.25f * (float) cs / (float) (nbVerticesOnCorner - 1); // [0..1], over a quarter of circle
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

            Vector3 front_inner_top_left = new Vector3(m, h2 - m, -thickness);
            Vector3 front_inner_top_right = new Vector3(width - m, h2 - m, -thickness);
            Vector3 front_inner_bottom_left = new Vector3(m, -h2 + m, -thickness);
            Vector3 front_inner_bottom_right = new Vector3(width - m, -h2 + m, -thickness);

            Vector3 back_outer_top_left = new Vector3(0.0f, h2, 0.0f);
            Vector3 back_outer_top_right = new Vector3(width, h2, 0.0f);
            Vector3 back_outer_bottom_left = new Vector3(0.0f, -h2, 0.0f);
            Vector3 back_outer_bottom_right = new Vector3(width, -h2, 0.0f);

            Vector3 back_inner_top_left = new Vector3(m, h2 - m, 0.0f);
            Vector3 back_inner_top_right = new Vector3(width - m, h2 - m, 0.0f);
            Vector3 back_inner_bottom_left = new Vector3(m, -h2 + m, 0.0f);
            Vector3 back_inner_bottom_right = new Vector3(width - m, -h2 + m, 0.0f);


            #region front-hollow-face

            vertices.Add(front_outer_top_left);
            vertices.Add(front_outer_top_right);
            vertices.Add(front_outer_bottom_left);
            vertices.Add(front_outer_bottom_right);
            vertices.Add(front_inner_top_left);
            vertices.Add(front_inner_top_right);
            vertices.Add(front_inner_bottom_left);
            vertices.Add(front_inner_bottom_right);

            for (int i = 0; i < 8; ++i) normals.Add(Vector3.back);

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

            #region back-hollow-face

            vertices.Add(back_outer_top_left);
            vertices.Add(back_outer_top_right);
            vertices.Add(back_outer_bottom_left);
            vertices.Add(back_outer_bottom_right);
            vertices.Add(back_inner_top_left);
            vertices.Add(back_inner_top_right);
            vertices.Add(back_inner_bottom_left);
            vertices.Add(back_inner_bottom_right);

            for (int i = 0; i < 8; ++i) normals.Add(Vector3.forward);

            int[] outer_back_hollow_face_indices = new int[]
            {
            0,4,1,
            4,5,1,
            5,7,1,
            7,3,1,
            7,6,3,
            6,2,3,
            6,4,2,
            4,0,2
            };

            for (int i = 0; i < outer_back_hollow_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + outer_back_hollow_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion

            #region inner-top-face

            vertices.Add(front_inner_top_left);
            vertices.Add(front_inner_top_right);
            vertices.Add(back_inner_top_left);
            vertices.Add(back_inner_top_right);

            for (int i = 0; i < 4; ++i) normals.Add(Vector3.down);

            int[] inner_top_face_indices = new int[]
            {
            0,1,2,
            2,1,3
            };

            for (int i = 0; i < inner_top_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + inner_top_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion

            #region inner-bottom-face

            vertices.Add(back_inner_bottom_left);
            vertices.Add(back_inner_bottom_right);
            vertices.Add(front_inner_bottom_left);
            vertices.Add(front_inner_bottom_right);

            for (int i = 0; i < 4; ++i) normals.Add(Vector3.up);

            int[] inner_bottom_face_indices = new int[]
            {
            0,1,2,
            2,1,3
            };

            for (int i = 0; i < inner_bottom_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + inner_bottom_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion

            #region inner-left-face

            vertices.Add(front_inner_bottom_left);
            vertices.Add(front_inner_top_left);
            vertices.Add(back_inner_bottom_left);
            vertices.Add(back_inner_top_left);

            for (int i = 0; i < 4; ++i) normals.Add(Vector3.right);

            int[] inner_left_face_indices = new int[]
            {
            0,1,2,
            2,1,3
            };

            for (int i = 0; i < inner_left_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + inner_left_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion

            #region inner-right-face

            vertices.Add(back_inner_bottom_right);
            vertices.Add(back_inner_top_right);
            vertices.Add(front_inner_bottom_right);
            vertices.Add(front_inner_top_right);

            for (int i = 0; i < 4; ++i) normals.Add(Vector3.left);

            int[] inner_right_face_indices = new int[]
            {
            0,1,2,
            2,1,3
            };

            for (int i = 0; i < inner_right_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + inner_right_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion

            #region outer-top-face

            vertices.Add(back_outer_top_left);
            vertices.Add(back_outer_top_right);
            vertices.Add(front_outer_top_left);
            vertices.Add(front_outer_top_right);

            for (int i = 0; i < 4; ++i) normals.Add(Vector3.down);

            int[] outer_top_face_indices = new int[]
            {
            0,1,2,
            2,1,3
            };

            for (int i = 0; i < outer_top_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + outer_top_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion

            #region outer-bottom-face

            vertices.Add(front_outer_bottom_left);
            vertices.Add(front_outer_bottom_right);
            vertices.Add(back_outer_bottom_left);
            vertices.Add(back_outer_bottom_right);

            for (int i = 0; i < 4; ++i) normals.Add(Vector3.down);

            int[] outer_bottom_face_indices = new int[]
            {
            0,1,2,
            2,1,3
            };

            for (int i = 0; i < outer_bottom_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + outer_bottom_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion

            #region outer-left-face

            vertices.Add(front_outer_top_left);
            vertices.Add(front_outer_bottom_left);
            vertices.Add(back_outer_top_left);
            vertices.Add(back_outer_bottom_left);

            for (int i = 0; i < 4; ++i) normals.Add(Vector3.down);

            int[] outer_left_face_indices = new int[]
            {
            0,1,2,
            2,1,3
            };

            for (int i = 0; i < outer_left_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + outer_left_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion

            #region outer-right-face

            vertices.Add(back_outer_top_right);
            vertices.Add(back_outer_bottom_right);
            vertices.Add(front_outer_top_right);
            vertices.Add(front_outer_bottom_right);

            for (int i = 0; i < 4; ++i) normals.Add(Vector3.down);

            int[] outer_right_face_indices = new int[]
            {
            0,1,2,
            2,1,3
            };

            for (int i = 0; i < outer_right_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + outer_right_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            return mesh;
        }

        public static Mesh BuildSliderKnob(float width, float height, float depth)
        {
            return BuildSliderKnobEx(width, height, depth, width, height, depth);
        }

        public static Mesh BuildSliderKnobEx(float head_width, float head_height, float head_depth, float foot_width, float foot_height, float foot_depth)
        {
            int currentIndex = 0;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> indices = new List<int>();

            float hw2 = head_width / 2.0f;
            float fw2 = foot_width / 2.0f;
            float hh2 = head_height / 2.0f;
            float fh2 = foot_height / 2.0f;
            float hd = head_depth;
            float fd = foot_depth;

            Vector3 back_foot_top_left = new Vector3(-fw2, fh2, 0.0f);
            Vector3 back_foot_top_right = new Vector3(fw2, fh2, 0.0f);
            Vector3 back_foot_bottom_left = new Vector3(-fw2, -fh2, 0.0f);
            Vector3 back_foot_bottom_right = new Vector3(fw2, -fh2, 0.0f);

            Vector3 front_foot_top_left = new Vector3(-fw2, fh2, -fd);
            Vector3 front_foot_top_right = new Vector3(fw2, fh2, -fd);
            Vector3 front_foot_bottom_left = new Vector3(-fw2, -fh2, -fd);
            Vector3 front_foot_bottom_right = new Vector3(fw2, -fh2, -fd);

            Vector3 back_head_top_left = new Vector3(-hw2, hh2, -fd);
            Vector3 back_head_top_right = new Vector3(hw2, hh2, -fd);
            Vector3 back_head_bottom_left = new Vector3(-hw2, -hh2, -fd);
            Vector3 back_head_bottom_right = new Vector3(hw2, -hh2, -fd);

            Vector3 front_head_top_left = new Vector3(-hw2, hh2, -fd - hd);
            Vector3 front_head_top_right = new Vector3(hw2, hh2, -fd - hd);
            Vector3 front_head_bottom_left = new Vector3(-hw2, -hh2, -fd - hd);
            Vector3 front_head_bottom_right = new Vector3(hw2, -hh2, -fd - hd);

            #region foot

            // TOP
            vertices.Add(back_foot_top_left);
            vertices.Add(back_foot_top_right);
            vertices.Add(front_foot_top_left);
            vertices.Add(front_foot_top_right);

            // BOTTOM
            vertices.Add(front_foot_bottom_left);
            vertices.Add(front_foot_bottom_right);
            vertices.Add(back_foot_bottom_left);
            vertices.Add(back_foot_bottom_right);

            // LEFT
            vertices.Add(back_foot_top_left);
            vertices.Add(front_foot_top_left);
            vertices.Add(back_foot_bottom_left);
            vertices.Add(front_foot_bottom_left);

            // RIGHT
            vertices.Add(front_foot_top_right);
            vertices.Add(back_foot_top_right);
            vertices.Add(front_foot_bottom_right);
            vertices.Add(back_foot_bottom_right);

            // FRONT
            vertices.Add(front_foot_top_left);
            vertices.Add(front_foot_top_right);
            vertices.Add(front_foot_bottom_left);
            vertices.Add(front_foot_bottom_right);

            // BACK
            vertices.Add(back_foot_top_right);
            vertices.Add(back_foot_top_left);
            vertices.Add(back_foot_bottom_right);
            vertices.Add(back_foot_bottom_left);

            for (int i = 0; i < 4; ++i) normals.Add(Vector3.up);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.down);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.left);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.right);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.back); // front
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.forward); // back

            int[] foot_face_indices = new int[]
            {
            0,1,2,
            2,1,3,

            4,5,6,
            6,5,7,

            8,9,10,
            10,9,11,

            12,13,14,
            14,13,15,

            16,17,18,
            18,17,19,

            20,21,22,
            22,21,23
            };

            for (int i = 0; i < foot_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + foot_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion

            #region head

            // TOP
            vertices.Add(back_head_top_left);
            vertices.Add(back_head_top_right);
            vertices.Add(front_head_top_left);
            vertices.Add(front_head_top_right);

            // BOTTOM
            vertices.Add(front_head_bottom_left);
            vertices.Add(front_head_bottom_right);
            vertices.Add(back_head_bottom_left);
            vertices.Add(back_head_bottom_right);

            // LEFT
            vertices.Add(back_head_top_left);
            vertices.Add(front_head_top_left);
            vertices.Add(back_head_bottom_left);
            vertices.Add(front_head_bottom_left);

            // RIGHT
            vertices.Add(front_head_top_right);
            vertices.Add(back_head_top_right);
            vertices.Add(front_head_bottom_right);
            vertices.Add(back_head_bottom_right);

            // FRONT
            vertices.Add(front_head_top_left);
            vertices.Add(front_head_top_right);
            vertices.Add(front_head_bottom_left);
            vertices.Add(front_head_bottom_right);

            // BACK
            vertices.Add(back_head_top_right);
            vertices.Add(back_head_top_left);
            vertices.Add(back_head_bottom_right);
            vertices.Add(back_head_bottom_left);

            for (int i = 0; i < 4; ++i) normals.Add(Vector3.up);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.down);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.left);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.right);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.back); // front
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.forward); // back

            int[] head_face_indices = new int[]
            {
            0,1,2,
            2,1,3,

            4,5,6,
            6,5,7,

            8,9,10,
            10,9,11,

            12,13,14,
            14,13,15,

            16,17,18,
            18,17,19,

            20,21,22,
            22,21,23
            };

            for (int i = 0; i < head_face_indices.Length; ++i)
            {
                indices.Add(currentIndex + head_face_indices[i]);
            }

            currentIndex = vertices.Count;

            #endregion

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            return mesh;
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

        public static Mesh BuildRoundedRectTubeEx(float width, float height, float margin, float radius, int circleSubdiv, int nbSubdivPerUnit, int nbSubdivCornerFixed, int nbSubdivCornerPerUnit)
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
            float sectionWidth = hTubeWidth / (float) (nbSubdivOnWidth + 1);
            float sectionHeight = vTubeHeight / (float) (nbSubdivOnHeight + 1);

            Vector3 panelTopLeft = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 panelTopRight = new Vector3(width, 0.0f, 0.0f);
            Vector3 panelBottomLeft = new Vector3(0.0f, -height, 0.0f);
            Vector3 panelBottomRight = new Vector3(width, -height, 0.0f);

            int indexOffset = 0;

            #region top-horizontal-bar: left to right, circle right axis = +Z
            {
                Vector3 sectionOffset = panelTopLeft + new Vector3(margin, 0, 0);
                for (int cs = 0; cs < nbCirclesOnWidth; ++cs)
                {
                    for (int i = 0; i < circleSubdiv; ++i)
                    {
                        float fi = (float) i / (float) circleSubdiv;
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
                        float fi = (float) i / (float) circleSubdiv;
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
                        float fi = (float) i / (float) circleSubdiv;
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
                        float fi = (float) i / (float) circleSubdiv;
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
                    float fQuarterPercent = 0.25f * (float) cs / (float) (nbCirclesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
                    Vector3 outerCirclePos = new Vector3(
                        -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                        +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                        0);
                    Vector3 innerCircleCenter = cornerCenter + outerCirclePos;
                    unitUp = outerCirclePos.normalized;
                    for (int i = 0; i < circleSubdiv; ++i)
                    {
                        float fi = (float) i / (float) circleSubdiv;

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

            #region top-right-corner: outer-circle from Y(top) to X(right), circle right axis = +Z
            {
                Vector3 cornerCenter = panelTopRight + new Vector3(-margin, -margin, 0);
                Vector3 unitRight = new Vector3(0, 0, 1);
                Vector3 unitUp = new Vector3(0, 1, 0);
                for (int cs = 0; cs < nbCirclesOnCorner; ++cs)
                {
                    float fQuarterPercent = 0.25f * (float) cs / (float) (nbCirclesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
                    Vector3 outerCirclePos = new Vector3(
                        +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                        +margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                        0);
                    Vector3 innerCircleCenter = cornerCenter + outerCirclePos;
                    unitUp = outerCirclePos.normalized;
                    for (int i = 0; i < circleSubdiv; ++i)
                    {
                        float fi = (float) i / (float) circleSubdiv;

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
                    float fQuarterPercent = 0.25f * (float) cs / (float) (nbCirclesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
                    Vector3 outerCirclePos = new Vector3(
                        +margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                        -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                        0);
                    Vector3 innerCircleCenter = cornerCenter + outerCirclePos;
                    unitUp = -outerCirclePos.normalized;
                    for (int i = 0; i < circleSubdiv; ++i)
                    {
                        float fi = (float) i / (float) circleSubdiv;

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
                    float fQuarterPercent = 0.25f * (float) cs / (float) (nbCirclesOnCorner - 1); // [0..1], from top to left of corner quarter of circle.
                    Vector3 outerCirclePos = new Vector3(
                        -margin * Mathf.Cos(2.0f * Mathf.PI * fQuarterPercent),
                        -margin * Mathf.Sin(2.0f * Mathf.PI * fQuarterPercent),
                        0);
                    Vector3 innerCircleCenter = cornerCenter + outerCirclePos;
                    unitUp = -outerCirclePos.normalized;
                    for (int i = 0; i < circleSubdiv; ++i)
                    {
                        float fi = (float) i / (float) circleSubdiv;

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

        public static Mesh BuildHSV(float width, float height, float thickness, float triangleRadiusPct, float innerRadiusPct, float outerRadiusPct, int nbSubdiv, Color hue)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Color> colors = new List<Color>();
            List<int> indices = new List<int>();

            float w2 = width / 2.0f;
            float h2 = height / 2.0f;
            float ir = innerRadiusPct * w2;
            float or = outerRadiusPct * w2;

            Vector3 mb = new Vector3(w2, -h2, 0.0f);

            int indexOffset = 0;

            #region FRONT/TOP FACE

            for (int i = 0; i < nbSubdiv; ++i) // [0..nb-1]
            {
                float pct = (float) i / (float) (nbSubdiv - 1);
                float cos = Mathf.Cos(2.0f * Mathf.PI * pct); // [1..0..-1..0..1]
                float sin = Mathf.Sin(2.0f * Mathf.PI * pct); // [0..1..0..-1..0]
                Vector3 innerCirclePos = new Vector3(w2 - ir * cos, -h2 + ir * sin, 0);
                Vector3 outerCirclePos = new Vector3(w2 - or * cos, -h2 + or * sin, 0);

                vertices.Add(innerCirclePos);
                vertices.Add(outerCirclePos);

                normals.Add(Vector3.back);
                normals.Add(Vector3.back);

                uvs.Add(new Vector2(pct, 0.0f)); // inner
                uvs.Add(new Vector2(pct, 1.0f)); // outer

                colors.Add(Color.white);
                colors.Add(Color.white);
            }

            for (int i = 0; i < nbSubdiv - 1; ++i) // [0..nb-2]
            {
                indices.Add(indexOffset + 2 * i + 0);
                indices.Add(indexOffset + 2 * i + 1);
                indices.Add(indexOffset + 2 * (i + 1) + 0);
                indices.Add(indexOffset + 2 * (i + 1) + 1);
                indices.Add(indexOffset + 2 * (i + 1) + 0);
                indices.Add(indexOffset + 2 * i + 1);
            }

            indexOffset = vertices.Count;

            #endregion

            #region INNER BAND

            for (int i = 0; i < nbSubdiv; ++i) // [0..nb-1]
            {
                float pct = (float) i / (float) (nbSubdiv - 1);
                float cos = Mathf.Cos(2.0f * Mathf.PI * pct); // [1..0..-1..0..1]
                float sin = Mathf.Sin(2.0f * Mathf.PI * pct); // [0..1..0..-1..0]
                Vector3 bottomInnerCirclePos = new Vector3(w2 - ir * cos, -h2 + ir * sin, thickness);
                Vector3 topInnerCirclePos = new Vector3(w2 - ir * cos, -h2 + ir * sin, 0);

                vertices.Add(bottomInnerCirclePos);
                vertices.Add(topInnerCirclePos);

                normals.Add(Vector3.Normalize(mb - bottomInnerCirclePos)); // inner band
                normals.Add(Vector3.Normalize(mb - bottomInnerCirclePos)); // inner band

                uvs.Add(new Vector2(pct, 0.0f));
                uvs.Add(new Vector2(pct, 0.0f));

                colors.Add(Color.white);
                colors.Add(Color.white);
            }

            for (int i = 0; i < nbSubdiv - 1; ++i) // [0..nb-2]
            {
                indices.Add(indexOffset + 2 * i + 0);
                indices.Add(indexOffset + 2 * i + 1);
                indices.Add(indexOffset + 2 * (i + 1) + 0);
                indices.Add(indexOffset + 2 * (i + 1) + 1);
                indices.Add(indexOffset + 2 * (i + 1) + 0);
                indices.Add(indexOffset + 2 * i + 1);
            }

            indexOffset = vertices.Count;

            #endregion

            #region OUTER BAND

            for (int i = 0; i < nbSubdiv; ++i) // [0..nb-1]
            {
                float pct = (float) i / (float) (nbSubdiv - 1);
                float cos = Mathf.Cos(2.0f * Mathf.PI * pct); // [1..0..-1..0..1]
                float sin = Mathf.Sin(2.0f * Mathf.PI * pct); // [0..1..0..-1..0]
                Vector3 bottomOuterCirclePos = new Vector3(w2 - or * cos, -h2 + or * sin, thickness);
                Vector3 topOuterCirclePos = new Vector3(w2 - or * cos, -h2 + or * sin, 0);

                vertices.Add(topOuterCirclePos);
                vertices.Add(bottomOuterCirclePos);

                normals.Add(Vector3.Normalize(bottomOuterCirclePos - mb)); // inner band
                normals.Add(Vector3.Normalize(bottomOuterCirclePos - mb)); // inner band

                uvs.Add(new Vector2(pct, 1.0f));
                uvs.Add(new Vector2(pct, 1.0f));

                colors.Add(Color.white);
                colors.Add(Color.white);
            }

            for (int i = 0; i < nbSubdiv - 1; ++i) // [0..nb-2]
            {
                indices.Add(indexOffset + 2 * i + 0);
                indices.Add(indexOffset + 2 * i + 1);
                indices.Add(indexOffset + 2 * (i + 1) + 0);
                indices.Add(indexOffset + 2 * (i + 1) + 1);
                indices.Add(indexOffset + 2 * (i + 1) + 0);
                indices.Add(indexOffset + 2 * i + 1);
            }

            indexOffset = vertices.Count;

            #endregion

            int firstMeshIndexCount = 3 * 6 * (nbSubdiv - 1);

            #region TRIANGLE

            float tr = triangleRadiusPct * w2;

            Vector3 top_front = new Vector3(w2, -h2 + tr, 0);
            Vector3 top_back = new Vector3(w2, -h2 + tr, thickness);

            Vector3 br_front = new Vector3(w2 + tr * Mathf.Cos(-Mathf.PI / 6.0f), -h2 + tr * Mathf.Sin(-Mathf.PI / 6.0f), 0);
            Vector3 br_back = new Vector3(w2 + tr * Mathf.Cos(-Mathf.PI / 6.0f), -h2 + tr * Mathf.Sin(-Mathf.PI / 6.0f), thickness);

            Vector3 bl_front = new Vector3(w2 - tr * Mathf.Cos(-Mathf.PI / 6.0f), -h2 + tr * Mathf.Sin(-Mathf.PI / 6.0f), 0);
            Vector3 bl_back = new Vector3(w2 - tr * Mathf.Cos(-Mathf.PI / 6.0f), -h2 + tr * Mathf.Sin(-Mathf.PI / 6.0f), thickness);

            Vector2 uvtop = new Vector2(.5f, 0.866f); // racine 3 / 2
            Vector2 uvbr = new Vector2(1, 0);
            Vector2 uvbl = new Vector2(0, 0);

            // FRONT
            vertices.Add(top_front);
            vertices.Add(br_front);
            vertices.Add(bl_front);

            uvs.Add(uvtop);
            uvs.Add(uvbr);
            uvs.Add(uvbl);

            colors.Add(Color.black);
            colors.Add(hue);
            colors.Add(Color.white);

            // UPPER RIGHT BORDER
            vertices.Add(br_back);
            vertices.Add(br_front);
            vertices.Add(top_back);
            vertices.Add(top_front);

            uvs.Add(uvbr);
            uvs.Add(uvbr);
            uvs.Add(uvtop);
            uvs.Add(uvtop);

            colors.Add(hue);
            colors.Add(hue);
            colors.Add(Color.black);
            colors.Add(Color.black);

            // UPPER LEFT BORDER
            vertices.Add(top_back);
            vertices.Add(top_front);
            vertices.Add(bl_back);
            vertices.Add(bl_front);

            uvs.Add(uvtop);
            uvs.Add(uvtop);
            uvs.Add(uvbl);
            uvs.Add(uvbl);

            colors.Add(Color.black);
            colors.Add(Color.black);
            colors.Add(Color.white);
            colors.Add(Color.white);

            // BOTTOM
            vertices.Add(bl_back);
            vertices.Add(bl_front);
            vertices.Add(br_back);
            vertices.Add(br_front);

            uvs.Add(uvbl);
            uvs.Add(uvbl);
            uvs.Add(uvbr);
            uvs.Add(uvbr);

            colors.Add(Color.white);
            colors.Add(Color.white);
            colors.Add(hue);
            colors.Add(hue);

            for (int i = 0; i < 3; ++i) normals.Add(Vector3.back);
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.Normalize(mb - bl_back)); // upper right
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.Normalize(mb - br_back)); // upper left
            for (int i = 0; i < 4; ++i) normals.Add(Vector3.down); // bottom

            int[] face_indices = new int[]
            {
            0,1,2,
            3,4,5,
            5,4,6,
            7,8,9,
            9,8,10,
            11,12,13,
            12,11,14
            };

            for (int i = 0; i < face_indices.Length; ++i)
            {
                indices.Add(indexOffset + face_indices[i]);
            }
            #endregion

            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.SetColors(colors);

            int secondMeshIndexStart = firstMeshIndexCount;
            int secondMeshIndexCount = vertices.Count;

            mesh.subMeshCount = 2;

            SubMeshDescriptor desc0 = new SubMeshDescriptor(0, firstMeshIndexCount);
            mesh.SetSubMesh(0, desc0);
            SubMeshDescriptor desc1 = new SubMeshDescriptor(secondMeshIndexStart, 21);
            mesh.SetSubMesh(1, desc1);

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

        #endregion

        #region Resource Loading
        static private Dictionary<string, Material> uiMaterials = new Dictionary<string, Material>();
        static private Dictionary<string, Sprite> uiSprites = new Dictionary<string, Sprite>();
        static private Dictionary<string, GameObject> uiPrefabs = new Dictionary<string, GameObject>();
        static private Dictionary<string, Mesh> uiMeshes = new Dictionary<string, Mesh>();

        public static Material LoadMaterial(string materialName)
        {
            if (!uiMaterials.ContainsKey(materialName))
                uiMaterials[materialName] = Resources.Load<Material>("Materials/UI/" + materialName);
            return uiMaterials[materialName];
        }

        public static Sprite LoadIcon(string iconName)
        {
            if (!uiSprites.ContainsKey(iconName))
                uiSprites[iconName] = Resources.Load<Sprite>("Textures/UI/" + iconName);
            return uiSprites[iconName];
        }

        public static GameObject LoadPrefab(string prefabName)
        {
            if (!uiPrefabs.ContainsKey(prefabName))
                uiPrefabs[prefabName] = Resources.Load<GameObject>("Prefabs/UI/" + prefabName);
            return uiPrefabs[prefabName];
        }

        public static Mesh LoadMesh(string meshName)
        {
            if (!uiMeshes.ContainsKey(meshName))
                uiMeshes[meshName] = Resources.Load<Mesh>("Models/" + meshName);
            return uiMeshes[meshName];
        }

        #endregion

        public static Component CopyComponent(Component original, GameObject destination)
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            // Copied fields can be restricted with BindingFlags
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy;
        }

        public static void SetRecursiveLayer(GameObject gObject, string layerName)
        {
            gObject.layer = LayerMask.NameToLayer(layerName);
            for (int i = 0; i < gObject.transform.childCount; i++)
            {
                SetRecursiveLayer(gObject.transform.GetChild(i).gameObject, layerName);
            }
        }

        public static bool HasParentOrConstraintSelected(Transform t, ref string parentLayerName)
        {
            Transform parent = t.parent;
            while(null != parent && parent.name != "RightHanded")
            {
                if (Selection.IsSelected(parent.gameObject))
                {
                    parentLayerName = LayerMask.LayerToName(parent.gameObject.layer);
                    return true;
                }
                parent = parent.parent;
            }

            ParentConstraint constraint = t.gameObject.GetComponent<ParentConstraint>();
            if (null != constraint)
            {
                if (constraint.sourceCount > 0)
                {
                    GameObject sourceObject = constraint.GetSource(0).sourceTransform.gameObject;
                    if (Selection.IsSelected(sourceObject))
                    {
                        parentLayerName = LayerMask.LayerToName(sourceObject.layer);
                        return true;
                    }
                }
            }

            LookAtConstraint lookAtConstraint = t.gameObject.GetComponent<LookAtConstraint>();
            if (null != lookAtConstraint)
            {
                if (lookAtConstraint.sourceCount > 0)
                {
                    GameObject sourceObject = constraint.GetSource(0).sourceTransform.gameObject;
                    if (Selection.IsSelected(lookAtConstraint.GetSource(0).sourceTransform.gameObject))
                    {
                        parentLayerName = LayerMask.LayerToName(sourceObject.layer);
                        return true;
                    }
                }
            }

            return false;
        }

        public static void SetRecursiveLayerSmart(GameObject gObject, LayerType layerType, bool isChild = false)
        {
            string layerName = LayerMask.LayerToName(gObject.layer);
            
            //
            // SELECT
            //
            if (layerType == LayerType.Selection)
            {
                if (layerName == "Default") 
                {
                    if (isChild && !Selection.IsSelected(gObject))
                        layerName = "SelectionChild";
                    else
                        layerName = "Selection";
                }
                else if (layerName == "Hover" || layerName == "HoverChild") 
                {
                    if (isChild && !Selection.IsSelected(gObject))
                        layerName = "SelectionChild";
                    else
                        layerName = "Selection";
                }
                else if (layerName == "CameraHidden") { layerName = "SelectionCameraHidden"; }
                else if (layerName == "HoverCameraHidden") { layerName = "SelectionCameraHidden"; }
            }
            //
            // HOVER
            //
            else if (layerType == LayerType.Hover)
            {
                if (layerName == "Default")
                {
                    if (isChild && !Selection.IsSelected(gObject))
                        layerName = "HoverChild";
                    else
                        layerName = "Hover";
                }
                else if (layerName == "Selection" || layerName == "SelectionChild")
                {
                    if (isChild && !Selection.IsSelected(gObject))
                        layerName = "HoverChild";
                    else
                        layerName = "Hover";
                }
                else if (layerName == "CameraHidden") { layerName = "HoverCameraHidden"; }
                else if (layerName == "SelectionCameraHidden") { layerName = "HoverCameraHidden"; }
            }
            //
            // RESET layer
            //
            else if (layerType == LayerType.Default)
            {
                if (layerName == "SelectionCameraHidden") { layerName = "CameraHidden"; }
                else if (layerName == "Hover" || layerName == "HoverChild") 
                {
                    string parentLayer = "";
                    if (HasParentOrConstraintSelected(gObject.transform, ref parentLayer))
                    {
                        layerName = parentLayer + "Child";
                    }
                    else
                    {
                        layerName = "Default";  
                    }
                }
                else if (layerName == "HoverCameraHidden") { layerName = "CameraHidden"; }
                else if (layerName == "Selection" || layerName == "SelectionChild") 
                {
                    string parentLayer = "";
                    if (HasParentOrConstraintSelected(gObject.transform, ref parentLayer))
                    {
                        layerName = parentLayer + "Child";
                    }
                    else
                    {
                        layerName = "Default";
                    }
                }
            }

            gObject.layer = LayerMask.NameToLayer(layerName);
            for (int i = 0; i < gObject.transform.childCount; i++)
            {
                SetRecursiveLayerSmart(gObject.transform.GetChild(i).gameObject, layerType, true);
            }

            ParametersController parametersConstroller = gObject.GetComponent<ParametersController>();
            if(null != parametersConstroller)
            {
                foreach (GameObject sourceConstraint in parametersConstroller.constraintHolders)
                {
                    if(!Selection.IsSelected(sourceConstraint))
                        SetRecursiveLayerSmart(sourceConstraint, layerType, true);
                }
            }
        }

        public static void SetTMProStyle(GameObject gobject, float minSize = 1f, float maxSize = 1.5f, bool autoSizing = true, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            TextMeshProUGUI[] texts = gobject.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                //text.fontStyle = FontStyles.Normal;
                text.enableAutoSizing = autoSizing;
                text.fontSizeMin = minSize;
                text.fontSizeMax = maxSize;
                text.alignment = alignment;
            }
        }
    }
}
