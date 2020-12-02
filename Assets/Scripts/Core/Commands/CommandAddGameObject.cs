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
            gObject.transform.parent.parent = SyncData.GetTrash().transform;

            Node node = SyncData.nodes[gObject.name];
            node.RemoveInstance(gObject);
        }
        public override void Redo()
        {
            if (null == gObject) { return; }
            gObject.transform.parent.parent = parent;
            gObject.transform.parent.localPosition = position;
            gObject.transform.parent.localRotation = rotation;
            gObject.transform.parent.localScale = scale;

            Node node = SyncData.nodes[gObject.name];
            node.AddInstance(gObject);

            RestoreFromTrash(gObject, parent);
        }
        public override void Submit()
        {
            position = gObject.transform.parent.localPosition;
            rotation = gObject.transform.parent.localRotation;
            scale = gObject.transform.parent.localScale;
            CommandManager.AddCommand(this);
            if (gObject.GetComponent<LightController>() != null)
            {
                SendLight();
            }
            else if (gObject.GetComponent<CameraController>() != null)
            {
                SendCamera();
            }
            else if (null != gObject.GetComponent<LocatorController>())
            {
                SendEmpty();
            }
            else if (gObject.GetComponent<MeshFilter>() != null)
            {
                SendMesh();
            }
            else
            {
                // For fbx import
                SendEmpty();
            }
        }
    }
}
