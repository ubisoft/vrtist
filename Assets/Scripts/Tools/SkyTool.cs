/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;

using UnityEngine;

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
            ToolsUIManager.Instance.onPaletteOpened.AddListener(OnPaletteOpened);
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

            OnSkyChanged(GlobalState.Instance.SkySettings);
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
            catch (Exception)
            {
                // Nothing
            }
        }
        void OnPaletteOpened()
        {
            gradientList.NeedsRebuild = true;
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
