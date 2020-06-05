using System;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class GameObjectArgs : EventArgs
    {
        public GameObject gobject;
    }

    //[Serializable]
    //public class GameObjectChangedEvent : UnityEvent<GameObject>
    //{

    //}

    [Serializable]
    public class GameObjectHashChangedEvent : UnityEvent<int>
    {

    }
}
