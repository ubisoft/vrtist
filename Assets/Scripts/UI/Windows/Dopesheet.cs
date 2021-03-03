/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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

        private int localFirstFrame = 0;
        private int localLastFrame = 250;

        public int LocalFirstFrame { get { return localFirstFrame; } set { localFirstFrame = value; UpdateFirstFrame(); } }
        public int LocalLastFrame { get { return localLastFrame; } set { localLastFrame = value; UpdateLastFrame(); } }

        private GameObject keyframePrefab;
        private GameObject currentObject = null;

        public class AnimKey
        {
            public AnimKey(float value, Interpolation interpolation)
            {
                this.value = value;
                this.interpolation = interpolation;
            }
            public float value;
            public Interpolation interpolation;
        }

        private readonly SortedList<int, List<AnimKey>> keys = new SortedList<int, List<AnimKey>>();
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

                currentRange.CurrentRange = new Vector2(GlobalState.Animation.StartFrame, GlobalState.Animation.EndFrame);
                localFirstFrame = GlobalState.Animation.StartFrame;
                localLastFrame = GlobalState.Animation.EndFrame;

                UpdateInterpolation();
            }
        }

        private void OnFrameChanged(int frame)
        {
            UpdateCurrentFrame();
        }

        private void OnRangeChanged(Vector2Int range)
        {
            UpdateFirstFrame();
            UpdateLastFrame();
            UpdateKeyframes();
            currentRange.UpdateGlobalRange();
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
            ToolsUIManager.Instance.OpenNumericKeyboard((float value) => OnChangeCurrentFrame((int)value), currentFrameLabel.transform, GlobalState.Animation.CurrentFrame);
        }

        public void OnGlobalRangeChanged(Vector2Int globalBounds)
        {
            SceneManager.SetFrameRange(globalBounds.x, globalBounds.y);
        }

        public void OnLocalRangeChanged(Vector2Int bounds)
        {
            LocalFirstFrame = bounds.x;
            LocalLastFrame = bounds.y;

            UpdateKeyframes();
        }

        private void OnAnimationStateChanged(AnimationState state)
        {
            titleBar.Pushed = false;
            titleBar.Hovered = false;

            switch (state)
            {
                case AnimationState.Playing: titleBar.Pushed = true; break;
                case AnimationState.Recording: titleBar.Hovered = true; break;
            }
        }

        private void Update()
        {
            bool enable = transform.localScale.x != 0f;
            if (enable)
            {
                if (!listenerAdded)
                {
                    GlobalState.Animation.onAnimationStateEvent.AddListener(OnAnimationStateChanged);
                    GlobalState.ObjectRenamedEvent.AddListener(OnCameraNameChanged);
                    GlobalState.Animation.onFrameEvent.AddListener(OnFrameChanged);
                    GlobalState.Animation.onRangeEvent.AddListener(OnRangeChanged);

                    Selection.onSelectionChanged.AddListener(OnSelectionChanged);
                    Selection.onAuxiliarySelectionChanged.AddListener(OnAuxiliaryChanged);

                    GlobalState.Animation.onAddAnimation.AddListener(UpdateCurrentObjectAnimation);
                    GlobalState.Animation.onRemoveAnimation.AddListener(UpdateCurrentObjectAnimation);
                    GlobalState.Animation.onChangeCurve.AddListener(OnCurveUpdated);
                    UpdateSelectionChanged();
                }
            }
            else
            {
                if (listenerAdded)
                {
                    GlobalState.Animation.onAnimationStateEvent.RemoveListener(OnAnimationStateChanged);
                    GlobalState.ObjectRenamedEvent.RemoveListener(OnCameraNameChanged);
                    GlobalState.Animation.onFrameEvent.RemoveListener(OnFrameChanged);
                    GlobalState.Animation.onRangeEvent.RemoveListener(OnRangeChanged);

                    Selection.onSelectionChanged.RemoveListener(OnSelectionChanged);
                    Selection.onAuxiliarySelectionChanged.RemoveListener(OnAuxiliaryChanged);

                    GlobalState.Animation.onAddAnimation.RemoveListener(UpdateCurrentObjectAnimation);
                    GlobalState.Animation.onRemoveAnimation.RemoveListener(UpdateCurrentObjectAnimation);
                    GlobalState.Animation.onChangeCurve.RemoveListener(OnCurveUpdated);
                }
            }
            listenerAdded = enable;
        }

        private void UpdateFirstFrame()
        {
            //currentRange.GlobalRange = new Vector2();

            if (timeBar != null)
            {
                timeBar.MinValue = localFirstFrame; // updates knob position
            }
        }

        private void UpdateLastFrame()
        {
            if (timeBar != null)
            {
                timeBar.MaxValue = localLastFrame; // updates knob position
            }
        }

        private void UpdateCurrentFrame()
        {
            if (currentFrameLabel != null)
            {
                int frames = GlobalState.Animation.CurrentFrame % (int)GlobalState.Animation.fps;
                TimeSpan t = TimeSpan.FromSeconds(GlobalState.Animation.CurrentFrame / GlobalState.Animation.fps);
                currentFrameLabel.Text = $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}:{frames:D2} / {GlobalState.Animation.CurrentFrame}";
            }
            if (timeBar != null)
            {
                timeBar.Value = GlobalState.Animation.CurrentFrame; // changes the knob's position
            }
        }

        public void Show(bool doShow)
        {
            if (mainPanel != null)
            {
                mainPanel.gameObject.SetActive(doShow);
            }
        }

        void OnCurveUpdated(GameObject gobject, AnimatableProperty property)
        {
            if (property == AnimatableProperty.PositionX)
            {
                UpdateCurrentObjectAnimation(gobject);
            }
        }
        void UpdateCurrentObjectAnimation(GameObject gObject)
        {
            if (null == currentObject || currentObject != gObject)
                return;

            Clear();

            AnimationSet animationSet = GlobalState.Animation.GetObjectAnimation(gObject);
            if (null == animationSet)
            {
                UpdateTrackName();
                return;
            }

            // Take only one curve (the first one) to add keys
            foreach (AnimationKey key in animationSet.curves[0].keys)
            {
                if (!keys.TryGetValue(key.frame, out List<AnimKey> keyList))
                {
                    keyList = new List<AnimKey>();
                    keys[key.frame] = keyList;
                }

                keyList.Add(new AnimKey(key.value, key.interpolation));
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
                float currentValue = (float)time;
                float pct = (float)(currentValue - localFirstFrame) / (float)(localLastFrame - localFirstFrame);

                float startX = 0.0f;
                float endX = timeBar.width;
                float posX = startX + pct * (endX - startX);

                Vector3 knobPosition = new Vector3(posX, -0.5f * track.height, 0.0f);

                if (time < LocalFirstFrame || time > LocalLastFrame)
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
                foreach (GameObject item in Selection.SelectedObjects)
                {
                    new CommandMoveKeyframes(item, frame, frame + delta).Submit();
                }
            }
            finally
            {
                group.Submit();
            }
        }

        void OnCameraNameChanged(GameObject gObject)
        {
            if (currentObject == gObject)
            {
                UpdateTrackName();
            }
        }

        protected void UpdateSelectionChanged()
        {
            if (Selection.ActiveObjects.Count == 0)
            {
                Clear();
                UpdateTrackName();
                return;
            }

            GameObject gObject = null;
            foreach (GameObject o in Selection.ActiveObjects)
            {
                gObject = o;
                break;
            }
            currentObject = gObject;
            UpdateCurrentObjectAnimation(gObject);
        }

        protected virtual void OnSelectionChanged(HashSet<GameObject> previousSelectedObjects, HashSet<GameObject> currentSelectedObjects)
        {
            UpdateSelectionChanged();
        }
        protected virtual void OnAuxiliaryChanged(GameObject previousAuxiliarySelectedObject, GameObject currentAuxiliarySelectedObject)
        {
            UpdateSelectionChanged();
        }

        public int GetNextKeyFrame()
        {
            foreach (int t in keys.Keys)
            {
                // TODO: dichotomic search
                if (t > GlobalState.Animation.CurrentFrame)
                    return t;
            }

            return LocalFirstFrame;
        }

        public int GetPreviousKeyFrame()
        {
            for (int i = keys.Keys.Count - 1; i >= 0; i--)
            {
                // TODO: dichotomic search
                int t = keys.Keys[i];
                if (t < GlobalState.Animation.CurrentFrame)
                    return t;
            }

            return LocalLastFrame;
        }

        public void UpdateTrackName()
        {
            TextMeshProUGUI trackLabel = transform.Find("MainPanel/Tracks/Summary/Label/Canvas/Text").GetComponent<TextMeshProUGUI>();
            int count = Selection.ActiveObjects.Count;
            if (count > 1)
            {
                trackLabel.text = count.ToString() + " Objects";
                return;
            }
            foreach (GameObject obj in Selection.ActiveObjects)
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
            GlobalState.Animation.CurrentFrame = i;
        }

        public void OnPrevKeyFrame()
        {
            onPreviousKeyframeEvent.Invoke(GlobalState.Animation.CurrentFrame);
        }

        public void OnNextKeyFrame()
        {
            onNextKeyframeEvent.Invoke(GlobalState.Animation.CurrentFrame);
        }

        public void OnAddKeyFrame()
        {
            CommandGroup group = new CommandGroup("Add Keyframe");
            try
            {
                foreach (GameObject item in Selection.SelectedObjects)
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
                foreach (GameObject gObject in Selection.SelectedObjects)
                {
                    if (GlobalState.Animation.ObjectHasKeyframeAt(gObject, GlobalState.Animation.CurrentFrame))
                    {
                        new CommandRemoveKeyframes(gObject).Submit();
                    }
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
                foreach (GameObject gObject in Selection.SelectedObjects)
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
            GlobalState.Animation.autoKeyEnabled = value;
        }

        public bool IsAutoKeyEnabled()
        {
            return GlobalState.Animation.autoKeyEnabled;
        }

    }
}
