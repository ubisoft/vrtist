using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace VRtist
{
    public class Sky : SelectorBase
    {
        private UIButton topButton;
        private UIButton middleButton;
        private UIButton bottomButton;
        static private Volume volume;

        void Start()
        {
            Init();
        }

        protected override void OnEnable()
        {
            if (null == topButton)
            {
                topButton = panel.Find("TopButton").GetComponent<UIButton>();
                middleButton = panel.Find("MiddleButton").GetComponent<UIButton>();
                bottomButton = panel.Find("BottomButton").GetComponent<UIButton>();

                topButton.ImageColor = GlobalState.Settings.sky.topColor;
                middleButton.ImageColor = GlobalState.Settings.sky.middleColor;
                bottomButton.ImageColor = GlobalState.Settings.sky.bottomColor;
            }

            GlobalState.colorChangedEvent.AddListener(OnColorPickerChanged);
        }

        protected override void OnDisable()
        {
            try
            {
                GlobalState.colorChangedEvent.RemoveListener(OnColorPickerChanged);
            }
            catch (Exception _)
            {
                // Nothing
            }
        }

        private void SetColorCheckerColor(Color color)
        {
            GlobalState.CurrentColor = color;
        }

        private void OnColorPickerChanged(Color color)
        {
            if (topButton.Checked) { topButton.ImageColor = color; }
            else if (middleButton.Checked) { middleButton.ImageColor = color; }
            else if (bottomButton.Checked) { bottomButton.ImageColor = color; }

            SetSkyColors(topButton.ImageColor, middleButton.ImageColor, bottomButton.ImageColor);
            MixerClient.GetInstance().SendEvent<SkySettings>(MessageType.Sky, new SkySettings { topColor = topButton.ImageColor, middleColor = middleButton.ImageColor, bottomColor = bottomButton.ImageColor });
        }

        public static void SetSkyColors(Color topColor, Color middleColor, Color bottomColor)
        {
            if (null == volume) { volume = Utils.FindVolume(); }
            GradientSky sky;
            volume.profile.TryGet(out sky);
            sky.top.value = topColor;
            sky.middle.value = middleColor;
            sky.bottom.value = bottomColor;

            GlobalState.Settings.sky.topColor = topColor;
            GlobalState.Settings.sky.middleColor = middleColor;
            GlobalState.Settings.sky.bottomColor = bottomColor;
        }

        public void OnTopButtonClicked()
        {
            topButton.Checked = true;
            middleButton.Checked = false;
            bottomButton.Checked = false;
            SetColorCheckerColor(topButton.ImageColor);
        }

        public void OnMiddleButtonClicked()
        {
            topButton.Checked = false;
            middleButton.Checked = true;
            bottomButton.Checked = false;
            SetColorCheckerColor(middleButton.ImageColor);
        }

        public void OnBottomButtonClicked()
        {
            topButton.Checked = false;
            middleButton.Checked = false;
            bottomButton.Checked = true;
            SetColorCheckerColor(bottomButton.ImageColor);
        }
    }
}
