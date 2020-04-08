using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class CameraPreviewWindow : MonoBehaviour
    {
        [SpaceHeader("Sub Widget Refs", 6, 0.8f, 0.8f, 0.8f)]
        [SerializeField] private Transform mainPanel = null;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public FloatChangedEvent onChangeFocalEvent = new FloatChangedEvent();
        public UnityEvent onRecordEvent = new UnityEvent();


//        private int firstFrame = 0;

//        public int FirstFrame { get { return firstFrame; } set { firstFrame = value; UpdateFirstFrame(); } }
  
        void Start()
        {
            mainPanel = transform.Find("MainPanel");
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

        public void UpdateFromController(ParametersController controller)
        {
            //Dictionary<string, AnimationChannel> channels = controller.GetAnimationChannels();
            //foreach(AnimationChannel channel in channels.Values)
            //{
            //    if(channel.name == "location[0]")
            //    {
            //        Transform keyframes = transform.Find("MainPanel/FakeTrackButton/Keyframes");
            //        for (int i = keyframes.childCount - 1 ; i >= 0 ; i--)
            //        {
            //            Destroy(keyframes.GetChild(i).gameObject);
            //        }

            //        foreach(AnimationKey key in channel.keys)
            //        {
            //            GameObject keyframe = GameObject.Instantiate(keyframePrefab, keyframes);

            //            float currentValue = key.time;
            //            float pct = (float)(currentValue - firstFrame) / (float)(lastFrame - firstFrame);

            //            float startX = 0.0f;
            //            float endX = timeBar.width;
            //            float posX = startX + pct * (endX - startX);

            //            Vector3 knobPosition = new Vector3(posX, 0.0f, 0.0f);

            //            keyframe.transform.localPosition = knobPosition;
            //        }
                    
            //    }
            //}
        }

        public void Clear()
        {

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
