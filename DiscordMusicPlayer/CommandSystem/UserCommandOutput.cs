using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace DiscordMusicPlayer.CommandSystem
{
    /// <summary>
    /// DS 2017-06-27: A commando output for a discord chat. 
    /// </summary>
    internal class UserCommandOutput : ICommandOutput
    {
        /// <summary>
        /// The user
        /// </summary>
        private SocketUser m_User;

        /// <summary>
        /// Creates the user command output
        /// </summary>
        /// <param name="socketUser"></param>
        public UserCommandOutput(SocketUser socketUser)
        {
            m_User = socketUser;
        }

        /// <summary>
        /// Sends a message to the user
        /// </summary>
        /// <param name="message"></param>
        public async Task SendAsync(string message)
        {
            await m_User.SendMessageAsync(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets if markdown commands are supported by this output
        /// </summary>
        public bool SupportMarkdown
        {
            // Markdown is supported by the discrod chat
            get { return true; }
        }
    }
}
