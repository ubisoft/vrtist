using UnityEngine;

namespace VRtist
{
    public class CameraFeedback : MonoBehaviour
    {
        public Transform targetCamera;
        private Transform backgroundFeedback;
        private void Awake()
        {
            Selection.OnActiveCameraChanged += OnCameraChanged;
        }

        void Start()
        {
            backgroundFeedback = transform.parent;
        }

        private void OnEnable()
        {
            SetActiveCamera(Selection.activeCamera);
        }

        protected void Update()
        {
            if (!backgroundFeedback.gameObject.activeSelf)
                return;

            GameObject currentCamera = Selection.activeCamera;
            float far = 1000f * 0.7f; // 70% of far clip plane
            float fov = 36.3f;
            float aspect = 16f / 9f;
            if (null != currentCamera)
            {
                far = Camera.main.farClipPlane * 0.7f;
                fov = Camera.main.fieldOfView;

                Camera cam = currentCamera.GetComponentInChildren<Camera>();
                aspect = cam.aspect;
            }
            float scale = far * Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 0.5f * GlobalState.Settings.cameraFeedbackScaleValue;
            backgroundFeedback.position = targetCamera.position + GlobalState.Instance.cameraPreviewDirection.normalized * far;
            backgroundFeedback.rotation = Quaternion.LookRotation(-GlobalState.Instance.cameraPreviewDirection) * Quaternion.Euler(0, 180, 0);
            backgroundFeedback.localScale = new Vector3(scale * aspect, scale, scale);

            GlobalState.Settings.cameraFeedbackPosition = backgroundFeedback.position;
            GlobalState.Settings.cameraFeedbackRotation = backgroundFeedback.rotation;
            GlobalState.Settings.cameraFeedbackScale = backgroundFeedback.localScale;
        }

        void OnCameraChanged(object sender, ActiveCameraChangedArgs args)
        {
            SetActiveCamera(args.activeCamera);
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