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

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "DeformerCollider")
            {
                deformer.SetActivePLane(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag == "DeformerCollider")
            {
                deformer.SetActivePLane(null);
            }
        }
    }
}
