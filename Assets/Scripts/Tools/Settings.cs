using System.Collections;
using System.Collections.Generic;
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

        [Header("UI Widgets")]
        public UICheckbox worldGridCheckbox;
        public UILabel versionLabel;

        private void Start()
        {
            // tmp
            mixer.SetFloat("Volume_Master", -25.0f);

            if(null != worldGridCheckbox) {
                worldGridCheckbox.Checked = true;
            }

            if(null != versionLabel)
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
