using System;

using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [Serializable]
    public class RangeChangedEventFloat : UnityEvent<Vector2>
    {

    }

    [Serializable]
    public class RangeChangedEventInt : UnityEvent<Vector2Int>
    {

    }
}
