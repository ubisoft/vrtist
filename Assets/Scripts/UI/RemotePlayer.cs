using System.Collections.Generic;
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
        private static Dictionary<GameObject, AnimationSet> animationSets = new Dictionary<GameObject, AnimationSet>();
        private static int recordCurrentFrame = 0;

        UIButton playButton = null;
        UIButton recordButton = null;

        public void Start()
        {
            playButton = transform.Find("PlayButton").GetComponent<UIButton>();
            GlobalState.Instance.onPlayingEvent.AddListener(OnPlayingChanged);

            recordButton = transform.Find("RecordButton").GetComponent<UIButton>();
            GlobalState.Instance.onRecordEvent.AddListener(OnRecordingChanged);
        }

        private void OnPlayingChanged(bool value)
        {
            playButton.Checked = value;
        }

        public void OnPlay()
        {
            NetworkClient.GetInstance().SendEvent<int>(MessageType.Play, 0);
            GlobalState.Instance.SetPlaying(true);
        }

        public void OnPause()
        {
            NetworkClient.GetInstance().SendEvent<int>(MessageType.Pause, 0);
            GlobalState.Instance.SetPlaying(false);
            OnStopRecord();
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

        private void OnRecordingChanged(bool value)
        {
            recordButton.Checked = value;
        }
        public void HandleRecord()
        {
            if (GlobalState.Instance.isRecording != GlobalState.RecordState.Recording)
                return;
            int frame = dopesheet.CurrentFrame;
            if (frame != recordCurrentFrame)
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
            if (GlobalState.Instance.isRecording != GlobalState.RecordState.Stopped)
                return;

            GlobalState.Instance.StartRecording(true);
            GlobalState.Instance.onCountdownFinished.AddListener(OnCountdownFinished);
        }

        public void OnCountdownFinished()
        {
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

            recordCurrentFrame = dopesheet.CurrentFrame - 1;
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
            if (GlobalState.Instance.isRecording == GlobalState.RecordState.Stopped)
                return;

            if (GlobalState.Instance.isRecording == GlobalState.RecordState.Preroll)
            {
                animationSets.Clear();
                GlobalState.Instance.StartRecording(false);
                return;
            }

            if (GlobalState.Instance.isPlaying)
                OnPause();

            GlobalState.Instance.StartRecording(false);

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
