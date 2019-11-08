using Newtonsoft.Json;
using System;
using UnityEngine;

namespace VRtist
{
    [Serializable]
    public class PaintParameters : Parameters
    {
        [JsonProperty("color")]
        public Color color;
        [JsonProperty("controlPoints")]
        public Vector3[] controlPoints;
        [JsonProperty("controlPointsRadius")]
        public float[] controlPointsRadius;

        public Transform Deserialize(Transform parent)
        {
            GameObject paint = Utils.CreatePaint(parent, color);
            paint.GetComponent<PaintController>().parameters = this;

            // set mesh components
            var freeDraw = new FreeDraw(controlPoints, controlPointsRadius);
            MeshFilter meshFilter = paint.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.mesh;
            mesh.Clear();
            mesh.vertices = freeDraw.vertices;
            mesh.normals = freeDraw.normals;
            mesh.triangles = freeDraw.triangles;

            MeshCollider collider = paint.AddComponent<MeshCollider>();

            return paint.transform;
        }
    }
}