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

    public class DeleteMeshInfos
    {
        public Transform meshTransform;
    }

    public class CommandAddGameObject : ICommand
    {
        protected GameObject gObject = null;
        Transform parent = null;
        Vector3 position;
        Quaternion rotation;
        Vector3 scale;

        public CommandAddGameObject(GameObject o)
        {
            gObject = o;
            parent = o.transform.parent;
        }

        private void SendDeleteMesh()
        {
            DeleteMeshInfos deleteMeshInfo = new DeleteMeshInfos();
            deleteMeshInfo.meshTransform = gObject.transform;
            CommandManager.SendEvent(MessageType.Delete, deleteMeshInfo);
        }

        private void SendMesh()
        {
            MeshInfos meshInfos = new MeshInfos();
            meshInfos.meshFilter = gObject.GetComponent<MeshFilter>();
            meshInfos.meshRenderer = gObject.GetComponent<MeshRenderer>();

            CommandManager.SendEvent(MessageType.Mesh, meshInfos);

            MeshConnectionInfos meshConnectionInfos = new MeshConnectionInfos();
            meshConnectionInfos.meshTransform = gObject.transform;

            CommandManager.SendEvent(MessageType.MeshConnection, meshConnectionInfos);
        }

        public override void Undo()
        {
            SendDeleteMesh();
            gObject.transform.parent = Utils.GetOrCreateTrash().transform;
        }
        public override void Redo()
        {
            gObject.transform.parent = parent;
            gObject.transform.localPosition = position;
            gObject.transform.localRotation = rotation;
            gObject.transform.localScale = scale;
            SendMesh();
        }
        public override void Submit()
        {
            position = gObject.transform.localPosition;
            rotation = gObject.transform.localRotation;
            scale = gObject.transform.localScale;
            CommandManager.AddCommand(this);
            SendMesh();
        }

        public override void Serialize(SceneSerializer serializer)
        {
            ParametersController parametersController = gObject.GetComponent<ParametersController>();
            if(parametersController)
            {
                serializer.AddAsset(parametersController);
            }
        }

    }
}
