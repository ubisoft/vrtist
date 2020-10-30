using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public enum AnimationState
    {
        Stopped,
        Preroll,
        Recording,
        Playing
    };

    public enum AnimatableProperty
    {
        PositionX, PositionY, PositionZ,
        RotationX, RotationY, RotationZ,
        ScaleX, ScaleY, ScaleZ,
        LightIntensity, ColorR, ColorG, ColorB,
        CameraFocal,
        Unknown
    }

    public class AnimationKey
    {
        public AnimationKey(int frame, float value, Interpolation interpolation = Interpolation.Bezier)
        {
            this.frame = frame;
            this.value = value;
            this.interpolation = interpolation;
        }
        public int frame;
        public float value;
        public Interpolation interpolation;
    }

    // TODO AnimationKey of Vector3 to optimize?????

    public class Curve
    {
        public AnimatableProperty property;
        public List<AnimationKey> keys;
        private int[] framedKeys;

        public Curve(AnimatableProperty property)
        {
            this.property = property;
            keys = new List<AnimationKey>();
            framedKeys = new int[GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1];
            for (int i = 0; i < framedKeys.Length; i++)
                framedKeys[i] = -1;
        }

        public void ClearCache()
        {
            framedKeys = null;
        }

        public void ComputeCache()
        {
            if (framedKeys.Length != GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1)
                framedKeys = new int[GlobalState.Animation.EndFrame - GlobalState.Animation.StartFrame + 1];

            if (keys.Count == 0)
            {
                for (int i = 0; i < framedKeys.Length; i++)
                    framedKeys[i] = -1;

                return;
            }

            bool firstKeyFoundInRange = false;
            int lastKeyIndex = 0;
            for (int i = 0; i < keys.Count - 1; i++)
            {
                float keyTime = keys[i].frame;
                if (keyTime < GlobalState.Animation.StartFrame || keyTime > GlobalState.Animation.EndFrame)
                    continue;

                int b1 = keys[i].frame - GlobalState.Animation.StartFrame;
                int b2 = keys[i + 1].frame - GlobalState.Animation.StartFrame;
                b2 = Mathf.Clamp(b2, b1, framedKeys.Length);

                if (!firstKeyFoundInRange) // Fill framedKeys from 0 to first key
                {
                    for (int j = 0; j < b1; j++)
                    {
                        framedKeys[j] = i - 1;
                    }
                    firstKeyFoundInRange = true;
                }

                for (int j = b1; j < b2; j++)
                    framedKeys[j] = i;
                lastKeyIndex = i;
            }

            // found no key in range
            if (!firstKeyFoundInRange)
            {
                int index = -1;
                if (keys[keys.Count - 1].frame < GlobalState.Animation.StartFrame)
                    index = keys.Count - 1;
                for (int i = 0; i < framedKeys.Length; i++)
                    framedKeys[i] = index;
                return;
            }

            // fill framedKey from last key found to end
            lastKeyIndex++;
            lastKeyIndex = Math.Min(lastKeyIndex, keys.Count - 1);
            int jmin = Math.Max(0, keys[lastKeyIndex].frame - GlobalState.Animation.StartFrame);
            for (int j = jmin; j < framedKeys.Length; j++)
            {
                framedKeys[j] = lastKeyIndex;
            }
        }

        private bool GetKeyIndex(int frame, out int index)
        {
            index = framedKeys[frame - GlobalState.Animation.StartFrame];
            if (index == -1)
                return false;

            AnimationKey key = keys[index];
            return key.frame == frame;
        }

        public void SetKeys(List<AnimationKey> k)
        {
            keys = k;
            ComputeCache();
        }

        public void RemoveKey(int frame)
        {
            if (GetKeyIndex(frame, out int index))
            {
                AnimationKey key = keys[index];
                int start = key.frame - GlobalState.Animation.StartFrame;
                int end = framedKeys.Length - 1;
                for (int i = start; i <= end; i++)
                    framedKeys[i]--;

                keys.RemoveAt(index);
            }
        }

        public void AppendKey(AnimationKey key)
        {
            keys.Add(key);
        }

        public void AddKey(AnimationKey key)
        {
            if (GetKeyIndex(key.frame, out int index))
            {
                keys[index] = key;
            }
            else
            {
                index++;
                keys.Insert(index, key);

                int end = framedKeys.Length - 1;
                if (index + 1 < keys.Count)
                {
                    end = keys[index + 1].frame - GlobalState.Animation.StartFrame - 1;
                    end = Mathf.Clamp(end, 0, framedKeys.Length - 1);
                }

                int start = key.frame - GlobalState.Animation.StartFrame;
                start = Mathf.Clamp(start, 0, end);
                for (int i = start; i <= end; i++)
                    framedKeys[i] = index;
                for (int i = end + 1; i < framedKeys.Length; i++)
                    framedKeys[i]++;
            }
        }

        public void MoveKey(int oldFrame, int newFrame)
        {
            if (GetKeyIndex(oldFrame, out int index))
            {
                AnimationKey key = keys[index];
                RemoveKey(key.frame);
                key.frame = newFrame;
                AddKey(key);
            }
        }

        public AnimationKey GetKey(int index)
        {
            return keys[index];
        }

        public AnimationKey GetPreviousKey(int frame)
        {
            --frame;
            frame -= GlobalState.Animation.StartFrame;
            if (frame >= 0 && frame < framedKeys.Length)
            {
                int index = framedKeys[frame];
                if (index != -1)
                {
                    return keys[index];
                }
            }
            return null;
        }

        public bool TryFindKey(int frame, out AnimationKey key)
        {
            if (GetKeyIndex(frame, out int index))
            {
                key = keys[index];
                return true;
            }
            key = null;
            return false;
        }

        private Vector2 CubicBezier(Vector2 A, Vector2 B, Vector2 C, Vector2 D, float t)
        {
            float invT1 = 1 - t;
            float invT2 = invT1 * invT1;
            float invT3 = invT2 * invT1;

            float t2 = t * t;
            float t3 = t2 * t;

            return (A * invT3) + (B * 3 * t * invT2) + (C * 3 * invT1 * t2) + (D * t3);
        }


        public float Evaluate(int frame)
        {
            if (keys.Count == 0)
                return 0;

            int prevIndex = framedKeys[frame - GlobalState.Animation.StartFrame];
            if (prevIndex == -1)
                return keys[0].value;
            if (prevIndex == keys.Count - 1)
                return keys[keys.Count - 1].value;

            AnimationKey prevKey = keys[prevIndex];
            switch (prevKey.interpolation)
            {
                case Interpolation.Constant:
                    return prevKey.value;

                case Interpolation.Linear:
                    {
                        AnimationKey nextKey = keys[prevIndex + 1];
                        float dt = (float) (frame - prevKey.frame) / (float) (nextKey.frame - prevKey.frame);
                        float oneMinusDt = 1f - dt;
                        return prevKey.value * oneMinusDt + nextKey.value * dt;
                    }

                case Interpolation.Bezier:
                    {
                        AnimationKey nextKey = keys[prevIndex + 1];
                        float rangeDt = (float) (nextKey.frame - prevKey.frame);

                        Vector2 A = new Vector2(prevKey.frame, prevKey.value);
                        Vector2 B, C;
                        Vector2 D = new Vector2(nextKey.frame, nextKey.value);

                        if (prevIndex == 0)
                        {
                            B = A + (D - A) / 3f;
                        }
                        else
                        {
                            AnimationKey prevPrevKey = keys[prevIndex - 1];
                            Vector2 V = (D - new Vector2(prevPrevKey.frame, prevPrevKey.value)).normalized;
                            Vector2 AD = D - A;
                            B = A + V * AD.magnitude / 3f;
                        }

                        if (prevIndex + 2 >= keys.Count)
                        {
                            C = D - (D - A) / 3f;
                        }
                        else
                        {
                            AnimationKey nextNextKey = keys[prevIndex + 2];
                            Vector2 V = (new Vector2(nextNextKey.frame, nextNextKey.value) - A).normalized;
                            Vector2 AD = D - A;
                            C = D - V * AD.magnitude / 3f;
                        }

                        float dt = (float) (frame - prevKey.frame) / rangeDt;
                        return CubicBezier(A, B, C, D, dt).y;
                    }

            }
            return 0f;
        }
    }

    public class AnimationSet
    {
        public Transform transform;
        public readonly Dictionary<AnimatableProperty, Curve> curves = new Dictionary<AnimatableProperty, Curve>();

        public AnimationSet(GameObject gobject)
        {
            transform = gobject.transform;
            CreateTransformCurves();
            LightController lightController = gobject.GetComponent<LightController>();
            if (null != lightController) { CreateLightCurves(); }
            CameraController cameraController = gobject.GetComponent<CameraController>();
            if (null != cameraController) { CreateCameraCurves(); }
        }

        public Curve GetCurve(AnimatableProperty property)
        {
            curves.TryGetValue(property, out Curve result);
            return result;
        }

        public void SetCurve(AnimatableProperty property, List<AnimationKey> keys)
        {
            if (!curves.TryGetValue(property, out Curve curve))
            {
                Debug.LogError("Curve not found : " + transform.name + " " + property.ToString());
                return;
            }

            curve.SetKeys(keys);
        }


        private void CreateTransformCurves()
        {
            curves.Add(AnimatableProperty.PositionX, new Curve(AnimatableProperty.PositionX));
            curves.Add(AnimatableProperty.PositionY, new Curve(AnimatableProperty.PositionY));
            curves.Add(AnimatableProperty.PositionZ, new Curve(AnimatableProperty.PositionZ));

            curves.Add(AnimatableProperty.RotationX, new Curve(AnimatableProperty.RotationX));
            curves.Add(AnimatableProperty.RotationY, new Curve(AnimatableProperty.RotationY));
            curves.Add(AnimatableProperty.RotationZ, new Curve(AnimatableProperty.RotationZ));

            curves.Add(AnimatableProperty.ScaleX, new Curve(AnimatableProperty.ScaleX));
            curves.Add(AnimatableProperty.ScaleY, new Curve(AnimatableProperty.ScaleY));
            curves.Add(AnimatableProperty.ScaleZ, new Curve(AnimatableProperty.ScaleZ));
        }

        private void CreateLightCurves()
        {
            curves.Add(AnimatableProperty.LightIntensity, new Curve(AnimatableProperty.LightIntensity));
            curves.Add(AnimatableProperty.ColorR, new Curve(AnimatableProperty.ColorR));
            curves.Add(AnimatableProperty.ColorG, new Curve(AnimatableProperty.ColorG));
            curves.Add(AnimatableProperty.ColorB, new Curve(AnimatableProperty.ColorB));
        }

        private void CreateCameraCurves()
        {
            curves.Add(AnimatableProperty.CameraFocal, new Curve(AnimatableProperty.CameraFocal));
        }

        public void ComputeCache()
        {
            foreach (Curve curve in curves.Values)
                curve.ComputeCache();
        }

        public void ClearCache()
        {
            foreach (Curve curve in curves.Values)
                curve.ClearCache();
        }
    }

    public class AnimationEngine : MonoBehaviour
    {
        // All animations
        Dictionary<GameObject, AnimationSet> animations = new Dictionary<GameObject, AnimationSet>();
        Dictionary<GameObject, AnimationSet> disabledAnimations = new Dictionary<GameObject, AnimationSet>();
        Dictionary<GameObject, AnimationSet> recordingObjects = new Dictionary<GameObject, AnimationSet>();
        Dictionary<GameObject, AnimationSet> oldAnimations = new Dictionary<GameObject, AnimationSet>();

        public float fps = 24f;
        float playStartTime;
        int playStartFrame;

        private int startFrame = 1;
        public int StartFrame
        {
            get { return startFrame; }
            set
            {
                startFrame = value;
                if (startFrame >= endFrame - 1)
                    startFrame = endFrame - 1;
                if (startFrame < 0)
                    startFrame = 0;
                onRangeEvent.Invoke(new Vector2Int(startFrame, endFrame));
            }
        }

        private int endFrame = 250;
        public int EndFrame
        {
            get { return endFrame; }
            set
            {
                endFrame = value;
                if (endFrame <= (startFrame + 1))
                    endFrame = startFrame + 1;
                if (endFrame < 1)
                    endFrame = 1;
                onRangeEvent.Invoke(new Vector2Int(startFrame, endFrame));
            }
        }
        public bool loop = true;
        private int currentFrame = 1;
        public int CurrentFrame
        {
            get { return currentFrame; }
            set
            {
                currentFrame = Mathf.Clamp(value, startFrame, endFrame);

                if (animationState != AnimationState.Playing && animationState != AnimationState.Recording)
                {
                    EvaluateAnimations();
                    onFrameEvent.Invoke(value);
                }
            }
        }

        public bool autoKeyEnabled = false;

        public UIButton playButtonShortcut;

        public AnimationState animationState = AnimationState.Stopped;
        public AnimationStateChangedEvent onAnimationStateEvent = new AnimationStateChangedEvent();
        public IntChangedEvent onFrameEvent = new IntChangedEvent();
        public GameObjectChangedEvent onAddAnimation = new GameObjectChangedEvent();
        public GameObjectChangedEvent onRemoveAnimation = new GameObjectChangedEvent();
        public CurveChangedEvent onChangeCurve = new CurveChangedEvent();

        public RangeChangedEventInt onRangeEvent = new RangeChangedEventInt();

        public Countdown countdown = null;

        Interpolation preRecordInterpolation;

        // Singleton
        private static AnimationEngine instance = null;
        public static AnimationEngine Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GameObject.FindObjectOfType<AnimationEngine>();
                }
                return instance;
            }
        }

        void Awake()
        {
            instance = Instance;
        }

        private void Start()
        {
            countdown.onCountdownFinished.AddListener(StartRecording);
            onRangeEvent.AddListener(RecomputeCurvesCache);

            GlobalState.ObjectAddedEvent.AddListener(OnObjectAdded);
            GlobalState.ObjectRemovedEvent.AddListener(OnObjectRemoved);
        }

        private void Update()
        {
            // Find current time and frame & Animate objects
            if (animationState == AnimationState.Playing || animationState == AnimationState.Recording)
            {
                // Compute new frame
                float deltaTime = Time.time - playStartTime;
                int newFrame = TimeToFrame(deltaTime) + playStartFrame;

                if (currentFrame != newFrame)
                {
                    if (newFrame > endFrame)
                    {
                        if (animationState == AnimationState.Recording)
                        {
                            // Stop recording when reaching the end of the timeline
                            newFrame = endFrame;
                            Pause();
                        }
                        else if (loop)
                        {
                            newFrame = startFrame;
                            playStartFrame = startFrame;
                            playStartTime = Time.time;
                        }
                    }

                    currentFrame = newFrame;
                    EvaluateAnimations();
                    onFrameEvent.Invoke(currentFrame);

                    // Record
                    if (animationState == AnimationState.Recording)
                    {
                        RecordFrame();
                    }
                }
            }
        }

        void OnObjectAdded(GameObject gobject)
        {
            if (disabledAnimations.TryGetValue(gobject, out AnimationSet animationSet))
            {
                disabledAnimations.Remove(gobject);
                animations.Add(gobject, animationSet);
                animationSet.ComputeCache();
            }
        }
        void OnObjectRemoved(GameObject gobject)
        {
            if (animations.TryGetValue(gobject, out AnimationSet animationSet))
            {
                animations.Remove(gobject);
                disabledAnimations.Add(gobject, animationSet);
                animationSet.ClearCache();
            }
        }

        private void RecomputeCurvesCache(Vector2Int _)
        {
            foreach (AnimationSet animationSet in animations.Values)
            {
                animationSet.ComputeCache();
            }
        }

        public bool IsAnimating()
        {
            return animationState == AnimationState.Playing || animationState == AnimationState.Recording;
        }

        private void EvaluateAnimations()
        {
            foreach (AnimationSet animationSet in animations.Values)
            {
                Transform trans = animationSet.transform;
                Vector3 position = trans.localPosition;
                Vector3 rotation = trans.localEulerAngles;
                Vector3 scale = trans.localScale;

                float lightIntensity = -1;
                Color color = Color.white;

                float cameraFocal = -1;

                foreach (Curve curve in animationSet.curves.Values)
                {
                    float value = curve.Evaluate(currentFrame);
                    switch (curve.property)
                    {
                        case AnimatableProperty.PositionX: position.x = value; break;
                        case AnimatableProperty.PositionY: position.y = value; break;
                        case AnimatableProperty.PositionZ: position.z = value; break;

                        case AnimatableProperty.RotationX: rotation.x = value; break;
                        case AnimatableProperty.RotationY: rotation.y = value; break;
                        case AnimatableProperty.RotationZ: rotation.z = value; break;

                        case AnimatableProperty.ScaleX: scale.x = value; break;
                        case AnimatableProperty.ScaleY: scale.y = value; break;
                        case AnimatableProperty.ScaleZ: scale.z = value; break;

                        case AnimatableProperty.LightIntensity: lightIntensity = value; break;
                        case AnimatableProperty.ColorR: color.r = value; break;
                        case AnimatableProperty.ColorG: color.g = value; break;
                        case AnimatableProperty.ColorB: color.b = value; break;

                        case AnimatableProperty.CameraFocal: cameraFocal = value; break;
                    }
                }

                trans.localPosition = position;
                trans.localEulerAngles = rotation;
                trans.localScale = scale;

                if (lightIntensity != -1)
                {
                    LightController controller = trans.GetComponent<LightController>();
                    controller.intensity = lightIntensity;
                    controller.color = color;
                }

                if (cameraFocal != -1)
                {
                    CameraController controller = trans.GetComponent<CameraController>();
                    controller.focal = cameraFocal;
                }
            }
        }

        public int TimeToFrame(float time)
        {
            return (int) (fps * time);
        }

        public float FrameToTime(int frame)
        {
            return (float) frame / fps;
        }

        public AnimationSet GetObjectAnimation(GameObject gobject)
        {
            animations.TryGetValue(gobject, out AnimationSet animationSet);
            return animationSet;
        }

        public void SetObjectAnimation(GameObject gobject, AnimationSet animationSet)
        {
            animations[gobject] = animationSet;
            onAddAnimation.Invoke(gobject);
        }

        public void ClearAnimations(GameObject gobject)
        {
            if (animations.Remove(gobject))
                onRemoveAnimation.Invoke(gobject);
        }

        public void MoveKeyframe(GameObject gobject, AnimatableProperty property, int frame, int newFrame)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
                return;

            animationSet.GetCurve(property).MoveKey(frame, newFrame);
            if (!IsAnimating())
                EvaluateAnimations();
            onChangeCurve.Invoke(gobject, property);
        }

        public AnimationSet GetOrCreateObjectAnimation(GameObject gobject)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
            {
                animationSet = new AnimationSet(gobject);
                animations.Add(gobject, animationSet);
            }
            return animationSet;
        }

        public void AddKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
            {
                animationSet = new AnimationSet(gobject);
                animations.Add(gobject, animationSet);
            }
            Curve curve = animationSet.GetCurve(property);
            curve.AddKey(key);
            onChangeCurve.Invoke(gobject, property);
        }

        // To only be used by in-app add key (not from networked keys)
        public void AddFilteredKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
            {
                animationSet = new AnimationSet(gobject);
                animations.Add(gobject, animationSet);
            }
            Curve curve = animationSet.GetCurve(property);

            // Filter rotation
            if (property == AnimatableProperty.RotationX || property == AnimatableProperty.RotationY || property == AnimatableProperty.RotationZ)
            {
                AnimationKey previousKey = curve.GetPreviousKey(key.frame);
                if (null != previousKey)
                {
                    float delta = Mathf.DeltaAngle(previousKey.value, key.value);
                    key.value = previousKey.value + delta;
                }
            }

            curve.AddKey(key);
            onChangeCurve.Invoke(gobject, property);
        }

        public void RemoveKeyframe(GameObject gobject, AnimatableProperty property, int frame)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
                return;
            Curve curve = animationSet.GetCurve(property);
            curve.RemoveKey(frame);
            if (!IsAnimating())
                EvaluateAnimations();
            onChangeCurve.Invoke(gobject, property);
        }

        public void Record()
        {
            if (animationState != AnimationState.Stopped)
                return;

            animationState = AnimationState.Preroll;
            countdown.gameObject.SetActive(true);
        }

        public void OnTogglePlayPause(bool play)
        {
            if (play) { Play(); }
            else { Pause(); }
        }

        public void Play()
        {
            animationState = AnimationState.Playing;
            onAnimationStateEvent.Invoke(animationState);

            playStartFrame = currentFrame;
            playStartTime = Time.time;
        }

        public void Pause()
        {
            switch (animationState)
            {
                case AnimationState.Preroll:
                    countdown.gameObject.SetActive(false);
                    break;
                case AnimationState.Recording:
                    StopRecording();
                    countdown.gameObject.SetActive(false);
                    break;
                case AnimationState.Playing:
                    playButtonShortcut.Checked = false;  // A déplacer !!!!
                    break;

            }
            animationState = AnimationState.Stopped;
            onAnimationStateEvent.Invoke(animationState);
        }

        public void StartRecording()
        {
            playStartFrame = currentFrame;
            playStartTime = Time.time;
            animationState = AnimationState.Recording;
            onAnimationStateEvent.Invoke(animationState);
            preRecordInterpolation = GlobalState.Settings.interpolation;
            GlobalState.Settings.interpolation = Interpolation.Linear;
        }

        void RecordFrame()
        {
            foreach (var selected in Selection.GetGrippedOrSelection())
            {
                if (!recordingObjects.TryGetValue(selected, out AnimationSet animationSet))
                {
                    oldAnimations.Add(selected, GetObjectAnimation(selected));

                    // Remove existing animation
                    animations.Remove(selected);
                    onRemoveAnimation.Invoke(selected);

                    // Create new one
                    animationSet = new AnimationSet(selected);
                    recordingObjects.Add(selected, animationSet);
                }

                foreach (Curve curve in animationSet.curves.Values)
                {
                    Vector3 position = selected.transform.localPosition;
                    Vector3 rotation = selected.transform.localEulerAngles;
                    Vector3 scale = selected.transform.localScale;

                    float lightIntensity = -1;
                    Color color = Color.white;
                    LightController lightController = selected.GetComponent<LightController>();
                    if (null != lightController)
                    {
                        lightIntensity = lightController.intensity;
                        color = lightController.color;
                    }

                    float cameraFocal = -1;
                    CameraController cameraController = selected.GetComponent<CameraController>();
                    if (null != cameraController)
                    {
                        cameraFocal = cameraController.focal;
                    }

                    switch (curve.property)
                    {
                        case AnimatableProperty.PositionX: curve.AppendKey(new AnimationKey(currentFrame, position.x)); break;
                        case AnimatableProperty.PositionY: curve.AppendKey(new AnimationKey(currentFrame, position.y)); break;
                        case AnimatableProperty.PositionZ: curve.AppendKey(new AnimationKey(currentFrame, position.z)); break;

                        case AnimatableProperty.RotationX: AppendFilteredKey(curve, currentFrame, rotation.x); break;
                        case AnimatableProperty.RotationY: AppendFilteredKey(curve, currentFrame, rotation.y); break;
                        case AnimatableProperty.RotationZ: AppendFilteredKey(curve, currentFrame, rotation.z); break;

                        case AnimatableProperty.ScaleX: curve.AppendKey(new AnimationKey(currentFrame, scale.x)); break;
                        case AnimatableProperty.ScaleY: curve.AppendKey(new AnimationKey(currentFrame, scale.y)); break;
                        case AnimatableProperty.ScaleZ: curve.AppendKey(new AnimationKey(currentFrame, scale.z)); break;

                        case AnimatableProperty.LightIntensity: curve.AppendKey(new AnimationKey(currentFrame, lightIntensity)); break;
                        case AnimatableProperty.ColorR: curve.AppendKey(new AnimationKey(currentFrame, color.r)); break;
                        case AnimatableProperty.ColorG: curve.AppendKey(new AnimationKey(currentFrame, color.g)); break;
                        case AnimatableProperty.ColorB: curve.AppendKey(new AnimationKey(currentFrame, color.b)); break;

                        case AnimatableProperty.CameraFocal: curve.AppendKey(new AnimationKey(currentFrame, cameraFocal)); break;
                    }
                }
            }
        }

        private void AppendFilteredKey(Curve curve, int frame, float value)
        {
            if (curve.keys.Count > 0)
            {
                AnimationKey previousKey = curve.keys[curve.keys.Count - 1];
                float delta = Mathf.DeltaAngle(previousKey.value, value);
                value = previousKey.value + delta;
            }
            curve.AppendKey(new AnimationKey(frame, value));
        }

        public void StopRecording()
        {
            CommandGroup recordGroup = new CommandGroup("Record");

            foreach (var animationSet in recordingObjects.Values)
            {
                GameObject gobject = animationSet.transform.gameObject;
                foreach (Curve curve in animationSet.curves.Values)
                    curve.ComputeCache();
                new CommandRecordAnimations(gobject, oldAnimations[gobject], animationSet).Submit();
            }

            recordGroup.Submit();

            recordingObjects.Clear();
            oldAnimations.Clear();
            GlobalState.Settings.interpolation = preRecordInterpolation;
        }
    }
}
