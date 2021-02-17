using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to duplicate an object.
    /// </summary>
    public class CommandDuplicateGameObject : ICommand
    {
        readonly GameObject srcObject;
        protected GameObject gObject = null;
        protected Transform parent = null;
        protected Vector3 position;
        protected Quaternion rotation;
        protected Vector3 scale;

        public CommandDuplicateGameObject(GameObject copy, GameObject src)
        {
            srcObject = src;
            gObject = copy;
            parent = copy.transform.parent.parent;
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
            position = gObject.transform.parent.localPosition;
            rotation = gObject.transform.parent.localRotation;
            scale = gObject.transform.parent.localScale;
            CommandManager.AddCommand(this);
        }
    }
}
