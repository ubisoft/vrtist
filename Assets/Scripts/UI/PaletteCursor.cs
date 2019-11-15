using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class PaletteCursor : MonoBehaviour
    {
        public GameObject tools = null;

        void Start()
        {
            ShowCursor(false);
        }

        void Update()
        {
            if (VRInput.TryGetDevices())
            {
                // Device rotation
                Vector3 position;
                Quaternion rotation;
                VRInput.GetControllerTransform(VRInput.rightController, out position, out rotation);

                transform.localPosition = position;
                transform.localRotation = rotation;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name == "Palette")
            {
                // deactivate all tools
                tools.SetActive(false);

                ShowCursor(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name == "Palette")
            {
                // reactivate all tools
                tools.SetActive(true);
                ShowCursor(false);
            }
        }

        private void ShowCursor(bool doShow)
        {
            // NOTE: will not work if we add more objects under the cursor root.
            GetComponentInChildren<MeshRenderer>().enabled = doShow;
        }
    }
}