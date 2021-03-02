using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to duplicate an object.
    /// </summary>
    public class CommandDuplicateGameObject : ICommand
    {
        protected GameObject gObject = null;
        protected Transform parent = null;

        public CommandDuplicateGameObject(GameObject copy)
        {
            gObject = copy;
            parent = SceneManager.GetObjectParent(copy).transform;
        }

        public override void Undo()
        {
            if (null == gObject) { return; }
            SceneManager.RemoveObject(gObject);
        }
        public override void Redo()
        {
            if (null == gObject) { return; }
            SceneManager.RestoreObject(gObject, parent);
        }
        public override void Submit()
        {
            CommandManager.AddCommand(this);
        }
    }
}
