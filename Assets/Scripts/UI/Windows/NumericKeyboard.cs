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

using System.Globalization;
using System.Text.RegularExpressions;

namespace VRtist
{
    public class NumericKeyboard : AbstractKeyboard
    {
        public FloatChangedEvent onSubmitEvent = new FloatChangedEvent();
        private static Regex floatRegex = new Regex("[-+]?([0-9]*[.])?[0-9]*", RegexOptions.Compiled);

        public override void OnSubmit()
        {
            // User input can be:
            //   "-.56", ".45", "3.", "-45.89", "98" ("." & "-" are invalid inputs)
            if (float.TryParse(textContent, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float value))
            {
                onSubmitEvent.Invoke(value);
                if (autoClose)
                {
                    ToolsUIManager.Instance.CloseNumericKeyboard();
                }
            }
        }

        public override void OnKeyFired(char character)
        {
            if (Selected)
            {
                Clear();
                Selected = false;
            }
            string text = textContent + character;
            if (floatRegex.IsMatch(text))
            {
                textContent = text;
                contentLabel.Text = textContent;
                onKeyStrokeEvent.Invoke(character);
            }
        }

        public void SetValue(float? value)
        {
            if (null != value)
            {
                textContent = value.ToString();
                contentLabel.Text = textContent;
            }
            else
            {
                textContent = "";
                contentLabel.Text = "";
            }
        }
    }
}
