using UnityEngine;

namespace VRtist
{
    public class ShotItem : MonoBehaviour
    {
        public GameObject shotObject;
        public UIDynamicListItem item;

        public UIButton cameraButton = null;
        public UICheckbox shotNameCheckbox = null;
        public UILabel startFrameLabel = null;
        public UILabel frameRangeLabel = null;
        public UILabel endFrameLabel = null;

        public void Start()
        {
            //cameraButton = transform.Find("CameraButton").gameObject.GetComponent<UIButton>();
            //shotNameCheckbox = transform.Find("ShotNameCheckbox").gameObject.GetComponent<UICheckbox>();
            //startFrameLabel = transform.Find("StartFrameLabel").gameObject.GetComponent<UILabel>();
            //frameRangeLabel = transform.Find("FrameRangeLabel").gameObject.GetComponent<UILabel>();
            //endFrameLabel = transform.Find("EndFrameLabel").gameObject.GetComponent<UILabel>();

            Selection.OnSelectionChanged += OnSelectionChanged;
        }

        public void OnDestroy()
        {
            Selection.OnSelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {

        }

        public void FixMaterials()
        {
            cameraButton?.ResetMaterial();
            shotNameCheckbox?.ResetMaterial();
            startFrameLabel?.ResetMaterial();
            frameRangeLabel?.ResetMaterial();
            endFrameLabel?.ResetMaterial();
        }

        public void SetShotObject(GameObject shotObject)
        {
            this.shotObject = shotObject;
            // set name
            // set start, end, range
            // set colors
            // ...
        }

        public static GameObject GenerateShotItem()
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

            cameraButton.ActivateText(false); // icon-only
            cameraButton.SetLightLayer(4);

            cx += 0.03f;

            // Add UICheckbox
            UICheckbox shotNameCheckbox =
                UICheckbox.CreateUICheckbox(
                    "ShotName",
                    root.transform,
                    new Vector3(cx, 0, 0),
                    0.20f,
                    0.03f,
                    0.005f,
                    0.001f,
                    UIUtils.LoadMaterial("UIPanel"),
                    UIElement.default_background_color,
                    "shot name",
                    UIUtils.LoadIcon("checkbox_checked"), 
                    UIUtils.LoadIcon("checkbox_unchecked")
                    );

            shotNameCheckbox.SetLightLayer(4);

            cx += 0.20f;

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

            startFrameLabel.SetLightLayer(4);

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

            frameRangeLabel.SetLightLayer(4);

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

            endFrameLabel.SetLightLayer(4);

            // Link widgets to the item script.
            shotItem.cameraButton = cameraButton;
            shotItem.shotNameCheckbox = shotNameCheckbox;
            shotItem.startFrameLabel = startFrameLabel;
            shotItem.frameRangeLabel = frameRangeLabel;
            shotItem.endFrameLabel = endFrameLabel;

            return root;
        }
    }
}
