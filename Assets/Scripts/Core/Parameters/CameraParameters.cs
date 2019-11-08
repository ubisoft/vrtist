using System;
using UnityEngine;

namespace VRtist
{
    [Serializable]
    public class CameraParameters : Parameters
    {
        public float focal = 35f;

        public Transform Deserialize(Transform parent)
        {
            GameObject cam = Utils.CreateInstance(Resources.Load("Prefabs/Camera") as GameObject, parent);
            cam.GetComponent<CameraController>().parameters = this;
            return cam.transform;
        }
    }    
}