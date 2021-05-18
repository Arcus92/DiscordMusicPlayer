using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordMusicPlayer.Music
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
            AddMusicFileToList(musicFile);
        }

        /// <summary>
        /// Adds music files
        /// </summary>
        /// <param name="musicFiles"></param>
        public void AddRange(IEnumerable<MusicFile> musicFiles)
        {
            foreach (var musicFile in musicFiles)
            {
                AddMusicFileToList(musicFile);
            }
        }

        /// <summary>
        /// Adds the music file to the playlist.
        /// This will add the track at the end of the list or
        /// if shuffle is enabled at a random position.
        /// </summary>
        /// <param name="musicFile"></param>
        private void AddMusicFileToList(MusicFile musicFile)
        {
            lock (ListLock)
            {
                if (m_IsShuffle)
                {
                    // Gets a random position for the music file but makes sure
                    // the file is always inserted after the current track.
                    // If the import of the library takes some time and the first tracks 
                    // were already played there is a chance that the track could be imported
                    // before the currently played track. This would shift m_CurrentPosition. 
                    int pos = m_CurrentPosition + m_Random.Next(m_Count - m_CurrentPosition) + 1;
                    m_MusicFiles.Insert(pos, musicFile);
                }
                else
                {
                    // Simply adds the track to the list
                    m_MusicFiles.Add(musicFile);
                }
                m_Count++;
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

        #region Shuffle

        /// <summary>
        /// A rng used for the shuffle function
        /// </summary>
        private Random m_Random = new Random();

        /// <summary>
        /// Stores that the playlist is shuffled
        /// </summary>
        private bool m_IsShuffle;

        /// <summary>
        /// Shuffles the playlist
        /// </summary>
        public void Shuffle()
        {
            // Sets the playlist to shuffled
            m_IsShuffle = true;

            // Is this still needed if we have the new shuffle flag that inserts 
            // the tracks in a random position anyway?
            // Maybe we'll add a shuffle command later so i leave this here.
            lock (ListLock)
            {
                m_CurrentPosition = 0;

                int n = m_MusicFiles.Count;
                while (n > 1)
                {
                    n--;
                    int k = m_Random.Next(n + 1);
                    var value = m_MusicFiles[k];
                    m_MusicFiles[k] = m_MusicFiles[n];
                    m_MusicFiles[n] = value;
                }
            }
        }

        #endregion Shuffle

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
                foreach (var musicFile in m_MusicFiles.Where(m => m.Title.Contains(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    yield return musicFile;
                    foundAny = true;
                }

                // We found a track by the title ... we do not need to check the rest
                if (foundAny) yield break;

                // Search every name field
                foreach (var musicFile in m_MusicFiles.Where(m => m.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase)))
                {
                    yield return musicFile;
                }
            }

        }

        #endregion Find
    }
}
