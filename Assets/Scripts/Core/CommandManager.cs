using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
    public class RenameInfo
    {
        public Transform srcTransform;
        public string newName;
    }
    public abstract class ICommand
    {
        abstract public void Undo();
        abstract public void Redo();
        abstract public void Submit();
        abstract public void Serialize(SceneSerializer serializer);
        protected static void SplitPropertyPath(string propertyPath, out string gameObjectPath, out string componentName, out string fieldName)
        {
            string [] values = propertyPath.Split('/');
            if(values.Length < 2)
                throw new ArgumentException("Bad argument number, expected componentName/fieldName");

            gameObjectPath = "";
            int count = values.Length;
            for (int i = 0; i < count - 3; i++)
            {
                gameObjectPath += values[i];
                gameObjectPath += "/";
            }
            if(count > 2)
                gameObjectPath += values[count - 3];

            componentName = values[count - 2];
            fieldName = values[count - 1];
        }

        protected string name;
    }
    public static class CommandManager
    {
        static List<ICommand> undoStack = new List<ICommand>();
        static List<ICommand> redoStack = new List<ICommand>();
        static List<CommandGroup> groupStack = new List<CommandGroup>();
        static CommandGroup currentGroup = null;
        static int maxUndo = 100;

        public static void Undo()
        {
            int count = undoStack.Count;
            if (count == 0)
                return;
            ICommand undoCommand = undoStack[count - 1];
            undoStack.RemoveAt(count - 1);
            undoCommand.Undo();
            redoStack.Add(undoCommand);
        }

        public static void Redo()
        {
            int count = redoStack.Count;
            if (redoStack.Count == 0)
                return;
            ICommand redoCommand = redoStack[count - 1];
            redoStack.RemoveAt(count - 1);
            redoCommand.Redo();
            undoStack.Add(redoCommand);
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

            int count = undoStack.Count;
            while (count > 0 && count > maxUndo)
            {
                ICommand firstCommand = undoStack[0];
                undoStack.RemoveAt(0);
                firstCommand.Serialize(SceneSerializer.CurrentSerializer);
                count--;
            }
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

        public static void Serialize(SceneSerializer serializer)
        {
            ICommand[] undos = undoStack.ToArray();
            for (int i = 0; i < undos.Length; i++)
                undos[i].Serialize(serializer);
        }

        public static void SendEvent<T>(MessageType messageType, T data)
        {
            NetworkClient.GetInstance().SendEvent<T>(messageType, data);
        }
    }
}