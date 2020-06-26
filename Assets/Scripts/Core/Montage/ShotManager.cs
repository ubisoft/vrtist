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
        public string cameraName = "";
        public Color shotColor = Color.black;
        public int moveOffset = 0;
        public int shotEnabled = -1;
    }


    public class ShotManager
    {
        public static ShotManager Instance
        {
            get
            {
                if(null == instance)
                    instance = new ShotManager();
                return instance;
            }
        }
        public void AddShot(Shot shot)
        {
            shots.Add(shot);
        }

        public void InsertShot(int index, Shot shot)
        {
            shots.Insert(index, shot);
        }

        public void RemoveShot(Shot shot)
        {
            shots.Remove(shot);
        }
        public void RemoveShot(int index)
        {
            try
            {
                shots.RemoveAt(index);
            }
            catch(ArgumentOutOfRangeException)
            {
                Debug.LogWarning($"Failed to remove shot at index {index}.");
            }
        }

        public void MoveCurrentShot(int offset)
        {
            int newIndex = currentShotIndex + offset;
            if (newIndex < 0)
                newIndex = 0;
            if (newIndex >= shots.Count)
                newIndex = shots.Count - 1;

            Shot shot = shots[currentShotIndex];
            shots.RemoveAt(currentShotIndex);
            shots.Insert(newIndex, shot);
            currentShotIndex = newIndex;
        }

        public void SetCurrentShotStart(int value)
        {
            Shot shot = shots[currentShotIndex];
            shot.start = value;
        }
        public void SetCurrentShotEnd(int value)
        {
            Shot shot = shots[currentShotIndex];
            shot.end = value;
        }
        public void SetCurrentShotName(string value)
        {
            Shot shot = shots[currentShotIndex];
            shot.name = value;
        }
        public void SetCurrentShotCamera(string value)
        {
            Shot shot = shots[currentShotIndex];
            GameObject cam = null;
            if(SyncData.nodes.ContainsKey(value))
            {
                Node node = SyncData.nodes[value];
                if (node.instances.Count > 0)
                    cam = node.instances[0].Item1;
            }
            shot.camera = cam;
        }

        public void SetCurrentShotColor(Color color)
        {
            Shot shot = shots[currentShotIndex];
            shot.color = color;
        }

        public void SetCurrentShotEnabled(bool value)
        {
            Shot shot = shots[currentShotIndex];
            shot.enabled = value;
        }

        public void UpdateShot(int index, Shot shot)
        {
            try
            {
                shots[index] = shot;
            }
            catch(ArgumentOutOfRangeException)
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

        private bool montageMode = false;
        public UnityEvent MontageModeChangedEvent = new UnityEvent();
        public bool MontageMode
        {
            get { return montageMode; }
            set
            {
                montageMode = value;
                MontageModeChangedEvent.Invoke();
            }
        }

        private static Regex shotNameRegex = new Regex(@"Sh(?<number>\d{4})", RegexOptions.Compiled);
        public string GetUniqueShotName()
        {
            int maxNumber = 0;
            foreach(Shot shot in shots)
            {
                MatchCollection matches = shotNameRegex.Matches(shot.name);
                if(matches.Count != 1) { continue; }

                GroupCollection groups = matches[0].Groups;
                int number = Int32.Parse(groups["number"].Value);
                if(number > maxNumber)
                {
                    maxNumber = number;
                }
            }

            return $"Sh{maxNumber + 10:D4}";
        }

        private int currentShotIndex = -1;
        public UnityEvent CurrentShotChangedEvent = new UnityEvent();
        public int CurrentShot
        {
            get { return currentShotIndex; }
            set
            {
                currentShotIndex = value;
                CurrentShotChangedEvent.Invoke();
            }
        }

        // Update current shot without invoking any event
        public void SetCurrentShot(int index)
        {
            currentShotIndex = index;
        }

        public List<Shot> shots = new List<Shot>();
        public UnityEvent ShotsChangedEvent = new UnityEvent();
        private static ShotManager instance = null;
    }

    public class Shot
    {
        public string name;
        public GameObject camera; // TODO, manage game object destroy
        public int start;
        public int end;
        public bool enabled;
        public Color color;
    }

}