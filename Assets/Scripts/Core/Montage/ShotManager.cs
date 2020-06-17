using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        public List<Shot> shots = new List<Shot>();
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