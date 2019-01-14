using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace DiscordMusicPlayer
{
    /// <summary>
    /// Ds 2017-06-25: The settings class
    /// </summary>
    internal class Settings
    {
        #region Properties

        /// <summary>
        /// Gets the token
        /// </summary>
        public string Token { get; private set; }

        /// <summary>
        /// Gets the token type
        /// </summary>
        public TokenType TokenType { get; private set; }

        /// <summary>
        /// Gets the guild name or id
        /// </summary>
        public string Guild { get; private set; }

        /// <summary>
        /// Gets the channel name or id
        /// </summary>
        public string Channel { get; private set; }

        /// <summary>
        /// Gets if the playlist is shuffled
        /// </summary>
        public bool Shuffle { get; private set; }

        /// <summary>
        /// Gets if the playlist should start when the bot joins the channel
        /// </summary>
        public bool Autoplay { get; private set; }

        /// <summary>
        /// Gets the music directories
        /// </summary>
        public string[] Directories { get; private set; }

        /// <summary>
        /// Gets the allowed user ids
        /// </summary>
        public string[] AllowedUsers { get; private set; }

        /// <summary>
        /// Gets the start volume
        /// </summary>
        public float Volume { get; private set; }

        #endregion Properties

        #region Load

        /// <summary>
        /// Opens the settings file
        /// </summary>
        /// <param name="file"></param>
        public Settings(string file)
        {
            // Default values
            Directories = new string[0];
            AllowedUsers = new string[0];
            TokenType = TokenType.Bot;
            Volume = 1f;
            Autoplay = true;

            // Load the file
            Load(file);
        }

        /// <summary>
        /// Loads the settings from file
        /// </summary>
        /// <param name="file"></param>
        private void Load(string file)
        {
            try
            {
                if (!File.Exists(file))
                {
                    Logger.Log("Settings", "Settings file '{0}' was not found!", file);
                    return;
                }

                // Log
                Logger.Log("Settings", "Reading the config file...");

                // Creates the file stream
                using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    // The xml reader
                    using (XmlReader reader = XmlReader.Create(fileStream))
                    {
                        while(reader.Read())
                        {
                            // Start element
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                // Read the token
                                if (reader.Name == "Config")
                                {
                                    ReadConfig(reader);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                // Log error
                Logger.Log("Settings", "Error while reading the settings file!");
                Logger.Log("Settings", ex.ToString());
            }
        }


        /// <summary>
        /// Read the config node
        /// </summary>
        /// <param name="reader"></param>
        private void ReadConfig(XmlReader reader)
        {
            while (reader.Read())
            {
                // Start element
                if (reader.NodeType == XmlNodeType.Element)
                {
                    // Read the token
                    if (reader.Name == "Token")
                    {
                        Token = reader.ReadElementContentAsString();
                    }

                    // Read the token type
                    if (reader.Name == "TokenType")
                    {
                        string text = reader.ReadElementContentAsString();

                        TokenType tokenType;
                        if (Enum.TryParse(text, out tokenType))
                        {
                            TokenType = tokenType;
                        }
                    }

                    // Read the guild name
                    if (reader.Name == "Guild")
                    {
                        Guild = reader.ReadElementContentAsString();
                    }

                    // Read the channel name
                    if (reader.Name == "Channel")
                    {
                        Channel = reader.ReadElementContentAsString();
                    }

                    // Read the shuffle
                    if (reader.Name == "Shuffle")
                    {
                        Shuffle = reader.ReadElementContentAsBoolean();
                    }

                    // Read the autoplay
                    if (reader.Name == "Autoplay")
                    {
                        Autoplay = reader.ReadElementContentAsBoolean();
                    }

                    // Read the volume
                    if (reader.Name == "Volume")
                    {
                        Volume = reader.ReadElementContentAsFloat() / 100f;
                        if (Volume > 1f) Volume = 1f;
                        if (Volume < 0f) Volume = 0f;
                    }

                    // Read a single directory
                    if (reader.Name == "Directory")
                    {
                        Directories = new string[] { reader.ReadElementContentAsString() };
                    }

                    // Read the music directories
                    if (reader.Name == "Directories")
                    {
                        Directories = ReadArray(reader, "Directory").ToArray();
                    }

                    // Read the allowed users
                    if (reader.Name == "AllowedUsers")
                    {
                        AllowedUsers = ReadArray(reader, "AllowedUser").ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Reads an array
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="itemName"></param>
        /// <returns></returns>
        private IEnumerable<string> ReadArray(XmlReader reader, string itemName)
        {
            while (reader.Read())
            {
                int level = 0;

                // Start element
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (!reader.IsEmptyElement)
                        level++;

                    if (level == 1)
                    {
                        if (reader.Name == itemName)
                        {
                            string value = reader.ReadElementContentAsString();

                            if (!string.IsNullOrEmpty(value))
                            {
                                yield return value;
                            }
                        }
                    }
                }

                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    level--;
                    if (level < 0)
                        break;
                }
            }
        }

        #endregion Load
    }
}
