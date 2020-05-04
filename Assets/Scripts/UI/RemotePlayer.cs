using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;
using UnityEngine;

namespace VRtist
{
    public class FrameInfo
    {
        public int frame;
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
    }

    public class RemotePlayer : MonoBehaviour
    {
        public Dopesheet dopesheet = null;
        private Dictionary<GameObject, AnimationSet> animationSets = new Dictionary<GameObject, AnimationSet>();
        private int recordCurrentFrame = 0;
        private bool isRecording = false;

        public void OnPlay()
        {
            NetworkClient.GetInstance().SendEvent<int>(MessageType.Play, 0);
        }

        public void OnPause()
        {
            NetworkClient.GetInstance().SendEvent<int>(MessageType.Pause, 0);
        }

        public void OnPlayOrPause(bool play)
        {
            if (play)
            {
                OnPlay();
            }
            else
            {
                OnPause();
            }
        }

        public void OnNextKey()
        {
            int keyTime = dopesheet.GetNextKeyFrame();
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnPrevKey()
        {
            int keyTime = dopesheet.GetPreviousKeyFrame();
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnNextFrame()
        {
            int keyTime = Mathf.Min(dopesheet.LastFrame, dopesheet.CurrentFrame + 1);
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnPrevFrame()
        {
            int keyTime = Mathf.Max(dopesheet.FirstFrame, dopesheet.CurrentFrame - 1);
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnFirstFrame()
        {
            int keyTime = dopesheet.FirstFrame;
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnLastFrame()
        {
            int keyTime = dopesheet.LastFrame;
            FrameInfo info = new FrameInfo() { frame = keyTime };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void HandleRecord()
        {
            if (!isRecording)
                return;
            int frame = dopesheet.CurrentFrame;
            if (frame > recordCurrentFrame)
            {
                foreach(var item in animationSets)
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

        public void Update()
        {
            HandleRecord();
        }

        public void OnBeginRecord()
        {
            if (isRecording)
                return;

            foreach(GameObject item in Selection.selection.Values)
            {
                if (null != item.GetComponent<CameraController>())
                {
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

            recordCurrentFrame = dopesheet.CurrentFrame - 1;
            isRecording = true;
            OnPlay();
        }

        private void SendAnimationChannel(string objectName, AnimationChannel animationChannel)
        {
            foreach (AnimationKey key in animationChannel.keys)
            {
                animationChannel.GetChannelInfo(out string channelName, out int channelIndex);
                dopesheet.SendKeyInfo(objectName, channelName, channelIndex, key.time, key.value);
            }
        }

        public void OnStopRecord()
        {
            if (!isRecording)
                return;

            OnPause();

            foreach (var item in animationSets)
            {
                GameObject gObject = item.Key;
                string name = gObject.name;
                SendAnimationChannel(name, item.Value.xPosition);
                SendAnimationChannel(name, item.Value.yPosition);
                SendAnimationChannel(name, item.Value.zPosition);
                SendAnimationChannel(name, item.Value.xRotation);
                SendAnimationChannel(name, item.Value.yRotation);
                SendAnimationChannel(name, item.Value.zRotation);
                SendAnimationChannel(name, item.Value.lens);
                NetworkClient.GetInstance().SendQueryObjectData(name);
            }
            isRecording = false;
            animationSets.Clear();
        }

        public void OnRecordOrStop(bool record)
        {
            if (record)
            {
                OnBeginRecord();
            }
            else
            {
                OnStopRecord();
            }
        }
    }
}
