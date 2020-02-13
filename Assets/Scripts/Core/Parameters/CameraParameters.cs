using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    [Serializable]
    public class CameraParameters : Parameters
    {
        public float focal = 35f;
        public float near = 0.07f;
        public float far = 1000f;

        public Transform Deserialize(Transform parent)
        {
            GameObject camObject = Utils.CreateInstance(Resources.Load("Prefabs/Camera") as GameObject, parent);
            camObject.GetComponent<CameraController>().parameters = this;
            return camObject.transform;
        }

        //
        // ANIM parameters
        //

        public abstract class KeyFrameData {}
        public class PositionKeyFrame : KeyFrameData
        {
            public Vector3 position;
            public Quaternion rotation;
            public float focal;
        }

        public SortedList<int, Vector3> position_kf = new SortedList<int, Vector3>();
        public SortedList<int, Quaternion> rotation_kf = new SortedList<int, Quaternion>();
        public SortedList<int, float> focal_kf = new SortedList<int, float>();
    }
}

/*
         //private static int FrameOfPreviousKeyFrame(int current, SortedList<int, KeyFrameData> keyframes)
        //{
        //    int prev = current >= 0 ? current : (keyframes.Keys.Count > 0 ? keyframes.Keys[0] : 0);
        //    foreach(int k in keyframes.Keys)
        //    {
        //        if (k >= current) break;
        //        prev = k;
        //    }
        //    return prev;
        //}
        //private static int FrameOfNextKeyFrame(int current, SortedList<int, KeyFrameData> keyframes)
        //{
        //    int next = current >= 0 ? current : (keyframes.Keys.Count > 0 ? keyframes.Keys[keyframes.Keys.Count - 1] : 0);
        //    foreach (int k in keyframes.Keys)
        //    {
        //        if (k > current) return k;
        //    }
        //    return next;
        //}

     */
