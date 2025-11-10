using BBSCLI.Logging;

namespace BBSCLI.Commands
{
    public abstract class CommandBase
    {
        protected readonly Logger Logger;
        public string Name { get; }
        public string Description { get; }

        protected CommandBase(string name, string description, Logger logger)
        {
            Name = name;
            Description = description;
            Logger = logger;
        }

        public abstract int Execute(string[] args);
    }
}
