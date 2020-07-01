using UnityEngine;

namespace VRtist
{
    [CreateAssetMenu(menuName = "VRtist/HDRColorVariable")]
    public class HDRColorVariable : ScriptableObject
    {
        [ColorUsage(true, true)]
        public Color value;
    }
}
