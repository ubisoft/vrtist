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
            undoStack.Push(command);
            redoStack.Clear();
        }

    }

}