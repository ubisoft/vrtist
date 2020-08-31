using UnityEngine;

namespace VRtist
{
    public class UIHandle : MonoBehaviour
    {
        private void Start()
        {
            GlobalState.Settings.LoadWindowPosition(transform);
        }

        private void OnApplicationQuit()
        {
            GlobalState.Settings.SaveWindowPosition(transform);
        }
    }
}
