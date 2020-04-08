using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class CameraPreviewWindow : MonoBehaviour
    {
        //[SpaceHeader("Sub Widget Refs", 6, 0.8f, 0.8f, 0.8f)]
        private Transform mainPanel = null;
        private Transform handle = null;
        private Transform previewImagePlane = null;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public FloatChangedEvent onChangeFocalEvent = new FloatChangedEvent();
        public UnityEvent onRecordEvent = new UnityEvent();

        private CameraController currentCameraController = null;

//        private int firstFrame = 0;

//        public int FirstFrame { get { return firstFrame; } set { firstFrame = value; UpdateFirstFrame(); } }
  
        void Start()
        {
            mainPanel = transform.Find("MainPanel");
            handle = transform.parent;
            previewImagePlane = mainPanel.Find("PreviewImage");

            //if (mainPanel != null)
            //{
            //    timeBar = mainPanel.Find("TimeBar").GetComponent<UITimeBar>();
            //    firstFrameLabel = mainPanel.Find("FirstFrameLabel").GetComponent<UILabel>();
            //    lastFrameLabel = mainPanel.Find("LastFrameLabel").GetComponent<UILabel>();
            //    currentFrameLabel = mainPanel.Find("CurrentFrameLabel").GetComponent<UILabel>();

            //    keyframePrefab = Resources.Load<GameObject>("Prefabs/UI/DOPESHEET/Keyframe");
            //}
        }

        //private void UpdateFirstFrame()
        //{
        //    if (firstFrameLabel != null)
        //    {
        //        firstFrameLabel.Text = firstFrame.ToString();
        //    }
        //    if (timeBar != null)
        //    {
        //        timeBar.MinValue = firstFrame; // updates knob position
        //    }
        //}

        public void Show(bool doShow)
        {
            if (mainPanel != null)
            {
                mainPanel.gameObject.SetActive(doShow);
            }
        }

        public void UpdateFromController(CameraController cameraController)
        {
            currentCameraController = cameraController;

            // Get the renderTexture of the camera, and set it on the material of the previewImagePanel
            RenderTexture rt = currentCameraController.gameObject.GetComponentInChildren<Camera>(true).targetTexture;
            previewImagePlane?.GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", rt);

            // Get the name of the camera, and set it in the title bar
            ToolsUIManager.Instance.SetWindowTitle(handle, cameraController.gameObject.name);
        }

        private void Update()
        {
            // refresh... things..
        }

        public void Clear()
        {
            currentCameraController = null;
            ToolsUIManager.Instance.SetWindowTitle(handle, "Camera Preview");
            previewImagePlane?.GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", null);
        }
        
        // called by the slider when moved
        public void OnChangeFocal(float f)
        {
            onChangeFocalEvent.Invoke(f);
        }

        public void OnRecord()
        {
            onRecordEvent.Invoke();
        }
    }
}
