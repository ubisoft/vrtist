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
            CommandManager.SendEvent(MessageType.Rename, new RenameInfo { srcTransform = transform, newName = oldName });
            SyncData.Rename(newName, oldName);
        }

        public override void Redo()
        {
            CommandManager.SendEvent(MessageType.Rename, new RenameInfo { srcTransform = transform, newName = newName });
            SyncData.Rename(oldName, newName);
        }

        public override void Submit()
        {
            CommandManager.AddCommand(this);
            Redo();
        }
    }
}
