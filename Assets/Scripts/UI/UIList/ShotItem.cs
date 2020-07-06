﻿using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace VRtist
{
    public class ShotItem : ListItemContent
    {
        public Shot shot = null;
        public UIDynamicListItem item;

        public UIButton cameraButton = null;
        public UICheckbox shotEnabledCheckbox = null;
        public UILabel shotNameLabel = null;
        public UILabel cameraNameLabel = null;
        public UISpinner startFrameSpinner = null;
        public UILabel frameRangeLabel = null;
        public UISpinner endFrameSpinner = null;
        public UIButton setCameraButton = null;

        private UnityAction<string> nameAction;
        private UnityAction<float> startAction;
        private UnityAction<float> endAction;
        private UnityAction<Color> colorAction;
        private UnityAction<bool> enabledAction;
        private UnityAction setCameraAction;

        public void AddListeners(UnityAction<string> nameAction, UnityAction<float> startAction, UnityAction<float> endAction, UnityAction<Color> colorAction, UnityAction<bool> enabledAction, UnityAction setCameraAction)
        {
            startFrameSpinner.onSpinEventInt.AddListener(UpdateShotRange);
            endFrameSpinner.onSpinEventInt.AddListener(UpdateShotRange);

            startFrameSpinner.onPressTriggerEvent.AddListener(InitSpinnerMinMax);
            endFrameSpinner.onPressTriggerEvent.AddListener(InitSpinnerMinMax);

            this.nameAction = nameAction;
            this.startAction = startAction;
            this.endAction = endAction;
            this.colorAction = colorAction;
            this.enabledAction = enabledAction;
            this.setCameraAction = setCameraAction;

            startFrameSpinner.onReleaseTriggerEvent.AddListener(startAction);
            endFrameSpinner.onReleaseTriggerEvent.AddListener(endAction);

            shotEnabledCheckbox.onCheckEvent.AddListener(enabledAction);

            setCameraButton.onClickEvent.AddListener(setCameraAction);
        }

        private void InitSpinnerMinMax()
        {
            startFrameSpinner.maxFloatValue = endFrameSpinner.FloatValue;
            startFrameSpinner.maxIntValue = endFrameSpinner.IntValue;
            endFrameSpinner.minFloatValue = startFrameSpinner.FloatValue;
            endFrameSpinner.minIntValue = startFrameSpinner.IntValue;
        }

        private void UpdateShotRange(int value)
        {
            frameRangeLabel.Text = (endFrameSpinner.IntValue - startFrameSpinner.IntValue + 1).ToString();
        }

        public override void SetSelected(bool value)
        {
            Color selectedColor = value ? UIOptions.CheckedColor : UIOptions.BackgroundColor;
            shotNameLabel.SetColor(selectedColor);
            cameraNameLabel.SetColor(selectedColor);
            cameraButton.SetColor(selectedColor);
            shotEnabledCheckbox.SetColor(selectedColor);
            startFrameSpinner.SetColor(selectedColor);
            endFrameSpinner.SetColor(selectedColor);
            setCameraButton.SetColor(selectedColor);
            frameRangeLabel.SetColor(selectedColor);

            if(null != shot.camera)
            {
                Selection.SetActiveCamera(shot.camera.GetComponent<CameraController>());
            }
        }

        public void Start()
        {
            Selection.OnSelectionChanged += OnSelectionChanged;
        }

        public void OnDestroy()
        {
            Selection.OnSelectionChanged -= OnSelectionChanged;

            startFrameSpinner.onSpinEventInt.RemoveListener(UpdateShotRange);
            endFrameSpinner.onSpinEventInt.RemoveListener(UpdateShotRange);
            startFrameSpinner.onPressTriggerEvent.RemoveListener(InitSpinnerMinMax);
            endFrameSpinner.onPressTriggerEvent.RemoveListener(InitSpinnerMinMax);
            startFrameSpinner.onReleaseTriggerEvent.RemoveListener(startAction);
            endFrameSpinner.onReleaseTriggerEvent.RemoveListener(endAction);
            shotEnabledCheckbox.onCheckEvent.RemoveListener(enabledAction);
            setCameraButton.onClickEvent.RemoveListener(setCameraAction);
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            // select line depending on camera selected ???
        }

        public void SetShot(Shot shot)
        {
            this.shot = shot;
            SetShotEnabled(shot.enabled);
            SetShotName(shot.name);
            SetShotCamera(shot.camera);
            SetStartFrame(shot.start);
            SetFrameRange(shot.end - shot.start + 1);
            SetEndFrame(shot.end);
        }

        public void SetShotEnabled(bool value)
        {
            if(shotEnabledCheckbox != null)
            {
                shotEnabledCheckbox.Checked = value;
                shot.enabled = value;
            }
        }

        public void SetShotName(string shotName)
        {
            if(shotNameLabel != null)
            {
                shotNameLabel.Text = shotName;
                shot.name = shotName;
            }
        }

        public void SetShotCamera(GameObject cam)
        {
            if(cameraNameLabel != null)
            {
                if(cam)
                    cameraNameLabel.Text = cam.name;
                else
                    cameraNameLabel.Text = "";
                shot.camera = cam;
            }
        }

        public void SetStartFrame(int startFrame)
        {
            if(startFrameSpinner != null)
            {
                startFrameSpinner.IntValue = startFrame;
                shot.start = startFrame;
            }
        }

        private void SetFrameRange(int frameRange)
        {
            if(frameRangeLabel != null)
            {
                frameRangeLabel.Text = frameRange.ToString();
            }
        }

        public void SetEndFrame(int endFrame)
        {
            if(endFrameSpinner != null)
            {
                endFrameSpinner.IntValue = endFrame;
                shot.end = endFrame;
            }
        }

        public static ShotItem GenerateShotItem(Shot shot)
        {
            GameObject root = new GameObject("shotItem");
            ShotItem shotItem = root.AddComponent<ShotItem>();
            root.layer = LayerMask.NameToLayer("UI");

            float cx = 0.0f;

            //
            // ACTIVE CAMERA Button
            //
            UIButton cameraButton = UIButton.Create(new UIButton.CreateButtonParams
            {
                parent = root.transform,
                widgetName = "CameraButton",
                relativeLocation = new Vector3(0, 0, -UIButton.default_thickness),
                width = 0.03f,
                height = 0.03f,
                icon = UIUtils.LoadIcon("icon-camera"),
                buttonContent = UIButton.ButtonContent.TextOnly
            });

            cameraButton.isCheckable = true;
            cameraButton.checkedSprite = UIUtils.LoadIcon("icon-camera");
            cameraButton.baseSprite = null;
            cameraButton.ActivateText(false); // icon-only
            cameraButton.SetLightLayer(5);

            cx += 0.03f;

            //
            // ENABLE Checkbox
            //
            UICheckbox shotEnabledCheckbox = UICheckbox.Create(new UICheckbox.CreateParams
            {
                parent = root.transform,
                widgetName = "ShotEnabledCheckbox",
                relativeLocation = new Vector3(cx, 0, -UICheckbox.default_thickness),
                width = 0.03f,
                height = 0.03f,
                content = UICheckbox.CheckboxContent.CheckboxOnly
            });

            shotEnabledCheckbox.SetLightLayer(5);

            cx += 0.03f;

            //
            // SHOT NAME Label
            //
            UILabel shotNameLabel = UILabel.Create(new UILabel.CreateLabelParams
            {
                parent = root.transform,
                widgetName = "ShotNameLabel",
                relativeLocation = new Vector3(cx, 0, -UIButton.default_thickness),
                width = 0.17f,
                height = 0.020f,
                margin = 0.001f,
                
            });

            shotNameLabel.SetLightLayer(5);
            Text text = shotNameLabel.GetComponentInChildren<Text>();
            text.fontStyle = FontStyle.Normal;
            text.fontSize = 8;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignByGeometry = true;

            //
            // CAMERA NAME Label
            //
            UILabel cameraNameLabel = UILabel.Create(new UILabel.CreateLabelParams
            {
                parent = root.transform,
                widgetName = "CameraNameLabel",
                relativeLocation = new Vector3(cx, -0.020f, -UIButton.default_thickness),
                width = 0.17f,
                height = 0.01f,
                margin = 0.001f,
                fgcolor = UIOptions.Instance.attenuatedTextColor
            });

            cameraNameLabel.SetLightLayer(5);
            text = cameraNameLabel.GetComponentInChildren<Text>();
            text.alignment = TextAnchor.LowerRight;
            text.fontStyle = FontStyle.Normal;
            text.fontSize = 8;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignByGeometry = true;
            
            cx += 0.17f;

            // START: Add UISpinner
            UISpinner startFrameSpinner = UISpinner.Create( new UISpinner.CreateArgs
            {
                parent = root.transform,
                widgetName = "StartFrame",
                relativeLocation = new Vector3(cx, 0, 0),
                width = 0.055f,
                height = 0.03f,
                visibility_type = UISpinner.TextAndValueVisibilityType.ShowValueOnly,
                value_type = UISpinner.SpinnerValueType.Int,
                min_spinner_value_int = 0, max_spinner_value_int = 10000, 
                cur_spinner_value_int = shot.start, spinner_value_rate_int = 30
            });

            startFrameSpinner.SetLightLayer(5);

            cx += 0.055f;

            // RANGE: Add UILabel
            UILabel frameRangeLabel = UILabel.Create(new UILabel.CreateLabelParams 
            { 
                parent = root.transform,
                widgetName = "FrameRange",
                relativeLocation = new Vector3(cx, 0, -UILabel.default_thickness),
                width = 0.04f,
                height = 0.03f
            });

            frameRangeLabel.SetLightLayer(5);
            Text frameRangeText = frameRangeLabel.GetComponentInChildren<Text>();
            frameRangeText.alignment = TextAnchor.MiddleCenter;
            cx += 0.04f;

            // END: Add UISpinner
            UISpinner endFrameSpinner = UISpinner.Create(new UISpinner.CreateArgs
            {
                parent = root.transform,
                widgetName = "EndFrame",
                relativeLocation = new Vector3(cx, 0, 0),
                width = 0.055f,
                height = 0.03f,
                visibility_type = UISpinner.TextAndValueVisibilityType.ShowValueOnly,
                value_type = UISpinner.SpinnerValueType.Int,
                min_spinner_value_int = 0,
                max_spinner_value_int = 10000,
                cur_spinner_value_int = shot.end,
                spinner_value_rate_int = 30
            });

            endFrameSpinner.SetLightLayer(5);

            cx += 0.055f;

            UIButton setCameraButton = UIButton.Create(new UIButton.CreateButtonParams
            {
                parent = root.transform,
                widgetName = "SetCameraButton",
                relativeLocation = new Vector3(0, 0, -UIButton.default_thickness),
                width = 0.03f,
                height = 0.03f,
                icon = UIUtils.LoadIcon("icon-camera"),
                buttonContent = UIButton.ButtonContent.ImageOnly
            });

            setCameraButton.SetLightLayer(5);


            // Link widgets to the item script.
            shotItem.cameraButton = cameraButton;
            shotItem.shotEnabledCheckbox = shotEnabledCheckbox;
            shotItem.shotNameLabel = shotNameLabel;
            shotItem.cameraNameLabel = cameraNameLabel;
            shotItem.startFrameSpinner = startFrameSpinner;
            shotItem.frameRangeLabel = frameRangeLabel;
            shotItem.endFrameSpinner = endFrameSpinner;
            shotItem.setCameraButton = setCameraButton;

            shotItem.SetShot(shot);

            return shotItem;
        }
    }
}
