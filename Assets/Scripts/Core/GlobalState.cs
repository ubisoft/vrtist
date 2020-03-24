using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class GlobalState : MonoBehaviour
    {
        [Header("Parameters")]
        public GameObject leftController = null;

        public static bool showFps = false;
        public static int fps { get; private set; }
        public static int fpsFrameRange = 60;
        private static int[] fpsBuffer = null;
        private static int fpsBufferIndex = 0;
        private GameObject displayTooltip = null;

        public static bool isGrippingWorld = false;
        public static float worldScale = 1f;

        public static int startFrame = 1;
        public static int endFrame = 250;
        public static int currentFrame = 1;

        private void Start() {
            if(null != leftController) {
                displayTooltip = Tooltips.CreateTooltip(leftController, Tooltips.Anchors.Info, "- fps");
            }
        }

        private void UpdateFps() {
            if(null != displayTooltip) {
                Tooltips.SetTooltipVisibility(displayTooltip, showFps);
            }

            if(!showFps) {
                return;
            }

            // Initialize
            if(null == fpsBuffer || fpsBuffer.Length != fpsFrameRange) {
                if(fpsFrameRange <= 0) { fpsFrameRange = 1; }
                fpsBuffer = new int[fpsFrameRange];
                fpsBufferIndex = 0;
            }

            // Bufferize
            fpsBuffer[fpsBufferIndex] = (int) (1f / Time.unscaledDeltaTime);
            ++fpsBufferIndex;
            if(fpsBufferIndex >= fpsFrameRange) {
                fpsBufferIndex = 0;
            }

            // Calculate mean fps
            int sum = 0;
            for(int i = 0; i < fpsFrameRange; ++i) {
                sum += fpsBuffer[i];
            }
            fps = sum / fpsFrameRange;

            if(null != displayTooltip) {
                Tooltips.SetTooltipText(displayTooltip, $"{fps} fps");
            }
        }

        private void Update()
        {
            UpdateFps();
        }

        public void LateUpdate()
        {
            VRInput.UpdateControllerValues();
        }
    }
}
