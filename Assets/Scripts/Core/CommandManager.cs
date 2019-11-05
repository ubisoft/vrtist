using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRtist
{
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
        static Stack<ICommand> undoStack = new Stack<ICommand>();
        static Stack<ICommand> redoStack = new Stack<ICommand>();
        static Stack<CommandGroup> groupStack = new Stack<CommandGroup>();
        static CommandGroup currentGroup = null;

        public static void Undo()
        {
            if (undoStack.Count == 0)
                return;
            ICommand undoCommand = undoStack.Pop();
            undoCommand.Undo();
            redoStack.Push(undoCommand);
        }

        public static void Redo()
        {
            if (redoStack.Count == 0)
                return;
            ICommand redoCommand = redoStack.Pop();
            redoCommand.Redo();
            undoStack.Push(redoCommand);
        }

        public static void AddCommand(ICommand command)
        {
            if (currentGroup != null)
            {
                currentGroup.AddCommand(command);
            }
            else
            {
                undoStack.Push(command);
                redoStack.Clear();
            }
        }

        public static void BeginGroup(CommandGroup command)
        {
            groupStack.Push(command);
            currentGroup = command;
        }

        public static void EndGroup()
        {
            CommandGroup groupCommand = groupStack.Pop();
            currentGroup = groupStack.Count == 0 ? null : groupStack.Peek();
        }

        public static void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            groupStack.Clear();
            currentGroup = null;
        }

        public static void Serialize(SceneSerializer serializer)
        {
            ICommand[] undos = undoStack.ToArray();
            for (int i = undos.Length - 1; i >= 0 ; i--)
                undos[i].Serialize(serializer);
        }

    }

}