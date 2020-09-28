using System;
using UnityEngine.Events;

namespace VRtist
{
    public struct Range<T>
    {
        public T min;
        public T max;
    }

    [Serializable]
    public class RangeChangedEvent<T> : UnityEvent<Range<T>>
    {

    }
}
