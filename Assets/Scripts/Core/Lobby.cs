using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace VRtist
{
    public class Lobby : MonoBehaviour
    {
        GameObject palette;
        GameObject vehicleHUD;
        GameObject sceneVolume;
        GameObject lobbyVolume;

        GameObject backToSceneButton;

        UIDynamicList projectList;
        GameObject itemPrefab;

        private void Awake()
        {
            palette = transform.parent.Find("Pivot/PaletteController/PaletteHandle").gameObject;
            vehicleHUD = transform.parent.Find("Vehicle_HUD").gameObject;

            Transform volumes = Utils.FindRootGameObject("Volumes").transform;
            sceneVolume = volumes.Find("VolumePostProcess").gameObject;
            lobbyVolume = volumes.Find("VolumeLobby").gameObject;

            backToSceneButton = transform.Find("UI/Panel/BackToScene Button").gameObject;

            projectList = transform.Find("UI/Projects Panel/List").GetComponent<UIDynamicList>();
            itemPrefab = Resources.Load<GameObject>("Prefabs/UI/ProjectItem");
        }

        private void Start()
        {
            palette.SetActive(false);
            vehicleHUD.SetActive(false);

            // Orient lobby
            float camY = Camera.main.transform.localEulerAngles.y;
            transform.localEulerAngles = new Vector3(0f, camY, 0f);

            LoadProjectItems();
        }

        private void LoadProjectItems()
        {
            projectList.Clear();
            List<string> paths = Serialization.SaveManager.Instance.GetProjectThumbnailPaths();
            foreach (string path in paths)
            {
                GameObject item = Instantiate(itemPrefab);
                item.name = Path.GetFileName(path);
                ProjectItem projectItem = item.GetComponent<ProjectItem>();
                UIDynamicListItem dlItem = projectList.AddItem(item.transform);
                projectItem.SetListItem(dlItem, path);
            }
        }

        private void Update()
        {
            float camY = Camera.main.transform.localEulerAngles.y;
            float lobbyY = transform.localEulerAngles.y;
            if (Mathf.Abs(Mathf.DeltaAngle(camY, lobbyY)) > 45f)
                transform.localEulerAngles = new Vector3(0f, camY, 0f);
        }

        public void OnSetVisible()
        {
            LoadProjectItems();

            GlobalState.Instance.playerController.IsInLobby = true;
            Utils.FindWorld().SetActive(false);
            gameObject.SetActive(true);

            // Stop play if playing
            AnimationEngine.Instance.Pause();

            // Orient lobby
            float camY = Camera.main.transform.localEulerAngles.y;
            transform.localEulerAngles = new Vector3(0f, camY, 0f);

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

            backToSceneButton.SetActive(true);

            // Set lobby tool active
            ToolsManager.ChangeTool("Lobby");
        }

        public void OnBackToScene()
        {
            GlobalState.Instance.playerController.IsInLobby = false;
            Utils.FindWorld().SetActive(true);
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
            Utils.ClearScene();

            // TODO set a valid name for the new project depending on existing "newProject" names
        }
    }
}
