/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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
