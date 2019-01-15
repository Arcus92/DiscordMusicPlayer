using DiscordMusicPlayer.CommandSystem;
using System;
using System.Reflection;

namespace DiscordMusicPlayer
{
    /// <summary>
    /// The programm
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

            // Register the console exit handler
            Win32.SetConsoleCtrlHandler(OnConsoleExitEvent, true);

            // Header
            Logger.Log("Program", "-------------------------------------");
            Logger.Log("Program", "{0} - {1}", ApplicationName, ApplicationVersion);
            Logger.Log("Program", "-------------------------------------");
            Logger.Log("Program", "Type <help> for a list of all commands.");
            Logger.Log("Program", "Type <exit> to close the program.");
            Logger.Log("Program", "Starting...");

            // Load the settings
            var settings = new Settings("config.xml");

            // Valid the inputs

            // Token
            if (string.IsNullOrEmpty(settings.Token))
            {
                Logger.Log(Tag, "Token is empty! Shutdown program...");
                Console.ReadKey();
                return;
            }

            // Guild
            if (string.IsNullOrEmpty(settings.Guild))
            {
                Logger.Log(Tag, "Guild name is empty! Shutdown program...");
                Console.ReadKey();
                return;
            }

            // Channel
            if (string.IsNullOrEmpty(settings.Channel))
            {
                Logger.Log(Tag, "Channel name is empty! Shutdown program...");
                Console.ReadKey();
                return;
            }

            // Allowed users
            if (settings.AllowedUsers == null || settings.AllowedUsers.Length == 0)
            {
                Logger.Log(Tag, "There are no allowed users to control this bot. You are not able to change the current track.");
            }

            // Load the playlist
            Playlist playlist = new Playlist();

            Logger.Log("Playlist", "Loading music files...");
            playlist.AddRange(Playlist.GetMusicFilesFromDirectories(settings.Directories, true));


            // We cannot start the tool with no music
            if (playlist.Count == 0)
            {
                Logger.Log("Playlist", "There were no music files found! Shutdown program...");
                Console.ReadKey();
                return;
            }
            else Logger.Log("Playlist", "{0} music files found!", playlist.Count);

            // Shuffle the music
            if (settings.Shuffle) playlist.Shuffle();


            // Create the discord player
            using (m_DiscordMusicPlayer = new DiscordMusicPlayer(settings.TokenType, settings.Token))
            {
                // Setup the app
                m_DiscordMusicPlayer.AllowedUsers = settings.AllowedUsers;
                m_DiscordMusicPlayer.MusicPlayer.Volume = settings.Volume;
                m_DiscordMusicPlayer.Autoplay = settings.Autoplay;

                // Sets the playlist
                m_DiscordMusicPlayer.Playlist = playlist;

                // Connect
                m_DiscordMusicPlayer.Connect().Wait();

                m_DiscordMusicPlayer.JoinAudioChannel(settings.Guild, settings.Channel).Wait();

                bool loop = true;

                // End on key
                while (loop)
                {
                    // Read the command
                    string str = Console.ReadLine();

                    // Parse the command
                    var command = Command.Parse(str);

                    // Is command
                    if (!command.IsEmpty)
                    {
                        switch (command.Name.ToLower())
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
        }

        #region Exit handler
        
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

        #endregion Exit handler
    }
}
