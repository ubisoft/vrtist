using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class ParametersEvent : UnityEvent<GameObject, AnimationChannel>
    {
    }

    public class AnimationKey
    {
        public AnimationKey(int time, float value, Interpolation interpolation = Interpolation.Bezier)
        {
            this.time = time;
            this.value = value;
            this.interpolation = interpolation;
        }
        public int time;
        public float value;
        public Interpolation interpolation;
    }

    public class AnimationChannel
    {
        public AnimationChannel(string name, int index)
        {
            this.name = name;
            this.index = index;
            keys = new List<AnimationKey>();
        }
        public AnimationChannel(string name, int index, List<AnimationKey> keys)
        {
            this.name = name;
            this.index = index;
            this.keys = keys;
        }
        public bool TryGetIndex(int time, out int index)
        {
            int count = keys.Count;
            if (count == 0)
            {
                index = 0;
                return false;
            }
            int id1 = 0, id2 = count - 1;
            while (true)
            {
                if (keys[id1].time == time)
                {
                    index = id1;
                    return true;
                }
                if (keys[id2].time == time)
                {
                    index = id2;
                    return true;
                }

                int center = (id1 + id2) / 2;
                if (time < keys[center].time)
                {
                    if (id2 == center)
                    {
                        index = id1;
                        return false;
                    }
                    id2 = center;
                }
                else
                {
                    if (id1 == center)
                    {
                        index = id2;
                        return false;
                    }
                    id1 = center;
                }
            }
        }

        public void AddKey(AnimationKey key)
        {
            if (TryGetIndex(key.time, out int index))
            {
                keys[index] = key;
            }
            else
            {
                keys.Insert(index, key);
            }
        }

        public AnimationKey GetKey(int index)
        {
            return keys[index];
        }

        public bool TryFindKey(int time, out AnimationKey key)
        {
            if (TryGetIndex(time, out int index))
            {
                key = keys[index];
                return true;
            }
            key = new AnimationKey(0, 0);
            return false;
        }

        public string name;
        public int index;
        public List<AnimationKey> keys;
    }

    public class AnimationSet
    {
        public AnimationChannel xPosition;
        public AnimationChannel yPosition;
        public AnimationChannel zPosition;
        public AnimationChannel xRotation;
        public AnimationChannel yRotation;
        public AnimationChannel zRotation;
        public AnimationChannel xScale;
        public AnimationChannel yScale;
        public AnimationChannel zScale;
        public AnimationChannel lens;
        public AnimationChannel energy;
        public AnimationChannel RColor;
        public AnimationChannel GColor;
        public AnimationChannel BColor;
        public ParametersController parametersController = null;
    }

    public class AnimationController
    {
        private ParametersEvent onChangedEvent = null;

        private Dictionary<GameObject, Dictionary<Tuple<string, int>, AnimationChannel>> animationChannels = new Dictionary<GameObject, Dictionary<Tuple<string, int>, AnimationChannel>>();
        private Dictionary<GameObject, AnimationSet> recordAnimationSets = new Dictionary<GameObject, AnimationSet>();
        private int recordCurrentFrame = 0;

        public void Start()
        {
            Selection.OnSelectionChanged += OnSelectionChanged;
        }

        public void Update()
        {
            HandleRecord();
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            foreach (GameObject gObject in Selection.GetSelectedObjects(SelectionType.Hovered | SelectionType.Selection))
            {
                MixerClient.GetInstance().SendEvent<string>(MessageType.QueryAnimationData, gObject.name);
            }
        }

        public void AddListener(UnityAction<GameObject, AnimationChannel> callback)
        {
            if (null == onChangedEvent)
                onChangedEvent = new ParametersEvent();
            onChangedEvent.AddListener(callback);
        }
        public void RemoveListener(UnityAction<GameObject, AnimationChannel> callback)
        {
            if (null != onChangedEvent)
                onChangedEvent.RemoveListener(callback);
        }
        public void FireAnimationChanged(GameObject gameObject, AnimationChannel channel)
        {
            if (null != onChangedEvent)
                onChangedEvent.Invoke(gameObject, channel);
        }

        public void ClearAnimations(GameObject gameObject)
        {
            if (!animationChannels.ContainsKey(gameObject))
                return;

            animationChannels[gameObject].Clear();

            FireAnimationChanged(gameObject, null);
        }
        public void RemoveAnimationChannel(GameObject gameObject, string name, int index)
        {
            if (animationChannels.TryGetValue(gameObject, out Dictionary<Tuple<string, int>, AnimationChannel> channels))
            {
                Tuple<string, int> channel = new Tuple<string, int>(name, index);
                if (channels.ContainsKey(channel))
                    channels.Remove(channel);
            }
        }

        public void AddAnimationChannel(GameObject gameObject, string name, int channelIndex, List<AnimationKey> keys)
        {
            if (!animationChannels.ContainsKey(gameObject))
                animationChannels[gameObject] = new Dictionary<Tuple<string, int>, AnimationChannel>();

            Dictionary<Tuple<string, int>, AnimationChannel> channels = animationChannels[gameObject];

            AnimationChannel channel = null;
            Tuple<string, int> c = new Tuple<string, int>(name, channelIndex);
            if (!channels.TryGetValue(c, out channel))
            {
                channel = new AnimationChannel(name, channelIndex, keys);
                channels[c] = channel;
            }
            else
            {
                channel.keys = keys;
            }

            FireAnimationChanged(gameObject, channel);
        }

        public bool HasAnimation(GameObject gameObject)
        {
            if (!animationChannels.ContainsKey(gameObject))
                return false;
            Dictionary<Tuple<string, int>, AnimationChannel> channels = animationChannels[gameObject];
            return channels.Count > 0;
        }

        public Dictionary<Tuple<string, int>, AnimationChannel> GetAnimationChannels(GameObject gameObject)
        {
            if (!animationChannels.ContainsKey(gameObject))
                return null;
            return animationChannels[gameObject];
        }

        public void SendKeyInfo(string objectName, string channelName, int channelIndex, int frame, float value, Interpolation interpolation)
        {
            SetKeyInfo keyInfo = new SetKeyInfo()
            {
                objectName = objectName,
                channelName = channelName,
                channelIndex = channelIndex,
                frame = frame,
                value = value,
                interpolation = interpolation,
            };
            MixerClient.GetInstance().SendEvent<SetKeyInfo>(MessageType.AddKeyframe, keyInfo);
        }

        public void SendAnimationChannel(string objectName, AnimationChannel animationChannel)
        {
            foreach (AnimationKey key in animationChannel.keys)
            {
                SendKeyInfo(objectName, animationChannel.name, animationChannel.index, key.time, key.value, key.interpolation);
            }
        }

        public void HandleRecord()
        {
            if (GlobalState.Animation.animationState != AnimationState.Recording)
                return;
            int frame = GlobalState.Animation.currentFrame;
            if (frame != recordCurrentFrame)
            {
                if (frame >= GlobalState.Animation.endFrame)
                {
                    GlobalState.Animation.Pause();
                    return;
                }
                /* Doesn't work in blender, blender will loop in previous range
                if(frame >= GlobalState.endFrame - 20)
                {
                    GlobalState.endFrame += 100;
                    FrameStartEnd info = new FrameStartEnd() { start = GlobalState.startFrame, end = GlobalState.endFrame };
                    MixerClient.GetInstance().SendEvent<FrameStartEnd>(MessageType.FrameStartEnd, info);
                }
                */

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
                        if (controllerType == typeof(LightController) || controllerType.IsSubclassOf(typeof(LightController)))
                        {
                            LightController lightController = item.Value.parametersController as LightController;
                            item.Value.energy.keys.Add(new AnimationKey(frame, lightController.GetPower()));
                            item.Value.RColor.keys.Add(new AnimationKey(frame, lightController.color.r));
                            item.Value.GColor.keys.Add(new AnimationKey(frame, lightController.color.g));
                            item.Value.BColor.keys.Add(new AnimationKey(frame, lightController.color.b));
                        }
                    }
                    else
                    {
                        item.Value.xScale.keys.Add(new AnimationKey(frame, item.Key.transform.localScale.x));
                        item.Value.yScale.keys.Add(new AnimationKey(frame, item.Key.transform.localScale.y));
                        item.Value.zScale.keys.Add(new AnimationKey(frame, item.Key.transform.localScale.z));
                    }
                }
            }
            recordCurrentFrame = frame;
        }

        public void StartRecording()
        {
            recordAnimationSets.Clear();
            foreach (GameObject item in Selection.selection.Values)
            {
                new CommandClearAnimations(item).Submit();

                AnimationSet animationSet = new AnimationSet();
                animationSet.xPosition = new AnimationChannel("location", 0);
                animationSet.yPosition = new AnimationChannel("location", 1);
                animationSet.zPosition = new AnimationChannel("location", 2);
                animationSet.xRotation = new AnimationChannel("rotation_euler", 0);
                animationSet.yRotation = new AnimationChannel("rotation_euler", 1);
                animationSet.zRotation = new AnimationChannel("rotation_euler", 2);
                CameraController cameraController = item.GetComponent<CameraController>();
                if (null != cameraController)
                {
                    animationSet.lens = new AnimationChannel("lens", -1);
                    animationSet.parametersController = cameraController;
                }

                LightController lcontroller = item.GetComponent<LightController>();
                if (null != lcontroller)
                {
                    animationSet.energy = new AnimationChannel("energy", -1);
                    animationSet.RColor = new AnimationChannel("color", 0);
                    animationSet.GColor = new AnimationChannel("color", 1);
                    animationSet.BColor = new AnimationChannel("color", 2);
                    animationSet.parametersController = lcontroller;
                }

                if (null == item.GetComponent<ParametersController>())
                {
                    animationSet.xScale = new AnimationChannel("scale", 0);
                    animationSet.yScale = new AnimationChannel("scale", 1);
                    animationSet.zScale = new AnimationChannel("scale", 2);
                }

                recordAnimationSets[item] = animationSet;
            }

            recordCurrentFrame = GlobalState.Animation.currentFrame - 1;

            MixerClient.GetInstance().SendEvent<int>(MessageType.Play, 0);
        }

        private void SendDeleteKeyInfo(string objectName, string channelName, int channelIndex, int frame)
        {
            SetKeyInfo keyInfo = new SetKeyInfo()
            {
                objectName = objectName,
                channelName = channelName,
                channelIndex = channelIndex,
                frame = frame,
                value = 0.0f
            };
            MixerClient.GetInstance().SendEvent<SetKeyInfo>(MessageType.RemoveKeyframe, keyInfo);
        }

        public void StopRecording()
        {
            CommandGroup group = new CommandGroup("Record Animation");
            try
            {
                foreach (var item in recordAnimationSets)
                {
                    new CommandRecordAnimations(item.Key, item.Value).Submit();
                }
            }
            finally
            {
                group.Submit();
                recordAnimationSets.Clear();
            }
        }

        public void Pause()
        {
            MixerClient.GetInstance().SendEvent<int>(MessageType.Pause, 0);
        }

        public void MoveKeyframe(GameObject gObject, string channelName, int channelIndex, int frame, int newFrame)
        {
            MoveKeyInfo keyInfo = new MoveKeyInfo()
            {
                objectName = gObject.name,
                channelName = channelName,
                channelIndex = channelIndex,
                frame = frame,
                newFrame = newFrame
            };
            MixerClient.GetInstance().SendEvent<MoveKeyInfo>(MessageType.MoveKeyframe, keyInfo);
        }

        public void MoveSelectionKeyframes(int frame, int newFrame)
        {
            foreach (GameObject item in Selection.selection.Values)
            {
                MoveKeyframe(item, "location", 0, frame, newFrame);
                MoveKeyframe(item, "location", 1, frame, newFrame);
                MoveKeyframe(item, "location", 2, frame, newFrame);
                MoveKeyframe(item, "rotation_euler", 0, frame, newFrame);
                MoveKeyframe(item, "rotation_euler", 1, frame, newFrame);
                MoveKeyframe(item, "rotation_euler", 2, frame, newFrame);

                CameraController controller = item.GetComponent<CameraController>();
                if (null != controller)
                {
                    MoveKeyframe(item, "lens", -1, frame, newFrame);
                }

                LightController lcontroller = item.GetComponent<LightController>();
                if (null != lcontroller)
                {
                    MoveKeyframe(item, "energy", -1, frame, newFrame);
                    MoveKeyframe(item, "color", 0, frame, newFrame);
                    MoveKeyframe(item, "color", 1, frame, newFrame);
                    MoveKeyframe(item, "color", 2, frame, newFrame);
                }

                ParametersController pController = item.GetComponent<ParametersController>();
                if (null == pController)
                {
                    MoveKeyframe(item, "scale", 0, frame, newFrame);
                    MoveKeyframe(item, "scale", 1, frame, newFrame);
                    MoveKeyframe(item, "scale", 2, frame, newFrame);
                }
                MixerClient.GetInstance().SendEvent<string>(MessageType.QueryAnimationData, item.name);
            }
        }


        public void AddKeyframe(GameObject gObject, string channelName, int channelIndex, int frame, float value, Interpolation interpolation)
        {
            SendKeyInfo(gObject.name, channelName, channelIndex, frame, value, interpolation);
        }

        public void RemoveKeyframe(GameObject gObject, string channelName, int channelIndex, int frame)
        {
            SendDeleteKeyInfo(gObject.name, channelName, channelIndex, frame);
        }

        public void RemoveSelectionKeyframes()
        {
            int currentFrame = GlobalState.Animation.currentFrame;
            foreach (GameObject item in Selection.selection.Values)
            {
                RemoveKeyframe(item, "location", 0, currentFrame);
                RemoveKeyframe(item, "location", 1, currentFrame);
                RemoveKeyframe(item, "location", 2, currentFrame);
                RemoveKeyframe(item, "rotation_euler", 0, currentFrame);
                RemoveKeyframe(item, "rotation_euler", 1, currentFrame);
                RemoveKeyframe(item, "rotation_euler", 2, currentFrame);

                CameraController controller = item.GetComponent<CameraController>();
                if (null != controller)
                {
                    RemoveKeyframe(item, "lens", -1, currentFrame);
                }

                LightController lcontroller = item.GetComponent<LightController>();
                if (null != lcontroller)
                {
                    RemoveKeyframe(item, "energy", -1, currentFrame);
                    RemoveKeyframe(item, "color", 0, currentFrame);
                    RemoveKeyframe(item, "color", 1, currentFrame);
                    RemoveKeyframe(item, "color", 2, currentFrame);
                }

                ParametersController pController = item.GetComponent<ParametersController>();
                if (null == pController)
                {
                    RemoveKeyframe(item, "scale", 0, currentFrame);
                    RemoveKeyframe(item, "scale", 1, currentFrame);
                    RemoveKeyframe(item, "scale", 2, currentFrame);
                }
                MixerClient.GetInstance().SendEvent<string>(MessageType.QueryAnimationData, item.name);
            }
        }
    }
}