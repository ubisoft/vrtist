using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    [System.Serializable]
    public class ParametersEvent : UnityEvent<GameObject>
    {
    }

    public class ParametersController : MonoBehaviour
    {
        private ParametersEvent onChangedEvent;
        protected Transform world = null;

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

        private void Start()
        {
            onChangedEvent = new ParametersEvent();
        }

        public void AddListener(UnityAction<GameObject> callback)
        {
            if (null == onChangedEvent)
                onChangedEvent = new ParametersEvent();
            onChangedEvent.AddListener(callback);
        }

        public void RemoveListener(UnityAction<GameObject> callback)
        {
            if (null != onChangedEvent)
                onChangedEvent.RemoveListener(callback);
        }

        public void FireValueChanged()
        {
            if(null != onChangedEvent)
                onChangedEvent.Invoke(gameObject);
        }

        public virtual Parameters GetParameters() { return null;  }
    }
}