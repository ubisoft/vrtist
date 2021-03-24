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

using UnityEngine;
using UnityEngine.Assertions;

namespace VRtist
{
    public class UIShotManager : MonoBehaviour
    {
        public UIDynamicList shotList;
        public UILabel activeShotCountLabel;

        UICheckbox montageCheckbox = null;
        UIButton videoOutputButton;

        void Start()
        {
            ShotManager.Instance.ShotsChangedEvent.AddListener(OnShotManagerChanged);
            ShotManager.Instance.ActiveShotChangedEvent.AddListener(OnActiveShotChanged);
            shotList.ItemClickedEvent += OnListItemClicked;

            montageCheckbox = transform.Find("MainPanel/Montage").GetComponent<UICheckbox>();
            ShotManager.Instance.MontageModeChangedEvent.AddListener(OnMontageModeChanged);

            videoOutputButton = transform.Find("MainPanel/VideoOutput").GetComponent<UIButton>();
            videoOutputButton.Disabled = !montageCheckbox.Checked;

            GlobalState.Animation.onFrameEvent.AddListener(OnCurrentFrameChanged);
            GlobalState.Animation.onAnimationStateEvent.AddListener(OnAnimationStateChanged);
            ToolsUIManager.Instance.onPaletteOpened.AddListener(OnPaletteOpened);
        }

        void OnPaletteOpened()
        {
            shotList.NeedsRebuild = true;
        }

        public void OnSetMontage(bool montage)
        {
            ShotManager.Instance.MontageEnabled = montage;
        }

        private void OnMontageModeChanged()
        {
            montageCheckbox.Checked = ShotManager.Instance.MontageEnabled;
            videoOutputButton.Disabled = !montageCheckbox.Checked;
        }

        void SetUIElementColors(UIElement spinner, Color baseColor, Color selectedColor)
        {
            bool apply = false;
            if (spinner.baseColor.constant != baseColor)
            {
                spinner.baseColor.constant = baseColor;
                apply = true;
            }
            if (spinner.selectedColor.constant != selectedColor)
            {
                spinner.selectedColor.constant = selectedColor;
                apply = true;
            }
            if (apply)
            {
                spinner.ResetColor();
            }
        }

        public void UpdateShotItemsColors(int currentFrame)
        {
            ShotManager sm = ShotManager.Instance;

            Color focusColor = UIOptions.FocusColor;

            for (int i = 0; i < sm.shots.Count; ++i)
            {
                ShotItem item = GetShotItem(i);
                Shot shot = sm.shots[i];
                Color defaultColor = UIOptions.BackgroundColor;
                Color defaultSelectedColor = UIOptions.SelectedColor;

                if (currentFrame == shot.start)
                {
                    SetUIElementColors(item.startFrameSpinner, focusColor, focusColor);
                }
                else
                {
                    SetUIElementColors(item.startFrameSpinner, defaultColor, defaultSelectedColor);
                }

                if (currentFrame == shot.end)
                {
                    SetUIElementColors(item.endFrameSpinner, focusColor, focusColor);
                }
                else
                {
                    SetUIElementColors(item.endFrameSpinner, defaultColor, defaultSelectedColor);
                }

                if (shot.end <= shot.start)
                {
                    SetUIElementColors(item.frameRangeLabel, defaultColor, UIOptions.ErrorColor);
                }
                else
                {
                    if (currentFrame > shot.start && currentFrame < shot.end)
                    {
                        SetUIElementColors(item.frameRangeLabel, focusColor, focusColor);
                    }
                    else
                    {
                        SetUIElementColors(item.frameRangeLabel, defaultColor, defaultSelectedColor);
                    }
                }
            }
        }

        private void OnAnimationStateChanged(AnimationState state)
        {
            if (!videoOutputButton.Disabled)
            {
                videoOutputButton.Checked = GlobalState.Animation.animationState == AnimationState.VideoOutput;
            }
            // Prevent unchecking montage while recording a video
            montageCheckbox.Disabled = GlobalState.Animation.animationState == AnimationState.VideoOutput;
        }

        private void OnCurrentFrameChanged(int currentFrame)
        {
            UpdateShotItemsColors(currentFrame);
        }

        public ShotItem GetShotItem(int index)
        {
            return shotList.GetItems()[index].Content.GetComponent<ShotItem>();
        }

        // Update UI: set current shot
        void OnActiveShotChanged()
        {
            int itemIndex = 0;
            int currentShotIndex = ShotManager.Instance.ActiveShotIndex;
            foreach (UIDynamicListItem item in shotList.GetItems())
            {
                ShotItem shotItem = item.Content.GetComponent<ShotItem>();
                shotItem.currentShotLabel.Selected = (itemIndex++ == currentShotIndex);
            }
        }

