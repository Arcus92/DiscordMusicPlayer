using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DiscordMusicPlayer
{
    /// <summary>
    /// DS 2017-06-24: The current playlist
    /// </summary>
    internal class Playlist
    {
        #region List

        /// <summary>
        /// The internal music file list
        /// </summary>
        private List<MusicFile> m_MusicFiles = new List<MusicFile>();

        /// <summary>
        /// A lock object to handle async thread access
        /// </summary>
        private object ListLock = new object();

        /// <summary>
        /// The current position
        /// </summary>
        private int m_CurrentPosition = 0;

        /// <summary>
        /// Use a count variable for a faster access
        /// </summary>
        private int m_Count;

        /// <summary>
        /// Returns the number of music tracks
        /// </summary>
        public int Count
        {
            get { return m_Count; }
        }

        /// <summary>
        /// Adds a music file
        /// </summary>
        /// <param name="musicFile"></param>
        public void Add(MusicFile musicFile)
        {
            lock (ListLock)
            {
                m_MusicFiles.Add(musicFile);
                m_Count++;
            }
        }

        /// <summary>
        /// Adds music files
        /// </summary>
        /// <param name="musicFiles"></param>
        public void AddRange(IEnumerable<MusicFile> musicFiles)
        {
            lock (ListLock)
            {
                foreach (var musicFile in musicFiles)
                {
                    m_MusicFiles.Add(musicFile);
                    m_Count++;
                }
            }
        }

        /// <summary>
        /// Goto the given music file
        /// </summary>
        /// <param name="musicFile"></param>
        public void Goto(MusicFile musicFile)
        {
            lock (ListLock)
            {
                int index = m_MusicFiles.IndexOf(musicFile);

                if (index >= 0) m_CurrentPosition = index;
            }
        }

        /// <summary>
        /// Shuffles the playlist
        /// </summary>
        public void Shuffle()
        {
            lock (ListLock)
            {
                m_CurrentPosition = 0;

                Random random = new Random();

                int n = m_MusicFiles.Count;
                while (n > 1)
                {
                    n--;
                    int k = random.Next(n + 1);
                    var value = m_MusicFiles[k];
                    m_MusicFiles[k] = m_MusicFiles[n];
                    m_MusicFiles[n] = value;
                }
            }
        }

        #endregion List

        /// <summary>
        /// Gets the current music file
        /// </summary>
        public MusicFile CurrentMusicFile
        {
            get
            {
                lock (ListLock)
                {
                    // Invalid
                    if (m_Count == 0 || m_CurrentPosition < 0 || m_CurrentPosition >= m_Count) return null;

                    return m_MusicFiles[m_CurrentPosition];
                }
            }
        }

        /// <summary>
        /// Returns the next music file
        /// </summary>
        /// <returns></returns>
        public MusicFile GetNextMusicFile()
        {
            lock (ListLock)
            {
                // Invalid
                if (m_Count == 0) return null;

                m_CurrentPosition++;

                if (m_CurrentPosition >= m_Count) m_CurrentPosition = 0;

                return m_MusicFiles[m_CurrentPosition];
            }
        }

        #region Find

        /// <summary>
        /// Finds a music track by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<MusicFile> Find(string name)
        {
            bool foundAny = false;
            lock (ListLock)
            {
                // Search the title
                foreach (var musicFile in m_MusicFiles.Where(m => m.Title.ToLower().Contains(name.ToLower())))
                {
                    yield return musicFile;
                    foundAny = true;
                }

                // We found a track by the title ... we do not neet to check the rest
                if (foundAny) yield break;

                // Search every name field
                foreach (var musicFile in m_MusicFiles.Where(m => m.Name.ToLower().Contains(name.ToLower())))
                {
                    yield return musicFile;
                }
            }

        }

        #endregion Find

        #region Static

        /// <summary>
        /// The allowed music extansion
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
