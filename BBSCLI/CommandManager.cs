using System;
using System.Collections.Generic;
using BBSCLI.Logging;
using BBSCLI.Commands;

namespace BBSCLI
{
    public class CommandManager
    {
        private readonly Logger _logger;
        private readonly Dictionary<string, CommandBase> _commands = new Dictionary<string, CommandBase>(StringComparer.OrdinalIgnoreCase);

        public CommandManager(Logger logger)
        {
            _logger = logger;
            RegisterDefaultCommands();
        }

        private void RegisterDefaultCommands()
        {
            Register(new InstallDriverCommand(_logger));
            Register(new UninstallDriverCommand(_logger));
            Register(new GuiCommand(_logger));
            Register(new SelfCheckCommand(_logger));
            Register(new CheckBaselinesCommand(_logger));
        }

        public void Register(CommandBase cmd)
        {
            if (cmd == null) return;
            _commands[cmd.Name] = cmd;
        }

        public int Execute(string name, string[] args)
        {
            if (!_commands.TryGetValue(name, out var cmd))
            {
                _logger.Error($"Unknown command: {name}");
                return 1;
            }
            return cmd.Execute(args ?? Array.Empty<string>());
        }

        public IEnumerable<CommandBase> ListCommands() => _commands.Values;
    }
}
