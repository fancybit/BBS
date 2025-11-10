using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Collections.Generic;
using System.Text;
using BBSCLI.Logging;
namespace BBSCLI
{
    static class Program
    {
        static int Main(string[] args)
        {
            var logger = new Logger(Path.Combine(AppContext.BaseDirectory ?? ".", "logs", "bbscli.log"));
            var manager = new CommandManager(logger);

            if (args.Length == 0)
            {
                return RunInteractive(manager, logger);
            }

            return HandleCommand(manager, args, logger);
        }

        static int RunInteractive(CommandManager manager, Logger logger)
        {
            logger.Info("Entering interactive mode");
            Console.WriteLine("Entering interactive mode. Type 'help' for commands, 'exit' to quit.");
            while (true)
            {
                Console.Write("BBS> ");
                var line = Console.ReadLine();
                if (line == null) break;
                line = line.Trim();
                if (line.Length == 0) continue;
                if (string.Equals(line, "exit", StringComparison.OrdinalIgnoreCase) || string.Equals(line, "quit", StringComparison.OrdinalIgnoreCase)) break;
                if (string.Equals(line, "help", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var c in manager.ListCommands())
                        Console.WriteLine($"  {c.Name} - {c.Description}");
                    continue;
                }
                var parts = SplitArguments(line);
                if (parts.Count == 0) continue;
                try
                {
                    var args = parts.ToArray();
                    int code = HandleCommand(manager, args, logger);
                    Console.WriteLine($"Command exited with code {code}");
                }
                catch (Exception ex)
                {
                    logger.Error("Error: " + ex.Message);
                }
            }
            return 0;
        }

        static int HandleCommand(CommandManager manager, string[] args, Logger logger)
        {
            if (args.Length == 0)
            {
                logger.Info("No args provided");
                return 0;
            }

            // Normalize inputs
            string verb = args[0].ToLowerInvariant();
            string sub = args.Length > 1 ? args[1].ToLowerInvariant() : string.Empty;

            // Built-in help
            if (verb == "help")
            {
                if (args.Length > 1)
                {
                    var name = args[1];
                    var cmd = manager.ListCommands().FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (cmd != null)
                    {
                        Console.WriteLine($"{cmd.Name} - {cmd.Description}");
                        return 0;
                    }
                    Console.WriteLine("Command not found: " + name);
                    return 1;
                }

                Console.WriteLine("Available commands:");
                foreach (var c in manager.ListCommands())
                    Console.WriteLine($" {c.Name} - {c.Description}");
                return 0;
            }

            // Gather commands for lookup
            var commands = manager.ListCommands().ToList();
            var commandDict = commands.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            //1) direct match: first token is command name
            if (commandDict.TryGetValue(verb, out var directCmd))
            {
                var remaining = args.Length > 1 ? args[1..] : Array.Empty<string>();
                logger.Info($"Executing command '{directCmd.Name}' with {remaining.Length} args");
                try { return manager.Execute(directCmd.Name, remaining); } catch (Exception ex) { logger.Error("Command failed: " + ex.Message); return 1; }
            }

            //2) combined patterns: verb-sub, sub-verb, verbsub
            if (!string.IsNullOrEmpty(sub))
            {
                var candidates = new[] { $"{verb}-{sub}", $"{sub}-{verb}", $"{verb}{sub}" };
                foreach (var cand in candidates)
                {
                    if (commandDict.TryGetValue(cand, out var cmd))
                    {
                        var remaining = args.Length > 2 ? args[2..] : Array.Empty<string>();
                        logger.Info($"Executing command '{cmd.Name}' (matched pattern '{cand}') with {remaining.Length} args");
                        try { return manager.Execute(cmd.Name, remaining); } catch (Exception ex) { logger.Error("Command failed: " + ex.Message); return 1; }
                    }
                }
            }

            //3) try prefix/contains matching to support short forms
            var fuzzy = commands.FirstOrDefault(c => c.Name.StartsWith(verb, StringComparison.OrdinalIgnoreCase) || c.Name.IndexOf(verb, StringComparison.OrdinalIgnoreCase) >= 0);
            if (fuzzy != null)
            {
                var remaining = args.Length > 1 ? args[1..] : Array.Empty<string>();
                logger.Info($"Fuzzy matched '{verb}' to command '{fuzzy.Name}'");
                try { return manager.Execute(fuzzy.Name, remaining); } catch (Exception ex) { logger.Error("Command failed: " + ex.Message); return 1; }
            }

            //4) special-case mappings for common multi-word verbs
            var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "driver install", "install-driver" },
                { "install driver", "install-driver" },
                { "driver uninstall", "uninstall-driver" },
                { "uninstall driver", "uninstall-driver" },
                { "run gui", "gui" },
                { "controlpanel", "gui" }
            };
            var key = string.IsNullOrEmpty(sub) ? verb : (verb + " " + sub);
            if (mapping.TryGetValue(key, out var mapped))
            {
                var remaining = args.Length > (string.IsNullOrEmpty(sub) ? 1 : 2) ? args[(string.IsNullOrEmpty(sub) ? 1 : 2)..] : Array.Empty<string>();
                logger.Info($"Mapped input '{key}' to command '{mapped}'");
                try { return manager.Execute(mapped, remaining); } catch (Exception ex) { logger.Error("Command failed: " + ex.Message); return 1 ; }
            }

            logger.Error("Unknown command: " + string.Join(' ', args));
            Console.WriteLine("Type 'help' to list available commands.");
            return 1;
        }

        static List<string> SplitArguments(string commandLine)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(commandLine)) return result;
            bool inQuotes = false;
            var cur = new StringBuilder();
            for (int i = 0; i < commandLine.Length; ++i)
            {
                char c = commandLine[i];
                if (c == '"') { inQuotes = !inQuotes; continue; }
                if (!inQuotes && char.IsWhiteSpace(c))
                {
                    if (cur.Length > 0) { result.Add(cur.ToString()); cur.Clear(); }
                }
                else cur.Append(c);
            }
            if (cur.Length > 0) result.Add(cur.ToString());
            return result;
        }
    }
}