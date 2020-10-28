using System;
using System.Collections.Generic;
using UnityEngine;
using UnityScript.Steps;

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
        CameraFocal
    }

    public class AnimationKey
    {
        public AnimationKey(int time, float value, Interpolation interpolation = Interpolation.Bezier)
        {
            this.time = time;
            this.value = value;
            this.interpolation = interpolation;
        }
        public int time;
        public float value;
        public Interpolation interpolation;
    }

    // TODO AnimationKey of Vector3 to optimize?????

    public class Curve
    {
        public AnimatableProperty property;
        public List<AnimationKey> keys;

        // TODO: lastAccessedIndex to optimize researches

        public Curve(AnimatableProperty property)
        {
            this.property = property;
            keys = new List<AnimationKey>();
        }
        public Curve(AnimatableProperty property, List<AnimationKey> keys)
        {
            this.property = property;
            this.keys = keys;
        }

        private bool TryGetIndex(int time, out int index)
        {
            int count = keys.Count;
            if (count == 0)
            {
                index = 0;
                return false;
            }
            int id1 = 0, id2 = count - 1;
            while (true)
            {
                if (keys[id1].time == time)
                {
                    index = id1;
                    return true;
                }
                if (keys[id2].time == time)
                {
                    index = id2;
                    return true;
                }

                int center = (id1 + id2) / 2;
                if (time < keys[center].time)
                {
                    if (id2 == center)
                    {
                        index = id1;
                        return false;
                    }
                    id2 = center;
                }
                else
                {
                    if (id1 == center)
                    {
                        index = id2;
                        return false;
                    }
                    id1 = center;
                }
            }
        }
        public void RemoveKey(int frame)
        {
            if (TryGetIndex(frame, out int index))
            {
                keys.RemoveAt(index);
            }
        }

        public void AddKey(AnimationKey key)
        {
            if (TryGetIndex(key.time, out int index))
            {
                keys[index] = key;
            }
            else
            {
                keys.Insert(index, key);
            }
        }

        public void MoveKey(int oldFrame, int newFrame)
        {
            if (TryGetIndex(oldFrame, out int index))
            {
                AnimationKey key = keys[index];
                key.time = newFrame;
                keys.RemoveAt(index);
                AddKey(key);
            }
        }

        public AnimationKey GetKey(int index)
        {
            return keys[index];
        }

        public bool TryFindKey(int time, out AnimationKey key)
        {
            if (TryGetIndex(time, out int index))
            {
                key = keys[index];
                return true;
            }
            key = null;
            return false;
        }

        public float Evaluate(int frame)
        {
            if (TryFindKey(frame, out AnimationKey key))
            {
                return key.value;
            }
            return 0f;
        }
    }

    public class AnimationSet
    {
        public Transform transform;
        public readonly Dictionary<AnimatableProperty,Curve> curves = new Dictionary<AnimatableProperty, Curve>();

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
    }

    public class AnimationEngine : MonoBehaviour
    {
        // All animations
        Dictionary<GameObject, AnimationSet> animations = new Dictionary<GameObject, AnimationSet>();
        Dictionary<GameObject, AnimationSet> recordingObjects = new Dictionary<GameObject, AnimationSet>();
        Dictionary<GameObject, AnimationSet> oldAnimations = new Dictionary<GameObject, AnimationSet>();

        public float fps = 24f;
        float startTime;

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
                currentFrame = value;
                if(animationState != AnimationState.Playing && animationState != AnimationState.Recording)
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
        public GameObjectChangedEvent onChangeAnimation = new GameObjectChangedEvent();

        public RangeChangedEventInt onRangeEvent = new RangeChangedEventInt();


        public Countdown countdown = null;

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
        }

        private void Update()
        {
            // Find current time and frame & Animate objects
            if (animationState == AnimationState.Playing || animationState == AnimationState.Recording)
            {
                // Compute new frame
                float currentTime = Time.time - startTime;
                int newFrame = TimeToFrame(currentTime);

                if (currentFrame != newFrame)
                {
                    if (loop && newFrame > endFrame) { newFrame = startFrame; }

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
                    }
                }

                trans.localPosition = position;
                trans.localEulerAngles = rotation;
                trans.localScale = scale;
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
            if(animations.Remove(gobject))
                onRemoveAnimation.Invoke(gobject);
        }

        public void MoveKeyframe(GameObject gobject, AnimatableProperty property, int frame, int newFrame)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
                return;

            animationSet.GetCurve(property).MoveKey(frame, newFrame);
            onChangeAnimation.Invoke(gobject);
        }

        public void AddKeyframe(GameObject gobject, AnimatableProperty property, AnimationKey key)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if(null == animationSet)
            {
                animationSet = new AnimationSet(gobject);
                animations.Add(gobject, animationSet);
            }
            Curve curve = animationSet.GetCurve(property);
            curve.AddKey(key);
            onChangeAnimation.Invoke(gobject);
        }

        public void RemoveKeyframe(GameObject gobject, AnimatableProperty property, int frame)
        {
            AnimationSet animationSet = GetObjectAnimation(gobject);
            if (null == animationSet)
                return;
            Curve curve = animationSet.GetCurve(property);
            curve.RemoveKey(frame);
            onChangeAnimation.Invoke(gobject);
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
            startTime = Time.time - FrameToTime(currentFrame);
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
            startTime = Time.time - FrameToTime(currentFrame);
            animationState = AnimationState.Recording;
            onAnimationStateEvent.Invoke(animationState);
        }

        void RecordFrame()
        {
            foreach (var selected in Selection.GetSelectedObjects())
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

                    switch (curve.property)
                    {
                        case AnimatableProperty.PositionX: curve.AddKey(new AnimationKey(currentFrame, position.x)); break;
                        case AnimatableProperty.PositionY: curve.AddKey(new AnimationKey(currentFrame, position.y)); break;
                        case AnimatableProperty.PositionZ: curve.AddKey(new AnimationKey(currentFrame, position.z)); break;

                        case AnimatableProperty.RotationX: curve.AddKey(new AnimationKey(currentFrame, rotation.x)); break;
                        case AnimatableProperty.RotationY: curve.AddKey(new AnimationKey(currentFrame, rotation.y)); break;
                        case AnimatableProperty.RotationZ: curve.AddKey(new AnimationKey(currentFrame, rotation.z)); break;

                        case AnimatableProperty.ScaleX: curve.AddKey(new AnimationKey(currentFrame, scale.x)); break;
                        case AnimatableProperty.ScaleY: curve.AddKey(new AnimationKey(currentFrame, scale.y)); break;
                        case AnimatableProperty.ScaleZ: curve.AddKey(new AnimationKey(currentFrame, scale.z)); break;
                    }
                }
            }
        }

        public void StopRecording()
        {
            CommandGroup recordGroup = new CommandGroup("Record");

            foreach (var animationSet in recordingObjects.Values)
            {
                GameObject gobject = animationSet.transform.gameObject;
                new CommandRecordAnimations(gobject, oldAnimations[gobject], animationSet).Submit();                
            }

            recordGroup.Submit();
            recordGroup = null;

            recordingObjects.Clear();
            oldAnimations.Clear();
        }
    }
}
