using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour
    {
        public enum Sounds
        {
            Spawn,
            Despawn
        }

        private AudioSource audioSource;
        private Dictionary<Sounds, AudioClip> clips = new Dictionary<Sounds, AudioClip>();

        public static SoundManager Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            audioSource = GetComponent<AudioSource>();

            clips[Sounds.Spawn] = Resources.Load<AudioClip>("Sounds/Spawn");
            clips[Sounds.Despawn] = Resources.Load<AudioClip>("Sounds/Despawn");
        }

        public void PlaySound(Sounds sound, bool force = false)
        {
            if (force || !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(clips[sound]);
            }
        }
    }
}
