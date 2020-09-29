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
