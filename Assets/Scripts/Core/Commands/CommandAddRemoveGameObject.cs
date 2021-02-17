using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Base command class to add or remove an object to/from the scene.
    /// </summary>
    public class CommandAddRemoveGameObject : ICommand
    {
        protected GameObject gObject = null;
        protected Transform parent = null;

        public override void Undo() { }
        public override void Redo() { }
        public override void Submit() { }

        public CommandAddRemoveGameObject(GameObject o)
        {
            gObject = o;
            parent = SceneManager.GetParent(o).transform;
        }
    }
}
