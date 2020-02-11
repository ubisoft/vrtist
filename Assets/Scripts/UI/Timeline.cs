using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class Timeline : MonoBehaviour
    {
        [SpaceHeader("Sub Widget Refs", 6, 0.8f, 0.8f, 0.8f)]
        [SerializeField] private Transform mainPanel = null;
        [SerializeField] private UITimeBar timeBar = null;
        [SerializeField] private UILabel firstFrameLabel = null;
        [SerializeField] private UILabel lastFrameLabel = null;
        [SerializeField] private UILabel currentFrameLabel = null;

        // AnimInfo currentObjectInfo;

        private int firstFrame = 0;
        private int lastFrame = 250;
        private int currentFrame = 0;

        public int FirstFrame { get { return firstFrame; } set { firstFrame = value; UpdateFirstFrame(); } }
        public int LastFrame { get { return lastFrame; } set { lastFrame = value; UpdateLastFrame(); } }
        public int CurrentFrame { get { return currentFrame; } set { currentFrame = value; UpdateCurrentFrame(); } }

        void Start()
        {
            mainPanel = transform.Find("MainPanel");
            if (mainPanel != null)
            {
                timeBar = mainPanel.Find("TimeBar").GetComponent<UITimeBar>();
                firstFrameLabel = mainPanel.Find("FirstFrameLabel").GetComponent<UILabel>();
                lastFrameLabel = mainPanel.Find("LastFrameLabel").GetComponent<UILabel>();
                currentFrameLabel = mainPanel.Find("CurrentFrameLabel").GetComponent<UILabel>();
            }
        }

        void Update()
        {

        }

        private void UpdateFirstFrame()
        {
            if (firstFrameLabel != null)
            {
                firstFrameLabel.Text = firstFrame.ToString();
            }
            if (timeBar != null)
            {
                timeBar.minValue = (float)firstFrame;
            }
        }

        private void UpdateLastFrame()
        {
            if (lastFrameLabel != null)
            {
                lastFrameLabel.Text = lastFrame.ToString();
            }
            if (timeBar != null)
            {
                timeBar.maxValue = (float)lastFrame;
            }
        }

        private void UpdateCurrentFrame()
        {
            if (timeBar != null)
            {
                // TODO: remove, used for the floating current frame text
                timeBar.Value = (int)currentFrame; // TODO: slider with int
                if (currentFrameLabel != null)
                {
                    currentFrameLabel.Text = ((int)currentFrame).ToString();
                }
            }
        }

        public void Show(bool doShow)
        {
            if (mainPanel != null)
            {
                mainPanel.gameObject.SetActive(doShow);

                // TMP
                FirstFrame = 11;
                LastFrame = 265;
                CurrentFrame = 54;
            }
        }

        public void OnChangeCurrentFrame(float f)
        {
            currentFrame = (int)f;
        }

        // TMP
        public class KeyFrameData
        {
            public int data;
        }
        private static SortedList<int, KeyFrameData> keyframes = new SortedList<int, KeyFrameData>();
        private static int currentKFFrame = -1;
        private static int FrameOfPreviousKeyFrame(int current, SortedList<int, KeyFrameData> keyframes)
        {
            int prev = current >= 0 ? current : (keyframes.Keys.Count > 0 ? keyframes.Keys[0] : 0);
            foreach(int k in keyframes.Keys)
            {
                if (k >= current) break;
                prev = k;
            }
            return prev;
        }
        private static int FrameOfNextKeyFrame(int current, SortedList<int, KeyFrameData> keyframes)
        {
            int next = current >= 0 ? current : (keyframes.Keys.Count > 0 ? keyframes.Keys[keyframes.Keys.Count - 1] : 0);
            foreach (int k in keyframes.Keys)
            {
                if (k > current) return k;
            }
            return next;
        }
        // TMP

        public void OnPrevKeyFrame()
        {
            currentKFFrame = FrameOfPreviousKeyFrame(currentKFFrame, keyframes);
            CurrentFrame = currentKFFrame; // updates the slider
            // TODO: use keyframes[currentKFFrame].data to apply current keyframe position/rotation/focal
        }

        public void OnNextKeyFrame()
        {
            currentKFFrame = FrameOfNextKeyFrame(currentKFFrame, keyframes);
            CurrentFrame = currentKFFrame; // updates the slider
            // TODO: use keyframes[currentKFFrame].data to apply current keyframe position/rotation/focal
        }

        public void OnAddKeyFrame()
        {
            int cf = CurrentFrame;
            KeyFrameData data = new KeyFrameData(){ data = 0 };
            keyframes[cf] = data;
        }

        public void OnRemoveKeyFrame()
        {
            int cf = CurrentFrame;
            if (keyframes.ContainsKey(cf))
            {
                keyframes.Remove(cf);
            }
        }
    }
}
