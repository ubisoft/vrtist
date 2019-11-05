using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class CommandDuplicateGameObject : CommandAddGameObject
    {
        GameObject srcObject;
        public CommandDuplicateGameObject(GameObject copy, GameObject src)
            : base(copy)
        {
            srcObject = src;
        }

        public override void Serialize(SceneSerializer serializer)
        {
            IOMetaData metaData = srcObject.GetComponentInParent<IOMetaData>();
            if(metaData)
            {
                AssetSerializer assetSerializer = serializer.GetAssetSerializer(metaData.id);
                string transformPath = Utils.BuildTransformPath(srcObject);
                assetSerializer.CreateDuplicateSerializer(transformPath, gObject.name);
            }
        }
    }
}