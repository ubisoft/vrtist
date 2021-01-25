using System.IO;

using UnityEngine;
using UnityEngine.Audio;

namespace VRtist
{
    public class Preferences : MonoBehaviour
    {
        [Header("Settings Parameters")]
        public AudioMixer mixer = null;
        public GameObject worldGrid;
        public Transform cursor;
        public Transform backgroundFeedback = null;

        [Header("UI Widgets")]
        public Transform panel = null;
        public Transform paletteHandle;
        public Transform consoleHandle;
        public ConsoleWindow consoleWindow;
        public UIButton showGizmosShortcut;
        public UIButton showLocatorsShortcut;
        public UIButton playShortcut;

        private UIButton displayOptionsButton;
        private UIButton soundsOptionsButton;
        private UIButton advancedOptionsButton;
        private UIButton saveOptionsButton;
        private UIButton infoOptionsButton;

        private GameObject displaySubPanel;
        private GameObject soundsSubPanel;
        private GameObject advancedSubPanel;
        private GameObject saveSubPanel;
        private GameObject infoSubPanel;

        private UICheckbox worldGridCheckbox;
        private UICheckbox displayGizmos;
        private UICheckbox displayLocators;
        private UICheckbox displayAvatars;
        private UICheckbox displayFPS;
        private UICheckbox display3DCurves;
        private UICheckbox showConsoleWindow;
        private UISlider masterVolume;
        private UISlider ambientVolume;
        private UISlider uiVolume;
        private UILabel assetBankDirectory;
        private UICheckbox rightHanded;
        private UICheckbox forcePaletteOpen;
        private UILabel versionLabel;

        private void Start()
        {
            // tmp
            // mixer.SetFloat("Volume_Master", -25.0f);

            GlobalState.Instance.onConnected.AddListener(OnConnected);

            displayOptionsButton = panel.Find("DisplayOptionsButton").GetComponent<UIButton>();
            soundsOptionsButton = panel.Find("SoundsOptionsButton").GetComponent<UIButton>();
            advancedOptionsButton = panel.Find("AdvancedOptionsButton").GetComponent<UIButton>();
            saveOptionsButton = panel.Find("SaveOptionsButton").GetComponent<UIButton>();
            infoOptionsButton = panel.Find("InfoOptionsButton").GetComponent<UIButton>();

            displaySubPanel = panel.Find("DisplayOptions").gameObject;
            soundsSubPanel = panel.Find("SoundsOptions").gameObject;
            advancedSubPanel = panel.Find("AdvancedOptions").gameObject;
            saveSubPanel = panel.Find("SaveOptions").gameObject;
            infoSubPanel = panel.Find("InfoOptions").gameObject;

            worldGridCheckbox = displaySubPanel.transform.Find("DisplayWorldGrid").GetComponent<UICheckbox>();
            displayGizmos = displaySubPanel.transform.Find("DisplayGizmos").GetComponent<UICheckbox>();
            displayLocators = displaySubPanel.transform.Find("DisplayLocators").GetComponent<UICheckbox>();
            displayAvatars = displaySubPanel.transform.Find("DisplayAvatars").GetComponent<UICheckbox>();
            display3DCurves = displaySubPanel.transform.Find("Display3DCurves").GetComponent<UICheckbox>();
            masterVolume = soundsSubPanel.transform.Find("Master Volume").GetComponent<UISlider>();
            ambientVolume = soundsSubPanel.transform.Find("Ambient Volume").GetComponent<UISlider>();
            uiVolume = soundsSubPanel.transform.Find("UI Volume").GetComponent<UISlider>();
            assetBankDirectory = advancedSubPanel.transform.Find("AssetBankDirectory").GetComponent<UILabel>();
            rightHanded = advancedSubPanel.transform.Find("RightHanded").GetComponent<UICheckbox>();
            forcePaletteOpen = advancedSubPanel.transform.Find("ForcePaletteOpened").GetComponent<UICheckbox>();
            displayFPS = advancedSubPanel.transform.Find("DisplayFPS").GetComponent<UICheckbox>();
            showConsoleWindow = advancedSubPanel.transform.Find("ShowConsoleWindow").GetComponent<UICheckbox>();
            versionLabel = infoSubPanel.transform.Find("Version").GetComponent<UILabel>();

            Apply();

            if (null != versionLabel && versionLabel.Text.Length == 0)
            {
                versionLabel.Text = $"<color=#0079FF>VRtist Version</color>: {Version.VersionString}\n<color=#0079FF>Sync Version</color>: {Version.syncVersion}";
            }

            OnSetDisplaySubPanel();
        }

        private void Apply()
        {
            OnDisplayGizmos(GlobalState.Settings.DisplayGizmos);
            OnDisplayLocators(GlobalState.Settings.DisplayLocators);
            OnDisplayAvatars(GlobalState.Settings.DisplayAvatars);
            OnShowConsoleWindow(GlobalState.Settings.ConsoleVisible);

            UpdateUIFromPreferences();
            worldGrid.SetActive(GlobalState.Settings.DisplayWorldGrid);
            OnChangeMasterVolume(GlobalState.Settings.masterVolume);
            OnChangeAmbientVolume(GlobalState.Settings.ambientVolume);
            OnChangeUIVolume(GlobalState.Settings.uiVolume);

            SetAssetBankDirectory(GlobalState.Settings.assetBankDirectory);

            OnRightHanded(GlobalState.Settings.rightHanded);
            backgroundFeedback.gameObject.SetActive(GlobalState.Settings.cameraFeedbackVisible);

            ToolsUIManager.Instance.InitPaletteState();
            ToolsUIManager.Instance.InitDopesheetState();
            ToolsUIManager.Instance.InitShotManagerState();
            ToolsUIManager.Instance.InitCameraPreviewState();
            ToolsUIManager.Instance.InitConsoleState();
        }

