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
        public UIButton shotNameButton = null;
        public UISpinner startFrameSpinner = null;
        public UILabel frameRangeLabel = null;
        public UISpinner endFrameSpinner = null;

        public void AddListeners(UnityAction<string> nameAction, UnityAction<float> startAction, UnityAction<float> endAction, UnityAction<string> cameraAction, UnityAction<Color> colorAction)
        {
            startFrameSpinner.onSpinEventInt.AddListener(UpdateShotRange);
            endFrameSpinner.onSpinEventInt.AddListener(UpdateShotRange);

            startFrameSpinner.onPressTriggerEvent.AddListener(InitSpinnerMinMax);
            endFrameSpinner.onPressTriggerEvent.AddListener(InitSpinnerMinMax);

            startFrameSpinner.onReleaseTriggerEvent.AddListener(startAction);
            endFrameSpinner.onReleaseTriggerEvent.AddListener(endAction);
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
            shotNameButton.BaseColor = value ? UIElement.default_pushed_color : UIElement.default_background_color;
        }

        public void Start()
        {
            Selection.OnSelectionChanged += OnSelectionChanged;
        }

        public void OnDestroy()
        {
            Selection.OnSelectionChanged -= OnSelectionChanged;
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
            if(shotNameButton != null)
            {
                shotNameButton.Text = shotName;
                shot.name = shotName;
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

            // Add Shot Name UIButton
            UIButton shotNameButton =
                UIButton.CreateUIButton(
                    "ShotNameButton",
                    root.transform,
                    new Vector3(cx, 0, 0),
                    0.17f, // width
                    0.03f, // height
                    0.005f, // margin
                    0.001f, // thickness
                    UIUtils.LoadMaterial("UIPanel"),
                    UIElement.default_background_color,
                    "tmp",
                    UIUtils.LoadIcon("icon-camera"));

            shotNameButton.ActivateIcon(false); // text-only
            shotNameButton.SetLightLayer(5);

            cx += 0.17f;

            // START: Add UISpinner
            UISpinner startFrameSpinner = UISpinner.CreateUISpinner(
                "StartFrame",
                root.transform,
                new Vector3(cx, 0, 0),
                0.06f,
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

            cx += 0.06f;

            // RANGE: Add UILabel
            UILabel frameRangeLabel = UILabel.CreateUILabel(
                "FrameRange",
                root.transform,
                new Vector3(cx, 0, 0),
                0.06f,
                0.03f,
                0.005f,
                UIUtils.LoadMaterial("UIElementTransparent"),
                UIElement.default_background_color,
                UIElement.default_color,
                "51"
                );

            frameRangeLabel.SetLightLayer(5);

            cx += 0.06f;

            // END: Add UISpinner
            UISpinner endFrameSpinner = UISpinner.CreateUISpinner(
                "EndFrame",
                root.transform,
                new Vector3(cx, 0, 0),
                0.06f,
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

            // Link widgets to the item script.
            shotItem.cameraButton = cameraButton;
            shotItem.shotEnabledCheckbox = shotEnabledCheckbox;
            shotItem.shotNameButton = shotNameButton;
            shotItem.startFrameSpinner = startFrameSpinner;
            shotItem.frameRangeLabel = frameRangeLabel;
            shotItem.endFrameSpinner = endFrameSpinner;

            shotItem.SetShot(shot);

            return shotItem;
        }
    }
}
