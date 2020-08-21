using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [System.Serializable]
    
    public class ParametersController : MonoBehaviour
    {
        protected Transform world = null;

        public bool locked = false;

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

        protected virtual void Start()
        {
        }
        
        public virtual Parameters GetParameters() { return null; }
    }
}