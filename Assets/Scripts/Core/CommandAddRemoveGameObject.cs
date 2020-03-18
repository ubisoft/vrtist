using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class MeshInfos
    {
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public Transform meshTransform;
    }

    public class DeleteInfo
    {
        public Transform meshTransform;
    }

    public class SendToTrashInfo
    {
        public Transform transform;
    }
    public class RestoreFromTrashInfo
    {
        public Transform transform;
    }

    public class CameraInfo
    {
        public Transform transform;
    }
    public class LightInfo
    {
        public Transform transform;
    }

    public class AddToCollectionInfo
    {
        public string collectionName;
        public Transform transform;
    }

    public class AddObjectToSceneInfo
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

        protected void AddObjectToScene()
        {
            AddToCollectionInfo addObjectToCollection = new AddToCollectionInfo();
            addObjectToCollection.collectionName = "Collection";
            addObjectToCollection.transform = gObject.transform;
            CommandManager.SendEvent(MessageType.AddObjectToCollection, addObjectToCollection);

            AddObjectToSceneInfo addObjectToScene = new AddObjectToSceneInfo();
            addObjectToScene.transform = gObject.transform;
            CommandManager.SendEvent(MessageType.AddObjectToScene, addObjectToScene);
        }

        protected void SendLight()
        {
            LightInfo lightInfo = new LightInfo();
            lightInfo.transform = gObject.transform;
            CommandManager.SendEvent(MessageType.Light, lightInfo);
            AddObjectToScene();
        }

        protected void SendCamera()
        {
            CameraInfo cameraInfo = new CameraInfo();
            cameraInfo.transform = gObject.transform;
            CommandManager.SendEvent(MessageType.Camera, cameraInfo);
            CommandManager.SendEvent(MessageType.Transform, gObject.transform);
            AddObjectToScene();
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

            AddObjectToScene();
        }
    }
}
