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
    public class Keyboard : AbstractKeyboard
    {
        private Transform mainPanel = null;
        private Transform alphaLowerPanel = null;
        private Transform alphaUpperPanel = null;
        private Transform digitsPanel = null;
        private Transform symbolsPanel = null;
        private UIButton shiftButton = null;
        private UIButton symbolsButton = null;

        public StringChangedEvent onSubmitEvent = new StringChangedEvent();

        protected override void Start()
        {
            mainPanel = transform.Find("MainPanel");
            if (mainPanel != null)
            {
                alphaLowerPanel = mainPanel.Find("AlphaLowerPanel");
                alphaUpperPanel = mainPanel.Find("AlphaUpperPanel");
                digitsPanel = mainPanel.Find("DigitsPanel");
                symbolsPanel = mainPanel.Find("SymbolsPanel");
                shiftButton = mainPanel.Find("ShiftButton")?.GetComponent<UIButton>();
                symbolsButton = mainPanel.Find("SymbolsButton")?.GetComponent<UIButton>();
                contentLabel = mainPanel.Find("TextContentLabel")?.GetComponent<UILabel>();

                if (alphaLowerPanel == null
                 || alphaUpperPanel == null
                 || digitsPanel == null
                 || symbolsPanel == null
                 || shiftButton == null
                 || symbolsButton == null
                 || contentLabel == null)
                {
                    Debug.LogError("Panels missing from Keyboard");
                }
                else
                {
                    digitsPanel.gameObject.SetActive(true);
                    alphaLowerPanel.gameObject.SetActive(true);

                    alphaUpperPanel.gameObject.SetActive(false);
                    symbolsPanel.gameObject.SetActive(false);

                    shiftButton.Checked = false;
                    symbolsButton.Checked = false;
                }
            }
        }

        public void OnShift(bool isChecked)
        {
            alphaLowerPanel.gameObject.SetActive(!symbolsButton.Checked && !isChecked);

            alphaUpperPanel.gameObject.SetActive(!symbolsButton.Checked && isChecked);

            symbolsPanel.gameObject.SetActive(symbolsButton.Checked);
        }

        public void OnSymbols(bool isChecked)
        {
            symbolsPanel.gameObject.SetActive(isChecked);

            alphaLowerPanel.gameObject.SetActive(!isChecked && !shiftButton.Checked);

            alphaUpperPanel.gameObject.SetActive(!isChecked && shiftButton.Checked);
        }

        public void OnSpacebar()
        {
            OnKeyFired(' ');
        }

        public override void OnSubmit()
        {
            onSubmitEvent.Invoke(textContent);
            if (autoClose)
            {
                ToolsUIManager.Instance.CloseKeyboard();
            }
        }

        public void SetText(string text)
        {
            textContent = text;
            contentLabel.Text = text;
        }
    }
}
