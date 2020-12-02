﻿using UnityEngine;

namespace VRtist
{
    public class VolumeController : ParametersController
    {
        public Color color;
        public float[,,] field;
        public float stepSize = 0.01f;
        public Vector3 origin; // position of the bottom-left-front (-x/-y/-z) point of the field.
        public Bounds bounds;
        public Vector3 resolution;
    }
}