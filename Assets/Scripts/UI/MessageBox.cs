using System.Collections;
using TMPro;
using UnityEngine;

namespace VRtist
{
    public class MessageBox : MonoBehaviour
    {
        TextMeshProUGUI text = null;

        void Init()
        {
            if (null == text)
            {
                text = transform.Find("Canvas/Panel/Text").GetComponent<TextMeshProUGUI>();
            }
        }

        public void SetText(string text)
        {
            Init();
            this.text.text = text;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void ShowMessage(string text, float duration = -1)
        {
            SetText(text);
            SetVisible(true);
            if (duration > 0)
            {
                StartCoroutine(AutoHide(duration));
            }
        }

        IEnumerator AutoHide(float duration)
        {
            yield return new WaitForSeconds(duration);
            SetVisible(false);
        }
    }
}
