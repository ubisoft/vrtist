using System;

using UnityEngine.Events;

namespace VRtist
{
    /// <summary>
    /// Sent when the global state of the animation engine changes.
    /// </summary>
    [Serializable]
    public class AnimationStateChangedEvent : UnityEvent<AnimationState>
    {

    }
}
