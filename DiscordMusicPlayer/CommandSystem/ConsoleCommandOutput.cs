using System.Threading.Tasks;

namespace DiscordMusicPlayer.CommandSystem
{
    /// <summary>
    /// DS 2017-06-27: The console output
    /// </summary>
    internal class ConsoleCommandOutput : ICommandOutput
    {
        /// <summary>
        /// Sends a message to the console
        /// </summary>
        /// <param name="message"></param>
        public Task SendAsync(string message)
        {
            Logger.Log("Command", message);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets if markdown commands are supported by this output
        /// </summary>
        public bool SupportMarkdown
        {
            // Markdown is not supported by the default console output
            get { return false; }
        }
    }
}
