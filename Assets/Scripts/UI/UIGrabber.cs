using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class UIGrabber : MonoBehaviour
    {
        public Deformer deformer = null;
        public GameObject prefab = null;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name == "Cursor")
                deformer.SetGrabbedObject(prefab);
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name == "Cursor")
                deformer.SetGrabbedObject(null);
        }
    }

}