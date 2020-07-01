using System;
using UnityEngine;

namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/ColorVariable"), Serializable]
    public class ColorVariable : ScriptableObject
    {
        public Color value;
    }
}
