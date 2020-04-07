using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CameraController : ParametersController
    {
        private Camera cameraObject = null;
        public float focal = 35f;
        public float near = 0.07f;
        public float far = 1000f;

        // Start is called before the first frame update
        void Awake()
        {
            cameraObject = transform.GetComponentInChildren<Camera>();
        }

        void Update()
        {
            if (null == cameraObject)
                return;

            if (null == world)
                GetWorldTransform();

            float scale = world.localScale.x;
            cameraObject.farClipPlane = far * scale;
            cameraObject.nearClipPlane = near * scale;

            cameraObject.focalLength = focal;
        }

        public override void CopyParameters(ParametersController otherController)
        {
            base.CopyParameters(otherController);

            CameraController other = otherController as CameraController;
            focal = other.focal;
            near = other.near;
            far = other.far;            
        }
    }
}