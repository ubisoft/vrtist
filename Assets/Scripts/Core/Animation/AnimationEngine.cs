using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace VRtist
{
    public enum AnimationState
    {
        Stopped,
        Preroll,
        Recording,
        Playing
    };

    struct KeyframeData
    {
        // General data
        public Vector3 position;
        public Quaternion rotation;
        public float time;

        // Light data
        public float intensity;
        public Color color;

        // Camera data
        public float focal;
    }

    struct TrackData
    {
        public TrackAsset trackAsset;
        public Color baseColor;
    }

    public class AnimationEngine : MonoBehaviour
    {
        [Header("Timeline & Animations")]
        [SerializeField] private PlayableDirector director;

        public Transform picked;  // transform to animate

        List<KeyframeData> keyframes = new List<KeyframeData>();
        Dictionary<int, TrackData> trackMap = new Dictionary<int, TrackData>();
        int currentAnimCount = 0;

        public float fps = 24f;
        public int startFrame = 1;
        public int endFrame = 250;
        public int currentFrame = 1;
        public bool autoKeyEnabled = false;
        public float recordStartTime;

        public UIButton playButtonShortcut;

        public AnimationState animationState = AnimationState.Stopped;
        public AnimationStateChangedEvent onAnimationStateEvent = new AnimationStateChangedEvent();
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
            Selection.OnSelectionChanged += OnSelectionChanged;
            countdown.onCountdownFinished.AddListener(StartRecording);
        }

        private void OnApplicationQuit()
        {
            TimelineAsset timelineAsset = director.playableAsset as TimelineAsset;
            foreach (TrackAsset track in timelineAsset.GetOutputTracks())
            {
                timelineAsset.DeleteTrack(track);
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedArgs args)
        {
            picked = Selection.IsEmpty() ? null : Selection.GetSelectedObjects()[0].transform;
        }

        //public void AddAnimationListener(UnityAction<GameObject, AnimationChannel> callback)
        //{
        //    //timelineController.AddListener(callback);
        //}
        //public void RemoveAnimationListener(UnityAction<GameObject, AnimationChannel> callback)
        //{
        //    //timelineController.RemoveListener(callback);
        //}
        public void ClearAnimations(GameObject gameObject)
        {
            //timelineController.ClearAnimations(gameObject);
        }
        //public void FireAnimationChanged(GameObject gameObject, AnimationChannel channel)
        //{
        //    //timelineController.FireAnimationChanged(gameObject, channel);
        //}

        public void AddAnimationChannel(GameObject gameObject, string channelName, int channelIndex, List<AnimationKey> keys)
        {
            //timelineController.AddAnimationChannel(gameObject, channelName, channelIndex, keys);
        }
        public void RemoveAnimationChannel(GameObject gameObject, string channelName, int channelIndex)
        {
            //timelineController.RemoveAnimationChannel(gameObject, channelName, channelIndex);
        }
        public bool HasAnimation(GameObject gameObject)
        {
            return false;
            //return timelineController.HasAnimation(gameObject);
        }

        public void SendAnimationChannel(string objectName, AnimationChannel animationChannel)
        {
            //timelineController.SendAnimationChannel(objectName, animationChannel);
        }

        public Dictionary<Tuple<string, int>, AnimationChannel> GetAnimationChannels(GameObject gameObject)
        {
            return null;// return timelineController.GetAnimationChannels(gameObject);
        }

        public void MoveKeyframe(GameObject gObject, string channelName, int channelIndex, int frame, int newFrame)
        {
            // timelineController.MoveKeyframe(gObject, channelName, channelIndex, frame, newFrame);
        }

        public void AddKeyframe(GameObject gObject, string channelName, int channelIndex, int frame, float value, Interpolation interpolation)
        {
            //timelineController.AddKeyframe(gObject, channelName, channelIndex, frame, value, interpolation);
        }

        public void RemoveKeyframe(GameObject gObject, string channelName, int channelIndex, int frame)
        {
            //timelineController.RemoveKeyframe(gObject, channelName, channelIndex, frame);
        }

        public void RemoveKeyframes()
        {
            //timelineController.RemoveSelectionKeyframes();
        }

        public void MoveKeyframes(GameObject gObject, string channelName, int channelIndex, int frame, int newFrame)
        {
            //timelineController.MoveKeyframe(gObject, channelName, channelIndex, frame, newFrame);
        }

        public void MoveKeyframes(int frame, int newFrame)
        {
            //timelineController.MoveSelectionKeyframes(frame, newFrame);
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
            director.Play();
        }

        public void Pause()
        {
            director.Stop();

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
            if (null == picked) { return; }

            animationState = AnimationState.Recording;
            recordStartTime = Time.time;
            keyframes.Clear();

            int hash = picked.gameObject.GetHashCode();
            if (trackMap.ContainsKey(hash))
            {
                trackMap[hash].trackAsset.muted = true;
            }

            onAnimationStateEvent.Invoke(animationState);
            StartCoroutine(RecordFrame());
        }

        IEnumerator RecordFrame()
        {
            while (animationState == AnimationState.Recording)
            {
                float delta = Time.time - recordStartTime;

                KeyframeData data = new KeyframeData();

                // Always add position, rotation and time
                data.position = picked.position;
                data.rotation = picked.rotation;
                data.time = delta;

                // Add light specific data
                LightController lightController = picked.GetComponent<LightController>();
                if (null != lightController)
                {
                    data.intensity = lightController.intensity;
                    data.color = lightController.color;
                }

                // Add camera specific data
                CameraController cameraController = picked.GetComponent<CameraController>();
                if (null != cameraController)
                {
                    data.focal = cameraController.focal;
                }

                keyframes.Add(data);

                yield return new WaitForSecondsRealtime(1f / fps);
            }
        }

        public void StopRecording()
        {
            Animator animator = picked.gameObject.GetComponent<Animator>();
            if (animator == null) { animator = picked.gameObject.AddComponent<Animator>(); }

            AnimationClip clip = new AnimationClip();
            clip.name = GetNewClipName(picked);

            Keyframe[] posX = new Keyframe[keyframes.Count];
            Keyframe[] posY = new Keyframe[keyframes.Count];
            Keyframe[] posZ = new Keyframe[keyframes.Count];
            Keyframe[] rotX = new Keyframe[keyframes.Count];
            Keyframe[] rotY = new Keyframe[keyframes.Count];
            Keyframe[] rotZ = new Keyframe[keyframes.Count];
            Keyframe[] rotW = new Keyframe[keyframes.Count];

            int i = 0;
            foreach (KeyframeData key in keyframes)
            {
                posX[i] = new Keyframe(key.time, key.position.x);
                posY[i] = new Keyframe(key.time, key.position.y);
                posZ[i] = new Keyframe(key.time, key.position.z);
                rotX[i] = new Keyframe(key.time, key.rotation.x);
                rotY[i] = new Keyframe(key.time, key.rotation.y);
                rotZ[i] = new Keyframe(key.time, key.rotation.z);
                rotW[i] = new Keyframe(key.time, key.rotation.w);
                ++i;
            }

            float fps = GlobalState.Animation.fps;
            AnimationCurve cx = CreateResampledCurve(posX, fps);
            AnimationCurve cy = CreateResampledCurve(posY, fps);
            AnimationCurve cz = CreateResampledCurve(posZ, fps);

            clip.SetCurve("", typeof(Transform), "localPosition.x", cx);
            clip.SetCurve("", typeof(Transform), "localPosition.y", cy);
            clip.SetCurve("", typeof(Transform), "localPosition.z", cz);
            clip.SetCurve("", typeof(Transform), "localRotation.x", CreateResampledCurve(rotX, fps));
            clip.SetCurve("", typeof(Transform), "localRotation.y", CreateResampledCurve(rotY, fps));
            clip.SetCurve("", typeof(Transform), "localRotation.z", CreateResampledCurve(rotZ, fps));
            clip.SetCurve("", typeof(Transform), "localRotation.w", CreateResampledCurve(rotW, fps));
            //clip.EnsureQuaternionContinuity();

            // Save the anim on disk
            //UnityEditor.AssetDatabase.CreateAsset(clip, "Assets/Resources/Anims/" + clip.name + ".anim");

            // Create a track for each game object
            // A track can contain one or more animation clips
            int hash = picked.gameObject.GetHashCode();
            TrackData trackData;
            if (trackMap.ContainsKey(hash))
            {
                trackData = trackMap[hash];
                trackData.trackAsset.muted = false;
            }
            else
            {
                TrackAsset track = (director.playableAsset as TimelineAsset).CreateTrack<AnimationTrack>(null, "Track_" + clip.name);
                trackData = new TrackData() { trackAsset = track, baseColor = Color.green };
                trackMap.Add(hash, trackData);
                //trajectoryColorIndex = (trajectoryColorIndex + 1) % colors.Length;

                // Bind the game object to the track
                director.SetGenericBinding(track, animator);
            }

            // Create a line renderer for trajectory
            //GameObject lineObject = CreateLine(pickedParent, clip.name, cx, cy, cz, 120f, trackData.baseColor);

            // Create and append a default clip with a duration of 300 frames
            TimelineClip timelineClip = trackData.trackAsset.CreateDefaultClip();
            timelineClip.duration = clip.length;
            AnimationPlayableAsset asset = timelineClip.asset as AnimationPlayableAsset;
            asset.clip = clip;
            asset.removeStartOffset = false;
            //asset.name = clip.name;  // does nothing :(
        }

        public string GetNewClipName(Transform obj)
        {
            return $"Clip_{obj.name}_{currentAnimCount++}";
        }

        public AnimationCurve CreateResampledCurve(Keyframe[] keyframes, float fps)
        {
            Keyframe[] results = ResampleKeyframes(keyframes, fps);

            AnimationCurve outputCurve = new AnimationCurve(results);
            for (int i = 1; i < results.Length - 1; i++)
            {
                outputCurve.SmoothTangents(i, 0);
            }

            return outputCurve;
        }

        public Keyframe[] ResampleKeyframes(Keyframe[] keyframes, float fps)
        {
            AnimationCurve inputCurve = new AnimationCurve(keyframes);
            float duration = keyframes[keyframes.Length - 1].time - keyframes[0].time;

            float timeStep = 1f / fps;
            int numKeys = (int) Mathf.Floor(duration * fps);
            Keyframe[] results = new Keyframe[numKeys];
            for (int i = 0; i < numKeys; i++)
            {
                float t = keyframes[0].time + i * timeStep;
                results[i] = new Keyframe() { time = t, value = inputCurve.Evaluate(t) };
            }

            return results;
        }
    }
}
