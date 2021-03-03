/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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
