using UnityEngine;

namespace VRtist
{
    public class ShotItem : MonoBehaviour
    {
        public Shot shot = null;
        public UIDynamicListItem item;

        public UIButton cameraButton = null;
        public UICheckbox shotEnabledCheckbox = null;
        public UIButton shotNameButton = null;
        public UILabel startFrameLabel = null;
        public UILabel frameRangeLabel = null;
        public UILabel endFrameLabel = null;

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
            if (shotEnabledCheckbox != null)
            {
                shotEnabledCheckbox.Checked = value;
                shot.enabled = value;
            }
        }

        public void SetShotName(string shotName)
        {
            if (shotNameButton != null)
            {
                shotNameButton.Text = shotName;
                shot.name = shotName;
            }
        }

        public void SetStartFrame(int startFrame)
        {
            if (startFrameLabel != null)
            {
                startFrameLabel.Text = startFrame.ToString();
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
            if (endFrameLabel != null)
            {
                endFrameLabel.Text = endFrame.ToString();
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

            // Add UILabel
            UILabel startFrameLabel = UILabel.CreateUILabel(
                "FirstFrame",
                root.transform,
                new Vector3(cx, 0, 0),
                0.06f,
                0.03f,
                0.005f,
                UIUtils.LoadMaterial("UIElementTransparent"),
                UIElement.default_background_color,
                UIElement.default_color,
                "1"
                );

            startFrameLabel.SetLightLayer(5);

            cx += 0.06f;

            // Add UILabel
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

            // Add UILabel
            UILabel endFrameLabel = UILabel.CreateUILabel(
                "FirstFrame",
                root.transform,
                new Vector3(cx, 0, 0),
                0.06f,
                0.03f,
                0.005f,
                UIUtils.LoadMaterial("UIElementTransparent"),
                UIElement.default_background_color,
                UIElement.default_color,
                "50"
                );

            endFrameLabel.SetLightLayer(5);

            // Link widgets to the item script.
            shotItem.cameraButton = cameraButton;
            shotItem.shotEnabledCheckbox = shotEnabledCheckbox;
            shotItem.shotNameButton = shotNameButton;
            shotItem.startFrameLabel = startFrameLabel;
            shotItem.frameRangeLabel = frameRangeLabel;
            shotItem.endFrameLabel = endFrameLabel;

            shotItem.SetShot(shot);

            return shotItem;
        }
    }
}
