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

        private void Start()
        {
            // tmp
            mixer.SetFloat("Volume_Master", -25.0f);
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
