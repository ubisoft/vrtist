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

using Unity.Formats.USD;

using UnityEngine;

using USD.NET;

namespace VRtist
{
    public class USD : MonoBehaviour
    {
        public GameObject m_exportRoot;
        int frameRate;

        public bool exportMaterials = true;
        public BasisTransformation convertHandedness = BasisTransformation.SlowAndSafe;
        public ActiveExportPolicy activePolicy = ActiveExportPolicy.ExportAsVisibility;

        string usdFileName;

        // The scene object to which the recording will be saved.
        private Scene usdScene;
        ExportContext context = new ExportContext();

        // Used by the custom editor to determine recording state.
        public bool IsRecording { get; private set; }

        private bool selectionWasEnabled = true;
        private bool isExportingUSD = false;
        private bool firstFrame = false;

        int previousApplicationFramerate;
        int previousCaptureFramerate;

        // ------------------------------------------------------------------------------------------ //
        // Recording Control.
        // ------------------------------------------------------------------------------------------ //
        private void Start()
        {
            GlobalState.Animation.onAnimationStateEvent.AddListener(OnExportStateChanged);
        }
        public void ToggleRecord()
        {
            if (IsRecording)
                StopRecording();
            else
                StartRecording();
        }

        public void StartRecording()
        {
            if (IsRecording)
            {
                return;
            }

            if (!m_exportRoot)
            {
                Debug.LogError("ExportRoot not assigned.");
                return;
            }

            if (usdScene != null)
            {
                usdScene.Close();
                usdScene = null;
            }

            try
            {
                if (string.IsNullOrEmpty(usdFileName))
                {
                    usdScene = Scene.Create();
                }
                else
                {
                    usdScene = Scene.Create(usdFileName);
                }
                usdScene.StartTime = GlobalState.Animation.StartFrame;
                usdScene.EndTime = GlobalState.Animation.EndFrame;

                // USD operates on frames, so the frame rate is required for playback.
                // We could also set this to 1 to indicate that the TimeCode is in seconds.
                previousApplicationFramerate = Application.targetFrameRate;
                Application.targetFrameRate = (int)frameRate;

                // This forces Unity to use a fixed time step, resulting in evenly spaced
                // time samples in USD. Unfortunately, non-integer frame rates are not supported.
                // When non-integer frame rates are needed, time can be manually paused and
                // and advanced 
                previousCaptureFramerate = Time.captureFramerate;
                Time.captureFramerate = Application.targetFrameRate;

                // Set the frame rate in USD  as well.
                //
                // This both set the "time samples per second" and the playback rate.
                // Setting times samples per second allows the authoring code to express samples as integer
                // values, avoiding floating point error; so by setting FrameRate = 60, the samples written
                // at time=0 through time=59 represent the first second of playback.
                //
                // Stage.TimeCodesPerSecond is set implicitly to 1 / FrameRate.
                usdScene.FrameRate = Application.targetFrameRate;

                // For simplicity in this example, adding game objects while recording is not supported.
                context = new ExportContext();
                context.scene = usdScene;
                context.basisTransform = convertHandedness;

                // Do this last, in case an exception is thrown above.
                IsRecording = true;
                firstFrame = true;
            }
            catch
            {
                if (usdScene != null)
                {
                    usdScene.Close();
                    usdScene = null;
                }

                throw;
            }
        }

        public void StopRecording()
        {
            if (!IsRecording)
            {
                return;
            }

            context = new ExportContext();

            // In a real exporter, additional error handling should be added here.
            if (!string.IsNullOrEmpty(usdFileName))
            {
                // We could use SaveAs here, which is fine for small scenes, though it will require
                // everything to fit in memory and another step where that memory is copied to disk.
                usdScene.Save();
            }

            // Release memory associated with the scene.
            usdScene.Close();
            usdScene = null;

            Application.targetFrameRate = previousApplicationFramerate;
            Time.captureFramerate = previousCaptureFramerate;

            IsRecording = false;
        }


        // ------------------------------------------------------------------------------------------ //
        // Unity Behavior Events.
        // ------------------------------------------------------------------------------------------ //

        void Awake()
        {
            // Init USD.
            InitUsd.Initialize();
        }

        string GetFilename()
        {
            return System.IO.Path.Combine(GlobalState.Settings.exportDirectory, GlobalState.Settings.ProjectName + "_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".usd");
        }

        public void ExportSnapshot()
        {
            Scene scene = Scene.Create(GetFilename());
            ExportContext context = new ExportContext();
            context.scene = scene;
            context.basisTransform = convertHandedness;
            context.exportMaterials = exportMaterials;

            context.scene.Time = null;
            context.activePolicy = ActiveExportPolicy.ExportAsVisibility;

            SceneExporter.SyncExportContext(m_exportRoot, context);
            SceneExporter.Export(m_exportRoot, context, zeroRootTransform: true);
            scene.Save();
            scene.Close();
        }

        public void ExportAnimation()
        {
            if (isExportingUSD)
                return;

            selectionWasEnabled = Selection.enabled;
            Selection.enabled = false;
            isExportingUSD = true;

            GlobalState.Animation.timeHooksEnabled = false;
            GlobalState.Animation.onFrameEvent.AddListener(OnFrameChanged);

            GlobalState.Animation.CurrentFrame = GlobalState.Animation.StartFrame;

            GlobalState.Animation.OnToggleStartVideoOutput(true);
            usdFileName = GetFilename();
            frameRate = (int)GlobalState.Animation.fps;

            StartRecording();
        }

        void OnFrameChanged(int frame)
        {
            if (GlobalState.Animation.animationState == AnimationState.VideoOutput)
            {
                if (frame == GlobalState.Animation.EndFrame)
                {
                    GlobalState.Animation.Pause();
                    StopRecording();
                }
            }
        }

        void StopExportUSD()
        {
            isExportingUSD = false;
            GlobalState.Animation.onFrameEvent.RemoveListener(OnFrameChanged);
            Selection.enabled = selectionWasEnabled;
            GlobalState.Animation.timeHooksEnabled = true;
            StopRecording();
        }

        void OnExportStateChanged(AnimationState animationState)
        {
            bool buttonsDisabled = GlobalState.Animation.animationState == AnimationState.VideoOutput;
            if (isExportingUSD && GlobalState.Animation.animationState != AnimationState.VideoOutput)
            {
                StopExportUSD();
            }
        }

        // Why LateUpdate()?
        // Because Update fires before the animation system applies computed values.
        void LateUpdate()
        {
            if (!IsRecording)
            {
                return;
            }

            // On the first frame, export all the unvarying data (e.g. mesh topology).
            // On subsequent frames, skip unvarying data to avoid writing redundant data.
            if (firstFrame)
            {
                // First write materials and unvarying values (mesh topology, etc).
                context.exportMaterials = exportMaterials;
                context.scene.Time = null;
                context.activePolicy = ActiveExportPolicy.ExportAsVisibility;

                SceneExporter.SyncExportContext(m_exportRoot, context);
                SceneExporter.Export(m_exportRoot, context, zeroRootTransform: false);
                firstFrame = false;
            }

            // Set the time at which to read samples from USD.
            // If the FramesPerSecond is set to 1 above, this should be Time.time instead of frame count.
            context.scene.Time = GlobalState.Animation.CurrentFrame;
            context.exportMaterials = false;

            // Record the time varying data that changes from frame to frame.
            SceneExporter.Export(m_exportRoot, context, zeroRootTransform: false);
        }

    }
}
