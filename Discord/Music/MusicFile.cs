using Id3;
using System;
using System.IO;
using System.Text;

namespace DiscordMusicPlayer.Music
{
    /// <summary>
    /// DS 2017-06-24: A single music file
    /// </summary>
    internal class MusicFile
    {
        /// <summary>
        /// Gets the file path
        /// </summary>
        public string File { get; private set; }

        /// <summary>
        /// Gets the title
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the artist
        /// </summary>
        public string Artists { get; private set; }

        /// <summary>
        /// Gets the Album
        /// </summary>
        public string Album { get; private set; }

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Creates a new music file
        /// </summary>
        /// <param name="file"></param>
        public MusicFile(string file)
        {
            // Store the file
            File = file;

            // The title is the file name
            Title = Path.GetFileNameWithoutExtension(file);

            try
            {
                // Mp3 file
                if (Path.GetExtension(file).ToLower() == ".mp3")
                {
                    // Open the file
                    using (FileStream fileStream = new FileStream(File, FileMode.Open))
                    {
                        // Use the mp3 reader
                        using (Mp3Stream mp3Stream = new Mp3Stream(fileStream, Mp3Permissions.Read))
                        {
                            // Has tags
                            if (mp3Stream.HasTags)
                            {
                                // Search in all tags
                                foreach (var tag in mp3Stream.GetAllTags())
                                {
                                    // Title
                                    if (tag.Title.IsAssigned) Title = RemoveInvalidCharsAndTrim(tag.Title);

                                    // Album
                                    if (tag.Album.IsAssigned) Album = RemoveInvalidCharsAndTrim(tag.Album);

                                    // Artist
                                    if (tag.Artists.IsAssigned) Artists = RemoveInvalidCharsAndTrim(tag.Artists);


                                    // We take the first tag with name we find
                                    if (tag.Title.IsAssigned) break;
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Could not read id3 tag from file: " + file);
                Console.WriteLine(ex.ToString());
            }



            // Build the title
            StringBuilder builder = new StringBuilder();

            // Title
            builder.Append(Title);

            // Album
            if (!string.IsNullOrEmpty(Album)) builder.AppendFormat(" - {0}", Album);

            // Artist
            if (!string.IsNullOrEmpty(Artists)) builder.AppendFormat(" - {0}", Artists);

            Name = builder.ToString();
        }

        /// <summary>
        /// Removes all invalid chars
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static string RemoveInvalidCharsAndTrim(string text)
        {
            // Remove NULL-chars
            text = text.Replace("\0", "");

            return text.Trim();
        }
    }
}
