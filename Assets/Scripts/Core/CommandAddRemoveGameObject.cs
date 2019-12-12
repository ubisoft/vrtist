using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class MeshInfos
    {
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
    }

    public class MeshConnectionInfos
    {
        public Transform meshTransform;
    }

    public class DeleteInfo
    {
        public Transform meshTransform;
    }

    public class CameraInfo
    {
        public Transform transform;
    }
    public class LightInfo
    {
        public Transform transform;
    }

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
        public override void Serialize(SceneSerializer serializer) { }

        public CommandAddRemoveGameObject(GameObject o)
        {
            gObject = o;
            parent = o.transform.parent;
        }

        protected void SendDelete()
        {
            DeleteInfo deleteInfo = new DeleteInfo();
            deleteInfo.meshTransform = gObject.transform;
            CommandManager.SendEvent(MessageType.Delete, deleteInfo);
        }

        protected void SendLight()
        {
            LightInfo lightInfo = new LightInfo();
            lightInfo.transform = gObject.transform;
            CommandManager.SendEvent(MessageType.Light, lightInfo);
        }

        protected void SendCamera()
        {
            CameraInfo cameraInfo = new CameraInfo();
            cameraInfo.transform = gObject.transform;
            CommandManager.SendEvent(MessageType.Camera, cameraInfo);
        }

        protected void SendMesh()
        {
            MeshInfos meshInfos = new MeshInfos();
            meshInfos.meshFilter = gObject.GetComponent<MeshFilter>();
            meshInfos.meshRenderer = gObject.GetComponent<MeshRenderer>();

            foreach (Material mat in meshInfos.meshRenderer.materials)
            {
                CommandManager.SendEvent(MessageType.Material, mat);
            }

            CommandManager.SendEvent(MessageType.Mesh, meshInfos);

            MeshConnectionInfos meshConnectionInfos = new MeshConnectionInfos();
            meshConnectionInfos.meshTransform = gObject.transform;

            CommandManager.SendEvent(MessageType.MeshConnection, meshConnectionInfos);

            CommandManager.SendEvent(MessageType.Transform, meshConnectionInfos.meshTransform);
        }
    }
}
