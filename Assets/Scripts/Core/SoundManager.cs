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
            Despawn,
            OpenWindow,
            CloseWindow,
            ClickIn,
            ClickOut
        }

        private AudioSource audioSource;
        private readonly Dictionary<Sounds, AudioClip> clips = new Dictionary<Sounds, AudioClip>();

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
            clips[Sounds.OpenWindow] = Resources.Load<AudioClip>("Sounds/ui_casual_open");
            clips[Sounds.CloseWindow] = Resources.Load<AudioClip>("Sounds/ui_casual_pops_close");
            clips[Sounds.ClickIn] = Resources.Load<AudioClip>("Sounds/click-in");
            clips[Sounds.ClickOut] = Resources.Load<AudioClip>("Sounds/click-out");
        }

        public void PlayUISound(Sounds sound, bool force = false)
        {
            if (force || !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(clips[sound]);
            }
        }

        public void Play3DSound(AudioSource source, Sounds sound)
        {
            source.PlayOneShot(clips[sound]);
        }
    }
}
