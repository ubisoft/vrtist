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
    public class KeyboardTouch : MonoBehaviour
    {
        public char keyValue = 'A';
        private UIButton touch = null;
        private AbstractKeyboard kb = null;

        //public CharChangedEvent onKeyTouched = new CharChangedEvent();

        void Start()
        {
            touch = GetComponent<UIButton>();
            if (touch != null)
            {
                //touch.onClickEvent.AddListener(OnTouchPressed);
                touch.onReleaseEvent.AddListener(OnTouchPressed);
                //keyValue = touch.textContent[0]; // get key directly from the text component of the button? what if we use an icon?
            }

            kb = GetComponentInParent<AbstractKeyboard>();
        }

        public void OnTouchPressed()
        {
            //onKeyTouched.Invoke(keyValue);
            kb.OnKeyFired(keyValue); // direct call, dont use events.
        }
    }
}
