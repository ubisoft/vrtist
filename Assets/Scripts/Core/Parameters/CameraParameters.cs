using System;
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
    }    
}