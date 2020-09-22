using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VRtist
{
    public class SceneHelper : MonoBehaviour
    {
        private TextMeshProUGUI text;
        private Image image;

        private Sprite playImage;
        private Sprite recordImage;

        void Start()
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
            image = transform.Find("Canvas/Panel/Image").GetComponent<Image>();

            playImage = UIUtils.LoadIcon("player_play");
            recordImage = UIUtils.LoadIcon("player_record");

            GlobalState.Instance.onPlayingEvent.AddListener(OnPlayingChanged);
            GlobalState.Instance.onRecordEvent.AddListener(OnRecordingChanged);

            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!GlobalState.Instance.isPlaying && GlobalState.Instance.recordState == GlobalState.RecordState.Stopped)
            {
                return;
            }

            text.text = GlobalState.currentFrame.ToString();
        }

        private void OnPlayingChanged(bool value)
        {
            gameObject.SetActive(value);
            // We receive play true when we record
            if (value && GlobalState.Instance.recordState == GlobalState.RecordState.Stopped)
            {
                image.sprite = playImage;
            }
        }

        private void OnRecordingChanged(bool value)
        {
            gameObject.SetActive(value);
            if (value)
            {
                image.sprite = recordImage;
            }
        }
    }
}
