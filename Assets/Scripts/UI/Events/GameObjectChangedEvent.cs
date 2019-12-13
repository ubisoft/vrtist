using System;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    //[Serializable]
    //public class GameObjectChangedEvent : UnityEvent<GameObject>
    //{

    //}

    [Serializable]
    public class GameObjectHashChangedEvent : UnityEvent<int>
    {

    }
}
