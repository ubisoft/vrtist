using UnityEngine;

namespace VRtist
{
    [CreateAssetMenu(fileName = "NetworkSettings", menuName = "VRtist/NetworkSettings")]
    public class NetworkSettings : ScriptableObject
    {
        public string host = "127.0.0.1";
        public int port = 12800;
        public string room = "Local";
        public string master;
        public string userName;
        public Color userColor;
    }
}
