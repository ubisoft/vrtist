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
            recordButton = transform.Find("RecordButton").GetComponent<UIButton>();
            GlobalState.Animation.onAnimationStateEvent.AddListener(OnAnimationStateChanged);
        }

        private void OnAnimationStateChanged(AnimationState state)
        {
            playButton.Checked = false;
            recordButton.Checked = false;

            switch (state)
            {
                case AnimationState.Playing: playButton.Checked = true; break;
                case AnimationState.Recording: recordButton.Checked = true; playButton.Checked = true; break;
            }
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

        public void OnRecordOrStop(bool record)
        {
            if (record)
            {
                GlobalState.Animation.Record();
            }
            else
            {
                GlobalState.Animation.Pause();
            }
        }

        public void OnPlayOrPause(bool play)
        {
            if (play)
            {
                GlobalState.Animation.Play();
            }
            else
            {
                GlobalState.Animation.Pause();
            }
        }

    }
}
