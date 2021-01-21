using System;
using System.Collections.Generic;

using UnityEngine.Events;

namespace VRtist
{
    [Serializable]
    public class BlenderBankListEvent : UnityEvent<List<string>, List<string>, List<string>>
    {
        // Empty
    }

    public class BlenderBankImportObjectEvent : UnityEvent<string, string>
    {
        // Empty
    }
}
