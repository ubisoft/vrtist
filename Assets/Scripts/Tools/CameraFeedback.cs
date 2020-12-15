using UnityEngine;
using UnityEngine.Assertions;

namespace VRtist
{
    public class CameraFeedback : MonoBehaviour
    {
        public Transform targetCamera;
        private GameObject cameraPlane;
        private GameObject feedbackCamera = null;

        private void Awake()
        {
            Selection.OnActiveCameraChanged += OnCameraChanged;
        }

        void Start()
        {
            Assert.IsTrue(transform.GetChild(0).name == "CameraFeedbackPlane");
            cameraPlane = transform.GetChild(0).gameObject;
        }

        protected void Update()
        {
            if (!gameObject.activeSelf)
                return;

            if (null == feedbackCamera)
                return;

            float far = Camera.main.farClipPlane * GlobalState.WorldScale * 0.7f;
            float fov = Camera.main.fieldOfView;

            Camera cam = feedbackCamera.GetComponentInChildren<Camera>();
            float aspect = cam.aspect;

            float scale = far * Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 0.5f * GlobalState.Settings.cameraFeedbackScaleValue;
            Transform rig = targetCamera.parent.parent;
            Vector3 direction = rig.TransformDirection(GlobalState.Instance.cameraPreviewDirection);
            transform.localPosition = targetCamera.localPosition + direction.normalized * far;
            transform.localRotation = Quaternion.LookRotation(-direction) * Quaternion.Euler(0, 180, 0);
            transform.localScale = new Vector3(scale * aspect, scale, scale);

            GlobalState.Settings.cameraFeedbackPosition = transform.position;
            GlobalState.Settings.cameraFeedbackRotation = transform.rotation;
            GlobalState.Settings.cameraFeedbackScale = transform.localScale;
        }

        void OnCameraChanged(object sender, ActiveCameraChangedArgs args)
        {
            SetActiveCamera(args.activeCamera);
        }

        private void SetActiveCamera(GameObject activeCamera)
        {
            if (feedbackCamera == activeCamera)
                return;
            feedbackCamera = activeCamera;
            if (null != feedbackCamera)
            {
                Camera cam = feedbackCamera.GetComponentInChildren<Camera>();
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