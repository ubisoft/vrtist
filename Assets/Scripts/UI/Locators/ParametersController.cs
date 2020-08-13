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
        public AnimationChannel(string name)
        {
            this.name = name;
            keys = new List<AnimationKey>();
        }
        public AnimationChannel(string name, List<AnimationKey> keys)
        {
            this.name = name;
            this.keys = keys;
        }

        public void GetChannelInfo(out string name, out int index)
        {
            int i = this.name.IndexOf('[');
            if (-1 == i)
            {
                name = this.name;
                index = -1;
            }
            else
            {
                name = this.name.Substring(0, i);
                index = int.Parse(this.name.Substring(i + 1, 1));
            }
        }

        public string name;
        public List<AnimationKey> keys;
    }

    public class ClearAnimationInfo
    {
        public GameObject gObject;
    }

    public class ParametersController : MonoBehaviour
    {
        private ParametersEvent onChangedEvent;
        protected Transform world = null;
        protected Dictionary<string, AnimationChannel> channels = new Dictionary<string, AnimationChannel>();

        public bool locked = false;

        public virtual void CopyParameters(ParametersController sourceController)
        {
            channels = new Dictionary<string, AnimationChannel>(sourceController.channels);
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
            if (null != onChangedEvent)
                onChangedEvent.Invoke(gameObject);
        }

        public void ClearAnimations()
        {
            channels.Clear();

            ClearAnimationInfo info = new ClearAnimationInfo { gObject = gameObject };
            NetworkClient.GetInstance().SendEvent<ClearAnimationInfo>(MessageType.ClearAnimations, info);

            FireValueChanged();
        }

        public void AddAnimationChannel(string name, List<AnimationKey> keys)
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

        public virtual Parameters GetParameters() { return null; }
    }
}