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
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public enum ShotManagerAction
    {
        AddShot = 0,
        DeleteShot,
        DuplicateShot,
        MoveShot,
        UpdateShot
    }

    public class ShotManagerActionInfo
    {
        public ShotManagerAction action;
        public int shotIndex = 0;
        public string shotName = "";
        public int shotStart = -1;
        public int shotEnd = -1;
        public GameObject camera;
        public Color shotColor = Color.black;
        public int moveOffset = 0;
        public int shotEnabled = -1;

        public ShotManagerActionInfo Copy()
        {
            return new ShotManagerActionInfo()
            {
                action = action,
                shotIndex = shotIndex,
                shotName = shotName,
                shotStart = shotStart,
                shotEnd = shotEnd,
                camera = camera,
                shotColor = shotColor,
                moveOffset = moveOffset,
                shotEnabled = shotEnabled
            };
        }
    }

    public class Shot
    {
        public string name;
        public GameObject camera = null; // TODO, manage game object destroy
        public int start = -1;
        public int end = -1;
        public bool enabled = true;
        public Color color = Color.black;

        public Shot Copy()
        {
            return new Shot { name = name, camera = camera, start = start, end = end, enabled = enabled, color = color };
        }
    }

    /// <summary>
    /// Manage shots.
    /// </summary>
    public class ShotManager : TimeHook
    {
        public static ShotManager Instance
        {
            get
            {
                if (null == instance)
                {
                    instance = new ShotManager();
                    GlobalState.Animation.RegisterTimeHook(instance);
                    GlobalState.Animation.onFrameEvent.AddListener(instance.OnFrameChanged);
                    GlobalState.Animation.onAnimationStateEvent.AddListener(instance.OnAnimationStateChanged);
                }
                return instance;
            }
        }

        private int activeShotIndex = -1;
        public UnityEvent ActiveShotChangedEvent = new UnityEvent();
        public int ActiveShotIndex
        {
            get { return activeShotIndex; }
            set
            {
                activeShotIndex = value;
                ActiveShotChangedEvent.Invoke();

                if (!montageEnabled || !GlobalState.Animation.IsAnimating() && GlobalState.Animation.animationState != AnimationState.VideoOutput)
                    return;

                if (activeShotIndex >= 0 && activeShotIndex < shots.Count)
                {
                    Shot shot = shots[activeShotIndex];
                    if (null != shot.camera)
                    {
                        CameraController controller = shot.camera.GetComponent<CameraController>();
                        if (null != controller) { CameraManager.Instance.ActiveCamera = controller.gameObject; }
                    }
                }
            }
        }

        private bool montageEnabled = false;
        public bool MontageEnabled
        {
            get { return montageEnabled; }
            set
            {
                montageEnabled = value;
                MontageModeChangedEvent.Invoke();
            }
        }
        public UnityEvent MontageModeChangedEvent = new UnityEvent();

        public List<Shot> shots = new List<Shot>();
        public UnityEvent ShotsChangedEvent = new UnityEvent();
        private static ShotManager instance = null;

        public int GetShotIndex(Shot shot)
        {
            for (int i = 0; i < shots.Count; i++)
            {
                if (shot == shots[i])
                    return i;
            }
            return -1;
        }

        int FindFirstShotIndexAt(int frame)
        {
            for (int i = 0; i < shots.Count; i++)
            {
                Shot shot = shots[i];
                if (frame >= shot.start && frame <= shot.end)
                    return i;
            }
            return -1;
        }

        void OnAnimationStateChanged(AnimationState state)
        {
            switch (state)
            {
                case AnimationState.Playing:
                    ActiveShotIndex = FindFirstShotIndexAt(AnimationEngine.Instance.CurrentFrame);
                    break;
                case AnimationState.VideoOutput:
                    if (shots.Count == 0)
                    {
                        GlobalState.Animation.Pause();
                        break;
                    }
                    // Force activating a camera
                    ActiveShotIndex = 0;
                    GlobalState.Animation.CurrentFrame = shots[0].start;
                    break;
            }
        }

        void OnFrameChanged(int frame)
        {
            if (ActiveShotIndex != -1)
            {
                Shot shot = shots[ActiveShotIndex];
                if (frame < shot.start || frame > shot.end)
                {
                    ActiveShotIndex = FindFirstShotIndexAt(frame);
                }
            }
            else
            {
                ActiveShotIndex = FindFirstShotIndexAt(frame);
            }
        }

        public override int HookTime(int frame)
        {
            if (!montageEnabled || shots.Count == 0)
                return frame;

            int shotIndex;
            if (ActiveShotIndex < 0 || ActiveShotIndex >= shots.Count)
            {
                shotIndex = FindFirstShotIndexAt(frame);
                if (-1 != shotIndex)
                {
                    ActiveShotIndex = shotIndex;
                }
                return frame;
            }

            shotIndex = ActiveShotIndex;
            Shot shot = shots[shotIndex];
            if (frame > shot.end)
            {
                shotIndex++;
                while (shotIndex < shots.Count && !shots[shotIndex].enabled)
                    shotIndex++;
                if (shotIndex >= shots.Count)
                {
                    shotIndex = 0;
                    if (GlobalState.Animation.animationState == AnimationState.VideoOutput)
                    {
                        GlobalState.Animation.OnToggleStartVideoOutput(false);
                        return shot.end;
                    }
                }
                while (shotIndex < shots.Count && !shots[shotIndex].enabled)
                    shotIndex++;

                if (shotIndex >= shots.Count)
                {
                    ActiveShotIndex = -1;
                    return frame;
                }
                ActiveShotIndex = shotIndex;
                return shots[ActiveShotIndex].start;
            }
            return frame;
        }

        // Update current shot without invoking any event
        public void SetCurrentShotIndex(int index)
        {
            activeShotIndex = index;
        }

        public void SetCurrentShot(Shot shot)
        {
            for (int i = 0; i < shots.Count; i++)
            {
                if (shot == shots[i])
                {
                    ActiveShotIndex = i;
                    return;
                }
            }
        }

        public void AddShot(Shot shot)
        {
            shots.Add(shot);
        }

        public void DuplicateShot(int index)
        {
            Shot shot = shots[index].Copy();
            shots.Insert(index + 1, shot);
        }

        public void InsertShot(int index, Shot shot)
        {
            shots.Insert(index, shot);
        }

        public void RemoveShot(Shot shot)
        {
            shots.Remove(shot);
            if (activeShotIndex >= shots.Count)
            {
                activeShotIndex = -1;
            }
        }

        public void RemoveShot(int index)
        {
            try
            {
                shots.RemoveAt(index);
                activeShotIndex = index - 1;
                if (activeShotIndex < 0 && shots.Count > 0) { activeShotIndex = 0; }
            }
            catch (ArgumentOutOfRangeException)
            {
                Debug.LogWarning($"Failed to remove shot at index {index}.");
            }
        }

        public void MoveShot(int shotIndex, int offset)
        {
            int newIndex = shotIndex + offset;
            if (newIndex < 0)
                newIndex = 0;
            if (newIndex >= shots.Count)
                newIndex = shots.Count - 1;

            Shot shot = shots[shotIndex];
            shots.RemoveAt(shotIndex);
            shots.Insert(newIndex, shot);
        }

        public void UpdateShot(int index, Shot shot)
        {
            try
            {
                shots[index] = shot;
            }
            catch (ArgumentOutOfRangeException)
            {
                Debug.LogWarning($"Failed to update shot at index {index}.");
            }
        }

        public void Clear()
        {
            shots.Clear();
            activeShotIndex = -1;
        }

        public void FireChanged()
        {
            ShotsChangedEvent.Invoke();
        }

        private static readonly Regex shotNameRegex = new Regex(@"Sh(?<number>\d{4})", RegexOptions.Compiled);
        public string GetUniqueShotName()
        {
            int maxNumber = 0;
            foreach (Shot shot in shots)
            {
                MatchCollection matches = shotNameRegex.Matches(shot.name);
                if (matches.Count != 1) { continue; }

                GroupCollection groups = matches[0].Groups;
                int number = Int32.Parse(groups["number"].Value);
                if (number > maxNumber)
                {
                    maxNumber = number;
                }
            }

            return $"Sh{maxNumber + 10:D4}";
        }
    }
}
