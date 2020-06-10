using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Internal;
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
            gameObject.GetComponentInChildren<MeshRenderer>(true).materials[0].SetColor("_BaseColor", new Color(0f, 0.6549f, 1f));
            gameObject.GetComponentInChildren<MeshRenderer>(true).materials[1].SetTexture("_UnlitColorMap", cam.targetTexture);
        }
    }
}
