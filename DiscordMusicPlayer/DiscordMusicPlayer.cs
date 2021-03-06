﻿using Discord;
using Discord.WebSocket;
using DiscordMusicPlayer.CommandSystem;
using DiscordMusicPlayer.Music;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordMusicPlayer
{
    /// <summary>
    /// DS 2017-06-24: The discord music player instance
    /// </summary>
    internal class DiscordMusicPlayer : IDisposable
    {
        /// <summary>
        /// Creates the discord music player
        /// </summary>
        /// <param name="token"></param>
        public DiscordMusicPlayer(string token) : this(TokenType.Bot, token)
        {
        }

        /// <summary>
        /// Creates the discord music player
        /// </summary>
        /// <param name="token"></param>
        public DiscordMusicPlayer(TokenType tokenType, string token)
        {
            // Stores the token
            TokenType = tokenType;
            Token = token;

            // Init the music player

            m_MusicPlayer = new MusicPlayer();
            m_MusicPlayer.OnCurrentMusicFileChanged += OnCurrentMusicTrackChanged;
        }

        #region Properties

        /// <summary>
        /// Gets the token type
        /// </summary>
        public TokenType TokenType { get; private set; }

        /// <summary>
        /// Gets the discord login token
        /// </summary>
        public string Token { get; private set; }

        /// <summary>
        /// Gets and sets the bot controller users
        /// </summary>
        public string[] AllowedUsers { get; set; }

        #endregion Properties

        #region Connection

        /// <summary>
        /// The discord client
        /// </summary>
        private DiscordSocketClient m_Client;

        /// <summary>
        /// The task completion source for the ready event
        /// </summary>
        private TaskCompletionSource<bool> m_TaskWaiterReady;

        /// <summary>
        /// Connects to the discord server
        /// </summary>
        /// <param name="token"></param>
        /// <param name=""></param>
        public async Task Connect()
        {
            // Creates the discord client
            m_Client = new DiscordSocketClient(new DiscordSocketConfig());

            // Add the event
            m_Client.MessageReceived += OnMessageReceived;
            m_Client.Ready += OnReady;
            m_Client.LoggedIn += OnLoggedIn;
            m_Client.LoggedOut += OnLoggedOut;
            m_Client.Log += OnLogMessage;
            m_Client.GuildAvailable += OnGuildAvailable;

            // Creates the task completion handler
            m_TaskWaiterReady = new TaskCompletionSource<bool>();

            // Login
            await m_Client.LoginAsync(TokenType, Token).ConfigureAwait(false);

            // Start the api
            await m_Client.StartAsync().ConfigureAwait(false);

            // Waits for the ready event
            await m_TaskWaiterReady.Task.ConfigureAwait(false);
        }

        /// <summary>
        /// Disconnect
        /// </summary>
        public async Task Disconnect()
        {
            if (m_Client != null)
            {
                // Leave the channel
                await m_MusicPlayer.LeaveAudioChannel().ConfigureAwait(false);
                
                // Logout
                await m_Client.LogoutAsync().ConfigureAwait(false);

                // Stop the api
                await m_Client.StopAsync().ConfigureAwait(false);
            }
        }

        #endregion Connection

        #region Audio channel

        /// <summary>
        /// Joins a audio channel
        /// </summary>
        /// <param name="channel"></param>
        private async Task JoinAudioChannel(IAudioChannel channel)
        {
            await m_MusicPlayer.JoinAudioChannel(channel).ConfigureAwait(false);

            // A audio channel was joined
            OnAudioChannelJoined();
        }

        /// <summary>
        /// Leaves the current audio channel
        /// </summary>
        /// <returns></returns>
        private async Task LeaveAudioChannel()
        {
            await m_MusicPlayer.LeaveAudioChannel().ConfigureAwait(false);
        }

        /// <summary>
        /// An audio channel was joined
        /// </summary>
        private void OnAudioChannelJoined()
        {
            // Only play if autostart is enabled
            if (Autoplay)
            {
                m_MusicPlayer.Play();
            }
        }

        /// <summary>
        /// The current connected guild
        /// </summary>
        private IGuild m_Guild;

        /// <summary>
        /// Joins the given audio channel on the given guild 
        /// </summary>
        /// <param name="guildName">The name (or id) of the channel to connect with</param>
        /// <param name="channelName">The name (or id) of the guild</param>
        /// <returns></returns>
        public async Task JoinAudioChannel(string guildName, string channelName)
        {
            const string Tag = "JoinAudioChannel";

            // Find the guild
            IGuild guild = GetGuildByNameOrId(m_Client.Guilds, guildName);

            // Found
            if (guild != null)
            {
                await JoinAudioChannel(guild, channelName).ConfigureAwait(false);
            }
            else
            {
                // Log error
                Logger.Log(Tag, "Guild '{0}' was not found!", guildName);
            }
        }

        /// <summary>
        /// Joins the given audio channel on the current guild 
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public async Task JoinAudioChannel(string channelName)
        {
            const string Tag = "JoinAudioChannel";

            if (m_Guild != null)
            {
                await JoinAudioChannel(m_Guild, channelName).ConfigureAwait(false);
            }
            else
            {
                // Log error
                Logger.Log(Tag, "There is no active guild!");
            }
        }

        /// <summary>
        /// Joins the given audio channel on the given guild 
        /// </summary>
        /// <param name="guild">The guild</param>
        /// <param name="channelName">The name (or id) of the guild</param>
        /// <returns></returns>
        private async Task JoinAudioChannel(IGuild guild, string channelName)
        {
            const string Tag = "JoinAudioChannel";

            // Stores the given guild
            m_Guild = guild;

            // Load all voice channel
            var channels = await guild.GetVoiceChannelsAsync().ConfigureAwait(false);

            // Finds the voice channel
            IVoiceChannel channel = GetChannelByNameOrId(channels, channelName);

            if (channel != null)
            {
                Logger.Log(Tag, "Joining audio channel '{0}' on '{1}'...", channelName, guild.Name);

                try
                {
                    await JoinAudioChannel(channel).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Log(Tag, "Failed to join audio channel '{0}' on '{1}'!", channelName, guild.Name);
                    Logger.Log(Tag, e.ToString());
                }

            }
            else
            {
                // Log error
                Logger.Log(Tag, "Audio channel '{0}' was not found on '{1}'!", channelName, guild.Name);
            }
        }

        #endregion Audio channel

        #region Notification channel

        /// <summary>
        /// The current notification channel.
        /// The program will post updates of the current playing
        /// track. This can create a lot of spam!
        /// This feature is optinal and should use at own risk.
        /// </summary>
        private ITextChannel m_CurrentNotificationChannel;

        /// <summary>
        /// Gets and sets the notification channel.
        /// </summary>
        public ITextChannel NotificationChannel
        {
            get { return m_CurrentNotificationChannel; }
        }

        /// <summary>
        /// Gets the notification message.
        /// The bot will write notifications in this text change when a
        /// music track changed.
        /// You can use the following placeholders: {{title}}, {{artist}},
        /// and {{album}}
        /// </summary>
        public string NotificationMessage { get; set; }

        /// <summary>
        /// Joins a notification channel
        /// </summary>
        /// <param name="channel"></param>
        public async Task JoinNotificationChannel(ITextChannel channel)
        {
            // Leave the old channel
            if (m_CurrentNotificationChannel != null)
            {
                LeaveNotificationChannel();
            }

            // Joins the new audio channel
            m_CurrentNotificationChannel = channel;

            // Updates the current track
            await UpdateTrackInNotificationChannel(m_MusicPlayer.Playlist.CurrentMusicFile).ConfigureAwait(false);
        }

        /// <summary>
        /// Leavse the current notification channel
        /// </summary>
        /// <returns></returns>
        public void LeaveNotificationChannel()
        {
            if (m_CurrentNotificationChannel == null) return;

            // Sets the channel to null
            m_CurrentNotificationChannel = null;
        }

        /// <summary>
        /// Posts the current track in the notification channel
        /// </summary>
        /// <param name="musicFile">The current music file</param>
        private async Task UpdateTrackInNotificationChannel(MusicFile musicFile)
        {
            // Skip if there is no notification channel
            if (m_CurrentNotificationChannel == null) return;

            // Also skip if the music file is null
            if (musicFile == null) return;

            // And also the notification message must be set
            if (string.IsNullOrEmpty(NotificationMessage)) return;

            // Builds the message
            string message = NotificationMessage;
            message = message.Replace("{{title}}", musicFile.Title, StringComparison.InvariantCulture);
            message = message.Replace("{{artist}}", musicFile.Artists, StringComparison.InvariantCulture);
            message = message.Replace("{{album}}", musicFile.Album, StringComparison.InvariantCulture);

            // Sends the message
            await m_CurrentNotificationChannel.SendMessageAsync(message).ConfigureAwait(false);
        }

        /// <summary>
        /// Joins the given notification channel on the given guild 
        /// </summary>
        /// <param name="guildName">The name (or id) of the channel to connect with</param>
        /// <param name="channelName">The name (or id) of the guild</param>
        /// <returns></returns>
        public async Task JoinNotificationChannel(string guildName, string channelName)
        {
            // Skip on empty channel name
            if (string.IsNullOrEmpty(channelName)) return;

            const string Tag = "JoinNotificationChannel";

            // Find the guild
            IGuild guild = GetGuildByNameOrId(m_Client.Guilds, guildName);

            // Found
            if (guild != null)
            {
                await JoinNotificationChannel(guild, channelName).ConfigureAwait(false);
            }
            else
            {
                // Log error
                Logger.Log(Tag, "Guild '{0}' was not found!", guildName);
            }
        }

        /// <summary>
        /// Joins the given notification channel on the current guild 
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        public async Task JoinNotificationChannel(string channelName)
        {
            // Skip on empty channel name
            if (string.IsNullOrEmpty(channelName)) return;

            const string Tag = "JoinNotificationChannel";

            if (m_Guild != null)
            {
                await JoinNotificationChannel(m_Guild, channelName).ConfigureAwait(false);
            }
            else
            {
                // Log error
                Logger.Log(Tag, "There is no active guild!");
            }
        }

        /// <summary>
        /// Joins the notification channel
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="channelName"></param>
        /// <returns></returns>
        private async Task JoinNotificationChannel(IGuild guild, string channelName)
        {
            // Skip on empty channel name
            if (string.IsNullOrEmpty(channelName)) return;

            const string Tag = "JoinNotificationChannel";

            // Load all voice channel
            var channels = await guild.GetTextChannelsAsync().ConfigureAwait(false);

            // Finds the voice channel
            ITextChannel channel = GetChannelByNameOrId(channels, channelName);

            if (channel != null)
            {
                Logger.Log(Tag, "Joining text channel '{0}' on '{1}'...", channelName, guild.Name);

                try
                {
                    await JoinNotificationChannel(channel).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Log(Tag, "Failed to join text channel '{0}' on '{1}'!", channelName, guild.Name);
                    Logger.Log(Tag, e.ToString());
                }
            }
            else
            {
                // Log error
                Logger.Log(Tag, "Text channel '{0}' was not found on '{1}'!", channelName, guild.Name);
            }
        }

        #endregion Notification channel

        #region Music player

        /// <summary>
        /// Creates the music player
        /// </summary>
        private MusicPlayer m_MusicPlayer;

        /// <summary>
        /// Gets the music player
        /// </summary>
        public MusicPlayer MusicPlayer
        {
            get { return m_MusicPlayer; }
        }

        /// <summary>
        /// Gets and sets if the player should start when the bot connects the audio channel
        /// </summary>
        public bool Autoplay { get; set; }

        /// <summary>
        /// Gets and sets the playlist
        /// </summary>
        public Playlist Playlist
        {
            get { return m_MusicPlayer.Playlist; }
            set { m_MusicPlayer.Playlist = value; }
        }

        #endregion Music player

        #region Events

        /// <summary>
        /// The current music track changes
        /// </summary>
        /// <param name="musicFile"></param>
        private async Task OnCurrentMusicTrackChanged(MusicFile musicFile)
        {
            if (musicFile != null)
            {
                // Write to log
                Logger.Log("Music", "Playing: {0}", musicFile.Name);

                // Change the console title
                Console.Title = musicFile.Name;

                // Change the discord status
                await m_Client.SetGameAsync(musicFile.Name, null, ActivityType.Listening).ConfigureAwait(false);

                // Posts a message to the notification channel
                await UpdateTrackInNotificationChannel(musicFile).ConfigureAwait(false);
            }
            else
            {
                // Change the console title
                Console.Title = "DiscordMusicPlayer";

                // Change the discord status
                await m_Client.SetGameAsync(null).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// A new guild is available
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private Task OnGuildAvailable(SocketGuild arg)
        {
            Logger.Log("Guild", "New guild '{0}' available.", arg.Name);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Receives a discord log message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private Task OnLogMessage(LogMessage log)
        {
            // Log
            Logger.Log("Discord.Net", "{0}: {1}", log.Severity, log.ToString());

            return Task.CompletedTask;
        }

        /// <summary>
        /// The user logged in
        /// </summary>
        /// <returns></returns>
        private Task OnLoggedIn()
        {
            // Log
            Logger.Log("User", "Logged in");

            return Task.CompletedTask;
        }

        /// <summary>
        /// The user logged out
        /// </summary>
        /// <returns></returns>
        private Task OnLoggedOut()
        {
            // Log
            Logger.Log("User", "Logged out");

            return Task.CompletedTask;
        }

        /// <summary>
        /// The bot is ready
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private Task OnReady()
        {
            // Log
            Logger.Log("User", "Ready");

            // Raise the ready task completion event
            if (m_TaskWaiterReady != null)
                m_TaskWaiterReady.SetResult(true);

            return Task.CompletedTask;
        }

        /// <summary>
        /// A private message was received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task OnMessageReceived(SocketMessage e)
        {
            // Do not register own messages
            if (e.Author.Id == m_Client.CurrentUser.Id) return;

            // Only listen to dm channels
            if (e.Channel is SocketDMChannel)
            {
                // Log
                Logger.Log("Chat", "{0}: {1}", e.Author.ToString(), e.Content);

                // TODO: Some clever admin protection
                if (IsUserInList(AllowedUsers, e.Author))
                {
                    // Parse the command
                    var command = Command.Parse(e.Content);

                    // Execute the command
                    await ExecuteCommand(command, new UserCommandOutput(e.Author)).ConfigureAwait(false);
                }
                else
                {
                    Logger.Log("Chat", "User '{0}' (id: {1}) is not allowed to control this bot!",  e.Author.ToString(), e.Author.Id);
                }
            }
        }

        /// <summary>
        /// Checks if the user is in the given list
        /// </summary>
        /// <param name="users"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private static bool IsUserInList(IEnumerable<string> users, SocketUser user)
        {
            // First scan ids
            if (users.Contains(user.Id.ToString(CultureInfo.InvariantCulture)))
                return true;

            // Than the usernames (with #xxxx)
            if (users.Contains(user.ToString()))
                return true;

            return false;
        }

        #endregion Events

        #region Commands

        /// <summary>
        /// Executes a command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        public async Task ExecuteCommand(Command command, ICommandOutput output)
        {
            // Ignore
            if (command.IsEmpty) return;

            StringBuilder builder;

            // Select the command name
            switch (command.Name.ToUpperInvariant())
            {
                // Skip
                case "NEXT":
                case "SKIP":
                    m_MusicPlayer.Next();
                    break;

                // Stop
                case "STOP":
                case "PAUSE":
                    m_MusicPlayer.Stop();
                    break;

                // Play
                case "PLAY":
                    // Continue
                    if (command.Arguments.Length == 0)
                    {
                        m_MusicPlayer.Play();
                    }
                    else
                    {
                        // Check playlist
                        if (m_MusicPlayer.Playlist != null)
                        {
                            // Finds all tracks with this name
                            MusicFile[] musicFiles = m_MusicPlayer.Playlist.Find(command.Argument).ToArray();

                            // No files
                            if (musicFiles.Length == 0)
                            {
                                await output.SendAsync("Song not found.").ConfigureAwait(false);
                            }
                            // Only one file was found ... play it
                            else if (musicFiles.Length == 1)
                            {
                                m_MusicPlayer.Play(musicFiles[0]);
                            }
                            else // More
                            {
                                builder = new StringBuilder();

                                builder.AppendLine("Please select:");

                                // Starts a code block if markdown is supported
                                if (output.SupportMarkdown)
                                    builder.AppendLine("```");

                                int c = 0;
                                foreach(var musicFile in musicFiles)
                                {
                                    builder.AppendLine(musicFile.Name);

                                    // Stop
                                    if (c++ > 10) {
                                        builder.AppendLine("...");
                                        break; }
                                }

                                // Ends the code block if markdown is supported
                                if (output.SupportMarkdown)
                                    builder.AppendLine("```");

                                await output.SendAsync(builder.ToString()).ConfigureAwait(false);
                            }
                        }
                    }
                    break;

                // Change the volume
                case "VOLUME":
                    int val;
                    // There is no argument
                    if (string.IsNullOrEmpty(command.Argument))
                    {
                        // Return the current volume
                        await output.SendAsync(string.Format("The volume is currently at {0}%.", Math.Round(m_MusicPlayer.Volume * 100))).ConfigureAwait(false);
                    }
                    else
                    {
                        if (int.TryParse(command.Argument, out val))
                        {
                            if (val < 0 || val > 100)
                            {
                                break;
                            }

                            // Sets the volume
                            m_MusicPlayer.Volume = val / 100f;
                        }
                    }
                    break;

                // Joins another guild or channel 
                case "JOIN":
                    if (command.Arguments.Length == 1)
                    {
                        await JoinAudioChannel(command.Arguments[0]).ConfigureAwait(false);
                    }
                    else if (command.Arguments.Length == 2)
                    {
                        await JoinAudioChannel(command.Arguments[0], command.Arguments[1]).ConfigureAwait(false);
                    }
                    else
                    {
                        await output.SendAsync("Please enter a channel name or id: join [<guild name or id>] <channel name or id>").ConfigureAwait(false);
                    }
                    break;

                // Prints a list of all commands
                case "HELP":
                    // Builds the text
                    builder = new StringBuilder();

                    builder.AppendLine("You can use the following commands:");

                    // Starts a code block if markdown is supported
                    if (output.SupportMarkdown)
                        builder.AppendLine("```");

                    builder.AppendLine("play                    - Resumes the playback of the current track.");
                    builder.AppendLine("play <title>            - Plays the track with the given title.");
                    builder.AppendLine("stop                    - Stops the playback.");
                    builder.AppendLine("next                    - Skips the current track and plays the next title on the playlist.");
                    builder.AppendLine("volume                  - Gets the current volume.");
                    builder.AppendLine("volume <volume>         - Sets the volume (0 - 100).");
                    builder.AppendLine("join <channel>          - Joins the audio channel on the current guild.");
                    builder.AppendLine("join <guild> <channel>  - Joins the audio channel on the given guild.");
                    builder.AppendLine("help                    - Shows this useful message.");
                    builder.AppendLine("info                    - Shows the version number and links to the creators homepage.");
                    builder.AppendLine("exit                    - Closes the application. This can only be used in console mode!");

                    // Ends the code block if markdown is supported
                    if (output.SupportMarkdown)
                        builder.AppendLine("```");

                    await output.SendAsync(builder.ToString()).ConfigureAwait(false);
                    break;

                // Print the application info
                case "INFO":
                case "VERSION":
                case "ABOUT":
                    // Builds the text
                    builder = new StringBuilder();

                    // Starts a code block if markdown is supported
                    if (output.SupportMarkdown)
                        builder.AppendLine("```");

                    builder.AppendLine(string.Format("{0} - {1}", Program.ApplicationName, Program.ApplicationVersion));
                    builder.AppendLine("Homepage:   https://www.david-schulte.de");
                    builder.AppendLine("GitHub:     https://github.com/Arcus92/DiscordMusicPlayer");
                    builder.AppendLine("E-Mail:     mail@david-schulte.de");
                    builder.AppendLine();
                    builder.AppendLine("Thank you for using this application!");

                    // Ends the code block if markdown is supported
                    if (output.SupportMarkdown)
                        builder.AppendLine("```");

                    
                    await output.SendAsync(builder.ToString()).ConfigureAwait(false);
                    break;
            }
        }

        #endregion Commands

        #region Static

        /// <summary>
        /// Gets the guild by the given name or id
        /// </summary>
        /// <param name="guilds"></param>
        /// <param name="nameOrId"></param>
        /// <returns></returns>
        private static IGuild GetGuildByNameOrId(IEnumerable<IGuild> guilds, string nameOrId)
        {
            // Try the id first
            ulong id;
            if (ulong.TryParse(nameOrId, out id))
            {
                IGuild guild = guilds.Where(g => g.Id == id).FirstOrDefault();
                if (guild != null)
                    return guild;
            }

            return guilds.Where(g => g.Name == nameOrId).FirstOrDefault();
        }

        /// <summary>
        /// Gets the channel by the given name or id
        /// </summary>
        /// <param name="channels"></param>
        /// <param name="nameOrId"></param>
        /// <returns></returns>
        /// <typeparam name="T"></typeparam>
        private static T GetChannelByNameOrId<T>(IEnumerable<T> channels, string nameOrId) where T : IChannel
        {
            // Try the id first
            ulong id;
            if (ulong.TryParse(nameOrId, out id))
            {
                T channel = channels.Where(g => g.Id == id).FirstOrDefault();
                if (channel != null)
                    return channel;
            }

            return channels.Where(g => g.Name == nameOrId).FirstOrDefault();
        }

        #endregion Static

        #region Dispose

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            if (m_Client != null)
            {
                m_Client.Dispose();
                m_Client = null;
            }
        }

        #endregion Dispose
    }
}
