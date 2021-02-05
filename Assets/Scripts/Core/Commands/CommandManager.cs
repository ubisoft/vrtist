using System;
using System.Collections.Generic;

using UnityEngine;

namespace VRtist
{
    /// <summary>
    /// Abstract base class for commands.
    /// </summary>
    public abstract class ICommand
    {
        abstract public void Undo();
        abstract public void Redo();
        abstract public void Submit();
        protected static void SplitPropertyPath(string propertyPath, out string gameObjectPath, out string componentName, out string fieldName)
        {
            string[] values = propertyPath.Split('/');
            if (values.Length < 2)
                throw new ArgumentException("Bad argument number, expected componentName/fieldName");

            gameObjectPath = "";
            int count = values.Length;
            for (int i = 0; i < count - 3; i++)
            {
                gameObjectPath += values[i];
                gameObjectPath += "/";
            }
            if (count > 2)
                gameObjectPath += values[count - 3];

            componentName = values[count - 2];
            fieldName = values[count - 1];
        }

        protected void SendDelete(GameObject gObject)
        {
            DeleteInfo deleteInfo = new DeleteInfo
            {
                meshTransform = gObject.transform
            };
            CommandManager.SendEvent(MessageType.Delete, deleteInfo);
        }

        protected void SendToTrash(GameObject gObject)
        {
            SendToTrashInfo trashInfo = new SendToTrashInfo
            {
                transform = gObject.transform
            };
            CommandManager.SendEvent(MessageType.SendToTrash, trashInfo);
        }

        protected void RestoreFromTrash(GameObject gObject, Transform parent)
        {
            RestoreFromTrashInfo trashInfo = new RestoreFromTrashInfo
            {
                transform = gObject.transform,
                parent = parent
            };
            CommandManager.SendEvent(MessageType.RestoreFromTrash, trashInfo);
        }

        protected string name;
    }

    /// <summary>
    /// Manage the undo/redo stack.
    /// </summary>
    public static class CommandManager
    {
        static readonly List<ICommand> undoStack = new List<ICommand>();
        static readonly List<ICommand> redoStack = new List<ICommand>();
        static readonly List<CommandGroup> groupStack = new List<CommandGroup>();
        static CommandGroup currentGroup = null;
        static ICommand cleanCommandRef = null;

        public static void Undo()
        {
            if (GlobalState.Animation.IsAnimating())
                return;
            if (null != currentGroup)
                return;
            int count = undoStack.Count;
            if (count == 0)
            {
                return;
            }
            ICommand undoCommand = undoStack[count - 1];
            undoStack.RemoveAt(count - 1);
            undoCommand.Undo();
            redoStack.Add(undoCommand);
        }

        public static void Redo()
        {
            if (GlobalState.Animation.IsAnimating())
                return;

            if (null != currentGroup)
                return;
            int count = redoStack.Count;
            if (redoStack.Count == 0)
                return;
            ICommand redoCommand = redoStack[count - 1];
            redoStack.RemoveAt(count - 1);
            redoCommand.Redo();
            undoStack.Add(redoCommand);
        }

        public static void SetSceneDirty(bool dirty)
        {
            if (undoStack.Count == 0)
                cleanCommandRef = null;
            else
                cleanCommandRef = undoStack[undoStack.Count - 1];
            GlobalState.sceneDirtyEvent.Invoke(dirty);
        }

        public static bool IsSceneDirty()
        {
            if (undoStack.Count == 0)
                return false;
            return cleanCommandRef != undoStack[undoStack.Count - 1];
        }

        public static bool IsUndoGroupOpened()
        {
            return groupStack.Count > 0;
        }

        public static void AddCommand(ICommand command)
        {
            if (currentGroup != null)
            {
                currentGroup.AddCommand(command);
            }
            else
            {
                undoStack.Add(command);
                redoStack.Clear();
            }

            /*
            int count = undoStack.Count;
            while (count > 0 && count > maxUndo)
            {
                ICommand firstCommand = undoStack[0];
                undoStack.RemoveAt(0);
                firstCommand.Serialize(SceneSerializer.CurrentSerializer);
                count--;
            }*/
        }

        public static void BeginGroup(CommandGroup command)
        {
            groupStack.Add(command);
            currentGroup = command;
        }

        public static void EndGroup()
        {
            int count = groupStack.Count;
            groupStack.RemoveAt(count - 1);
            count--;
            currentGroup = count == 0 ? null : groupStack[count - 1];
        }

        public static void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            groupStack.Clear();
            Selection.ClearSelection();
            currentGroup = null;
        }

        public static void SendEvent<T>(MessageType messageType, T data)
        {
            MixerClient.Instance.SendEvent<T>(messageType, data);
        }
    }
}