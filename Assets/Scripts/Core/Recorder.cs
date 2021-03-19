using System.Collections;
using System.IO;

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace VRtist
{
    public class Recorder : MonoBehaviour
    {
        public bool recording;

        private string outputDir;

        private Camera activeCamera;
        private int currentFrame;

        private UTJ.FrameCapturer.MovieEncoder encoder;
        private UTJ.FrameCapturer.MovieEncoderConfigs encoderConfigs = new UTJ.FrameCapturer.MovieEncoderConfigs(UTJ.FrameCapturer.MovieEncoder.Type.MP4);

        private static Recorder instance;
        public static Recorder Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GameObject.FindObjectOfType<Recorder>();
                }
                return instance;
            }
        }

        void Awake()
        {
            instance = Instance;
        }

        void Start()
        {
            outputDir = Application.persistentDataPath + "/records/";
            activeCamera = CameraManager.Instance.GetActiveCameraComponent();

            CameraManager.Instance.onActiveCameraChanged.AddListener(OnActiveCameraChanged);
            AnimationEngine.Instance.onFrameEvent.AddListener(OnFrameChanged);
            AnimationEngine.Instance.onAnimationStateEvent.AddListener(OnAnimationStateChanged);
        }

        private void OnAnimationStateChanged(AnimationState state)
        {
            if (state == AnimationState.VideoOutput)
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }

        public void StartRecording()
        {
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            encoderConfigs.captureVideo = true;
            encoderConfigs.captureAudio = false;
            encoderConfigs.mp4EncoderSettings.videoTargetBitrate = 10240000;

            CameraManager.Instance.CurrentResolution = CameraManager.Instance.videoOutputResolution;

            encoderConfigs.Setup(CameraManager.Instance.CurrentResolution.width, CameraManager.Instance.CurrentResolution.height, 3, (int)AnimationEngine.Instance.fps);
            encoder = UTJ.FrameCapturer.MovieEncoder.Create(encoderConfigs, outputDir + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            if (encoder == null || !encoder.IsValid())
            {
                StopRecording();
                return;
            }

            recording = true;
            currentFrame = 0;
        }

        public void StopRecording()
        {
            if (null != activeCamera)
            {
                HDAdditionalCameraData camData = activeCamera.gameObject.GetComponent<HDAdditionalCameraData>();
                camData.flipYMode = HDAdditionalCameraData.FlipYMode.Automatic;
            }
            if (encoder != null)
            {
                encoder.Release();
                encoder = null;
            }
            recording = false;
        }

        private void OnActiveCameraChanged(GameObject oldCamera, GameObject newCamera)
        {
            if (null != activeCamera)
            {
                HDAdditionalCameraData camData = activeCamera.gameObject.GetComponent<HDAdditionalCameraData>();
                camData.flipYMode = HDAdditionalCameraData.FlipYMode.Automatic;
            }
            activeCamera = CameraManager.Instance.GetActiveCameraComponent();
        }

        private void OnFrameChanged(int frame)
        {
            if (!recording) { return; }

            if (null != activeCamera)
            {
                HDAdditionalCameraData camData = activeCamera.gameObject.GetComponent<HDAdditionalCameraData>();
                camData.flipYMode = HDAdditionalCameraData.FlipYMode.ForceFlipY;
            }

            StartCoroutine(Capture());
        }

        IEnumerator Capture()
        {
            yield return new WaitForEndOfFrame();

            if (null != activeCamera)
            {
                UTJ.FrameCapturer.fcAPI.fcLock(CameraManager.Instance.RenderTexture, TextureFormat.RGB24, AddVideoFrame);
            }
            else
            {
                UTJ.FrameCapturer.fcAPI.fcLock(CameraManager.Instance.EmptyTexture, TextureFormat.RGB24, AddVideoFrame);
            }
            currentFrame++;
        }

        private void AddVideoFrame(byte[] data, UTJ.FrameCapturer.fcAPI.fcPixelFormat fmt)
        {
            if (null != encoder)
                encoder.AddVideoFrame(data, fmt, currentFrame / AnimationEngine.Instance.fps);
        }
    }
}
