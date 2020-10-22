using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class RemotePlayer : MonoBehaviour
    {
        public Dopesheet dopesheet = null;

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

        public void OnNextKey()
        {
            int keyTime = dopesheet.GetNextKeyFrame();
            FrameInfo info = new FrameInfo() { frame = keyTime };
            MixerClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnPrevKey()
        {
            int keyTime = dopesheet.GetPreviousKeyFrame();
            FrameInfo info = new FrameInfo() { frame = keyTime };
            MixerClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnNextFrame()
        {
            int keyTime = Mathf.Min(dopesheet.LastFrame, dopesheet.CurrentFrame + 1);
            FrameInfo info = new FrameInfo() { frame = keyTime };
            MixerClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnPrevFrame()
        {
            int keyTime = Mathf.Max(dopesheet.FirstFrame, dopesheet.CurrentFrame - 1);
            FrameInfo info = new FrameInfo() { frame = keyTime };
            MixerClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnFirstFrame()
        {
            int keyTime = dopesheet.FirstFrame;
            FrameInfo info = new FrameInfo() { frame = keyTime };
            MixerClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnLastFrame()
        {
            int keyTime = dopesheet.LastFrame;
            FrameInfo info = new FrameInfo() { frame = keyTime };
            MixerClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        private void OnRecordingChanged(bool value)
        {
            recordButton.Checked = value;
        }

        public void Update()
        {            
        }


        public void OnRecordOrStop(bool record)
        {
            if (record)
            {
                GlobalState.Instance.Record();
            }
            else
            {
                GlobalState.Instance.Pause();
            }
        }

        public void OnPlayOrPause(bool play)
        {
            if (play)
            {
                GlobalState.Instance.Play();
            }
            else
            {
                GlobalState.Instance.Pause();
            }
        }

    }
}
