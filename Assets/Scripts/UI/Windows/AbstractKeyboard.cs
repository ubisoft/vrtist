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
