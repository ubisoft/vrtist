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

        public void SendKeyInfo(ParametersController controller, string channelName, int channelIndex, int frame, float value)
        {
            SetKeyInfo keyInfo = new SetKeyInfo()
            {
                objectName = controller.gameObject.name,
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

        private void SendAnimationChannel(ParametersController controller, AnimationChannel animationChannel)
        {
            foreach (AnimationKey key in animationChannel.keys)
            {
                animationChannel.GetChannelInfo(out string channelName, out int channelIndex);
                SendKeyInfo(controller, channelName, channelIndex, key.time, key.value);
            }
        }

        public void ApplyAnimations()
        {
            foreach (var item in animationSets)
            {
                GameObject gObject = item.Key;
                string name = gObject.name;
                ParametersController controller = gObject.GetComponent<ParametersController>();
                SendAnimationChannel(controller, item.Value.xPosition);
                SendAnimationChannel(controller, item.Value.yPosition);
                SendAnimationChannel(controller, item.Value.zPosition);
                SendAnimationChannel(controller, item.Value.xRotation);
                SendAnimationChannel(controller, item.Value.yRotation);
                SendAnimationChannel(controller, item.Value.zRotation);
                SendAnimationChannel(controller, item.Value.lens);
                NetworkClient.GetInstance().SendQueryObjectData(name);
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
                    string name = controller.gameObject.name;
                    int frame = GlobalState.currentFrame;
                    SendKeyInfo(controller, "location", 0, frame, controller.transform.localPosition.x);
                    SendKeyInfo(controller, "location", 1, frame, controller.transform.localPosition.y);
                    SendKeyInfo(controller, "location", 2, frame, controller.transform.localPosition.z);
                    Quaternion q = controller.transform.localRotation;
                    // convert to ZYX euler
                    Vector3 angles = Maths.ThreeAxisRotation(q);
                    SendKeyInfo(controller, "rotation_euler", 0, frame, angles.x);
                    SendKeyInfo(controller, "rotation_euler", 1, frame, angles.y);
                    SendKeyInfo(controller, "rotation_euler", 2, frame, angles.z);
                    SendKeyInfo(controller, "lens", -1, frame, (controller as CameraController).focal);

                    NetworkClient.GetInstance().SendEvent<string>(MessageType.QueryObjectData, controller.gameObject.name);
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