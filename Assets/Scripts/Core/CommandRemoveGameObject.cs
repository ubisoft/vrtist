using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandRemoveGameObject : ICommand
    {
        GameObject gObject = null;
        Transform parent = null;
        Vector3 position;
        Quaternion rotation;
        Vector3 scale;

        public CommandRemoveGameObject(GameObject o)
        {
            gObject = o;
            parent = o.transform.parent;
        }
        public override void Undo()
        {
            gObject.transform.parent = parent;
            gObject.transform.localPosition = position;
            gObject.transform.localRotation = rotation;
            gObject.transform.localScale = scale;
            gObject.SetActive(true);
        }
        public override void Redo()
        {
            gObject.SetActive(false);
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
            ParametersController parametersController = gObject.GetComponentInParent<ParametersController>();
            if (parametersController)
            {
                Parameters parameters = parametersController.GetParameters();
                AssetSerializer assetSerializer = serializer.GetAssetSerializer(parameters.id);
                GameObject root = parametersController.gameObject;
                if(parametersController.GetType() == typeof(GeometryParameters) && root.transform.childCount > 0)
                {
                    string transformPath = Utils.BuildTransformPath(gObject);
                    assetSerializer.CreateDeletedSerializer(transformPath);
                }
                else
                {
                    serializer.RemoveAsset(parameters);
                }
            }
        }

    }
}