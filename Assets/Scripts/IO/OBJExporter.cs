using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class OBJExporter
{
    public static void Export(string filename, GameObject gameObject)
    {
        IOUtilities.mkdir(filename);
        StringBuilder content = new StringBuilder(1024 * 1024);
        content.Append("g " + gameObject.name + "\n\n");

        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;

        int vertexCount = mesh.vertexCount;
        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 vertex = mesh.vertices[i];
            content.AppendFormat("v {0} {1} {2}\n", vertex.x, vertex.y, vertex.z);
        }
        content.Append("\n");

        for (int i = 0; i < vertexCount; i++)
        {
            Vector3 normal = mesh.normals[i];
            content.AppendFormat("vn {0} {1} {2}\n", normal.x, normal.y, normal.z);
        }

        int triangleCount = mesh.triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int i1 = mesh.triangles[3 * i + 0] + 1;
            int i2 = mesh.triangles[3 * i + 1] + 1;
            int i3 = mesh.triangles[3 * i + 2] + 1;
            content.AppendFormat("f {0}//{0} {1}//{1} {2}//{2}\n", i1, i2, i3);
        }

        content.Append("\n");
        File.WriteAllText(filename, content.ToString());
    }
}
