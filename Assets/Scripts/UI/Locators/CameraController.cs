using TMPro;
using UnityEngine;

namespace VRtist
{
    public class CameraController : ParametersController
    {
        public Camera cameraObject = null;
        public float focal = 35f;
        public float near = 0.07f;
        public float far = 1000f;

        private LineRenderer frustumRenderer = null;

        private void Awake()
        {
            Init();
        }

        protected override void Start()
        {
            base.Start();
            Init();
        }

        private void Init()
        {
            if (null == cameraObject)
            {
                cameraObject = gameObject.GetComponentInChildren<Camera>(true);
            }
            if (null == frustumRenderer)
            {
                GameObject frustum = transform.Find("Frustum").gameObject;
                frustumRenderer = frustum.GetComponent<LineRenderer>();
                frustumRenderer.enabled = false;
            }
            GlobalState.ObjectRenamedEvent.AddListener(OnCameraRenamed);
        }

        void Update()
        {
            if (null == cameraObject)
                cameraObject = gameObject.GetComponentInChildren<Camera>(true);
            if (null != cameraObject)
            {
                float scale = GlobalState.worldScale;
                cameraObject.farClipPlane = far * scale;
                cameraObject.nearClipPlane = near * scale;
                cameraObject.focalLength = focal;

                // Only draw frustum for selected camera
                if (CameraTool.showCameraFrustum && (gameObject.layer == LayerMask.NameToLayer("Selection") || gameObject.layer == LayerMask.NameToLayer("Hover")))
                {
                    DrawFrustum();
                }
                else
                {
                    frustumRenderer.enabled = false;
                }
            }
        }

        public void OnCameraRenamed(GameObject gObject)
        {
            if (gObject == gameObject)
                SetName(gObject.name);
        }

        public override void CopyParameters(ParametersController otherController)
        {
            base.CopyParameters(otherController);

            CameraController other = otherController as CameraController;
            focal = other.focal;
            near = other.near;
            far = other.far;
        }

        public override void SetName(string name)
        {
            base.SetName(name);
            TextMeshProUGUI text = gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
            text.text = name;
        }

        private void DrawFrustum()
        {
            frustumRenderer.enabled = true;
            frustumRenderer.gameObject.layer = LayerMask.NameToLayer("UI");  // we don't want the selection outline

            float halfWidthFactor = cameraObject.sensorSize.x * 0.5f / focal;
            float halfHeightFactor = cameraObject.sensorSize.y * 0.5f / focal;

            float nearHalfWidth = halfWidthFactor * near;
            float nearHalfHeight = halfHeightFactor * near;
            float farHalfWidth = halfWidthFactor * far;
            float farHalfHeight = halfHeightFactor * far;

            Vector3[] points = new Vector3[16];
            points[0] = new Vector3(nearHalfWidth, -nearHalfHeight, near);
            points[1] = new Vector3(nearHalfWidth, nearHalfHeight, near);
            points[2] = new Vector3(-nearHalfWidth, nearHalfHeight, near);
            points[3] = new Vector3(-nearHalfWidth, -nearHalfHeight, near);
            points[4] = new Vector3(nearHalfWidth, -nearHalfHeight, near);

            points[5] = new Vector3(farHalfWidth, -farHalfHeight, far);
            points[6] = new Vector3(farHalfWidth, farHalfHeight, far);
            points[7] = new Vector3(-farHalfWidth, farHalfHeight, far);
            points[8] = new Vector3(-farHalfWidth, -farHalfHeight, far);
            points[9] = new Vector3(farHalfWidth, -farHalfHeight, far);

            points[10] = new Vector3(farHalfWidth, farHalfHeight, far);
            points[11] = new Vector3(nearHalfWidth, nearHalfHeight, near);
            points[12] = new Vector3(-nearHalfWidth, nearHalfHeight, near);
            points[13] = new Vector3(-farHalfWidth, farHalfHeight, far);
            points[14] = new Vector3(-farHalfWidth, -farHalfHeight, far);
            points[15] = new Vector3(-nearHalfWidth, -nearHalfHeight, near);

            // Remove camera object scale
            float invScale = 1f / frustumRenderer.transform.parent.lossyScale.x;
            invScale *= GlobalState.worldScale;
            frustumRenderer.transform.localScale = new Vector3(invScale, invScale, invScale);

            frustumRenderer.positionCount = points.Length;
            frustumRenderer.SetPositions(points);
        }
    }
}
