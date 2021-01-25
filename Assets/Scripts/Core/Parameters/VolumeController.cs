using UnityEngine;

namespace VRtist
{
    public class VolumeController : ParametersController
    {
        public Vector3 origin; // position of the bottom-left-front (-x/-y/-z) point of the field.
        public Bounds bounds;
        public Vector3Int resolution;

        public Color color;
        public float[,,] field;
        public float stepSize = 0.01f;
    }
}
