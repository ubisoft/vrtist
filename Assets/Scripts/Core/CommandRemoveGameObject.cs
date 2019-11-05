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
            IOMetaData metaData = gObject.GetComponentInParent<IOMetaData>();
            if (metaData)
            {
                AssetSerializer assetSerializer = serializer.GetAssetSerializer(metaData.id);
                GameObject root = metaData.gameObject;
                if(root.transform.childCount > 0)
                {
                    string transformPath = Utils.BuildTransformPath(gObject);
                    assetSerializer.CreateDeletedSerializer(transformPath);
                }
                else
                {
                    serializer.RemoveAsset(metaData);
                }
            }
        }

    }
}