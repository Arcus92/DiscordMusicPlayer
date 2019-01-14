using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscordMusicPlayer.CommandSystem
{
    /// <summary>
    /// DS 2019-01-14: A command struct. This is used to control the bot via
    /// the console api or a discord chat.
    /// </summary>
    public struct Command
    {
        /// <summary>
        /// Gets the name of the command
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the arguments array
        /// </summary>
        public string[] Arguments { get; private set; }

        /// <summary>
        /// Gets the argument line without parsing.
        /// This is used as fallback argument for old commands with only one argument like 'play <title>'.
        /// </summary>
        public string Argument { get; private set; }

        /// <summary>
        /// Creates a new command with the command name and its arguments
        /// </summary>
        /// <param name="name"></param>
        /// <param name="arguments"></param>
        public Command(string name, string[] arguments)
        {
            Name = name;
            Arguments = arguments;
            if (arguments != null)
                Argument = string.Join(" ", arguments);
            else
                Argument = null;
        }

        /// <summary>
        /// Returns if the command is empty or invalid
        /// </summary>
        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(Name); }
        }

        #region Static

        /// <summary>
        /// Parses a string into a command
        /// </summary>
        /// <param name="commandLine"></param>
        /// <returns></returns>
        public static Command Parse(string commandLine)
        {
            // Trims the command line
            commandLine = commandLine.Trim();

            // Creates the command
            var command = new Command();

            // Parse the command line
            bool escape = false;
            char currentString = '\0';
            var arguments = new List<string>();
            var builder = new StringBuilder();
            for(int i = 0; i< commandLine.Length; i++)
            {
                char c = commandLine[i];

                switch (c)
                {
                    // The begin or end of an escaped argument
                    case '"':
                    case '\'':
                        if (!escape)
                        {
                            if (currentString == '\0')
                            {
                                if (builder.Length == 0)
                                {
                                    currentString = c;
                                }
                                else
                                {
                                    // This is not valid but we will add the char anyway.
                                    builder.Append(c);
                                }
                            }
                            else
                            {
                                // This is the end of the parameter.
                                arguments.Add(builder.ToString());
                                builder.Clear();
                            }
                        }
                        else
                        {
                            // Adds the char to the current argument.
                            builder.Append(c);

                            // Reset the escape flag.
                            escape = false;
                        }
                        break;

                    // A space
                    case ' ':
                        // The current argument is not escaped.
                        // This is the end of the current argument.
                        if (currentString == '\0' && !escape)
                        {
                            // Adds the new argument
                            if (builder.Length > 0)
                            {
                                arguments.Add(builder.ToString());
                                builder.Clear();
                            }
                        }
                        else
                        {
                            // Adds the whitespace to the current argument.
                            builder.Append(c);

                            // Reset the escape flag.
                            escape = false;
                        }
                        break;

                    // A backslash to escape the next char
                    case '\\':
                        if (!escape)
                        {
                            // The next char is escaped
                            escape = true;
                        }
                        else
                        {
                            // The previous char was also an escape char.
                            // Add the backslash to the argument
                            builder.Append('\\');
                        }
                        break;

                    // All other chars
                    default:
                        builder.Append(c);

                        // Reset the escape flag.
                        escape = false;
                        break;
                }
            }

            // Adds the last argument
            if (builder.Length > 0)
            {
                arguments.Add(builder.ToString());
            }

            // Sets the name
            if (arguments.Count > 0)
            {
                command.Name = arguments[0];
            }

            // Sets the arguments
            if (arguments.Count > 1)
            {
                command.Arguments = arguments.Skip(1).ToArray();
                command.Argument = string.Join(" ", command.Arguments);
            }
            else
            {
                command.Arguments = new string[0];
                command.Argument = string.Empty;
            }

            // Returns the command
            return command;
        }

        #endregion Static
    }
}
