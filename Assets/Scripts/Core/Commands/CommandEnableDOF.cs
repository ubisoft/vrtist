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

namespace VRtist
{
    /// <summary>
    /// Command to enable/disable the DoF of a camera. In the case of enabling the DoF it may create a colimator object.
    /// </summary>
    public class CommandEnableDOF : ICommand
    {
        private static GameObject colimatorPrefab = null;
        readonly GameObject camera;
        readonly bool enable;

        public CommandEnableDOF(GameObject camera, bool enable)
        {
            this.camera = camera;
            this.enable = enable;
        }

        private void CreateColimator(GameObject camera)
        {
            if (null == colimatorPrefab)
            {
                colimatorPrefab = Resources.Load<GameObject>("Prefabs/UI/Colimator");
            }

            GameObject colimator = SceneManager.InstantiateUnityPrefab(colimatorPrefab);
            colimator = SceneManager.AddObject(colimator);

            SceneManager.SetObjectParent(colimator, camera);

            CameraController cameraController = camera.GetComponent<CameraController>();
            cameraController.colimator = colimator.transform;

            ColimatorController colimatorController = colimator.GetComponent<ColimatorController>();
            colimatorController.isVRtist = true;
        }

        private void DestroyColimator(GameObject camera)
        {
            CameraController controller = camera.GetComponent<CameraController>();
            if (null != controller.colimator)
            {
                SceneManager.RemoveObject(controller.colimator.gameObject);
            }
        }

        private void SetDOFEnabled(bool value)
        {
            CameraController cameraController = camera.GetComponent<CameraController>();
            cameraController.EnableDOF = value;
            if (null == cameraController.colimator)
            {
                if (value)
                {
                    CreateColimator(camera);
                    SceneManager.SetObjectTransform(cameraController.colimator.gameObject, new Vector3(0, 0, -cameraController.Focus), cameraController.colimator.localRotation, cameraController.colimator.localScale);
                    SceneManager.SendCameraInfo(camera.transform);
                }
            }
            else
            {
                if (value)
                {
                    cameraController.colimator.gameObject.SetActive(true);
                    SceneManager.RestoreObject(cameraController.colimator.gameObject, camera.transform);
                    SceneManager.SetObjectTransform(cameraController.colimator.gameObject, new Vector3(0, 0, -cameraController.Focus), cameraController.colimator.localRotation, cameraController.colimator.localScale);
                    SceneManager.SendCameraInfo(camera.transform);
                }
                else
                {
                    if (null != cameraController.colimator)
                    {
                        ColimatorController colimatorController = cameraController.colimator.GetComponent<ColimatorController>();
                        colimatorController.gameObject.SetActive(false);
                        if (colimatorController.isVRtist)
                        {
                            DestroyColimator(camera);
                        }
                    }
                    SceneManager.SendCameraInfo(camera.transform);
                }
            }
        }

        public override void Undo()
        {
            SetDOFEnabled(!enable);
        }

        public override void Redo()
        {
            SetDOFEnabled(enable);
        }

        public override void Submit()
        {
            CommandManager.AddCommand(this);
            Redo();
        }
    }
}
