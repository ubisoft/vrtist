using UnityEngine;

namespace VRtist
{
    public class Lobby : MonoBehaviour
    {
        GameObject palette;
        GameObject vehicleHUD;
        GameObject sceneVolume;
        GameObject lobbyVolume;

        private void Awake()
        {
            palette = transform.parent.Find("Pivot/PaletteController").gameObject;
            vehicleHUD = transform.parent.Find("Vehicle_HUD").gameObject;

            Transform volumes = Utils.FindRootGameObject("Volumes").transform;
            sceneVolume = volumes.Find("VolumePostProcess").gameObject;
            lobbyVolume = volumes.Find("VolumeLobby").gameObject;
        }

        public void OnSetVisible()
        {
            Utils.FindWorld().SetActive(false);
            gameObject.SetActive(true);

            // Stop play if playing

            // Orient lobby

            // Change volume
            sceneVolume.SetActive(false);
            lobbyVolume.SetActive(true);

            // Hide & disable palette
            palette.SetActive(false);

            // Hide all windows (vehicle_hud)
            vehicleHUD.SetActive(false);

            // Active no-op tool

            // Activate no-op palette

            // Deactivate selection helper
        }

        public void OnBackToScene()
        {
            Utils.FindWorld().SetActive(true);
            gameObject.SetActive(false);

            // Change volume
            sceneVolume.SetActive(true);
            lobbyVolume.SetActive(false);

            // Activate palette
            palette.SetActive(true);

            // Show windows (vehicle_hud)
            vehicleHUD.SetActive(true);

            // Active previous tool

            // Activate palette

            // Activate selection helper
        }
    }
}
