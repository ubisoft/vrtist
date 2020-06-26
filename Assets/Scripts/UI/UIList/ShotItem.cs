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

        public void AddListeners(UnityAction<string> nameAction, UnityAction<int> startAction, UnityAction<int> endAction, UnityAction<string> cameraAction, UnityAction<Color> colorAction)
        {
            // TODO only on release
            startFrameSpinner.onSpinEventInt.AddListener(startAction);
            endFrameSpinner.onSpinEventInt.AddListener(endAction);
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

            //
            // CAMERA NAME
            //
            UIButton cameraButton = UIButton.Create(new UIButton.CreateButtonParams
            {
                parent = root.transform,
                buttonName = "CameraButton",
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

            //shotEnabledCheckbox.ActivateText(false);
            shotEnabledCheckbox.SetLightLayer(5);

            cx += 0.03f;

            // Add Shot Name UIButton
            UIButton shotNameButton = UIButton.Create(new UIButton.CreateButtonParams
            {
                parent = root.transform,
                buttonName = "ShotNameButton",
                relativeLocation = new Vector3(cx, 0, -UIButton.default_thickness),
                width = 0.17f,
                height = 0.03f,
                icon = UIUtils.LoadIcon("icon-camera"),
                buttonContent = UIButton.ButtonContent.TextOnly
            });

            //shotNameButton.ActivateIcon(false); // text-only
            shotNameButton.SetLightLayer(5);

            cx += 0.17f;

            // START: Add UISpinner
            UISpinner startFrameSpinner = 
                UISpinner.CreateUISpinner(
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
            UILabel frameRangeLabel = 
                UILabel.CreateEx(
                    root.transform,
                    "FrameRange",
                    new Vector3(cx, 0, -UILabel.default_thickness),
                    0.06f,
                    0.03f,
                    UILabel.default_margin,
                    UILabel.default_thickness,
                    UIUtils.LoadMaterial("UIElementTransparent"),
                    UIElement.default_background_color,
                    UIElement.default_foreground_color,
                    UILabel.LabelContent.TextOnly,
                    "51",
                    UIUtils.LoadIcon("icon-camera")
                );

            frameRangeLabel.SetLightLayer(5);

            cx += 0.06f;

            // END: Add UISpinner
            UISpinner endFrameSpinner = 
                UISpinner.CreateUISpinner(
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
