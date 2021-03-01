using UnityEngine;
using UnityEngine.Assertions;

namespace VRtist
{
    public class CameraFeedback : MonoBehaviour
    {
        public Transform vrCamera;
        public Transform rig;
        private GameObject cameraPlane;
        private GameObject feedbackCamera = null;

        private void Awake()
        {
            CameraManager.Instance.onActiveCameraChanged.AddListener(OnCameraChanged);
            Assert.IsTrue(transform.GetChild(0).name == "CameraFeedbackPlane");
            cameraPlane = transform.GetChild(0).gameObject;
        }

        private void OnEnable()
        {
            SetActiveCamera(CameraManager.Instance.ActiveCamera);
        }

        protected void Update()
        {
            if (!gameObject.activeSelf)
                return;

            if (null == feedbackCamera)
                return;

            float far = Camera.main.farClipPlane * GlobalState.WorldScale * 0.7f;
            float fov = Camera.main.fieldOfView;

            Camera cam = feedbackCamera.GetComponentInChildren<Camera>(true);
            float aspect = cam.aspect;

            float scale = far * Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 0.5f * GlobalState.Settings.cameraFeedbackScaleValue;
            Vector3 direction = GlobalState.Settings.cameraFeedbackDirection;
            transform.localPosition = direction.normalized * far;
            transform.localRotation = Quaternion.LookRotation(-direction) * Quaternion.Euler(0, 180, 0);
            transform.localScale = new Vector3(scale * aspect, scale, scale);
        }

        void OnCameraChanged(GameObject _, GameObject activeCamera)
        {
            SetActiveCamera(activeCamera);
        }

        private void SetActiveCamera(GameObject activeCamera)
        {
            if (feedbackCamera == activeCamera)
                return;
            feedbackCamera = activeCamera;
            if (null != feedbackCamera)
            {
                Camera cam = feedbackCamera.GetComponentInChildren<Camera>(true);
                cameraPlane.SetActive(true);
                cameraPlane.GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", cam.targetTexture);
            }
            else
            {
                cameraPlane.SetActive(false);
            }
        }
    }
}
