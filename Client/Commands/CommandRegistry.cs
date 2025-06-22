using System;
using System.Collections.Generic;

namespace Client.Commands
{
    public class CommandRegistry
    {
        private readonly Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>();

        public void RegisterCommand(string key, ICommand command)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Command key cannot be empty", nameof(key));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            commands[key] = command;
        }

        public bool TryGetCommand(string key, out ICommand command)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                command = null;
                return false;
            }

            return commands.TryGetValue(key, out command);
        }

        public IEnumerable<KeyValuePair<string, ICommand>> GetAllCommands()
        {
            return commands;
        }
    }
}
