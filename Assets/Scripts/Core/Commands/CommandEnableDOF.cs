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
                    ColimatorController colimatorController = cameraController.colimator.GetComponent<ColimatorController>();
                    colimatorController.gameObject.SetActive(false);
                    if (colimatorController.isVRtist)
                    {
                        DestroyColimator(camera);
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
