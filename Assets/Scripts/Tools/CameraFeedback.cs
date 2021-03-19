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

using UnityEngine;
using UnityEngine.Assertions;

namespace VRtist
{
    public class CameraFeedback : MonoBehaviour
    {
        public Transform vrCamera;
        public Transform rig;
        private GameObject cameraPlane;

        private void Start()
        {
            Assert.IsTrue(transform.GetChild(0).name == "CameraFeedbackPlane");
            cameraPlane = transform.GetChild(0).gameObject;
            cameraPlane.GetComponent<MeshRenderer>().material.SetTexture("_UnlitColorMap", CameraManager.EmptyTexture);
            CameraManager.Instance.RegisterScreen(cameraPlane.GetComponent<MeshRenderer>().material);
            UpdateTransform();
        }

        public void UpdateTransform()
        {
            Camera cam = CameraManager.Instance.GetActiveCameraComponent();
            float aspect = cam == null ? 16f / 9f : cam.aspect;
            float far = Camera.main.farClipPlane * GlobalState.WorldScale * 0.7f;
            float fov = Camera.main.fieldOfView;
            float scale = far * Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 0.5f * GlobalState.Settings.cameraFeedbackScaleValue;
            Vector3 direction = GlobalState.Settings.cameraFeedbackDirection;
            transform.localPosition = direction.normalized * far;
            transform.localRotation = Quaternion.LookRotation(-direction) * Quaternion.Euler(0, 180, 0);
            transform.localScale = new Vector3(scale * aspect, scale, scale);
        }
    }
}
