using UnityEngine;

namespace VRtist
{
    public class LocatorController : ParametersController
    {
        public override void SetGizmoVisible(bool value)
        {
            // Disable colliders
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                collider.enabled = value;
            }

            // Hide geometry
            MeshRenderer[] meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.enabled = value;
            }
        }
    }
}
