/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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
        UIButton firstPageButton;
        UIButton lastPageButton;
        UIButton previousPageButton;
        UIButton nextPageButton;
        GameObject itemPrefab;

        readonly List<GameObject> projects = new List<GameObject>();
        GameObject currentProject;

        // View parameters in scene
        Vector3 viewPosition = Vector3.zero;
        Quaternion viewRotation = Quaternion.identity;
        float viewScale = 1f;

        bool needToSetCameraRef = true;

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
            firstPageButton = transform.Find("UI/Control Panel/List Panel/FirstPageButton").GetComponent<UIButton>();
            lastPageButton = transform.Find("UI/Control Panel/List Panel/LastPageButton").GetComponent<UIButton>();
            previousPageButton = transform.Find("UI/Control Panel/List Panel/PreviousPageButton").GetComponent<UIButton>();
            nextPageButton = transform.Find("UI/Control Panel/List Panel/NextPageButton").GetComponent<UIButton>();
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

            UpdateButtons();
        }

        private void Update()
        {
            // ROTATE 45
            float camY = Camera.main.transform.localEulerAngles.y;
            float lobbyY = transform.localEulerAngles.y;
            bool headHasRotated = Mathf.Abs(Mathf.DeltaAngle(camY, lobbyY)) > 45f;
            if (headHasRotated)
            {
                transform.localEulerAngles = new Vector3(0f, camY, 0f);
                needToSetCameraRef = true;
            }

            // ROTATE while HOVER
            foreach (var pi in projectList.GetItems())
            {
                ProjectItem projectItem = pi.Content.GetComponent<ProjectItem>();
                if (pi.Hovered)
                {
                    projectItem.Rotate();
                }
                else
                {
                    projectItem.ResetRotation(camY);
                }
            }

            // SET Camera Ref position
            if (needToSetCameraRef)
            {
                Vector3 currentCamPos = Camera.main.transform.position;
                if (currentCamPos.sqrMagnitude > 1e-5)
                {
                    foreach (var pi in projectList.GetItems())
                    {
                        ProjectItem projectItem = pi.Content.GetComponent<ProjectItem>();
                        projectItem.SetCameraRef(Camera.main.transform.position);
                    }
                    needToSetCameraRef = false; // only reset it if cam not null.
                }
            }
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

            needToSetCameraRef = true;
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
            SceneManager.ClearScene();
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
            UpdateButtons();
        }

        public void OnPreviousPage()
        {
            projectList.OnPreviousPage();
            UpdateButtons();
        }

        public void OnFirstPage()
        {
            projectList.OnFirstPage();
            UpdateButtons();
        }

        public void OnLastPage()
        {
            projectList.OnLastPage();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            firstPageButton.Disabled = projectList.currentPage == 0;
            previousPageButton.Disabled = projectList.currentPage == 0;
            lastPageButton.Disabled = projectList.currentPage == projectList.pagesCount;
            nextPageButton.Disabled = projectList.currentPage == projectList.pagesCount;
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
