using UnityEngine;
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

            setCameraButton.onCheckEvent.AddListener(TogglePickCamera);            
        }

        private void TogglePickCamera(bool value)
        {
            if(value)
                Selection.OnActiveCameraChanged += OnActiveCameraChanged;
            else
                Selection.OnActiveCameraChanged -= OnActiveCameraChanged;
        }

        private void OnActiveCameraChanged(object sender, ActiveCameraChangedArgs args)
        {
            Selection.OnActiveCameraChanged -= OnActiveCameraChanged;
            setCameraButton.Checked = false;
            setCameraAction();
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
            shotNameLabel.BaseColor = value ? UIElement.default_checked_color : UIElement.default_background_color;
            cameraNameLabel.BaseColor = shotNameLabel.BaseColor;
            cameraButton.BaseColor = shotNameLabel.BaseColor;
            shotEnabledCheckbox.BaseColor = shotNameLabel.BaseColor;
            startFrameSpinner.BaseColor = shotNameLabel.BaseColor;
            endFrameSpinner.BaseColor = shotNameLabel.BaseColor;
            setCameraButton.BaseColor = shotNameLabel.BaseColor;
            frameRangeLabel.BaseColor = shotNameLabel.BaseColor;

            if(value)
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

        public static ShotItem GenerateShotItem(Shot shot)
        {
            GameObject root = new GameObject("shotItem");
            ShotItem shotItem = root.AddComponent<ShotItem>();
            root.layer = LayerMask.NameToLayer("UI");

            // Set the item non active in order to hide it while it is not added into
            // a list. We will activate it after it is added
            root.SetActive(false);

            float cx = 0.0f;

            // Add UIButton
            UIButton cameraButton =
                UIButton.CreateUIButton(
                    "CameraButton",
                    root.transform,
                    Vector3.zero,
                    0.03f, // width
                    0.03f, // height
                    0.005f, // margin
                    0.001f, // thickness
                    UIUtils.LoadMaterial("UIPanel"),
                    UIElement.default_background_color,
                    "tmp",
                    UIUtils.LoadIcon("icon-camera"));

            cameraButton.isCheckable = true;
            cameraButton.checkedSprite = UIUtils.LoadIcon("icon-camera");
            cameraButton.uncheckedSprite = null;
            cameraButton.ActivateText(false); // icon-only
            cameraButton.SetLightLayer(5);

            cx += 0.03f;

            // Add UICheckbox
            UICheckbox shotEnabledCheckbox =
                UICheckbox.CreateUICheckbox(
                    "ShotEnabledCheckbox",
                    root.transform,
                    new Vector3(cx, 0, 0),
                    0.03f,
                    0.03f,
                    0.005f,
                    0.001f,
                    UIUtils.LoadMaterial("UIPanel"),
                    UIElement.default_background_color,
                    "shot name",
                    UIUtils.LoadIcon("checkbox_checked"),
                    UIUtils.LoadIcon("checkbox_unchecked")
                    );

            shotEnabledCheckbox.ActivateText(false);
            shotEnabledCheckbox.SetLightLayer(5);

            cx += 0.03f;

            // Add Shot Name UILabel
            UILabel shotNameLabel =
                UILabel.CreateUILabel(
                    "ShotNameLabel",
                    root.transform,
                    new Vector3(cx, 0, 0),
                    0.17f, // width
                    0.020f, // height
                    0.001f, // margin
                    UIUtils.LoadMaterial("UIPanel"),
                    UIElement.default_background_color,
                    UIElement.default_color,
                    "tmp"
                );

            shotNameLabel.SetLightLayer(5);
            Text text = shotNameLabel.GetComponentInChildren<Text>();
            text.fontStyle = FontStyle.Normal;
            text.fontSize = 8;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignByGeometry = true;

            // Add Camera Name UILabel
            UILabel cameraNameLabel =
                UILabel.CreateUILabel(
                    "CameraNameLabel",
                    root.transform,
                    new Vector3(cx, -0.020f, 0),
                    0.17f, // width
                    0.010f, // height
                    0.001f, // margin
                    UIUtils.LoadMaterial("UIPanel"),
                    UIElement.default_background_color,
                    UIElement.default_color,
                    "tmp"
                );

            cameraNameLabel.SetLightLayer(5);
            cameraNameLabel.TextColor = new Color(0.7f, 0.7f, 0.7f);
            text = cameraNameLabel.GetComponentInChildren<Text>();
            text.alignment = TextAnchor.LowerRight;
            text.fontStyle = FontStyle.Normal;
            text.fontSize = 8;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.alignByGeometry = true;

            cx += 0.17f;

            // START: Add UISpinner
            UISpinner startFrameSpinner = UISpinner.CreateUISpinner(
                "StartFrame",
                root.transform,
                new Vector3(cx, 0, 0),
                0.055f,
                0.03f,
                0.005f,
                0.001f,
                0.65f,
                UISpinner.TextAndValueVisibilityType.ShowValueOnly,
                UISpinner.SpinnerValueType.Int,
                0.0f, 1.0f, 0.5f, 0.1f,
                0, 10000, shot.start, 30,
                UIUtils.LoadMaterial("UIElementTransparent"),
                UIElement.default_background_color,
                "Start"
                );

            startFrameSpinner.SetLightLayer(5);

            cx += 0.055f;

            // RANGE: Add UILabel
            UILabel frameRangeLabel = UILabel.CreateUILabel(
                "FrameRange",
                root.transform,
                new Vector3(cx, 0, 0),
                0.04f,
                0.03f,
                0.005f,
                UIUtils.LoadMaterial("UIElementTransparent"),
                UIElement.default_background_color,
                UIElement.default_color,
                "51"
                );

            frameRangeLabel.SetLightLayer(5);
            Text frameRangeText = frameRangeLabel.GetComponentInChildren<Text>();
            frameRangeText.alignment = TextAnchor.MiddleCenter;
            cx += 0.04f;

            // END: Add UISpinner
            UISpinner endFrameSpinner = UISpinner.CreateUISpinner(
                "EndFrame",
                root.transform,
                new Vector3(cx, 0, 0),
                0.055f,
                0.03f,
                0.005f,
                0.001f,
                0.65f,
                UISpinner.TextAndValueVisibilityType.ShowValueOnly,
                UISpinner.SpinnerValueType.Int,
                0.0f, 1.0f, 0.5f, 0.1f,
                0, 10000, shot.end, 30,
                UIUtils.LoadMaterial("UIElementTransparent"),
                UIElement.default_background_color,
                "End"
                );

            endFrameSpinner.SetLightLayer(5);

            cx += 0.055f;
            // Add Shot Name UIButton
            UIButton setCameraButton =
                UIButton.CreateUIButton(
                    "SetCameraButton",
                    root.transform,
                    new Vector3(cx, 0, 0),
                    0.03f, // width
                    0.03f, // height
                    0.005f, // margin
                    0.001f, // thickness
                    UIUtils.LoadMaterial("UIPanel"),
                    UIElement.default_background_color,
                    "tmp",
                    UIUtils.LoadIcon("icon-camera"));

            setCameraButton.ActivateIcon(true); // text-only
            setCameraButton.ActivateText(false);
            setCameraButton.isCheckable = true;
            setCameraButton.checkedSprite = UIUtils.LoadIcon("icon-camera");
            setCameraButton.checkedColor = UIElement.default_focus_color;
            setCameraButton.uncheckedSprite = UIUtils.LoadIcon("icon-camera");
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
