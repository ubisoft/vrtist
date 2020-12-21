using UnityEngine;

namespace VRtist
{
    public class CommandEnableDOF : ICommand
    {
        GameObject camera;
        bool enable;
        public CommandEnableDOF(GameObject camera, bool enable)
        {
            this.camera = camera;
            this.enable = enable;
        }

        private void SetDOFEnabled(bool value)
        {
            CameraController cameraController = camera.GetComponent<CameraController>();
            Transform colimator = cameraController.colimator;
            colimator.gameObject.SetActive(value);
            if (value)
            {                
                colimator.position = camera.transform.position + new Vector3(0, 0, cameraController.focus);
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
