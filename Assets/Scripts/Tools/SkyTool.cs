﻿using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace VRtist
{
    public class SkyTool : SelectorBase
    {
        private UIButton topButton;
        private UIButton middleButton;
        private UIButton bottomButton;
        private UIDynamicList gradientList;
        SkySettings previousSky;

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
            GlobalState.colorReleasedEvent.AddListener(OnColorPickerReleased);
            GlobalState.colorClickedEvent.AddListener(OnColorPickerPressed);
            GlobalState.Instance.skyChangedEvent.AddListener(OnSkyChanged);
            

            if (null == gradientList)
            {
                gradientList = panel.Find("ListPanel/List").GetComponent<UIDynamicList>();
                gradientList.ItemClickedEvent += OnListItemClicked;

                // Fill list from Settings
                RebuildGradientList();
                gradientList.CurrentIndex = -1;
            }
        }

        protected override void OnDisable()
        {
            try
            {
                GlobalState.colorChangedEvent.RemoveListener(OnColorPickerChanged);
                GlobalState.colorReleasedEvent.RemoveListener(OnColorPickerReleased);
                GlobalState.colorClickedEvent.RemoveListener(OnColorPickerPressed);
                GlobalState.Instance.skyChangedEvent.RemoveListener(OnSkyChanged);
            }
            catch (Exception _)
            {
                // Nothing
            }
        }

        private void OnListItemClicked(object sender, IndexedGameObjectArgs args)
        {
            GameObject item = args.gobject;
            GradientItem gradientItem = item.GetComponent<GradientItem>();
            SkySettings itemSky = gradientItem.Colors;

            SkySettings oldSky = new SkySettings { topColor = topButton.ImageColor, middleColor = middleButton.ImageColor, bottomColor = bottomButton.ImageColor };
            SetSkyColors(oldSky, itemSky);
        }

        private void OnColorPickerChanged(Color color)
        {
            if (topButton.Checked) { topButton.ImageColor = color; }
            else if (middleButton.Checked) { middleButton.ImageColor = color; }
            else if (bottomButton.Checked) { bottomButton.ImageColor = color; }

            SkySettings sky = new SkySettings { topColor = topButton.ImageColor, middleColor = middleButton.ImageColor, bottomColor = bottomButton.ImageColor };
            GlobalState.Instance.SkySettings = sky;
        }

        private void OnColorPickerPressed()
        {
            previousSky = GlobalState.Instance.SkySettings;
        }

        private void OnColorPickerReleased(Color _)
        {
            SetSkyColors(previousSky, GlobalState.Instance.SkySettings);
        }

        void OnSkyChanged(SkySettings skySettings)
        {
            // update buttons
            topButton.ImageColor = skySettings.topColor;
            middleButton.ImageColor = skySettings.middleColor;
            bottomButton.ImageColor = skySettings.bottomColor;

            Color color = skySettings.topColor;
            if (middleButton.Checked)
                color = skySettings.middleColor;
            if (bottomButton.Checked)
                color = skySettings.bottomColor;
            GlobalState.CurrentColor = color;
        }

        public static void SetSkyColors(SkySettings oldSky, SkySettings newSky)
        {
            GlobalState.Settings.sky = newSky;
            new CommandSky(oldSky, newSky).Submit();
        }

        public void OnTopButtonClicked()
        {
            topButton.Checked = true;
            middleButton.Checked = false;
            bottomButton.Checked = false;

            GlobalState.CurrentColor = topButton.ImageColor;
        }

        public void OnMiddleButtonClicked()
        {
            topButton.Checked = false;
            middleButton.Checked = true;
            bottomButton.Checked = false;
            GlobalState.CurrentColor = middleButton.ImageColor;
        }

        public void OnBottomButtonClicked()
        {
            topButton.Checked = false;
            middleButton.Checked = false;
            bottomButton.Checked = true;
            GlobalState.CurrentColor = bottomButton.ImageColor;
        }

        public void OnSaveNewGradientButtonClicked()
        {
            GlobalState.Settings.skies.Add(GlobalState.Settings.sky);
            AddGradient(GlobalState.Settings.sky);
        }

        public void OnSaveCurrentGradientButtonClicked()
        {
            if (gradientList.CurrentIndex >= 0)
            {
                GlobalState.Settings.skies[gradientList.CurrentIndex] = GlobalState.Settings.sky;

                GradientItem currentItem = gradientList.GetItems()[gradientList.CurrentIndex].Content.GetComponent<GradientItem>();
                currentItem.Colors = GlobalState.Settings.sky;
            }
        }

        public void OnDeleteGradientItem()
        {
            GlobalState.Settings.skies.RemoveAt(gradientList.CurrentIndex);

            var currentDLItem = gradientList.GetItems()[gradientList.CurrentIndex];
            gradientList.RemoveItem(currentDLItem);

            gradientList.CurrentIndex = -1; // TODO: select the previous one? the one at the same index?
            RebuildGradientList();
        }

        public void OnDuplicateGradientItem()
        {
            SkySettings sourceSky = GlobalState.Settings.skies[gradientList.CurrentIndex];
            GlobalState.Settings.skies.Add(sourceSky);
            AddGradient(sourceSky); // or rebuild all
            // TODO: make the new one SELECTED?
        }

        private void AddGradient(SkySettings sky)
        {
            GradientItem gradientItem = GradientItem.GenerateGradientItem(sky);
            gradientItem.AddListeners(OnDuplicateGradientItem, OnDeleteGradientItem);
            UIDynamicListItem dlItem = gradientList.AddItem(gradientItem.transform);
            dlItem.UseColliderForUI = false; // dont use the default global collider, sub-widget will catch UI events and propagate them.
            gradientItem.transform.localScale = Vector3.one; // Items are hidden (scale 0) while they are not added into a list, so activate the item here.
            gradientItem.SetListItem(dlItem); // link individual elements to their parent list in order to be able to send messages upwards.
        }

        private void RebuildGradientList()
        {
            gradientList.Clear();
            foreach (SkySettings sky in GlobalState.Settings.skies)
            {
                AddGradient(sky);
            }
        }
    }
}