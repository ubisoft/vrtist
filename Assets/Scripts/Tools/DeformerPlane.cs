using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRtist
{
    public class DeformerPlane : MonoBehaviour
    {
        public Deformer deformer;
        public Transform opposite;
        public Vector3 direction;

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "DeformerCollider")
            {
                if(deformer.ActivePlane() == null)
                    deformer.SetActivePLane(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag == "DeformerCollider")
            {
                if (deformer.ActivePlane() == this)
                    deformer.SetActivePLane(null);
            }
        }
    }
}
