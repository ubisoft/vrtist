using UnityEngine;

namespace VRtist
{
    public class LocatorController : ParametersController
    {
        public override void SetGizmoVisible(bool value)
        {
            // Disable colliders
            Collider collider = gameObject.GetComponent<Collider>();
            collider.enabled = value;

            // Hide geometry
            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            meshRenderer.enabled = value;
        }

        public override bool IsDeformable()
        {
            return false;
        }

    }
}
