using UnityEngine;

namespace VRtist
{
    public class CameraFeedback : MonoBehaviour
    {
        private void Awake()
        {
            Selection.OnActiveCameraChanged += OnCameraChanged;
        }

        private void OnEnable()
        {
            SetActiveCamera(Selection.activeCamera);
        }

        void OnCameraChanged(object sender, ActiveCameraChangedArgs args)
        {
            SetActiveCamera(args.activeCamera);
        }

        private void OnApplicationQuit()
        {
            GlobalState.Settings.SetWindowPosition(transform.parent);
        }

        private void SetActiveCamera(GameObject activeCamera)
        {
            if(null != activeCamera)
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