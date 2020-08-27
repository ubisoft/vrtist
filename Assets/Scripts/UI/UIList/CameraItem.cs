using TMPro;
using UnityEngine;

namespace VRtist
{
    public class CameraItem : ListItemContent
    {
        public GameObject cameraObject;
        public UIDynamicListItem item;

        public void Start()
        {
            Selection.OnSelectionChanged += OnSelectionChanged;
            Selection.OnActiveCameraChanged += OnActiveCameraChanged;
            GlobalState.ObjectRenamedEvent.AddListener(OnCameraNameChanged);
        }

        public void OnDestroy()
        {
            Selection.OnSelectionChanged -= OnSelectionChanged;
            Selection.OnActiveCameraChanged -= OnActiveCameraChanged;
            GlobalState.ObjectRenamedEvent.RemoveListener(OnCameraNameChanged);
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            if (Selection.IsSelected(cameraObject))
            {
                SetColor(UIOptions.PushedColor);
            }
            else
            {
                SetColor(UIOptions.BackgroundColor);
            }
        }

        private void OnActiveCameraChanged(object sender, ActiveCameraChangedArgs args)
        {
            if (args.activeCamera == cameraObject)
            {
                SetColor(UIOptions.SceneHoverColor);
            }
            else
            {
                if (Selection.IsSelected(cameraObject))
                {
                    SetColor(UIOptions.PushedColor);
                }
                else
                {
                    SetColor(UIOptions.BackgroundColor);
                }
            }
        }

        public void SetColor(Color color)
        {
            gameObject.GetComponentInChildren<MeshRenderer>(true).materials[0].SetColor("_BaseColor", color);
        }

        void OnCameraNameChanged(GameObject gObject)
        {
            if (gObject == cameraObject)
            {
                Camera cam = cameraObject.GetComponentInChildren<Camera>(true);
                TextMeshProUGUI text = transform.Find("Canvas/Panel/Text").gameObject.GetComponent<TextMeshProUGUI>();
                text.text = cameraObject.name;
            }
        }

        public void SetCameraObject(GameObject cameraObject)
        {
            this.cameraObject = cameraObject;
            Camera cam = cameraObject.GetComponentInChildren<Camera>(true);
            SetColor(UIOptions.BackgroundColor);
            gameObject.GetComponentInChildren<MeshRenderer>(true).materials[1].SetTexture("_UnlitColorMap", cam.targetTexture);
            TextMeshProUGUI text = transform.Find("Canvas/Panel/Text").gameObject.GetComponent<TextMeshProUGUI>();
            text.text = cameraObject.name;
        }
    }
}
