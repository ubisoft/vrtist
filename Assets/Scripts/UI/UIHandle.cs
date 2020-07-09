using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class UIHandle : MonoBehaviour
    {
        private void OnApplicationQuit()
        {
            GlobalState.Settings.SetWindowPosition(transform);
        }


    }
}
