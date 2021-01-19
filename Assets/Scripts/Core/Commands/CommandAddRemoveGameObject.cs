using UnityEngine;

namespace VRtist
{
    public class CommandAddRemoveGameObject : ICommand
    {
        protected GameObject gObject = null;
        protected Transform parent = null;
        protected Vector3 position;
        protected Quaternion rotation;
        protected Vector3 scale;

        public override void Undo() { }
        public override void Redo() { }
        public override void Submit() { }

        public CommandAddRemoveGameObject(GameObject o)
        {
            gObject = o;
            parent = o.transform.parent.parent;
        }

        protected void SendLight()
        {
            LightInfo lightInfo = new LightInfo();
            lightInfo.transform = gObject.transform;
            CommandManager.SendEvent(MessageType.Light, lightInfo);
            MixerUtils.AddObjectToScene(gObject);
        }

        protected void SendCamera()
        {
            CameraInfo cameraInfo = new CameraInfo();
            cameraInfo.transform = gObject.transform;
            CommandManager.SendEvent(MessageType.Camera, cameraInfo);
            CommandManager.SendEvent(MessageType.Transform, gObject.transform);
            MixerUtils.AddObjectToScene(gObject);
        }

        protected void SendMesh()
        {
            MeshInfos meshInfos = new MeshInfos();
            meshInfos.meshFilter = gObject.GetComponent<MeshFilter>();
            meshInfos.meshRenderer = gObject.GetComponent<MeshRenderer>();
            meshInfos.meshTransform = gObject.transform;

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
