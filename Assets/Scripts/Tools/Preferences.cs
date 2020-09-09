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
        public Transform backgroundFeedback = null;

        [Header("UI Widgets")]
        public Transform panel = null;
        public Transform paletteHandle;
        public Transform consoleHandle;
        public ConsoleWindow consoleWindow;

        private UIButton displayOptionsButton;
        private UIButton soundsOptionsButton;
        private UIButton advancedOptionsButton;
        private UIButton infoOptionsButton;

        private GameObject displaySubPanel;
        private GameObject soundsSubPanel;
        private GameObject advancedSubPanel;
        private GameObject infoSubPanel;

        private UICheckbox worldGridCheckbox;
        private UICheckbox displayGizmos;
        private UICheckbox displayAvatars;
        private UICheckbox displayFPS;
        private UICheckbox display3DCurves;
        private UICheckbox showConsoleWindow;
        private UISlider masterVolume;
        private UISlider ambientVolume;
        private UISlider uiVolume;
        private UICheckbox rightHanded;
        private UICheckbox forcePaletteOpen;
        private UILabel versionLabel;

        private bool firstTimeShowConsole = true;

        private void Start()
        {
            // tmp
            // mixer.SetFloat("Volume_Master", -25.0f);

            GlobalState.Instance.onConnected.AddListener(OnConnected);

            displayOptionsButton = panel.Find("DisplayOptionsButton").GetComponent<UIButton>();
            soundsOptionsButton = panel.Find("SoundsOptionsButton").GetComponent<UIButton>();
            advancedOptionsButton = panel.Find("AdvancedOptionsButton").GetComponent<UIButton>();
            infoOptionsButton = panel.Find("InfoOptionsButton").GetComponent<UIButton>();

            displaySubPanel = panel.Find("DisplayOptions").gameObject;
            soundsSubPanel = panel.Find("SoundsOptions").gameObject;
            advancedSubPanel = panel.Find("AdvancedOptions").gameObject;
            infoSubPanel = panel.Find("InfoOptions").gameObject;

            worldGridCheckbox = displaySubPanel.transform.Find("DisplayWorldGrid").GetComponent<UICheckbox>();
            displayGizmos = displaySubPanel.transform.Find("DisplayGizmos").GetComponent<UICheckbox>();
            displayAvatars = displaySubPanel.transform.Find("DisplayAvatars").GetComponent<UICheckbox>();
            display3DCurves = displaySubPanel.transform.Find("Display3DCurves").GetComponent<UICheckbox>();
            masterVolume = soundsSubPanel.transform.Find("Master Volume").GetComponent<UISlider>();
            ambientVolume = soundsSubPanel.transform.Find("Ambient Volume").GetComponent<UISlider>();
            uiVolume = soundsSubPanel.transform.Find("UI Volume").GetComponent<UISlider>();
            rightHanded = advancedSubPanel.transform.Find("RightHanded").GetComponent<UICheckbox>();
            forcePaletteOpen = advancedSubPanel.transform.Find("ForcePaletteOpened").GetComponent<UICheckbox>();
            displayFPS = advancedSubPanel.transform.Find("DisplayFPS").GetComponent<UICheckbox>();
            showConsoleWindow = advancedSubPanel.transform.Find("ShowConsoleWindow").GetComponent<UICheckbox>();
            versionLabel = infoSubPanel.transform.Find("Version").GetComponent<UILabel>();

            Apply(onStart: true);

            if (null != versionLabel && versionLabel.Text.Length == 0)
            {
                versionLabel.Text = $"<color=#0079FF>VRtist Version</color>: {Version.version}\n<color=#0079FF>Sync Version</color>: {Version.syncVersion}";
            }

            OnSetDisplaySubPanel();
        }

        private void Apply(bool onStart = false)
        {
            OnDisplayGizmos(GlobalState.Settings.displayGizmos);
            OnDisplayAvatars(GlobalState.Settings.displayAvatars);
            OnShowConsoleWindow(GlobalState.Settings.consoleVisible);

            bool value = GlobalState.Settings.displayWorldGrid;
            worldGridCheckbox.Checked = value;
            worldGrid.SetActive(value);

            displayGizmos.Checked = GlobalState.Settings.displayGizmos;
            displayFPS.Checked = GlobalState.Settings.displayFPS;
            display3DCurves.Checked = GlobalState.Settings.display3DCurves;
            displayAvatars.Checked = GlobalState.Settings.displayAvatars;

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
            showConsoleWindow.Checked = GlobalState.Settings.consoleVisible;

            backgroundFeedback.localPosition = GlobalState.Settings.cameraFeedbackPosition;
            backgroundFeedback.localRotation = GlobalState.Settings.cameraFeedbackRotation;
            backgroundFeedback.localScale = GlobalState.Settings.cameraFeedbackScale;
            backgroundFeedback.gameObject.SetActive(GlobalState.Settings.cameraFeedbackVisible);
        }

        private void OnConnected()
        {
            versionLabel.Text = $"<color=#0079FF>VRtist Version</color>: {Version.version}\n" +
                $"<color=#0079FF>Sync Version</color>: {Version.syncVersion}\n\n" +
                $"<color=#0079FF>Client ID</color>: {GlobalState.networkUser.id}\n" +
                $"<color=#0079FF>Master ID</color>: {GlobalState.networkUser.masterId}";
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

        public void OnDisplay3DCurves(bool show)
        {
            GlobalState.Settings.display3DCurves = show;
        }

        public void OnDisplayGizmos(bool show)
        {
            GlobalState.SetDisplayGizmos(show);
        }

        public void OnDisplayAvatars(bool show)
        {
            GlobalState.SetDisplayAvatars(show);
        }

        public void OnDisplayWorldGrid(bool show)
        {
            worldGrid.SetActive(show);
            GlobalState.Settings.displayWorldGrid = show;
        }

        private void ResetSubPanels()
        {
            displayOptionsButton.Checked = false;
            displaySubPanel.SetActive(false);
            soundsOptionsButton.Checked = false;
            soundsSubPanel.SetActive(false);
            advancedOptionsButton.Checked = false;
            advancedSubPanel.SetActive(false);
            infoOptionsButton.Checked = false;
            infoSubPanel.SetActive(false);
        }

        public void OnSetDisplaySubPanel()
        {
            ResetSubPanels();
            displayOptionsButton.Checked = true;
            displaySubPanel.SetActive(true);
        }

        public void OnSetSoundsSubPanel()
        {
            ResetSubPanels();
            soundsOptionsButton.Checked = true;
            soundsSubPanel.SetActive(true);
        }

        public void OnSetAdvancedSubPanel()
        {
            ResetSubPanels();
            advancedOptionsButton.Checked = true;
            advancedSubPanel.SetActive(true);
        }

        public void OnSetInfoSubPanel()
        {
            ResetSubPanels();
            infoOptionsButton.Checked = true;
            infoSubPanel.SetActive(true);
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

        public void OnShowConsoleWindow(bool value)
        {
            GlobalState.Settings.consoleVisible = value;
            if (consoleWindow != null && consoleHandle != null)
            {
                if (value)
                {
                    if (firstTimeShowConsole && consoleHandle.position == Vector3.zero)
                    {
                        Vector3 offset = new Vector3(0.5f, 0.5f, 0.0f);
                        consoleHandle.position = paletteHandle.TransformPoint(offset);
                        consoleHandle.rotation = paletteHandle.rotation;
                        firstTimeShowConsole = false;
                    }
                    ToolsUIManager.Instance.OpenWindow(consoleHandle, 0.7f);
                }
                else
                {
                    ToolsUIManager.Instance.CloseWindow(consoleHandle, 0.7f);
                }
            }
        }

        public void OnCloseConsoleWindow()
        {
            OnShowConsoleWindow(false);
            showConsoleWindow.Checked = false;
        }
    }
}
