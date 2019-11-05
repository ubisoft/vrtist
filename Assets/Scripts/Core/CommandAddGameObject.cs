using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
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

        public override void Undo()
        {
            gObject.SetActive(false);
            gObject.transform.parent = Utils.GetOrCreateTrash().transform;
        }
        public override void Redo()
        {
            gObject.transform.parent = parent;
            gObject.transform.localPosition = position;
            gObject.transform.localRotation = rotation;
            gObject.transform.localScale = scale;
            gObject.SetActive(true);
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
            IOMetaData metaData = gObject.GetComponent<IOMetaData>();
            if(metaData)
            {
                serializer.AddAsset(metaData);
            }
        }

    }
}
