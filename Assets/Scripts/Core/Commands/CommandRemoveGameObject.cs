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
            SceneManager.RestoreObject(gObject, parent);
        }

        public override void Redo()
        {
            if (null == gObject) { return; }
            SceneManager.RemoveObject(gObject);
        }

        public override void Submit()
        {
            ParametersController controller = gObject.GetComponent<ParametersController>();
            if (null != controller && !controller.IsDeletable())
                return;

            ToolsUIManager.Instance.SpawnDeleteInstanceVFX(gObject);

            Redo();
            CommandManager.AddCommand(this);
        }
    }
}
