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

            GlobalState.Animation.onAnimationStateEvent.AddListener(OnAnimationStateChanged);

            gameObject.SetActive(false);
        }

        void Update()
        {
            if (GlobalState.Animation.animationState != AnimationState.Recording && GlobalState.Animation.animationState != AnimationState.Playing)
            {
                return;
            }
            text.text = GlobalState.Animation.currentFrame.ToString();
        }

        private void OnAnimationStateChanged(AnimationState state)
        {
            bool playOrRecord = GlobalState.Animation.animationState == AnimationState.Recording || GlobalState.Animation.animationState == AnimationState.Playing;
            gameObject.SetActive(playOrRecord);

            switch (state)
            {
                case AnimationState.Playing: image.sprite = playImage; break;
                case AnimationState.Recording: image.sprite = recordImage; break;
            }
        }
    }
}
