using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
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

    public class AnimationSet
    {
        public AnimationChannel xPosition;
        public AnimationChannel yPosition;
        public AnimationChannel zPosition;
        public AnimationChannel xRotation;
        public AnimationChannel yRotation;
        public AnimationChannel zRotation;
        public AnimationChannel lens;
        public ParametersController parametersController = null;
    }

    public class AnimationController : MonoBehaviour
    {
        private ParametersEvent onChangedEvent = null;

        private Dictionary<GameObject, Dictionary<string, AnimationChannel>> animationChannels = new Dictionary<GameObject, Dictionary<string, AnimationChannel>>();
        private Dictionary<GameObject, AnimationSet> recordAnimationSets = new Dictionary<GameObject, AnimationSet>();
        private int recordCurrentFrame = 0;

        void Update()
        {
            HandleRecord();
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
        public void FireValueChanged(GameObject gameObject)
        {
            if (null != onChangedEvent)
                onChangedEvent.Invoke(gameObject);
        }

        public void ClearAnimations(GameObject gameObject)
        {
            if (!animationChannels.ContainsKey(gameObject))
                return;

            animationChannels[gameObject].Clear();

            ClearAnimationInfo info = new ClearAnimationInfo { gObject = gameObject };
            NetworkClient.GetInstance().SendEvent<ClearAnimationInfo>(MessageType.ClearAnimations, info);

            FireValueChanged(gameObject);
        }

        public void AddAnimationChannel(GameObject gameObject, string name, List<AnimationKey> keys)
        {
            if (!animationChannels.ContainsKey(gameObject))
                animationChannels[gameObject] = new Dictionary<string, AnimationChannel>();

            Dictionary<string, AnimationChannel> channels = animationChannels[gameObject];

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

        public bool HasAnimation(GameObject gameObject)
        {
            if (!animationChannels.ContainsKey(gameObject))
                return false;
            Dictionary<string, AnimationChannel> channels = animationChannels[gameObject];
            return channels.Count > 0;
        }

        public Dictionary<string, AnimationChannel> GetAnimationChannels(GameObject gameObject)
        {
            if (!animationChannels.ContainsKey(gameObject))
                return null;
            return animationChannels[gameObject];
        }

        public void HandleRecord()
        {
            if (GlobalState.Instance.recordState != GlobalState.RecordState.Recording)
                return;
            int frame = GlobalState.currentFrame;
            if (frame != recordCurrentFrame)
            {
                foreach (var item in recordAnimationSets)
                {
                    item.Value.xPosition.keys.Add(new AnimationKey(frame, item.Key.transform.localPosition.x));
                    item.Value.yPosition.keys.Add(new AnimationKey(frame, item.Key.transform.localPosition.y));
                    item.Value.zPosition.keys.Add(new AnimationKey(frame, item.Key.transform.localPosition.z));

                    Quaternion q = item.Key.transform.localRotation;
                    // convert to ZYX euler
                    Vector3 angles = Maths.ThreeAxisRotation(q);

                    item.Value.xRotation.keys.Add(new AnimationKey(frame, angles.x));
                    item.Value.yRotation.keys.Add(new AnimationKey(frame, angles.y));
                    item.Value.zRotation.keys.Add(new AnimationKey(frame, angles.z));

                    if (null != item.Value.parametersController)
                    {
                        ParametersController controller = item.Value.parametersController;
                        System.Type controllerType = controller.GetType();
                        if (controllerType == typeof(CameraController) || controllerType.IsSubclassOf(typeof(CameraController)))
                        {
                            CameraController cameraController = item.Value.parametersController as CameraController;
                            item.Value.lens.keys.Add(new AnimationKey(frame, cameraController.focal));
                        }
                    }
                }
            }
            recordCurrentFrame = frame;
        }

        public void OnCountdownFinished()
        {
            recordAnimationSets.Clear();
            foreach (GameObject item in Selection.selection.Values)
            {
                CameraController cameraController = item.GetComponent<CameraController>();
                if (null != cameraController)
                {
                    GlobalState.Instance.ClearAnimations(item);

                    AnimationSet animationSet = new AnimationSet();
                    animationSet.xPosition = new AnimationChannel("location[0]");
                    animationSet.yPosition = new AnimationChannel("location[1]");
                    animationSet.zPosition = new AnimationChannel("location[2]");
                    animationSet.xRotation = new AnimationChannel("rotation_euler[0]");
                    animationSet.yRotation = new AnimationChannel("rotation_euler[1]");
                    animationSet.zRotation = new AnimationChannel("rotation_euler[2]");
                    animationSet.lens = new AnimationChannel("lens");
                    animationSet.parametersController = cameraController;
                    recordAnimationSets[item] = animationSet;
                }
            }

            recordCurrentFrame = GlobalState.currentFrame - 1;
        }

        public void SendKeyInfo(string objectName, string channelName, int channelIndex, int frame, float value)
        {
            SetKeyInfo keyInfo = new SetKeyInfo()
            {
                objectName = objectName,
                channelName = channelName,
                channelIndex = channelIndex,
                frame = frame,
                value = value
            };
            NetworkClient.GetInstance().SendEvent<SetKeyInfo>(MessageType.AddKeyframe, keyInfo);
        }

        private void SendDeleteKeyInfo(string objectName, string channelName, int channelIndex)
        {
            SetKeyInfo keyInfo = new SetKeyInfo()
            {
                objectName = objectName,
                channelName = channelName,
                channelIndex = channelIndex,
                value = 0.0f
            };
            NetworkClient.GetInstance().SendEvent<SetKeyInfo>(MessageType.RemoveKeyframe, keyInfo);
        }

        private void SendAnimationChannel(string objectName, AnimationChannel animationChannel)
        {
            foreach (AnimationKey key in animationChannel.keys)
            {
                animationChannel.GetChannelInfo(out string channelName, out int channelIndex);
                SendKeyInfo(objectName, channelName, channelIndex, key.time, key.value);
            }
        }

        public void ApplyAnimations()
        {
            foreach (var item in recordAnimationSets)
            {
                GameObject gObject = item.Key;
                string objectName = gObject.name;
                SendAnimationChannel(objectName, item.Value.xPosition);
                SendAnimationChannel(objectName, item.Value.yPosition);
                SendAnimationChannel(objectName, item.Value.zPosition);
                SendAnimationChannel(objectName, item.Value.xRotation);
                SendAnimationChannel(objectName, item.Value.yRotation);
                SendAnimationChannel(objectName, item.Value.zRotation);
                SendAnimationChannel(objectName, item.Value.lens);
                NetworkClient.GetInstance().SendQueryObjectData(objectName);
            }
            recordAnimationSets.Clear();
        }

        public void AddKeyframe()
        {
            int frame = GlobalState.currentFrame;
            foreach (GameObject item in Selection.selection.Values)
            {
                CameraController controller = item.GetComponent<CameraController>();
                if (null != controller)
                {
                    string name = item.name;
                    SendKeyInfo(name, "location", 0, frame, item.transform.localPosition.x);
                    SendKeyInfo(name, "location", 1, frame, item.transform.localPosition.y);
                    SendKeyInfo(name, "location", 2, frame, item.transform.localPosition.z);
                    Quaternion q = item.transform.localRotation;
                    // convert to ZYX euler
                    Vector3 angles = Maths.ThreeAxisRotation(q);
                    SendKeyInfo(name, "rotation_euler", 0, frame, angles.x);
                    SendKeyInfo(name, "rotation_euler", 1, frame, angles.y);
                    SendKeyInfo(name, "rotation_euler", 2, frame, angles.z);
                    SendKeyInfo(name, "lens", -1, frame, controller.focal);

                    NetworkClient.GetInstance().SendEvent<string>(MessageType.QueryObjectData, name);
                }
            }
        }

        public void RemoveKeyframe()
        {
            foreach (GameObject item in Selection.selection.Values)
            {
                string objectName = item.name;
                CameraController controller = item.GetComponent<CameraController>();
                if (null != controller)
                {
                    SendDeleteKeyInfo(objectName, "location", 0);
                    SendDeleteKeyInfo(objectName, "location", 1);
                    SendDeleteKeyInfo(objectName, "location", 2);
                    SendDeleteKeyInfo(objectName, "rotation_euler", 0);
                    SendDeleteKeyInfo(objectName, "rotation_euler", 1);
                    SendDeleteKeyInfo(objectName, "rotation_euler", 2);
                    SendDeleteKeyInfo(objectName, "lens", -1);
                    NetworkClient.GetInstance().SendEvent<string>(MessageType.QueryObjectData, objectName);
                }
            }
        }
    }
}