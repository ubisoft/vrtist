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

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    public class SceneHelper : MonoBehaviour
    {
        private TextMeshProUGUI text;
        private Image image;

        private Sprite playImage;
        private Sprite recordImage;

        void Start()
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
            image = transform.Find("Canvas/Panel/Image").GetComponent<Image>();

            playImage = UIUtils.LoadIcon("player_play");
            recordImage = UIUtils.LoadIcon("player_record");

            GlobalState.Animation.onAnimationStateEvent.AddListener(OnAnimationStateChanged);

            gameObject.SetActive(false);
        }

        void Update()
        {
            if (GlobalState.Animation.animationState != AnimationState.AnimationRecording && GlobalState.Animation.animationState != AnimationState.Playing)
            {
                return;
            }
            text.text = GlobalState.Animation.CurrentFrame.ToString();
        }

        private void OnAnimationStateChanged(AnimationState state)
        {
            bool playOrRecord = GlobalState.Animation.animationState == AnimationState.AnimationRecording || GlobalState.Animation.animationState == AnimationState.Playing;
            gameObject.SetActive(playOrRecord);

            switch (state)
            {
                case AnimationState.Playing: image.sprite = playImage; break;
                case AnimationState.AnimationRecording: image.sprite = recordImage; break;
            }
        }
    }
}
