using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Command to rename an object of the scene.
    /// </summary>
    public class CommandRenameGameObject : ICommand
    {
        readonly Transform transform;
        readonly string oldName;
        readonly string newName;

        public CommandRenameGameObject(string commandName, GameObject gobject, string newName)
        {
            name = commandName;
            transform = gobject.transform;
            oldName = gobject.name;
            this.newName = newName;
        }

        public override void Undo()
        {
            SceneManager.RenameObject(transform.gameObject, oldName);
        }

        public override void Redo()
        {
            SceneManager.RenameObject(transform.gameObject, newName);
        }

        public override void Submit()
        {
            CommandManager.AddCommand(this);
            Redo();
        }
    }
}
