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
