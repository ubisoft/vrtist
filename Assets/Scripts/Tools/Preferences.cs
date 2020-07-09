using UnityEngine;
using UnityEngine.Audio;

namespace VRtist
{
    public class Preferences : MonoBehaviour
    {
        [Header("Settings Parameters")]
        public AudioMixer mixer = null;
        public GameObject worldGrid;
        public Transform leftHandle;
        public Transform rightHandle;
        public Transform cursor;

        [Header("UI Widgets")]
        public Transform panel = null;
        private UICheckbox worldGridCheckbox;
        private UICheckbox displayGizmos;
        private UICheckbox displayFPS;
        private UISlider masterVolume;
        private UISlider ambientVolume;
        private UISlider uiVolume;
        private UICheckbox rightHanded;
        private UICheckbox forcePaletteOpen;
        private UILabel versionLabel;

        private void Start()
        {
            // tmp
            // mixer.SetFloat("Volume_Master", -25.0f);

            worldGridCheckbox = panel.Find("DisplayWorldGrid").GetComponent<UICheckbox>();
            displayGizmos = panel.Find("DisplayGizmos").GetComponent<UICheckbox>();
            displayFPS = panel.Find("DisplayFPS").GetComponent<UICheckbox>();
            masterVolume = panel.Find("Master Volume").GetComponent<UISlider>();
            ambientVolume = panel.Find("Ambient Volume").GetComponent<UISlider>();
            uiVolume = panel.Find("UI Volume").GetComponent<UISlider>();
            rightHanded = panel.Find("RightHanded").GetComponent<UICheckbox>();
            forcePaletteOpen = panel.Find("ForcePaletteOpened").GetComponent<UICheckbox>();
            versionLabel = panel.Find("Version").GetComponent<UILabel>();

            Apply(onStart: true);

            if (null != versionLabel)
            {
                versionLabel.Text = $"VRtist Version: {Version.version}\nSync Version: {Version.syncVersion}";
            }
        }

        private void Apply(bool onStart = false)
        {
            OnDisplayGizmos(GlobalState.Settings.displayGizmos);

            bool value = GlobalState.Settings.displayWorldGrid;
            worldGridCheckbox.Checked = value;
            worldGrid.SetActive(value);

            displayGizmos.Checked = GlobalState.Settings.displayGizmos;
            displayFPS.Checked = GlobalState.Settings.displayFPS;

            masterVolume.Value = GlobalState.Settings.masterVolume;
            OnChangeMasterVolume(GlobalState.Settings.masterVolume);
            ambientVolume.Value = GlobalState.Settings.ambientVolume;
            OnChangeAmbientVolume(GlobalState.Settings.ambientVolume);
            uiVolume.Value = GlobalState.Settings.uiVolume;
            OnChangeUIVolume(GlobalState.Settings.uiVolume);

            rightHanded.Checked = GlobalState.Settings.rightHanded;
            if (!onStart || !GlobalState.Settings.rightHanded)
                OnRightHanded(GlobalState.Settings.rightHanded);

            forcePaletteOpen.Checked = GlobalState.Settings.forcePaletteOpen;
        }

        public void OnReset()
        {
            GlobalState.Settings.Reset();
            Apply();
        }

        public void OnDisplayFPS(bool show)
        {
            GlobalState.Settings.displayFPS = show;
        }

        public void OnDisplayGizmos(bool show)
        {
            GlobalState.SetDisplayGizmos(show);
        }

        public void OnDisplayWorldGrid(bool show)
        {
            worldGrid.SetActive(show);
            GlobalState.Settings.displayWorldGrid = show;
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
            GlobalState.Settings.rightHanded = value;
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
            if (GlobalState.Settings.rightHanded)
                palette.localPosition = new Vector3(-0.02f, currentPalettePosition.y, currentPalettePosition.z);
            else
                palette.localPosition = new Vector3(-0.2f, currentPalettePosition.y, currentPalettePosition.z);
        }

        public void OnChangeMasterVolume(float volume)
        {
            GlobalState.Settings.masterVolume = volume;
            if (mixer)
            {
                mixer.SetFloat("Volume_Master", volume);
            }
        }

        public void OnChangeAmbientVolume(float volume)
        {
            GlobalState.Settings.ambientVolume = volume;
            if (mixer)
            {
                mixer.SetFloat("Volume_Ambient", volume);
            }
        }

        public void OnChangeUIVolume(float volume)
        {
            GlobalState.Settings.uiVolume = volume;
            if (mixer)
            {
                mixer.SetFloat("Volume_UI", volume);
            }
        }
    }
}
