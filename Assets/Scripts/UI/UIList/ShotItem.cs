using TMPro;
using UnityEngine;
using UnityEngine.Events;

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
        public UIButton startFrameButton = null;
        public UILabel frameRangeLabel = null;
        public UISpinner endFrameSpinner = null;
        public UIButton endFrameButton = null;
        public UIButton setCameraButton = null;

        private UnityAction<string> nameAction;
        private UnityAction<float> startAction;
        private UnityAction<float> endAction;
        private UnityAction<Color> colorAction;
        private UnityAction<bool> enabledAction;
        private UnityAction setCameraAction;

        private void OnEnable()
        {
            // TMPro bug, some attributes are not taken into account while the widget is not enabled
            // So force them here
            foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.fontSizeMin = 1f;
                text.fontSizeMax = 1.5f;
            }
        }

        public void AddListeners(UnityAction<string> nameAction, UnityAction<float> startAction, UnityAction<float> endAction, UnityAction<Color> colorAction, UnityAction<bool> enabledAction, UnityAction setCameraAction)
        {
            startFrameSpinner.onSpinEventInt.AddListener(UpdateShotRange);
            endFrameSpinner.onSpinEventInt.AddListener(UpdateShotRange);

            startFrameSpinner.onClickEvent.AddListener(InitSpinnerMinMax);
            endFrameSpinner.onClickEvent.AddListener(InitSpinnerMinMax);

            this.nameAction = nameAction;
            this.startAction = startAction;
            this.endAction = endAction;
            this.colorAction = colorAction;
            this.enabledAction = enabledAction;
            this.setCameraAction = setCameraAction;

            startFrameSpinner.onReleaseEvent.AddListener(OnEndEditStartSpinner);
            endFrameSpinner.onReleaseEvent.AddListener(OnEndEditEndSpinner);

            startFrameButton.onReleaseEvent.AddListener(() => OnSetStartEndFromCurrentFrame(startAction, startFrameSpinner));
            endFrameButton.onReleaseEvent.AddListener(() => OnSetStartEndFromCurrentFrame(endAction, endFrameSpinner));

            shotEnabledCheckbox.onCheckEvent.AddListener(enabledAction);

            setCameraButton.onCheckEvent.AddListener(TogglePickCamera);

            GlobalState.ObjectRenamedEvent.AddListener(OnObjectRenamed);
        }

        private void TogglePickCamera(bool value)
        {
            // If there is already a selected camera, pick it
            CameraController controller = Selection.GetSelectedCamera();
            if (null != controller)
            {
                Selection.SetActiveCamera(controller);
                setCameraButton.Checked = false;
                setCameraAction();
            }

            // Else set everything up to be able to pick a camera
            else
            {
                if (value)
                    Selection.OnActiveCameraChanged += OnActiveCameraChanged;
                else
                    Selection.OnActiveCameraChanged -= OnActiveCameraChanged;
            }
        }

        private void OnActiveCameraChanged(object sender, ActiveCameraChangedArgs args)
        {
            Selection.OnActiveCameraChanged -= OnActiveCameraChanged;
            setCameraButton.Checked = false;
            setCameraAction();
        }

        private void OnSetStartEndFromCurrentFrame(UnityAction<float> action, UISpinner spinner)
        {
            spinner.IntValue = GlobalState.Animation.CurrentFrame;
            action.Invoke(GlobalState.Animation.CurrentFrame);
            UpdateShotRange(0);  // 0: unused
        }

        private void OnEndEditStartSpinner()
        {
            float fValue = startFrameSpinner.IntValue;
            startAction.Invoke(fValue);
        }

        private void OnEndEditEndSpinner()
        {
            float fValue = endFrameSpinner.IntValue;
            endAction.Invoke(fValue);
        }

        private void InitSpinnerMinMax()
        {
            startFrameSpinner.maxValue = endFrameSpinner.FloatValue;
            endFrameSpinner.minValue = startFrameSpinner.FloatValue;
        }

        void OnObjectRenamed(GameObject gObject)
        {
            if (shot.camera == gObject)
                cameraNameLabel.Text = shot.camera.name;
        }

        private void UpdateShotRange(int value)
        {
            frameRangeLabel.Text = (endFrameSpinner.IntValue - startFrameSpinner.IntValue + 1).ToString();
        }

        public override void SetSelected(bool value)
        {
            shotNameLabel.Selected = value;
            cameraNameLabel.Selected = value;
            cameraButton.Selected = value;
            shotEnabledCheckbox.Selected = value;
            startFrameSpinner.Selected = value;
            startFrameButton.Selected = value;
            endFrameSpinner.Selected = value;
            endFrameButton.Selected = value;
            setCameraButton.Selected = value;
            frameRangeLabel.Selected = value;

            if (value)
            {
                CameraController camController = null;
                if (null != shot.camera)
                    camController = shot.camera.GetComponent<CameraController>();
                Selection.SetActiveCamera(camController);
            }
        }

        public void Start()
        {
            Selection.OnSelectionChanged += OnSelectionChanged;
        }

        public void OnDestroy()
        {
            Selection.OnSelectionChanged -= OnSelectionChanged;

            cameraButton.onClickEvent.RemoveAllListeners();

            shotEnabledCheckbox.onCheckEvent.RemoveAllListeners();
            shotEnabledCheckbox.onClickEvent.RemoveAllListeners();

            shotNameLabel.onClickEvent.RemoveAllListeners();

            cameraNameLabel.onClickEvent.RemoveAllListeners();

            startFrameSpinner.onSpinEventInt.RemoveAllListeners();
            startFrameSpinner.onClickEvent.RemoveAllListeners();
            startFrameSpinner.onReleaseEvent.RemoveAllListeners();

            startFrameButton.onReleaseEvent.RemoveAllListeners();
            startFrameButton.onClickEvent.RemoveAllListeners();

            frameRangeLabel.onClickEvent.RemoveAllListeners();

            endFrameSpinner.onSpinEventInt.RemoveAllListeners();
            endFrameSpinner.onClickEvent.RemoveAllListeners();
            endFrameSpinner.onReleaseEvent.RemoveAllListeners();

            endFrameButton.onReleaseEvent.RemoveAllListeners();
            endFrameButton.onClickEvent.RemoveAllListeners();

            setCameraButton.onCheckEvent.RemoveAllListeners();
            setCameraButton.onClickEvent.RemoveAllListeners();
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
            if (shotEnabledCheckbox != null)
            {
                shotEnabledCheckbox.Checked = value;
                shot.enabled = value;
            }
        }

        public void SetShotName(string shotName)
        {
            if (shotNameLabel != null)
            {
                shotNameLabel.Text = shotName;
                shot.name = shotName;
            }
        }

        public void SetShotCamera(GameObject cam)
        {
            if (cameraNameLabel != null)
            {
                if (cam)
                    cameraNameLabel.Text = cam.name;
                else
                    cameraNameLabel.Text = "";
                shot.camera = cam;
            }
        }

        public void SetStartFrame(int startFrame)
        {
            if (startFrameSpinner != null)
            {
                startFrameSpinner.IntValue = startFrame;
                shot.start = startFrame;
            }
        }

        private void SetFrameRange(int frameRange)
        {
            if (frameRangeLabel != null)
            {
                frameRangeLabel.Text = frameRange.ToString();
            }
        }

        public void SetEndFrame(int endFrame)
        {
            if (endFrameSpinner != null)
            {
                endFrameSpinner.IntValue = endFrame;
                shot.end = endFrame;
            }
        }

        public void SetListItem(UIDynamicListItem dlItem)
        {
            item = dlItem;

            cameraButton.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
            shotEnabledCheckbox.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
            shotNameLabel.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
            cameraNameLabel.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
            startFrameSpinner.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
            startFrameButton.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
            frameRangeLabel.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
            endFrameSpinner.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
            endFrameButton.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
            setCameraButton.onClickEvent.AddListener(dlItem.OnAnySubItemClicked);
        }

        public static ShotItem GenerateShotItem(Shot shot)
        {
            GameObject root = new GameObject("shotItem");
            ShotItem shotItem = root.AddComponent<ShotItem>();
            root.layer = LayerMask.NameToLayer("UI");

            // Set the item invisible in order to hide it while it is not added into
            // a list. We will activate it after it is added
            root.transform.localScale = Vector3.zero;

            float cx = 0.0f;

            //
            // ACTIVE CAMERA Button
            //
            UIButton cameraButton = UIButton.Create(new UIButton.CreateButtonParams
            {
                parent = root.transform,
                widgetName = "CameraButton",
                relativeLocation = new Vector3(0, 0, -UIButton.default_thickness),
                width = 0.01f,
                height = 0.03f,
                icon = UIUtils.LoadIcon("empty"),
                buttonContent = UIButton.ButtonContent.ImageOnly,
                margin = 0.001f,
            });

            cameraButton.isCheckable = true;
            cameraButton.checkedSprite = UIUtils.LoadIcon("empty");
            cameraButton.baseSprite = null;
            cameraButton.SetLightLayer(3);

            cx += 0.01f;

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
                content = UICheckbox.CheckboxContent.CheckboxOnly,
                margin = 0.001f,
            });

            shotEnabledCheckbox.SetLightLayer(3);

            cx += 0.03f;

            //
            // SHOT NAME Label
            //
            UILabel shotNameLabel = UILabel.Create(new UILabel.CreateLabelParams
            {
                parent = root.transform,
                widgetName = "ShotNameLabel",
                relativeLocation = new Vector3(cx, 0, -UIButton.default_thickness),
                width = 0.15f,
                height = 0.020f,
                margin = 0.001f,
            });

            shotNameLabel.SetLightLayer(3);
            UIUtils.SetTMProStyle(shotNameLabel.gameObject);

            //
            // CAMERA NAME Label
            //
            UILabel cameraNameLabel = UILabel.Create(new UILabel.CreateLabelParams
            {
                parent = root.transform,
                widgetName = "CameraNameLabel",
                relativeLocation = new Vector3(cx, -0.020f, -UIButton.default_thickness),
                width = 0.15f,
                height = 0.01f,
                margin = 0.001f,
                fgcolor = UIOptions.Instance.attenuatedTextColor
            });

            cameraNameLabel.SetLightLayer(3);
            UIUtils.SetTMProStyle(cameraNameLabel.gameObject, alignment: TextAlignmentOptions.BottomRight);

            cx += 0.15f;

            // Start frame button
            UIButton startFrameButton = UIButton.Create(new UIButton.CreateButtonParams
            {
                parent = root.transform,
                widgetName = "StartFrameButton",
                relativeLocation = new Vector3(cx, 0, -UIButton.default_thickness),
                width = 0.02f,
                height = 0.03f,
                icon = UIUtils.LoadIcon("next"),
                buttonContent = UIButton.ButtonContent.ImageOnly,
                margin = 0.001f,
            });

            startFrameButton.SetLightLayer(3);

            cx += 0.02f;

            // START: Add UISpinner
            UISpinner startFrameSpinner = UISpinner.Create(new UISpinner.CreateArgs
            {
                parent = root.transform,
                widgetName = "StartFrame",
                relativeLocation = new Vector3(cx, 0, -UISpinner.default_thickness),
                width = 0.055f,
                height = 0.03f,
                visibility_type = UISpinner.TextAndValueVisibilityType.ShowValueOnly,
                value_type = UISpinner.SpinnerValueType.Int,
                min_spinner_value = 0,
                max_spinner_value = 10000,
                cur_spinner_value = shot.start,
                spinner_value_rate = 30,
                spinner_value_rate_ray = 30,
                margin = 0.001f,
            });

            startFrameSpinner.baseColor.useConstant = true;
            startFrameSpinner.baseColor.constant = UIOptions.BackgroundColor;
            startFrameSpinner.selectedColor.useConstant = true;
            startFrameSpinner.selectedColor.constant = UIOptions.SelectedColor;
            startFrameSpinner.SetLightLayer(3);
            UIUtils.SetTMProStyle(startFrameSpinner.gameObject);

            cx += 0.055f;

            // END: Add UISpinner
            UISpinner endFrameSpinner = UISpinner.Create(new UISpinner.CreateArgs
            {
                parent = root.transform,
                widgetName = "EndFrame",
                relativeLocation = new Vector3(cx, 0, -UISpinner.default_thickness),
                width = 0.055f,
                height = 0.03f,
                visibility_type = UISpinner.TextAndValueVisibilityType.ShowValueOnly,
                value_type = UISpinner.SpinnerValueType.Int,
                min_spinner_value = 0,
                max_spinner_value = 10000,
                cur_spinner_value = shot.end,
                spinner_value_rate = 30,
                spinner_value_rate_ray = 30,
                margin = 0.001f,
            });

            endFrameSpinner.baseColor.useConstant = true;
            endFrameSpinner.baseColor.constant = UIOptions.BackgroundColor;
            endFrameSpinner.selectedColor.useConstant = true;
            endFrameSpinner.selectedColor.constant = UIOptions.SelectedColor;
            endFrameSpinner.SetLightLayer(3);
            UIUtils.SetTMProStyle(endFrameSpinner.gameObject);

            cx += 0.055f;

            // End frame button
            UIButton endFrameButton = UIButton.Create(new UIButton.CreateButtonParams
            {
                parent = root.transform,
                widgetName = "EndFrameButton",
                relativeLocation = new Vector3(cx, 0, -UIButton.default_thickness),
                width = 0.02f,
                height = 0.03f,
                icon = UIUtils.LoadIcon("prev"),
                buttonContent = UIButton.ButtonContent.ImageOnly,
                margin = 0.001f,
            });

            endFrameButton.SetLightLayer(3);

            cx += 0.02f;

            // RANGE: Add UILabel
            UILabel frameRangeLabel = UILabel.Create(new UILabel.CreateLabelParams
            {
                parent = root.transform,
                widgetName = "FrameRange",
                relativeLocation = new Vector3(cx, 0, -UILabel.default_thickness),
                width = 0.04f,
                height = 0.03f,
                margin = 0.001f,
            });

            frameRangeLabel.baseColor.useConstant = true;
            frameRangeLabel.baseColor.constant = UIOptions.BackgroundColor;
            frameRangeLabel.selectedColor.useConstant = true;
            frameRangeLabel.selectedColor.constant = UIOptions.SelectedColor;
            frameRangeLabel.SetLightLayer(3);
            UIUtils.SetTMProStyle(frameRangeLabel.gameObject, alignment: TextAlignmentOptions.Center);
            cx += 0.04f;

            // Set camera
            UIButton setCameraButton = UIButton.Create(new UIButton.CreateButtonParams
            {
                parent = root.transform,
                widgetName = "SetCameraButton",
                relativeLocation = new Vector3(cx, 0, -UIButton.default_thickness),
                width = 0.03f,
                height = 0.03f,
                icon = UIUtils.LoadIcon("icon-camera"),
                buttonContent = UIButton.ButtonContent.ImageOnly,
                margin = 0.001f,
            });

            setCameraButton.isCheckable = true;
            setCameraButton.checkedSprite = UIUtils.LoadIcon("icon-camera");
            setCameraButton.checkedColor.useConstant = false;
            setCameraButton.checkedColor.constant = UIOptions.FocusColor;
            setCameraButton.checkedColor.reference = UIOptions.FocusColorVar;
            setCameraButton.baseSprite = UIUtils.LoadIcon("icon-camera");
            setCameraButton.SetLightLayer(3);

            // Link widgets to the item script.
            shotItem.cameraButton = cameraButton;
            shotItem.shotEnabledCheckbox = shotEnabledCheckbox;
            shotItem.shotNameLabel = shotNameLabel;
            shotItem.cameraNameLabel = cameraNameLabel;
            shotItem.startFrameSpinner = startFrameSpinner;
            shotItem.startFrameButton = startFrameButton;
            shotItem.frameRangeLabel = frameRangeLabel;
            shotItem.endFrameSpinner = endFrameSpinner;
            shotItem.endFrameButton = endFrameButton;
            shotItem.setCameraButton = setCameraButton;

            shotItem.SetShot(shot);

            return shotItem;
        }
    }
}
