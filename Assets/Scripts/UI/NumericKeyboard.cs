using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VRtist
{
    public class NumericKeyboard : AbstractKeyboard
    {
        public FloatChangedEvent onSubmitEvent = new FloatChangedEvent();
        private Transform mainPanel = null;
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
