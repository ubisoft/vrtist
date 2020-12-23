using UnityEngine;

namespace VRtist
{
    public class CommandEnableDOF : ICommand
    {
        private static GameObject cameraColimator = null;

        GameObject camera;
        bool enable;
        public CommandEnableDOF(GameObject camera, bool enable)
        {
            this.camera = camera;
            this.enable = enable;
        }

        private void CreateColimator(GameObject camera)
        {
            if(null == cameraColimator)
            {
                cameraColimator = Resources.Load<GameObject>("Prefabs/UI/Colimator");
            }
            CameraController cameraController = camera.GetComponent<CameraController>();

            GameObject colimator = SyncData.CreateInstance(cameraColimator, SyncData.prefab, isPrefab: true);
            colimator.transform.localPosition = new Vector3(0, 0, -cameraController.Focus);

            ColimatorController colimatorController = colimator.GetComponent<ColimatorController>();
            colimatorController.isVRtist = true;

            Node cameraNode = SyncData.nodes[camera.name];
            Node colimatorNode = SyncData.GetOrCreateNode(colimator);
            cameraNode.AddChild(colimatorNode);
            GameObject colimatorInstance = SyncData.InstantiatePrefab(colimator);
            cameraController.colimator = colimatorInstance.transform;

            MixerClient.GetInstance().SendEmpty(colimator.transform);
            MixerClient.GetInstance().SendTransform(colimator.transform);
            MixerUtils.AddObjectToScene(colimator);
            MixerClient.GetInstance().SendCamera(new CameraInfo { transform = camera.transform });
        }

        private void DestroyColimator(GameObject camera)
        {
            CameraController controller = camera.GetComponent<CameraController>();
            if (null != controller.colimator)
            {
                GameObject.Destroy(controller.colimator.gameObject);
                MixerClient.GetInstance().SendDelete(new DeleteInfo { meshTransform = controller.colimator });
            }
        }

        private bool IsVRtistColimator()

        {
            return true;
        }
        private void SetDOFEnabled(bool value)
        {
            CameraController cameraController = camera.GetComponent<CameraController>();
            Transform colimator = cameraController.colimator;
            if(null == colimator)
            {
                if(value)
                {
                    CreateColimator(camera);
                }
            }
            else
            {
                if (value)
                {
                    colimator.gameObject.SetActive(true);
                    colimator.transform.localPosition = new Vector3(0, 0, -cameraController.Focus);
                    MixerClient.GetInstance().SendTransform(colimator.transform);
                }
                else
                {
                    ColimatorController colimatorController = colimator.GetComponent<ColimatorController>();
                    if(colimatorController.isVRtist)
                    {
                        DestroyColimator(camera);
                    }
                }
            }
            cameraController.EnableDOF = value;
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
