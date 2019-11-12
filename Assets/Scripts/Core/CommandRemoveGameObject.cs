using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandRemoveGameObject : ICommand
    {
        GameObject gObject = null;
        Transform parent = null;
        ParametersController parametersController = null;
        string objectPath;
        Vector3 position;
        Quaternion rotation;
        Vector3 scale;

        public CommandRemoveGameObject(GameObject o)
        {
            gObject = o;
            parent = o.transform.parent;

            parametersController = gObject.GetComponentInParent<ParametersController>();
            if (parametersController)
            {
                Parameters parameters = parametersController.GetParameters();
                GameObject root = parametersController.gameObject;
                if (parameters.GetType() == typeof(GeometryParameters) && root.transform.childCount > 0)
                {
                    objectPath = Utils.BuildTransformPath(gObject);
                }
            }
        }
        public override void Undo()
        {
            gObject.transform.parent = parent;
            gObject.transform.localPosition = position;
            gObject.transform.localRotation = rotation;
            gObject.transform.localScale = scale;
        }
        public override void Redo()
        {
            gObject.transform.parent = Utils.GetOrCreateTrash().transform;
        }
        public override void Submit()
        {
            position = gObject.transform.localPosition;
            rotation = gObject.transform.localRotation;
            scale = gObject.transform.localScale;
            CommandManager.AddCommand(this);
        }

        public override void Serialize(SceneSerializer serializer)
        {
            if(parametersController)
            {
                Parameters parameters = parametersController.GetParameters();
                if (objectPath != null)
                {
                    AssetSerializer assetSerializer = serializer.GetAssetSerializer(parameters.id);
                    assetSerializer.CreateDeletedSerializer(objectPath);
                }
                else
                {
                    serializer.RemoveAsset(parameters);
                }
            }
        }

    }
}