        protected virtual void OnEnable()
        {
            Settings.onSettingsChanged.AddListener(UpdateUIFromPreferences);
            if (null != GlobalState.Animation)
                GlobalState.Animation.onAnimationStateEvent.AddListener(OnAnimationStateChanged);
        }

        protected virtual void OnDisable()
        {
            Settings.onSettingsChanged.RemoveListener(UpdateUIFromPreferences);
            if (null != GlobalState.Animation)
                GlobalState.Animation.onAnimationStateEvent.RemoveListener(OnAnimationStateChanged);
        }

        private void OnAnimationStateChanged(AnimationState state)
        {
            playShortcut.Checked = state == AnimationState.Playing || state == AnimationState.Recording;
        }

        protected void UpdateUIFromPreferences()
        {
            showConsoleWindow.Checked = GlobalState.Settings.ConsoleVisible;
            worldGridCheckbox.Checked = GlobalState.Settings.DisplayWorldGrid;

            displayGizmos.Checked = GlobalState.Settings.DisplayGizmos;
            showGizmosShortcut.Checked = GlobalState.Settings.DisplayGizmos;
            displayLocators.Checked = GlobalState.Settings.DisplayLocators;
            showLocatorsShortcut.Checked = GlobalState.Settings.DisplayLocators;
            displayFPS.Checked = GlobalState.Settings.DisplayFPS;
            display3DCurves.Checked = GlobalState.Settings.Display3DCurves;
            displayAvatars.Checked = GlobalState.Settings.DisplayAvatars;

            masterVolume.Value = GlobalState.Settings.masterVolume;
            ambientVolume.Value = GlobalState.Settings.ambientVolume;
            uiVolume.Value = GlobalState.Settings.uiVolume;

            rightHanded.Checked = GlobalState.Settings.rightHanded;
            forcePaletteOpen.Checked = GlobalState.Settings.forcePaletteOpen;
            showConsoleWindow.Checked = GlobalState.Settings.ConsoleVisible;
        }

        private void OnConnected()
        {
            versionLabel.Text = $"<color=#0079FF>VRtist Version</color>: {Version.VersionString}\n" +
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
            GlobalState.Settings.DisplayFPS = show;
        }

        public void OnDisplay3DCurves(bool show)
        {
            GlobalState.Settings.Display3DCurves = show;
        }

        public void OnDisplayGizmos(bool show)
        {
            GlobalState.SetDisplayGizmos(show);
        }
        public void OnDisplayLocators(bool show)
        {
            GlobalState.SetDisplayLocators(show);
        }

        public void OnDisplayAvatars(bool show)
        {
            GlobalState.SetDisplayAvatars(show);
        }

        public void OnDisplayWorldGrid(bool show)
        {
            worldGrid.SetActive(show);
            GlobalState.Settings.DisplayWorldGrid = show;
        }

        public void OnEditAssetBankDirectory()
        {
            ToolsUIManager.Instance.OpenKeyboard(SetAssetBankDirectory, assetBankDirectory.transform, assetBankDirectory.Text);
        }

        private void SetAssetBankDirectory(string value)
        {
            assetBankDirectory.Text = value;
            assetBankDirectory.Image = UIUtils.LoadIcon(Directory.Exists(value) ? "validate-icon" : "error");
            GlobalState.Settings.assetBankDirectory = value;
        }

        public void OnEditProjectName()
        {
            //ToolsUIManager.Instance.OpenKeyboard(SetProjectName, )
        }

        private void ResetSubPanels()
        {
            displayOptionsButton.Checked = false;
            displaySubPanel.SetActive(false);
            soundsOptionsButton.Checked = false;
            soundsSubPanel.SetActive(false);
            advancedOptionsButton.Checked = false;
            advancedSubPanel.SetActive(false);
            saveOptionsButton.Checked = false;
            saveSubPanel.SetActive(false);
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

        public void OnSetSaveSubPanel()
        {
            ResetSubPanels();
            saveOptionsButton.Checked = true;
            saveSubPanel.SetActive(true);
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

        public void OnRightHanded(bool value)
        {
            GlobalState.SetRightHanded(value);
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
            if (consoleWindow != null && consoleHandle != null)
            {
                if (value)
                {
                    ToolsUIManager.Instance.OpenWindow(consoleHandle, 0.7f);
                }
                else
                {
                    ToolsUIManager.Instance.CloseWindow(consoleHandle, 0.7f);
                }
            }
            GlobalState.Settings.ConsoleVisible = value;
        }

        public void OnCloseConsoleWindow()
        {
            OnShowConsoleWindow(false);
            showConsoleWindow.Checked = false;
        }

        public void OnSaveProject()
        {
            GlobalState.Instance.messageBox.ShowMessage("Saving scene, please wait...");
            Serialization.SaveManager.Instance.Save("Plop");
            GlobalState.Instance.messageBox.SetVisible(false);
        }

        public void OnLoadProject()
        {
            GlobalState.Instance.messageBox.ShowMessage("Loading scene, please wait...");
            Serialization.SaveManager.Instance.Load("Plop");
            GlobalState.Instance.messageBox.SetVisible(false);
        }
    }
}
