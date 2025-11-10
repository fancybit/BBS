using System;
using System.IO;
using BBSCLI.Logging;

namespace BBSCLI.Commands
{
    public class GuiCommand : CommandBase
    {
        public GuiCommand(Logger logger) : base("gui", "Launch the ControlPanel GUI", logger) { }

        public override int Execute(string[] args)
        {
            Logger.Info("Launching ControlPanel GUI...");
            System.Diagnostics.Process.Start("BBSGUI.exe");
            return 0;
        }
    }
}
