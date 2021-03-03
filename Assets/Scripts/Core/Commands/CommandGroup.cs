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

using System.Collections.Generic;


namespace VRtist
{
    /// <summary>
    /// Command to group multiple commands so it creates only one block of undo/redo.
    /// </summary>
    public class CommandGroup : ICommand
    {
        readonly List<ICommand> commands = new List<ICommand>();
        protected string groupName;

        public CommandGroup()
        {
            groupName = "Undefined";
            CommandManager.BeginGroup(this);
        }

        public CommandGroup(string value)
        {
            groupName = value;
            CommandManager.BeginGroup(this);
        }

        public override void Undo()
        {
            int commandCount = commands.Count;
            for (int i = commandCount - 1; i >= 0; i--)
            {
                commands[i].Undo();
            }
        }

        public override void Redo()
        {
            foreach (var command in commands)
            {
                command.Redo();
            }
        }

        public void AddCommand(ICommand command)
        {
            commands.Add(command);
        }

        public override void Submit()
        {
            CommandManager.EndGroup();
            if (commands.Count > 0)
                CommandManager.AddCommand(this);
        }
    }
}
