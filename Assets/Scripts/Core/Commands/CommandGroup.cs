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
