/* MIT License
 *
 * Copyright (c) 2021 Ubisoft
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;

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
        static bool forceDirty = false;

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

            SceneManager.sceneDirtyEvent.Invoke(IsSceneDirty());
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

            SceneManager.sceneDirtyEvent.Invoke(IsSceneDirty());
        }

        public static void SetSceneDirty(bool dirty)
        {
            forceDirty = dirty;
            if (!dirty)
            {
                if (undoStack.Count == 0)
                    cleanCommandRef = null;
                else
                    cleanCommandRef = undoStack[undoStack.Count - 1];
            }
            SceneManager.sceneDirtyEvent.Invoke(dirty);
        }

        public static bool IsSceneDirty()
        {
            if (forceDirty) { return true; }
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
            SceneManager.sceneDirtyEvent.Invoke(IsSceneDirty());

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
    }
}