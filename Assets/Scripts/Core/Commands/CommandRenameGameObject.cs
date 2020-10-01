using UnityEngine;

namespace VRtist
{
    public class RenameInfo
    {
        public Transform srcTransform;
        public string newName;
    }

    public class CommandRenameGameObject : ICommand
    {
        Transform transform;
        string oldName;
        string newName;

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

        public override void Serialize(SceneSerializer serializer)
        {
            // Empty
        }
    }
}
