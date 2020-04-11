using DiscordMusicPlayer.CommandSystem;
using DiscordMusicPlayer.Music;
using System;
using System.Reflection;

namespace DiscordMusicPlayer
{
    /// <summary>
    /// The program
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The current discord music player
        /// </summary>
        private static DiscordMusicPlayer m_DiscordMusicPlayer;

        /// <summary>
        /// Gets the application name
        /// </summary>
        public const string ApplicationName = "Discord-Music-Player";

        /// <summary>
        /// Gets the application version
        /// </summary>
        public static readonly Version ApplicationVersion = Assembly.GetEntryAssembly().GetName().Version;

        /// <summary>
        /// The application entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // The tag
            const string Tag = "Program";

            // Header
            Logger.Log(Tag, "-------------------------------------");
            Logger.Log(Tag, "{0} - {1}", ApplicationName, ApplicationVersion);
            Logger.Log(Tag, "-------------------------------------");
            Logger.Log(Tag, "Type <exit> to close the program.");
            Logger.Log(Tag, "Type <help> to list all commands.");
            Logger.Log(Tag, "Starting...");

#if WINDOWS
            // Use a try-catch block to set the console exit handler.
            // This win32 api will fail on non-Windows systems.
            // This is completely optional so we can ignore it.
            try
            {
                // Register the console exit handler
                Win32.SetConsoleCtrlHandler(OnConsoleExitEvent, true);
            }
            catch
            {
                Logger.Log(Tag, "Could not set console close handler. Please use the 'exit' command to close the application properly.");
            }
#endif // WINDOWS

            // Load the settings
            var settings = new Settings("config.xml");

            // Valid the inputs

            // Token
            if (string.IsNullOrEmpty(settings.Token))
            {
                Logger.Log(Tag, "Token is empty! Press any key to shutdown...");
                Console.ReadKey();
                return;
            }

            // Guild
            if (string.IsNullOrEmpty(settings.Guild))
            {
                Logger.Log(Tag, "Guild name is empty! Press any key to shutdown...");
                Console.ReadKey();
                return;
            }

            // Channel
            if (string.IsNullOrEmpty(settings.Channel))
            {
                Logger.Log(Tag, "Channel name is empty! Press any key to shutdown...");
                Console.ReadKey();
                return;
            }

            // Load the playlist
            var playlist = new Playlist();
            var scanner = new MusicFileScanner();

            // Starts the music scanner
            scanner.Start(playlist, settings.Directories);

            // Wait two seconds to index at least a few tracks before starting the playback.
            scanner.WaitForScanner(2000);


            // Shuffle the music
            if (settings.Shuffle) playlist.Shuffle();

            // This is a user account
            if ((int)settings.TokenType == 0) // Discord.TokenType.User
            {
                Logger.Log(Tag, "--------------------------------------------------");
                Logger.Log(Tag, "Do not use a user account for this application!");
                Logger.Log(Tag, "This violates the Terms of Service of Discord and");
                Logger.Log(Tag, "can result in an account ban.");
                Logger.Log(Tag, "Press any key to continue at your own risk!");
                Logger.Log(Tag, "--------------------------------------------------");

                Console.ReadKey();
            }

            // Create the discord player
            using (m_DiscordMusicPlayer = new DiscordMusicPlayer(settings.TokenType, settings.Token))
            {
                // Setup the app
                m_DiscordMusicPlayer.AllowedUsers = settings.AllowedUsers;
                m_DiscordMusicPlayer.MusicPlayer.Volume = settings.Volume;
                m_DiscordMusicPlayer.Autoplay = settings.Autoplay;
                m_DiscordMusicPlayer.NotificationMessage = settings.NotificationMessage;

                // Sets the playlist
                m_DiscordMusicPlayer.Playlist = playlist;

                // Connect
                m_DiscordMusicPlayer.Connect().Wait();

                // Join the default notification channel
                m_DiscordMusicPlayer.JoinNotificationChannel(settings.Guild, settings.NotificationChannel).Wait();

                // Join the default guild and channel
                m_DiscordMusicPlayer.JoinAudioChannel(settings.Guild, settings.Channel).Wait();

                bool loop = true;

                // End on key
                while (loop)
                {
                    // Read the command
                    string str = Console.ReadLine();
                    if (str == null)
                        continue;

                    // Parse the command
                    var command = Command.Parse(str);

                    // Is command
                    if (!command.IsEmpty)
                    {
                        switch (command.Name.ToLowerInvariant())
                        {
                            case "exit":
                            case "quit":
                                loop = false;
                                break;
                            default:
                                // Use the discord command parser
                                m_DiscordMusicPlayer.ExecuteCommand(command, new ConsoleCommandOutput()).Wait();
                                break;
                        }
                    }
                }
            }

            // Abort the scanner if it is still active.
            scanner.Abort();
        }

        #region Exit handler

#if WINDOWS
        /// <summary>
        /// The console exit event 
        /// </summary>
        /// <param name="ctrlType"></param>
        /// <returns></returns>
        private static bool OnConsoleExitEvent(Win32.ConsoleCtrlTypes ctrlType)
        {
            if (m_DiscordMusicPlayer != null)
            {
                // Disconnect
                m_DiscordMusicPlayer.Disconnect().Wait();
            }

            // Returns true success the exit event
            return true;
        }
#endif // WINDOWS

        #endregion Exit handler
    }
}
