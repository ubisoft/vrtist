using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{
    [System.Serializable]
    public class ParametersController : MonoBehaviour, IGizmo
    {
        protected Transform world = null;

        public bool Lock
        {
            get { return lockPosition && lockRotation && lockScale; }
            set
            {
                lockPosition = value;
                lockRotation = value;
                lockScale = value;
            }
        }

        public bool lockPosition = false;
        public bool lockRotation = false;
        public bool lockScale = false;
        public List<GameObject> constraintHolders = new List<GameObject>();

        public bool isImported = false;
        public string importPath;

        public virtual bool IsDeletable()
        {
            return true;
        }

        public virtual void CopyParameters(ParametersController sourceController)
        {
            lockPosition = sourceController.lockPosition;
            lockRotation = sourceController.lockRotation;
            lockScale = sourceController.lockScale;
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

        public virtual bool IsSnappable()
        {
            if (lockPosition)
                return false;
            return true;
        }

        public void AddConstraintHolder(GameObject gobject)
        {
            constraintHolders.Add(gobject);
        }

        public void RemoveConstraintHolder(GameObject gobject)
        {
            constraintHolders.Remove(gobject);
        }
    }
}
