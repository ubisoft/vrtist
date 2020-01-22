using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CameraController : ParametersController
    {
        public CameraParameters parameters = new CameraParameters();
        public override Parameters GetParameters() { return parameters; }

        private Transform world;
        private Camera cameraObject = null;

        bool firstTime = true;

        // Start is called before the first frame update
        void Awake()
        {
            world = transform.parent;
            while (null != world.parent)
            {
                world = world.parent;
            }

            cameraObject = transform.GetComponentInChildren<Camera>();
        }

        void Update()
        {
            if (null == cameraObject)
                return;

            float scale = world.localScale.x;
            cameraObject.farClipPlane = parameters.far * scale;
            cameraObject.nearClipPlane = parameters.near * scale;

            cameraObject.focalLength = parameters.focal;

            /*
            bool isSelected = Selection.IsSelected(gameObject);
            cameraObject.enabled = isSelected || firstTime;
            firstTime = false;
            */
        }
    }
}