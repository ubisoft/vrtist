using UnityEngine;

namespace VRtist
{
    [CreateAssetMenu(fileName = "NetworkSettings", menuName = "VRtist/NetworkSettings")]
    public class NetworkSettings : ScriptableObject
    {
        public string host = "localhost";
        public int port = 12800;
        public string room = "Local";
        public string master;
        public string userName;
    }
}
