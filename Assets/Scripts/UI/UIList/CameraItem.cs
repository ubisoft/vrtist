using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CameraItem : MonoBehaviour
    {
        public GameObject cameraObject;

        public void SetCameraObject(GameObject cameraObject)
        {
            this.cameraObject = cameraObject;
            Camera cam = cameraObject.GetComponentInChildren<Camera>(true);
            gameObject.GetComponentInChildren<MeshRenderer>(true).material.SetTexture("_UnlitColorMap", cam.targetTexture);
        }
    }
}
