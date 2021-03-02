using UnityEngine;

namespace VRtist
{
    public class UIHandle : MonoBehaviour
    {
        private void Start()
        {
            GlobalState.Settings.LoadWindowPosition(transform);
        }

        private void OnDisable()
        {
            GlobalState.Settings.SaveWindowPosition(transform);
        }
    }
}
