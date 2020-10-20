using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VRtist
{
    public class Dopesheet : MonoBehaviour
    {
        [SpaceHeader("Sub Widget Refs", 6, 0.8f, 0.8f, 0.8f)]
        [SerializeField] private Transform mainPanel = null;
        [SerializeField] private UITimeBar timeBar = null;
        [SerializeField] private UILabel currentFrameLabel = null;
        [SerializeField] private UIRange currentRange = null;
        private UILabel titleBar = null;

        UIButton constantInterpolationButton = null;
        UIButton linearInterpolationButton = null;
        UIButton bezierInterpolationButton = null;
        Color constantInterpolationColor;
        Color linearInterpolationColor;
        Color bezierInterpolationColor;

        [SpaceHeader("Callbacks", 6, 0.8f, 0.8f, 0.8f)]
        public IntChangedEvent onAddKeyframeEvent = new IntChangedEvent();
        public IntChangedEvent onRemoveKeyframeEvent = new IntChangedEvent();
        public IntChangedEvent onPreviousKeyframeEvent = new IntChangedEvent();
        public IntChangedEvent onNextKeyframeEvent = new IntChangedEvent();

        private int firstFrame = 0;
        private int lastFrame = 250;
        private int currentFrame = 0;

        public int FirstFrame { get { return firstFrame; } set { firstFrame = value; UpdateFirstFrame(); } }
        public int LastFrame { get { return lastFrame; } set { lastFrame = value; UpdateLastFrame(); } }
        public int CurrentFrame { get { return currentFrame; } set { currentFrame = value; UpdateCurrentFrame(); } }

        private GameObject keyframePrefab;
        private GameObject currentObject = null;

        public class AnimKey
        {
            public AnimKey(string name, float value, Interpolation interpolation)
            {
                this.name = name;
                this.value = value;
                this.interpolation = interpolation;
            }
            public string name;
            public float value;
            public Interpolation interpolation;
        }

        private SortedList<int, List<AnimKey>> keys = new SortedList<int, List<AnimKey>>();
        private bool listenerAdded = false;

        void Start()
        {
            mainPanel = transform.Find("MainPanel");
            if (mainPanel != null)
            {
                timeBar = mainPanel.Find("TimeBar").GetComponent<UITimeBar>();
                currentRange = mainPanel.Find("Range").GetComponent<UIRange>();
                currentFrameLabel = mainPanel.Find("CurrentFrameLabel").GetComponent<UILabel>();
                titleBar = transform.parent.Find("TitleBar").GetComponent<UILabel>();
                keyframePrefab = Resources.Load<GameObject>("Prefabs/UI/DOPESHEET/Keyframe");

                constantInterpolationButton = mainPanel.Find("Constant").GetComponent<UIButton>();
                linearInterpolationButton = mainPanel.Find("Linear").GetComponent<UIButton>();
                bezierInterpolationButton = mainPanel.Find("Bezier").GetComponent<UIButton>();
                ColorUtility.TryParseHtmlString("#5985FF", out constantInterpolationColor);
                ColorUtility.TryParseHtmlString("#FFB600", out linearInterpolationColor);
                ColorUtility.TryParseHtmlString("#FF2D5E", out bezierInterpolationColor);

                GlobalState.Instance.onPlayingEvent.AddListener(OnPlayingChanged);
                GlobalState.Instance.onRecordEvent.AddListener(OnRecordingChanged);
                GlobalState.ObjectRenamedEvent.AddListener(OnCameraNameChanged);

                currentRange.GlobalRange = new Vector2(GlobalState.startFrame, GlobalState.endFrame);
                currentRange.CurrentRange = new Vector2(GlobalState.startFrame, GlobalState.endFrame);

                UpdateInterpolation();
            }
        }

        private void UpdateInterpolation()
        {
            Interpolation interpolation = GlobalState.Settings.interpolation;
            constantInterpolationButton.Checked = interpolation == Interpolation.Constant;
            linearInterpolationButton.Checked = interpolation == Interpolation.Linear;
            bezierInterpolationButton.Checked = interpolation == Interpolation.Bezier;
        }
        public void OnSetInterpolationConstant()
        {
            GlobalState.Settings.interpolation = Interpolation.Constant;
            UpdateInterpolation();
        }
        public void OnSetInterpolationLinear()
        {
            GlobalState.Settings.interpolation = Interpolation.Linear;
            UpdateInterpolation();
        }
        public void OnSetInterpolationBezier()
        {
            GlobalState.Settings.interpolation = Interpolation.Bezier;
            UpdateInterpolation();
        }

        public void OnEditCurrentFrame()
        {
            ToolsUIManager.Instance.OpenNumericKeyboard((float value) => OnChangeCurrentFrame((int) value), currentFrameLabel.transform, GlobalState.currentFrame);
        }

        public void OnGlobalRangeChanged(Vector2Int globalBounds)
        {
            GlobalState.startFrame = globalBounds.x;
            GlobalState.endFrame = globalBounds.y;
            FrameStartEnd info = new FrameStartEnd() { start = globalBounds.x, end = globalBounds.y };
            NetworkClient.GetInstance().SendEvent<FrameStartEnd>(MessageType.FrameStartEnd, info);
        }

        public void OnLocalRangeChanged(Vector2Int bounds)
        {
            FirstFrame = bounds.x;
            LastFrame = bounds.y;

            UpdateKeyframes();
        }

        private void OnPlayingChanged(bool value)
        {
            if (GlobalState.Instance.recordState != GlobalState.RecordState.Recording)
            {
                titleBar.Pushed = value;
            }
            else
            {
                titleBar.Pushed = false;
            }
        }

        private void OnRecordingChanged(bool value)
        {
            titleBar.Pushed = false;
            titleBar.Hovered = value;
        }

        private void Update()
        {
            bool enable = transform.localScale.x != 0f;
            if (enable)
            {
                if (!listenerAdded)
                    GlobalState.Instance.AddAnimationListener(UpdateCurrentObjectChannel);
            }
            else
            {
                if (listenerAdded)
                    GlobalState.Instance.RemoveAnimationListener(UpdateCurrentObjectChannel);
            }
            listenerAdded = enable;

            if (currentRange.GlobalRange.x != GlobalState.startFrame || currentRange.GlobalRange.y != GlobalState.endFrame)
            {
                currentRange.GlobalRange = new Vector2(GlobalState.startFrame, GlobalState.endFrame);
            }

            if (CurrentFrame != GlobalState.currentFrame)
            {
                CurrentFrame = GlobalState.currentFrame;
            }
        }

        private void UpdateFirstFrame()
        {
            //currentRange.GlobalRange = new Vector2();

            if (timeBar != null)
            {
                timeBar.MinValue = firstFrame; // updates knob position
            }
        }

        private void UpdateLastFrame()
        {
            if (timeBar != null)
            {
                timeBar.MaxValue = lastFrame; // updates knob position
            }
        }

        private void UpdateCurrentFrame()
        {
            if (currentFrameLabel != null)
            {
                int frames = GlobalState.currentFrame % 24;
                TimeSpan t = TimeSpan.FromSeconds(GlobalState.currentFrame / 24f);
                currentFrameLabel.Text = $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}:{frames:D2} / {GlobalState.currentFrame}";

            }
            if (timeBar != null)
            {
                timeBar.Value = GlobalState.currentFrame; // changes the knob's position
            }
        }

        public void Show(bool doShow)
        {
            if (mainPanel != null)
            {
                mainPanel.gameObject.SetActive(doShow);
            }
        }

        protected void UpdateCurrentObjectChannel(GameObject gObject, AnimationChannel channel)
        {
            if (null == currentObject)
                return;
            if (currentObject != gObject)
                return;

            if (null == channel) // channel == null => remove animations
            {
                Clear();
                UpdateTrackName();
                return;
            }

            // operates only on location.z
            if (channel.name != "location" || channel.index != 2)
                return;

            Clear();
            foreach (AnimationKey key in channel.keys)
            {
                List<AnimKey> keyList = null;
                if (!keys.TryGetValue(key.time, out keyList))
                {
                    keyList = new List<AnimKey>();
                    keys[key.time] = keyList;
                }

                keyList.Add(new AnimKey(channel.name, key.value, key.interpolation));
            }

            UpdateTrackName();

            Transform keyframes = transform.Find("MainPanel/Tracks/Summary/Keyframes");
            foreach (var key in keys)
            {
                GameObject keyframe = GameObject.Instantiate(keyframePrefab, keyframes);
                List<AnimKey> animKeys = key.Value;
                AnimKey firstKey = animKeys[0];
                switch (firstKey.interpolation)
                {
                    case Interpolation.Constant:
                        keyframe.GetComponent<MeshRenderer>().material.SetColor("_UnlitColor", constantInterpolationColor);
                        break;
                    case Interpolation.Linear:
                        keyframe.GetComponent<MeshRenderer>().material.SetColor("_UnlitColor", linearInterpolationColor);
                        break;
                    case Interpolation.Bezier:
                        keyframe.GetComponent<MeshRenderer>().material.SetColor("_UnlitColor", bezierInterpolationColor);
                        break;
                }
            }

            UpdateKeyframes();
        }

        void UpdateKeyframes()
        {
            Transform keyframes = transform.Find("MainPanel/Tracks/Summary/Keyframes");
            UIKeyView track = keyframes.gameObject.GetComponent<UIKeyView>();
            int i = 0;
            foreach (var key in keys)
            {
                GameObject keyframe = keyframes.GetChild(i++).gameObject;

                float time = key.Key;
                float currentValue = (float) time;
                float pct = (float) (currentValue - firstFrame) / (float) (lastFrame - firstFrame);

                float startX = 0.0f;
                float endX = timeBar.width;
                float posX = startX + pct * (endX - startX);

                Vector3 knobPosition = new Vector3(posX, -0.5f * track.height, 0.0f);

                if (time < FirstFrame || time > LastFrame)
                {
                    keyframe.SetActive(false); // clip out of range keyframes
                }
                else
                {
                    keyframe.SetActive(true);
                }

                keyframe.transform.localPosition = knobPosition;
            }
        }

        int GetKeyAtFrame(int index)
        {
            if (index < 0 || index >= keys.Count)
                return -1;

            return keys.Keys[index];
        }

        // delta = +/- 1
        public void OnUpdateKeyframe(int i, int delta)
        {
            int frame = GetKeyAtFrame(i);
            if (frame == -1)
                return;

            CommandGroup group = new CommandGroup("Add Keyframe");
            try
            {
                foreach (GameObject item in Selection.selection.Values)
                {
                    new CommandMoveKeyframes(item, frame, frame+delta).Submit();
                }
            }
            finally
            {
                group.Submit();
            }
        }

        void OnCameraNameChanged(GameObject gObject)
        {
            if (Selection.IsSelected(gObject) || Selection.GetHoveredObject() == gObject)
                OnSelectionChanged(gObject);
        }

        public void OnSelectionChanged(GameObject gObject)
        {
            if (currentObject != gObject)
            {
                currentObject = gObject;
                if (null == currentObject)
                {
                    Clear();
                }
                UpdateTrackName();
            }
        }

        public int GetNextKeyFrame()
        {
            foreach (int t in keys.Keys)
            {
                // TODO: dichotomic search
                if (t > CurrentFrame)
                    return t;
            }

            return FirstFrame;
        }

        public int GetPreviousKeyFrame()
        {
            for (int i = keys.Keys.Count - 1; i >= 0; i--)
            {
                // TODO: dichotomic search
                int t = keys.Keys[i];
                if (t < CurrentFrame)
                    return t;
            }

            return LastFrame;
        }

        public void UpdateTrackName()
        {
            TextMeshProUGUI trackLabel = transform.Find("MainPanel/Tracks/Summary/Label/Canvas/Text").GetComponent<TextMeshProUGUI>();
            List<GameObject> selectedObjects = Selection.GetSelectedObjects(SelectionType.Hovered | SelectionType.Selection | SelectionType.Gripped);
            int count = selectedObjects.Count;
            if (count > 1)
            {
                trackLabel.text = count.ToString() + " Objects";
                return;
            }
            foreach (GameObject obj in selectedObjects)
            {
                trackLabel.text = obj.name;
                return;
            }
            trackLabel.text = "";
        }

        public void Clear()
        {
            Transform tracks = transform.Find("MainPanel/Tracks");
            for (int i = 0; i < tracks.childCount; ++i)
            {
                Transform track = tracks.GetChild(i);
                string channelName = track.name;
                string trackName = $"MainPanel/Tracks/{channelName}/Keyframes";
                Transform keyframes = transform.Find(trackName);
                for (int j = keyframes.childCount - 1; j >= 0; j--)
                {
                    GameObject kfo = keyframes.GetChild(j).gameObject;
                    kfo.transform.parent = null;
                    Destroy(kfo);
                }
            }

            keys.Clear();
        }

        // called by the slider when moved
        public void OnChangeCurrentFrame(int i)
        {
            FrameInfo info = new FrameInfo() { frame = i };
            NetworkClient.GetInstance().SendEvent<FrameInfo>(MessageType.Frame, info);
        }

        public void OnPrevKeyFrame()
        {
            onPreviousKeyframeEvent.Invoke(CurrentFrame);
        }

        public void OnNextKeyFrame()
        {
            onNextKeyframeEvent.Invoke(CurrentFrame);
        }

        public void OnAddKeyFrame()
        {
            CommandGroup group = new CommandGroup("Add Keyframe");
            try
            {
                foreach (GameObject item in Selection.selection.Values)
                {
                    new CommandAddKeyframes(item).Submit();
                }
            }
            finally
            {
                group.Submit();
            }
        }

        public void OnRemoveKeyFrame()
        {
            CommandGroup group = new CommandGroup("Remove Keyframe");
            try
            {
                foreach (GameObject item in Selection.selection.Values)
                {
                    new CommandRemoveKeyframes(item).Submit();
                }
            }
            finally
            {
                group.Submit();
            }
        }

        public void OnClearAnimations()
        {
            CommandGroup group = new CommandGroup("Clear Animations");
            try
            {
                foreach (GameObject gObject in Selection.selection.Values)
                {
                    new CommandClearAnimations(gObject).Submit();
                }
            }
            finally
            {
                group.Submit();
            }
        }

        public void OnEnableAutoKey(bool value)
        {
            GlobalState.autoKeyEnabled = value;
        }

        public bool IsAutoKeyEnabled()
        {
            return GlobalState.autoKeyEnabled;
        }

    }
}
