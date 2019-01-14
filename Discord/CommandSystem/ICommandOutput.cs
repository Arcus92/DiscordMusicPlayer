using System.Threading.Tasks;

namespace DiscordMusicPlayer.CommandSystem
{
    /// <summary>
    /// DS 2017-06-27: The command output
    /// </summary>
    internal interface ICommandOutput
    {
        /// <summary>
        /// Sends a message to the output
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendAsync(string message);
    }
}
