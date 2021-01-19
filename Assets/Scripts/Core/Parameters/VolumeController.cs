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

        // TODO: see where we can put it, maybe in Paint.cs
        //public void UpdateBoundsRenderer()
        //{
        //    LineRenderer line = gameObject.GetComponent<LineRenderer>();
        //    if (null != line)
        //    {
        //        Vector3 C = bounds.center;
        //        Vector3 E = bounds.extents;

        //        Vector3 tlf = transform.TransformPoint(C + new Vector3(-E.x, E.y, -E.z));
        //        Vector3 trf = transform.TransformPoint(C + new Vector3(E.x, E.y, -E.z));
        //        Vector3 blf = transform.TransformPoint(C + new Vector3(-E.x, -E.y, -E.z));
        //        Vector3 brf = transform.TransformPoint(C + new Vector3(E.x, -E.y, -E.z));
        //        Vector3 tlb = transform.TransformPoint(C + new Vector3(-E.x, E.y, E.z));
        //        Vector3 trb = transform.TransformPoint(C + new Vector3(E.x, E.y, E.z));
        //        Vector3 blb = transform.TransformPoint(C + new Vector3(-E.x, -E.y, E.z));
        //        Vector3 brb = transform.TransformPoint(C + new Vector3(E.x, -E.y, E.z));

        //        line.positionCount = 16;
        //        line.SetPositions(new Vector3[] { 
        //            blf, tlf, brf, trf, brb, trb, blb,
        //            blf, brf, brb, blb,
        //            tlb, tlf, trf, trb, tlb
        //        });
        //        line.startWidth = 0.001f / GlobalState.WorldScale;
        //        line.endWidth = 0.001f / GlobalState.WorldScale;
        //    }
        //}
    }
}
