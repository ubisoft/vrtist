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
            Parameters parameters = srcObject.GetComponentInParent<ParametersController>().GetParameters();
            if(parameters.GetType() == typeof(GeometryParameters))
            {
                AssetSerializer assetSerializer = serializer.GetAssetSerializer(parameters.id);
                string transformPath = Utils.BuildTransformPath(srcObject);
                assetSerializer.CreateDuplicateSerializer(transformPath, gObject.name);
            }
            else
            {
                base.Serialize(serializer);
            }
        }
    }
}