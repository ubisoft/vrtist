using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CameraFeedback : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            GameObject activeCamera = Selection.activeCamera;
            if (null != activeCamera)
            {
                Camera cam = activeCamera.GetComponentInChildren<Camera>();
                gameObject.SetActive(true);
                GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", cam.targetTexture);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}