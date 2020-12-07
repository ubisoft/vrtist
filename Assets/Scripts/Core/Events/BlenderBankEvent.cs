using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace VRtist
{
    [Serializable]
    public class BlenderBankEvent : UnityEvent<List<string>, List<string>, List<string>>
    {
        // Empty
    }
}