        // Update UI: rebuild shots list
        void OnShotManagerChanged()
        {
            shotList.Clear();
            ShotManager sm = ShotManager.Instance;
            int activeShotCount = 0;
            foreach (Shot shot in sm.shots)
            {
                ShotItem shotItem = ShotItem.GenerateShotItem(shot);
                shotItem.AddListeners(OnUpdateShotName, OnUpdateShotStart, OnUpdateShotEnd, OnUpdateShotColor, OnUpdateShotEnabled, OnSetCamera);
                UIDynamicListItem dlItem = shotList.AddItem(shotItem.transform);
                dlItem.UseColliderForUI = false; // dont use the default global collider, sub-widget will catch UI events and propagate them.
                shotItem.transform.localScale = Vector3.one; // Items are hidden (scale 0) while they are not added into a list, so activate the item here.
                shotItem.SetListItem(dlItem); // link individual elements to their parent list in order to be able to send messages upwards.

                if (shot.enabled)
                    activeShotCount++;
            }
            shotList.CurrentIndex = sm.ActiveShotIndex;
            activeShotCountLabel.Text = activeShotCount.ToString() + "/" + sm.shots.Count.ToString();
        }

        void OnListItemClicked(object sender, IndexedGameObjectArgs args)
        {
            ShotManager.Instance.SetCurrentShotIndex(args.index);
        }

        // Update data: add a shot
        public void OnAddShot()
        {
            ShotManager sm = ShotManager.Instance;

            // Take the current shot to find the start frame of the new shot
            // Keep same camera as the current shot if anyone
            int start = GlobalState.Animation.CurrentFrame;
            GameObject camera;
            if (shotList.CurrentIndex != -1)
            {
                Shot selectedShot = sm.shots[shotList.CurrentIndex];
                start = selectedShot.end + 1;
                camera = CameraManager.Instance.ActiveCamera != null ? CameraManager.Instance.ActiveCamera : selectedShot.camera;
            }
            else
            {
                camera = CameraManager.Instance.ActiveCamera;
            }
            int end = start + 50;  // arbitrary duration
            end = Mathf.Min(end, GlobalState.Animation.EndFrame);

            // Look at all the shots to find a name for the new shot
            string name = sm.GetUniqueShotName();

            Shot shot = new Shot { name = name, camera = camera, enabled = true, end = end, start = start };
            int shotIndex = shotList.CurrentIndex;

            // Send network message
            ShotManagerActionInfo info = new ShotManagerActionInfo
            {
                action = ShotManagerAction.AddShot,
                camera = camera,
                shotIndex = shotIndex,
                shotName = shot.name,
                shotStart = start,
                shotEnd = end,
                shotEnabled = 1,
                shotColor = Color.blue  // TODO: find a unique color
            };
            new CommandShotManager(info).Submit();
            // Add the shot to ShotManager singleton
            shotIndex++;
            sm.SetCurrentShotIndex(shotIndex);

            // Rebuild UI
            OnShotManagerChanged();
        }

        // Update data: delete shot
        public void OnDeleteShot()
        {
            ShotManager sm = ShotManager.Instance;

            if (shotList.CurrentIndex == -1) { return; }

            // Delete the current shot
            int shotIndex = shotList.CurrentIndex;
            Shot shot = sm.shots[shotIndex];

            // Send network message
            ShotManagerActionInfo info = new ShotManagerActionInfo
            {
                action = ShotManagerAction.DeleteShot,
                shotName = shot.name,
                camera = shot.camera,
                shotStart = shot.start,
                shotEnd = shot.end,
                shotColor = shot.color,
                shotEnabled = shot.enabled ? 1 : 0,
                shotIndex = shotIndex
            };

            new CommandShotManager(info).Submit();
            // Rebuild UI
            OnShotManagerChanged();
        }

        // Duplicate Shot
        public void OnDuplicateShot()
        {
            ShotManager sm = ShotManager.Instance;

            // Take the current shot to find the start frame of the new shot
            // Keep same camera as the current shot if anyone
            if (shotList.CurrentIndex == -1)
                return;

            Shot shot = sm.shots[shotList.CurrentIndex].Copy();
            shot.name = sm.GetUniqueShotName();
            int shotIndex = shotList.CurrentIndex;

            // Send network message
            ShotManagerActionInfo info = new ShotManagerActionInfo
            {
                action = ShotManagerAction.AddShot,
                camera = shot.camera,
                shotIndex = shotIndex,
                shotName = shot.name,
                shotStart = shot.start,
                shotEnd = shot.end,
                shotEnabled = shot.enabled ? 1 : 0,
                shotColor = Color.blue  // TODO: find a unique color
            };
            new CommandShotManager(info).Submit();

            // Add the shot to ShotManager singleton
            shotIndex++;
            sm.SetCurrentShotIndex(shotIndex);

            // Rebuild UI
            OnShotManagerChanged();
        }


