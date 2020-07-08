using UnityEngine;

namespace VRtist
{
    public class KeyboardTouch : MonoBehaviour
    {
        public char keyValue = 'A';
        private UIButton touch = null;
        private Keyboard kb = null;

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

            // TODO: pas tres future proof
            kb = transform.parent.parent.parent.GetComponent<Keyboard>();
        }

        public void OnTouchPressed()
        {
            //onKeyTouched.Invoke(keyValue);
            kb.OnKeyFired(keyValue); // direct call, dont use events.
        }
    }
}
