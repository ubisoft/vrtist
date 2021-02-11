using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace VRtist
{
    public class Lobby : MonoBehaviour
    {
        GameObject world;
        GameObject palette;
        GameObject vehicleHUD;
        GameObject sceneVolume;
        GameObject lobbyVolume;
        Transform cameraRig;

        UIButton backToSceneButton;
        GameObject projectButtons;
        UIButton launchProjectButton;

        UIDynamicList projectList;
        GameObject itemPrefab;

        readonly List<GameObject> projects = new List<GameObject>();
        GameObject currentProject;

        // View parameters in scene
        Vector3 viewPosition = Vector3.zero;
        Quaternion viewRotation = Quaternion.identity;
        float viewScale = 1f;

        private void Awake()
        {
            world = Utils.FindWorld();
            palette = transform.parent.Find("Pivot/PaletteController/PaletteHandle").gameObject;
            vehicleHUD = transform.parent.Find("Vehicle_HUD").gameObject;
            cameraRig = transform.parent;

            Transform volumes = Utils.FindRootGameObject("Volumes").transform;
            sceneVolume = volumes.Find("VolumePostProcess").gameObject;
            lobbyVolume = volumes.Find("VolumeLobby").gameObject;

            backToSceneButton = transform.Find("UI/Control Panel/Panel/BackToSceneButton").GetComponent<UIButton>();
            backToSceneButton.Disabled = true;

            projectButtons = transform.Find("UI/Control Panel/Panel/Project").gameObject;
            projectButtons.SetActive(false);
            launchProjectButton = projectButtons.transform.Find("LaunchProjectButton").GetComponent<UIButton>();

            projectList = transform.Find("UI/Projects Panel/List").GetComponent<UIDynamicList>();
            itemPrefab = Resources.Load<GameObject>("Prefabs/UI/ProjectItem");
        }

        private void Start()
        {
            // Read command line arguments to know if we start in the lobby or directly into a scene
            string[] args = System.Environment.GetCommandLineArgs();
            string projectName = null;
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] == "--startScene")
                {
                    try
                    {
                        projectName = Serialization.SaveManager.Instance.NormalizeProjectName(args[i + 1]);
                    }
                    catch (Exception)
                    {
                        projectName = Serialization.SaveManager.Instance.GetNextValidProjectName();
                        Debug.LogWarning("Expected a project name. Using " + projectName);
                    }
                    if (projectName.Length == 0)
                    {
                        projectName = Serialization.SaveManager.Instance.GetNextValidProjectName();
                    }
                }
            }

            // Load the lobby
            if (projectName is null)
            {
                OnSetVisible(start: true);

                //// DEBUG Auto-load
                //OnBackToScene();
                //Serialization.SaveManager.Instance.Load("london");
                //// END DEBUG
            }

            // Start the scene
            else
            {
                GlobalState.Settings.ProjectName = projectName;
                OnBackToScene();
            }

            projectList.ItemClickedEvent += OnProjectClicked;
        }

        private void HighlightSelectedProject()
        {
            if (currentProject == null || projects.Count == 0) { return; }

            foreach (GameObject project in projects)
            {
                project.transform.Find("Frame").gameObject.SetActive(true);
                project.transform.Find("SelectedFrame").gameObject.SetActive(false);
            }
            currentProject.transform.Find("Frame").gameObject.SetActive(false);
            currentProject.transform.Find("SelectedFrame").gameObject.SetActive(true);
        }

        private void OnProjectClicked(object sender, IndexedGameObjectArgs args)
        {
            if (currentProject != args.gobject)
            {
                currentProject = args.gobject;
                HighlightSelectedProject();
                launchProjectButton.Disabled = false;
                projectButtons.SetActive(true);
            }
        }

        private void LoadProjectItems()
        {
            projects.Clear();
            projectList.Clear();
            List<string> paths = Serialization.SaveManager.Instance.GetProjectThumbnailPaths();
            foreach (string path in paths)
            {
                GameObject item = Instantiate(itemPrefab);
                item.name = Directory.GetParent(path).Name;
                ProjectItem projectItem = item.GetComponent<ProjectItem>();
                UIDynamicListItem dlItem = projectList.AddItem(item.transform);
                projectItem.SetListItem(dlItem, path);
                projects.Add(item);
            }
        }

        private void Update()
        {
            float camY = Camera.main.transform.localEulerAngles.y;
            float lobbyY = transform.localEulerAngles.y;
            if (Mathf.Abs(Mathf.DeltaAngle(camY, lobbyY)) > 45f)
                transform.localEulerAngles = new Vector3(0f, camY, 0f);
        }

        void StoreViewParameters()
        {
            viewPosition = cameraRig.localPosition;
            viewRotation = cameraRig.localRotation;
            viewScale = 1f / GlobalState.WorldScale;
        }

        void RestoreViewParameters()
        {
            cameraRig.localPosition = viewPosition;
            cameraRig.localRotation = viewRotation;
            GlobalState.WorldScale = 1f / viewScale;
            cameraRig.localScale = Vector3.one * viewScale;
            Camera.main.nearClipPlane = 0.1f * viewScale;
            Camera.main.farClipPlane = 1000f * viewScale;
        }

        void ResetVRCamera()
        {
            StoreViewParameters();

            cameraRig.localPosition = Vector3.zero;
            cameraRig.localRotation = Quaternion.identity;
            cameraRig.localScale = Vector3.one;
            Camera.main.nearClipPlane = 0.1f;
            Camera.main.farClipPlane = 1000f;
            GlobalState.WorldScale = 1f;
        }

        public void OnSetVisible(bool start = false)
        {
            // Stop play if playing
            AnimationEngine.Instance.Pause();

            ResetVRCamera();

            GlobalState.Instance.playerController.IsInLobby = true;
            world.SetActive(false);
            gameObject.SetActive(true);

            // Change volume
            sceneVolume.SetActive(false);
            lobbyVolume.SetActive(true);

            // Hide & disable palette
            palette.SetActive(false);

            // Hide all windows (vehicle_hud)
            vehicleHUD.SetActive(false);

            // Deactive current tool
            ToolsManager.ActivateCurrentTool(false);

            // Deactivate selection helper
            GlobalState.Instance.toolsController.Find("SelectionHelper").gameObject.SetActive(false);

            // Orient lobby
            float camY = Camera.main.transform.localEulerAngles.y;
            transform.localEulerAngles = new Vector3(0f, camY, 0f);

            LoadProjectItems();
            if (!start)
            {
                HighlightSelectedProject();
                launchProjectButton.Disabled = true;
            }
            else
            {
                currentProject = null;
            }

            backToSceneButton.Disabled = start;

            // Set lobby tool active
            ToolsManager.ChangeTool("Lobby");
        }

        public void OnBackToScene()
        {
            RestoreViewParameters();

            GlobalState.Instance.playerController.IsInLobby = false;
            world.SetActive(true);
            gameObject.SetActive(false);

            // Change volume
            sceneVolume.SetActive(true);
            lobbyVolume.SetActive(false);

            // Activate palette
            palette.SetActive(true);

            // Show windows (vehicle_hud)
            vehicleHUD.SetActive(true);

            // Active tool
            ToolsManager.ActivateCurrentTool(true);

            // Activate selection helper
            GlobalState.Instance.toolsController.Find("SelectionHelper").gameObject.SetActive(true);

            // Set selector tool active
            ToolsUIManager.Instance.ChangeTab("Selector");
            ToolsManager.ChangeTool("Selector");
        }

        public void OnCreateNewProject()
        {
            OnBackToScene();
            GlobalState.ClearScene();
            GlobalState.Settings.ProjectName = Serialization.SaveManager.Instance.GetNextValidProjectName();
        }

        public void OnLaunchProject()
        {
            // Clear undo/redo stack
            CommandManager.Clear();

            OnBackToScene();
            Serialization.SaveManager.Instance.Load(currentProject.name);
        }

        public void OnCloneProject()
        {
            string newName = $"{currentProject.name}_copy";
            // Copy files
            Serialization.SaveManager.Instance.Duplicate(currentProject.name, newName);
            LoadProjectItems();
            HighlightSelectedProject();
        }

        public void OnDeleteProject()
        {
            // TODO add a confirmation dialog
            Serialization.SaveManager.Instance.Delete(currentProject.name);
            currentProject = null;
            projectButtons.SetActive(false);
            LoadProjectItems();
        }

        public void OnNextPage()
        {
            projectList.OnNextPage();
        }

        public void OnPreviousPage()
        {
            projectList.OnPreviousPage();
        }

        public void OnFirstPage()
        {
            projectList.OnFirstPage();
        }

        public void OnLastPage()
        {
            projectList.OnLastPage();
        }

        public void OnExitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
