using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.XR;

namespace VRtist
{
    public class Settings : MonoBehaviour
    {
        [Header("Settings Parameters")]
        public AudioMixer mixer = null;
        public GameObject worldGrid;
        public Transform leftHandle;
        public Transform rightHandle;
        public Transform cursor;

        [Header("UI Widgets")]
        public UICheckbox worldGridCheckbox;
        public UILabel versionLabel;
        public UICheckbox rightHandedCheckbox;

        private void Start()
        {
            // tmp
            mixer.SetFloat("Volume_Master", -25.0f);

            if(null != worldGridCheckbox) {
                worldGridCheckbox.Checked = true;
            }

            if(null != rightHandedCheckbox) {
                rightHandedCheckbox.Checked = GlobalState.rightHanded;
            }

            if (null != versionLabel)
            {
                versionLabel.Text = $"VRtist Version: {Version.version}\nSync Version: {Version.syncVersion}";
            }
        }

        public void OnDisplayFPS(bool show) {
            GlobalState.showFps = show;
        }

        public void OnDisplayGizmos(bool show) {
            GlobalState.SetDisplayGizmos(show);
        }

        public void OnDisplayWorldGrid(bool show) {
            worldGrid.SetActive(show);
        }

        public void OnExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void InvertTooltip(Transform anchor)
        {
            for (int i = 0; i < anchor.childCount; i++)
            {
                Transform lineTooltip = anchor.GetChild(i);
                LineRenderer line = lineTooltip.GetComponent<LineRenderer>();
                Vector3 currentPosition = line.GetPosition(1);
                line.SetPosition(1, new Vector3(-currentPosition.x, currentPosition.y, currentPosition.z));

                Transform tooltip = lineTooltip.Find("Frame");
                Vector3 tooltipPosition = tooltip.localPosition;
                tooltip.localPosition = new Vector3(-tooltipPosition.x, tooltipPosition.y, tooltipPosition.z);
            }
        }

        public void OnRightHanded(bool value) 
        {
            GlobalState.rightHanded = value;
            GameObject leftController = Resources.Load("Prefabs/left_controller") as GameObject;
            GameObject rightController = Resources.Load("Prefabs/right_controller") as GameObject;

            Transform leftControllerInstance = leftHandle.Find("left_controller");
            Transform rightControllerInstance = rightHandle.Find("right_controller");

            Mesh leftControllerMesh = leftController.GetComponent<MeshFilter>().sharedMesh;
            Mesh rightControllerMesh = rightController.GetComponent<MeshFilter>().sharedMesh;

            if (value)
            {
                leftControllerInstance.GetComponent<MeshFilter>().mesh = leftControllerMesh;
                leftControllerInstance.localPosition = leftController.transform.localPosition;

                rightControllerInstance.GetComponent<MeshFilter>().mesh = rightControllerMesh;
                rightControllerInstance.localPosition = rightController.transform.localPosition;
            }
            else
            {
                leftControllerInstance.GetComponent<MeshFilter>().mesh = rightControllerMesh;
                leftControllerInstance.localPosition = rightController.transform.localPosition;

                rightControllerInstance.GetComponent<MeshFilter>().mesh = leftControllerMesh;
                rightControllerInstance.localPosition = leftController.transform.localPosition;
            }

            // switch anchors positions            
            for (int i = 0; i < leftControllerInstance.childCount; i++)
            {
                Transform leftChild = leftControllerInstance.GetChild(i);
                Transform rightChild = rightControllerInstance.GetChild(i);

                InvertTooltip(leftChild);
                InvertTooltip(rightChild);

                Vector3 tmpPos = leftChild.localPosition;
                leftChild.localPosition = rightChild.localPosition;
                rightChild.localPosition = tmpPos;
            }

            // Move Palette
            Transform palette = leftHandle.Find("PaletteHandle");
            Vector3 currentPalettePosition = palette.localPosition;
            if (GlobalState.rightHanded)
                palette.localPosition = new Vector3(-0.02f, currentPalettePosition.y, currentPalettePosition.z);
            else
                palette.localPosition = new Vector3(-0.2f, currentPalettePosition.y, currentPalettePosition.z);
        }

        public void OnChangeMasterVolume(float volume)
        {
            if (mixer)
            {
                mixer.SetFloat("Volume_Master", volume);
            }
        }

        public void OnChangeAmbientVolume(float volume)
        {
            if (mixer)
            {
                mixer.SetFloat("Volume_Ambient", volume);
            }
        }

        public void OnChangeUIVolume(float volume)
        {
            if (mixer)
            {
                mixer.SetFloat("Volume_UI", volume);
            }
        }
    }
}
