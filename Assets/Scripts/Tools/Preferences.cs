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
        public Transform leftHandle;
        public Transform rightHandle;
        public Transform cursor;
        public Transform backgroundFeedback = null;

        [Header("UI Widgets")]
        public Transform panel = null;
        public Transform paletteHandle;
        public Transform consoleHandle;
        public ConsoleWindow consoleWindow;
        public UIButton showGizmosShortcut;
        public UIButton showLocatorsShortcut;

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

        private bool previousRightHandedValue = true;

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

            previousRightHandedValue = GlobalState.Settings.rightHanded;

            Apply(onStart: true);

            if (null != versionLabel && versionLabel.Text.Length == 0)
            {
                versionLabel.Text = $"<color=#0079FF>VRtist Version</color>: {Version.version}\n<color=#0079FF>Sync Version</color>: {Version.syncVersion}";
            }

            OnSetDisplaySubPanel();
        }

        private void Apply(bool onStart = false)
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

            if (!(onStart && GlobalState.Settings.rightHanded))
                OnRightHanded(GlobalState.Settings.rightHanded);

            backgroundFeedback.localPosition = GlobalState.Settings.cameraFeedbackPosition;
            backgroundFeedback.localRotation = GlobalState.Settings.cameraFeedbackRotation;
            backgroundFeedback.localScale = GlobalState.Settings.cameraFeedbackScale;
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
        }

        protected virtual void OnDisable()
        {
            Settings.onSettingsChanged.RemoveListener(UpdateUIFromPreferences);
        }

        protected void UpdateUIFromPreferences()
        {
            showConsoleWindow.Checked = GlobalState.Settings.ConsoleVisible;
            worldGridCheckbox.Checked = GlobalState.Settings.DisplayWorldGrid;

            displayGizmos.Checked = GlobalState.Settings.DisplayGizmos;
            displayLocators.Checked = GlobalState.Settings.DisplayLocators;
            showGizmosShortcut.Checked = GlobalState.Settings.DisplayGizmos;
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
            // TODO (Right / Left) handed
            Transform tooltip = anchor.Find("Tooltip");
            if(null != tooltip)
            {/*
                FreeDraw freeDraw = new FreeDraw();
                freeDraw.AddControlPoint(Vector3.zero, 0.00025f);
                freeDraw.AddControlPoint(linePosition, 0.00025f);
                MeshFilter meshFilter = tooltip.GetComponent<MeshFilter>();
                Mesh mesh = meshFilter.mesh;
                mesh.Clear();
                mesh.vertices = freeDraw.vertices;
                mesh.normals = freeDraw.normals;
                mesh.triangles = freeDraw.triangles;
                */
            }
        }

        public void OnRightHanded(bool value)
        {
            if (previousRightHandedValue == value)
                return;

            previousRightHandedValue = value;
            GlobalState.Settings.rightHanded = value;

            GameObject leftPrefab = Resources.Load("Prefabs/left_controller") as GameObject;
            GameObject rightPrefab = Resources.Load("Prefabs/right_controller") as GameObject;

            Transform leftInstance = leftHandle.Find("left_controller");
            Transform rightInstance = rightHandle.Find("right_controller");

            // Get the prefab and instance controller meshes/parts
            MeshFilter[] leftPrefabMeshFilters = leftPrefab.GetComponentsInChildren<MeshFilter>();
            MeshFilter[] rightPrefabMeshFilters = rightPrefab.GetComponentsInChildren<MeshFilter>();
            MeshFilter[] leftInstanceMeshFilters = leftInstance.GetComponentsInChildren<MeshFilter>();
            MeshFilter[] rightInstanceMeshFilters = rightInstance.GetComponentsInChildren<MeshFilter>();

            // Copy prefab/default (right handed) transforms and meshes to the corresponding controller.
            if (value)
            {
                Canvas fpsPrefabCanvas = leftPrefab.transform.Find("Canvas").GetComponent<Canvas>();
                Canvas fpsInstanceCanvas = leftInstance.Find("Canvas").GetComponent<Canvas>();
                fpsInstanceCanvas.transform.localPosition = fpsPrefabCanvas.transform.localPosition;

                for (int i = 0; i < leftPrefabMeshFilters.Length; ++i)
                {
                    leftInstanceMeshFilters[i].mesh = leftPrefabMeshFilters[i].sharedMesh;
                }

                leftInstance.localPosition = leftPrefab.transform.localPosition;
                for (int i = 0; i < leftPrefabMeshFilters.Length; ++i)
                {
                    if (leftInstanceMeshFilters[i].gameObject.name != "left_controller")
                    {
                        // copy the translations/rotations of the pivots of mesh objects (parents)
                        leftInstanceMeshFilters[i].transform.parent.localPosition = leftPrefabMeshFilters[i].transform.parent.localPosition;
                        leftInstanceMeshFilters[i].transform.parent.localRotation = leftPrefabMeshFilters[i].transform.parent.localRotation;
                    }
                }

                for (int i = 0; i < rightPrefabMeshFilters.Length; ++i)
                {
                    rightInstanceMeshFilters[i].mesh = rightPrefabMeshFilters[i].sharedMesh;
                }

                rightInstance.localPosition = rightPrefab.transform.localPosition;
                for (int i = 0; i < rightPrefabMeshFilters.Length; ++i)
                {
                    if (rightInstanceMeshFilters[i].gameObject.name != "right_controller")
                    {
                        // copy the translations/rotations of the pivots of mesh objects (parents)
                        rightInstanceMeshFilters[i].transform.parent.localPosition = rightPrefabMeshFilters[i].transform.parent.localPosition;
                        rightInstanceMeshFilters[i].transform.parent.localRotation = rightPrefabMeshFilters[i].transform.parent.localRotation;
                    }
                }
            }
            else
            {
                Canvas fpsPrefabCanvas = leftPrefab.transform.Find("Canvas").GetComponent<Canvas>();
                Canvas fpsInstanceCanvas = leftInstance.Find("Canvas").GetComponent<Canvas>();
                fpsInstanceCanvas.transform.localPosition = Vector3.Scale(fpsPrefabCanvas.transform.localPosition, new Vector3(-1,1,1));

                for (int i = 0; i < leftPrefabMeshFilters.Length; ++i)
                {
                    leftInstanceMeshFilters[i].mesh = rightPrefabMeshFilters[i].sharedMesh;
                }

                leftInstance.localPosition = rightPrefab.transform.localPosition;
                for (int i = 0; i < leftPrefabMeshFilters.Length; ++i)
                {
                    if (leftInstanceMeshFilters[i].gameObject.name != "left_controller")
                    {
                        // copy the translations/rotations of the pivots of mesh objects (parents)
                        leftInstanceMeshFilters[i].transform.parent.localPosition = rightPrefabMeshFilters[i].transform.parent.localPosition;
                        leftInstanceMeshFilters[i].transform.parent.localRotation = rightPrefabMeshFilters[i].transform.parent.localRotation;
                    }
                }

                for (int i = 0; i < rightPrefabMeshFilters.Length; ++i)
                {
                    rightInstanceMeshFilters[i].mesh = leftPrefabMeshFilters[i].sharedMesh;
                }

                rightInstance.localPosition = leftPrefab.transform.localPosition;
                for (int i = 0; i < rightPrefabMeshFilters.Length; ++i)
                {
                    if (rightInstanceMeshFilters[i].gameObject.name != "right_controller")
                    {
                        // copy the translations/rotations of the pivots of mesh objects (parents)
                        rightInstanceMeshFilters[i].transform.parent.localPosition = leftPrefabMeshFilters[i].transform.parent.localPosition;
                        rightInstanceMeshFilters[i].transform.parent.localRotation = leftPrefabMeshFilters[i].transform.parent.localRotation;
                    }
                }
            }

            AnimateControllerButtons acmLeft = leftInstance.parent.GetComponent<AnimateControllerButtons>();
            AnimateControllerButtons acmRight = rightInstance.parent.GetComponent<AnimateControllerButtons>();

            acmLeft.OnRightHanded(value);
            acmRight.OnRightHanded(value);

            // Swap tooltips anchors            
            for (int i = 0; i < leftInstance.childCount; i++)
            {
                Transform leftChild = leftInstance.GetChild(i);
                if (leftChild.name.EndsWith("Anchor"))
                {
                    Transform rightChild = rightInstance.GetChild(i);
                    InvertTooltip(leftChild);
                    InvertTooltip(rightChild);

                    Vector3 tmpPos = leftChild.localPosition;
                    leftChild.localPosition = rightChild.localPosition;
                    rightChild.localPosition = tmpPos;
                }
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
    }
}
