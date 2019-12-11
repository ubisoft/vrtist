using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{

    [System.AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class SpaceHeaderAttribute : PropertyAttribute
    {
        public readonly string caption;
        public readonly float spaceHeight;
        public readonly Color lineColor = Color.red;

        public SpaceHeaderAttribute(string caption, float spaceHeight, float r, float g, float b)
        {
            this.caption = caption;
            this.spaceHeight = spaceHeight;
            this.lineColor = new Color(r, g, b);
        }
    }
}
