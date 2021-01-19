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
            GlobalState.Animation.CurrentFrame = dopesheet.GetNextKeyFrame();
        }

        public void OnPrevKey()
        {
            GlobalState.Animation.CurrentFrame = dopesheet.GetPreviousKeyFrame();
        }

        public void OnNextFrame()
        {
            GlobalState.Animation.CurrentFrame = Mathf.Min(dopesheet.LocalLastFrame, GlobalState.Animation.CurrentFrame + 1);
        }

        public void OnPrevFrame()
        {
            GlobalState.Animation.CurrentFrame = Mathf.Max(dopesheet.LocalFirstFrame, GlobalState.Animation.CurrentFrame - 1);
        }

        public void OnFirstFrame()
        {
            GlobalState.Animation.CurrentFrame = dopesheet.LocalFirstFrame;
        }

        public void OnLastFrame()
        {
            GlobalState.Animation.CurrentFrame = dopesheet.LocalLastFrame;
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
