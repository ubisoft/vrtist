using UnityEngine;

namespace VRtist
{
    public class ConstraintLineController : ParametersController
    {
        public override void SetGizmoVisible(bool value)
        {
            // Disable colliders
            LineRenderer line = gameObject.GetComponent<LineRenderer>();
            line.enabled = value;
        }
    }
}
