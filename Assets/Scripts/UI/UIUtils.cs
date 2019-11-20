using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class UIUtils
    {
        // TODO: put BuidRoundedRectangle here.

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
    }
}
