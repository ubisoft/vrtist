using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class AnimationSet
    {
        public AnimationChannel xPosition;
        public AnimationChannel yPosition;
        public AnimationChannel zPosition;
        public AnimationChannel xRotation;
        public AnimationChannel yRotation;
        public AnimationChannel zRotation;
        public AnimationChannel lens;
    }

    public class AnimationController : MonoBehaviour
    {
        private Dictionary<GameObject, AnimationSet> animationSets = new Dictionary<GameObject, AnimationSet>();
        private int recordCurrentFrame = 0;

        void Update()
        {
            HandleRecord();
        }

        public void HandleRecord()
        {
            if (GlobalState.Instance.recordState != GlobalState.RecordState.Recording)
                return;
            int frame = GlobalState.currentFrame;
            if (frame != recordCurrentFrame)
            {
                foreach (var item in animationSets)
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

                    item.Value.lens.keys.Add(new AnimationKey(frame, item.Key.GetComponent<CameraController>().focal));
                }
            }
            recordCurrentFrame = frame;
        }

        public void OnCountdownFinished()
        {
            animationSets.Clear();
            foreach (GameObject item in Selection.selection.Values)
            {
                CameraController cameraController = item.GetComponent<CameraController>();
                if (null != cameraController)
                {
                    cameraController.ClearAnimations();

                    AnimationSet animationSet = new AnimationSet();
                    animationSet.xPosition = new AnimationChannel("location[0]");
                    animationSet.yPosition = new AnimationChannel("location[1]");
                    animationSet.zPosition = new AnimationChannel("location[2]");
                    animationSet.xRotation = new AnimationChannel("rotation_euler[0]");
                    animationSet.yRotation = new AnimationChannel("rotation_euler[1]");
                    animationSet.zRotation = new AnimationChannel("rotation_euler[2]");
                    animationSet.lens = new AnimationChannel("lens");
                    animationSets[item] = animationSet;
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

        private void SendDeleteKeyInfo(ParametersController controller, string channelName, int channelIndex)
        {
            SetKeyInfo keyInfo = new SetKeyInfo()
            {
                objectName = controller.gameObject.name,
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
            foreach (var item in animationSets)
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
            animationSets.Clear();
        }

        public void AddKeyframe()
        {
            foreach (GameObject item in Selection.selection.Values)
            {
                CameraController controller = item.GetComponent<CameraController>();
                if (null != controller)
                {
                    string name = item.name;
                    int frame = GlobalState.currentFrame;
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
                CameraController controller = item.GetComponent<CameraController>();
                if (null != controller)
                {
                    SendDeleteKeyInfo(controller, "location", 0);
                    SendDeleteKeyInfo(controller, "location", 1);
                    SendDeleteKeyInfo(controller, "location", 2);
                    SendDeleteKeyInfo(controller, "rotation_euler", 0);
                    SendDeleteKeyInfo(controller, "rotation_euler", 1);
                    SendDeleteKeyInfo(controller, "rotation_euler", 2);
                    SendDeleteKeyInfo(controller, "lens", -1);
                    NetworkClient.GetInstance().SendEvent<string>(MessageType.QueryObjectData, controller.gameObject.name);
                }
            }
        }
    }
}