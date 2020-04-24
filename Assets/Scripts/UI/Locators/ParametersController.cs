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

    public class AnimationKey
    {
        public AnimationKey(int time, float value)
        {
            this.time = time;
            this.value = value;
        }
        public int time;
        public float value;
    }

    public class AnimationChannel
    {
        public AnimationChannel(string name, AnimationKey[] keys)
        {
            this.name = name;
            this.keys = keys;
        }

        public string name;
        public AnimationKey[] keys;
    }

    public class ParametersController : MonoBehaviour
    {
        private ParametersEvent onChangedEvent;
        protected Transform world = null;
        protected Dictionary<string, AnimationChannel> channels = new Dictionary<string, AnimationChannel>();

        public virtual void CopyParameters(ParametersController sourceController)
        {
            channels = new Dictionary<string, AnimationChannel>(sourceController.channels);
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

        public void AddAnimationChannel(string name, AnimationKey[] keys)
        {
            AnimationChannel channel = null;
            if (!channels.TryGetValue(name, out channel))
            {
                channel = new AnimationChannel(name, keys);
                channels[name] = channel;
            }
            else
            {
                channel.keys = keys;
            }
        }

        public bool HasAnimation()
        {
            return channels.Count > 0;
        }

        public Dictionary<string, AnimationChannel> GetAnimationChannels()
        {
            return channels;
        }

        public virtual Parameters GetParameters() { return null;  }
    }
}