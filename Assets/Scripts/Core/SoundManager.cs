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
            ClickOut,
            Snapshot
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
            clips[Sounds.Snapshot] = Resources.Load<AudioClip>("Sounds/snapshot");
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
