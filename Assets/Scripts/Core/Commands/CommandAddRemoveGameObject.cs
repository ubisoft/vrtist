using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Base command class to add or remove an object to/from the scene.
    /// </summary>
    public class CommandAddRemoveGameObject : ICommand
    {
        protected GameObject gObject = null;
        protected Transform parent = null;

        public override void Undo() { }
        public override void Redo() { }
        public override void Submit() { }

        public CommandAddRemoveGameObject(GameObject o)
        {
            gObject = o;
            parent = SceneManager.GetParent(o).transform;
        }

        protected void SendLight()
        {
            LightInfo lightInfo = new LightInfo
            {
                transform = gObject.transform
            };
            CommandManager.SendEvent(MessageType.Light, lightInfo);
            MixerUtils.AddObjectToScene(gObject);
        }

        protected void SendCamera()
        {
            CameraInfo cameraInfo = new CameraInfo
            {
                transform = gObject.transform
            };
            CommandManager.SendEvent(MessageType.Camera, cameraInfo);
            CommandManager.SendEvent(MessageType.Transform, gObject.transform);
            MixerUtils.AddObjectToScene(gObject);
        }

        protected void SendMesh()
        {
            MeshInfos meshInfos = new MeshInfos
            {
                meshFilter = gObject.GetComponent<MeshFilter>(),
                meshRenderer = gObject.GetComponent<MeshRenderer>(),
                meshTransform = gObject.transform
            };

            foreach (Material mat in meshInfos.meshRenderer.materials)
            {
                CommandManager.SendEvent(MessageType.Material, mat);
            }

            CommandManager.SendEvent(MessageType.Mesh, meshInfos);
            CommandManager.SendEvent(MessageType.Transform, gObject.transform);

            MixerUtils.AddObjectToScene(gObject);
        }

        protected void SendEmpty()
        {
            MixerClient.Instance.SendEmpty(gObject.transform);
            MixerClient.Instance.SendTransform(gObject.transform);
            MixerUtils.AddObjectToScene(gObject);
        }
    }
}
