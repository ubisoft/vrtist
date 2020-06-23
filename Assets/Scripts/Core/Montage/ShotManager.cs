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
        UpdateShot,
        DuplicateShot,
        MoveShot
    }


    public class ShotManagerActionInfo
    {
        public ShotManagerAction action;
        public int shotIndex;
        public string shotName;
        public int shotStart;
        public int shotEnd;
        public string cameraName;
        public Color shotColor;
        public int moveOffset;
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

        private int currentShot = -1;
        public UnityEvent CurrentShotChangedEvent = new UnityEvent();
        public int CurrentShot
        {
            get { return currentShot; }
            set
            {
                currentShot = value;
                CurrentShotChangedEvent.Invoke();
            }
        }

        // Update current shot without invoking any event
        public void SetCurrentShot(int index)
        {
            currentShot = index;
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
    }

}