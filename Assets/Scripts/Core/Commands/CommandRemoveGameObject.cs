using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to remove (delete) an object from the scene.
    /// </summary>
    public class CommandRemoveGameObject : CommandAddRemoveGameObject
    {
        public CommandRemoveGameObject(GameObject o) : base(o) { }

        public override void Undo()
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

        public override void Redo()
        {
            if (null == gObject) { return; }
            SendToTrash(gObject);
            gObject.transform.parent.parent = SyncData.GetTrash().transform;

            Node node = SyncData.nodes[gObject.name];
            node.RemoveInstance(gObject);
        }

        public override void Submit()
        {
            ParametersController controller = gObject.GetComponent<ParametersController>();
            if (null != controller && !controller.IsDeletable())
                return;

            ToolsUIManager.Instance.SpawnDeleteInstanceVFX(gObject);

            position = gObject.transform.parent.localPosition;
            rotation = gObject.transform.parent.localRotation;
            scale = gObject.transform.parent.localScale;
            Redo();
            CommandManager.AddCommand(this);
        }
    }
}
