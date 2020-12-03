using UnityEngine;
using UnityEngine.Animations;

namespace VRtist
{
    [System.Serializable]

    public class ParametersController : MonoBehaviour, IGizmo
    {
        protected Transform world = null;

        public bool locked = false;
        public string controllerName;

        // world scale when constraint is created
        public float initParentConstraintScale;
        public Vector3 initParentConstraintOffset;

        public void ConnectWorldScale()
        {
            GlobalState.onWorldScaleEvent.AddListener(OnWorldScaleChanged);
        }

        public void DisconnectWorldScale()
        {
            GlobalState.onWorldScaleEvent.RemoveListener(OnWorldScaleChanged);
        }

        private void OnWorldScaleChanged()
        {
            ParentConstraint parentConstraint = GetComponent<ParentConstraint>();
            if (null == parentConstraint)
                return;
            ConstraintUtility.UpdateParentConstraintTranslationOffset(parentConstraint, initParentConstraintOffset, initParentConstraintScale);
        }

        public virtual void CopyParameters(ParametersController sourceController)
        {
            locked = sourceController.locked;
        }

        public virtual void SetName(string name)
        {
            gameObject.name = name;
        }

        protected Transform GetWorldTransform()
        {
            if (null != world)
                return world;
            world = transform.parent;
            while (world != null && world.parent)
            {
                world = world.parent;
            }
            return world;
        }

        public virtual void SetGizmoVisible(bool value)
        {
            // Disable colliders
            Collider[] colliders = gameObject.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                collider.enabled = value;
            }

            // Hide geometry
            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter meshFilter in meshFilters)
            {
                meshFilter.gameObject.SetActive(value);
            }

            // Hide UI
            Canvas[] canvases = gameObject.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                canvas.gameObject.SetActive(value);
            }
        }
    }
}
