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
