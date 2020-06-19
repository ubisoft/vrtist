using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class ShotManager
    {
        public static ShotManager Instance
        {
            get
            {
                if (null == instance)
                    instance = new ShotManager();
                return instance;
            }
        }
        public void AddShot(Shot shot)
        {
            shots.Add(shot);
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

        private int currentShot = -1;
        public UnityEvent CurrentShotChangedEvent = new UnityEvent();
        public int CurrentShot
        {
            get { return currentShot;  }
            set
            {
                currentShot = value;
                CurrentShotChangedEvent.Invoke();
            }
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