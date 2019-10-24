using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace VRtist
{
    public class CommandGroup : ICommand
    {
        List<ICommand> commands = new List<ICommand>();

        public CommandGroup()
        {
            CommandManager.BeginGroup(this);
        }

        public override void Undo()
        {
            int commandCount = commands.Count;
            for(int i = commandCount - 1; i >= 0; i--)
            {
                commands[i].Undo();
            }
        }
        public override void Redo()
        {
            foreach(var command in commands)
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
            if(commands.Count > 0)
                CommandManager.AddCommand(this);
        }
    }
}