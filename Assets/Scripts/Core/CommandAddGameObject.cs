using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandAddGameObject : CommandAddRemoveGameObject
    {
        public CommandAddGameObject(GameObject o) : base(o)
        {
        }

        public override void Undo()
        {
            if (null == gObject) { return; }
            SendToTrash(gObject);
            gObject.transform.parent = Utils.GetTrash().transform;

            Node node = SyncData.nodes[gObject.name];
            node.RemoveInstance(gObject);
        }
        public override void Redo()
        {
            if (null == gObject) { return; }
            gObject.transform.parent = parent;
            gObject.transform.localPosition = position;
            gObject.transform.localRotation = rotation;
            gObject.transform.localScale = scale;

            Node node = SyncData.nodes[gObject.name];
            node.AddInstance(gObject);

            RestoreFromTrash(gObject);
        }
        public override void Submit()
        {
            position = gObject.transform.localPosition;
            rotation = gObject.transform.localRotation;
            scale = gObject.transform.localScale;
            CommandManager.AddCommand(this);
            if (gObject.GetComponent<LightController>() != null)
            {
                SendLight();
            }
            else if (gObject.GetComponent<CameraController>() != null)
            {
                SendCamera();
            }
            else if (gObject.GetComponent<MeshFilter>() != null)
            {
                SendMesh();
            }

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