        // Update data: move shot
        public void OnMoveShot(int offset)
        {
            ShotManager sm = ShotManager.Instance;

            int shotIndex = shotList.CurrentIndex;
            if (offset < 0 && shotIndex <= 0)
                return;
            if (offset > 0 && shotIndex >= (sm.shots.Count - 1))
                return;

            // Send network message
            ShotManagerActionInfo info = new ShotManagerActionInfo
            {
                action = ShotManagerAction.MoveShot,
                shotIndex = shotIndex,
                moveOffset = offset
            };
            new CommandShotManager(info).Submit();
            shotList.CurrentIndex = shotIndex + offset;
            // Rebuild UI
            OnShotManagerChanged();
        }

        public void OnUpdateShotStart(Shot shot, float value)
        {
            ShotManager sm = ShotManager.Instance;

            int intValue = Mathf.FloorToInt(value);
            int endValue = shot.end;
            if (intValue > endValue)
                intValue = endValue;

            // Send network message
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.GetShotIndex(shot),
                shotStart = shot.start
            };
            ShotManagerActionInfo info = oldInfo.Copy();
            info.shotStart = intValue;
            shot.start = intValue;

            new CommandShotManager(oldInfo, info).Submit();

            UpdateShotItemsColors(GlobalState.Animation.CurrentFrame);
        }

        public void OnUpdateShotEnd(Shot shot, float value)
        {
            ShotManager sm = ShotManager.Instance;

            int intValue = Mathf.FloorToInt(value);
            int startValue = shot.start;
            if (intValue < startValue)
                intValue = startValue;

            // Send network message
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.GetShotIndex(shot),
                shotEnd = shot.end,
            };

            ShotManagerActionInfo info = oldInfo.Copy();
            info.shotEnd = intValue;
            shot.end = intValue;

            new CommandShotManager(oldInfo, info).Submit();

            UpdateShotItemsColors(GlobalState.Animation.CurrentFrame);
        }

        public void OnUpdateShotName(Shot shot, string value)
        {
            ShotManager sm = ShotManager.Instance;

            // Send network message
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.GetShotIndex(shot),
                shotName = shot.name
            };

            ShotManagerActionInfo info = oldInfo.Copy();
            info.shotName = value;
            shot.name = value;

            new CommandShotManager(oldInfo, info).Submit();
            OnShotManagerChanged();
        }

        public void OnUpdateShotColor(Shot shot, Color value)
        {
            ShotManager sm = ShotManager.Instance;

            // Send network message
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.GetShotIndex(shot),
                shotColor = shot.color
            };

            ShotManagerActionInfo info = oldInfo.Copy();
            info.shotColor = value;
            shot.color = value;

            new CommandShotManager(oldInfo, info).Submit();
        }

        public void OnUpdateShotEnabled(Shot shot, bool value)
        {
            Assert.IsTrue(ShotManager.Instance.ActiveShotIndex >= 0); // TODO: voir si le probleme persiste une fois que le rayon fonctionne.

            ShotManager sm = ShotManager.Instance;

            // Send network message
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = sm.GetShotIndex(shot),
                shotEnabled = shot.enabled ? 1 : 0
            };

            ShotManagerActionInfo info = oldInfo.Copy();
            info.shotEnabled = value ? 1 : 0;
            shot.enabled = value;

            new CommandShotManager(oldInfo, info).Submit();
        }

        public void OnSetCamera(Shot shot)
        {
            ShotManager sm = ShotManager.Instance;
            int shotIndex = sm.GetShotIndex(shot);
            ShotManagerActionInfo oldInfo = new ShotManagerActionInfo
            {
                action = ShotManagerAction.UpdateShot,
                shotIndex = shotIndex,
                camera = shot.camera,
            };

            ShotManagerActionInfo info = oldInfo.Copy();
            info.camera = CameraManager.Instance.ActiveCamera;

            new CommandShotManager(oldInfo, info).Submit();

            // Update Camera UI Button
            ShotItem uiItem = GetShotItem(shotIndex);
            if (null != info.camera)
                uiItem.cameraNameLabel.Text = info.camera.name;
            else
                uiItem.cameraNameLabel.Text = "";
        }
    }
}
