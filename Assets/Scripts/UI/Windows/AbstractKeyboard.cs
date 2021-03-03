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
    public class AbstractKeyboard : MonoBehaviour
    {
        protected UILabel contentLabel = null;
        protected string textContent = "";

        public bool autoClose = true;
        private bool selected = false;
        public bool Selected
        {
            get
            {
                return selected;
            }
            set
            {
                selected = value;
                if (null != contentLabel)
                {
                    contentLabel.Selected = value;
                }
            }
        }

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public CharChangedEvent onKeyStrokeEvent = new CharChangedEvent();

        protected virtual void Start()
        {
            Transform mainPanel = transform.Find("MainPanel");
            if (mainPanel != null)
            {
                contentLabel = mainPanel.Find("TextContentLabel")?.GetComponent<UILabel>();
            }
        }

        public virtual void OnKeyFired(char character)
        {
            if (Selected)
            {
                Clear();
                Selected = false;
            }
            textContent += character;
            contentLabel.Text = textContent;

            onKeyStrokeEvent.Invoke(character);
        }

        public virtual void OnBackspace()
        {
            if (Selected)
            {
                Clear();
                Selected = false;
            }
            if (textContent.Length > 0)
            {
                if (textContent.Length > 1)
                    textContent = textContent.Substring(0, textContent.Length - 1);
                else
                    textContent = "";
            }

            contentLabel.Text = textContent;
        }

        public virtual void OnSubmit() { }

        public virtual void Clear()
        {
            textContent = "";
            contentLabel.Text = textContent;
        }

        public virtual void OnDeselect()
        {
            Selected = false;
        }
    }
}
