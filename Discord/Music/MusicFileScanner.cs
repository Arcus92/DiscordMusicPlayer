using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DiscordMusicPlayer.Music
{
    /// <summary>
    /// DS 2019-01-19: A file scanner that scans given directories for music tracks.
    /// The scanner works asynchronous and collects the music library in the background.
    /// So the player can start the playback after a few seconds loading even if this is
    /// a very very large music library.
    /// </summary>
    internal class MusicFileScanner
    {
        /// <summary>
        /// Gets the current playlist to insert the found music files.
        /// </summary>
        public Playlist Playlist { get; private set; }

        /// <summary>
        /// Gets the list of directories to scan.
        /// </summary>
        public string[] Directories { get; private set; }

        /// <summary>
        /// The tag for the logger
        /// </summary>
        private const string Tag = "MusicScanner";

        /// <summary>
        /// Starts the scanner with the given directories.
        /// </summary>
        /// <param name="directories">The music directories</param>
        public void Start(params string[] directories)
        {
            // Creates a new playlist
            var playlist = new Playlist();

            Start(playlist, directories);
        }

        /// <summary>
        /// Starts the scanner with the given directories.
        /// </summary>
        /// <param name="playlist">The playlist</param>
        /// <param name="directories">The music directories</param>
        public void Start(Playlist playlist, params string[] directories)
        {
            // There are no directories to check
            if (directories == null || directories.Length == 0)
                return;

            if (m_Thread != null)
                throw new Exception("The scanner has already been started. You can not start a scanner twice.");

            // Store the values
            Playlist = playlist;
            Directories = directories;

            // Sets the scanner flag
            IsScanning = true;

            // Creates the thread and starts it
            m_Thread = new Thread(BackgroundThread);
            m_Thread.Start();
        }

        /// <summary>
        /// Abort the scanning of the library.
        /// </summary>
        public void Abort()
        {
            if (m_Thread != null)
            {
                m_Thread.Abort();
            }
        }

        #region Thread

        /// <summary>
        /// The scanner thread
        /// </summary>
        private Thread m_Thread;

        /// <summary>
        /// Gets if the scanner is still active
        /// </summary>
        public bool IsScanning { get; private set; }

        /// <summary>
        /// The background task to scan the music directories.
        /// </summary>
        private void BackgroundThread()
        {
            Logger.Log(Tag, "Starts scanning music library...");

            // Finds all music tracks and adds them to the playlist
            Playlist.AddRange(GetMusicFilesFromDirectories(Directories, true));

            Logger.Log(Tag, "Scanner finished!");
            Logger.Log(Tag, "{0} music files were found!", Playlist.Count);

            // Release the scanner flag
            IsScanning = false;
        }

        /// <summary>
        /// Waits for the scanner to index the music library.
        /// You should use it wait for the scanner or at least a few seconds until enough tracks were imported
        /// to start the playback.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait for the scanner.</param>
        public void WaitForScanner(int millisecondsTimeout)
        {
            // There is no background thread
            if (m_Thread == null)
                return;

            // Waits for the thread
            m_Thread.Join(millisecondsTimeout);

            // The scanner is still running.
            // Start the playback with the currently collected tracks
            // and continue to scan in the background. 
            if (IsScanning)
            {
                Logger.Log(Tag, "The scanner didn't finish in time. It will collect all music files in the background.");
                Logger.Log(Tag, "Not all tracks are available yet. You may have to wait for the scanner to finish.");
            }
        }

        #endregion Thread

        #region Static

        /// <summary>
        /// The allowed music extension
        /// </summary>
        public static readonly string[] AllowedMusicExtansions = new string[] { ".mp3", ".wav" };

        /// <summary>
        /// Gets all music files from a directory
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="includeSubDirectories"></param>
        /// <returns></returns>
        public static IEnumerable<MusicFile> GetMusicFilesFromDirectory(string directory, bool includeSubDirectories)
        {
            // Check for existence
            if (Directory.Exists(directory))
            {
                // Search all music files
                foreach (string file in Directory.EnumerateFiles(directory, "*.*", includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
                    .Where(f => AllowedMusicExtansions.Contains(Path.GetExtension(f).ToLower())))
                {
                    yield return new MusicFile(file);
                }
            }
            else
            {
                Logger.Log("Playlist", "Invalid directory: {0}", directory);
            }
        }

        /// <summary>
        /// Gets all music files from directories
        /// </summary>
        /// <param name="directories"></param>
        /// <param name="includeSubDirectories"></param>
        /// <returns></returns>
        public static IEnumerable<MusicFile> GetMusicFilesFromDirectories(IEnumerable<string> directories, bool includeSubDirectories)
        {
            foreach (var directory in directories)
            {
                // Load all files from this directory
                foreach (var musicFile in GetMusicFilesFromDirectory(directory, includeSubDirectories))
                {
                    yield return musicFile;
                }
            }
        }

        #endregion Static
    }
}
