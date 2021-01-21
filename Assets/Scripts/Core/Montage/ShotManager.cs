using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
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

                if (!montageEnabled || !GlobalState.Animation.IsAnimating())
                    return;

                if (activeShotIndex >= 0 && activeShotIndex < shots.Count)
                {
                    Shot shot = shots[activeShotIndex];
                    if (null != shot.camera)
                    {
                        CameraController controller = shot.camera.GetComponent<CameraController>();
                        if (null != controller) { Selection.SetActiveCamera(controller); }
                    }
                }
            }
        }

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
                    shotIndex = 0;
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

        public void SetShotCamera(Shot shot, string value)
        {
            GameObject cam = null;
            if (SyncData.nodes.ContainsKey(value))
            {
                Node node = SyncData.nodes[value];
                if (node.instances.Count > 0)
                    cam = node.instances[0].Item1;
            }
            shot.camera = cam;
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
        }

        public void FireChanged()
        {
            ShotsChangedEvent.Invoke();
        }

        private bool montageEnabled = false;
        public UnityEvent MontageModeChangedEvent = new UnityEvent();
        public bool MontageEnabled
        {
            get { return montageEnabled; }
            set
            {
                montageEnabled = value;
                MontageModeChangedEvent.Invoke();
            }
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
