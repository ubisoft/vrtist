using System;

using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [Serializable]
    public class CurveChangedEvent : UnityEvent<GameObject, AnimatableProperty>
    {

    }
}
