using System;
using System.Globalization;

namespace DiscordMusicPlayer
{
    /// <summary>
    /// DS 2017-06-25: The internal logger
    /// </summary>
    internal class Logger
    {
        #region Singelton

        /// <summary>
        /// Gets the current instance
        /// </summary>
        private static Logger m_Instance;

        /// <summary>
        /// Gets the current logger instance
        /// </summary>
        public static Logger Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new Logger();

                return m_Instance;
            }
        }

        /// <summary>
        /// Private constructor
        /// </summary>
        private Logger()
        { }

        #endregion Singelton

        #region Log

        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        public void LogMessage(string tag, string message)
        {
            Console.WriteLine("[{0}] {1}", tag, message);
        }

        #endregion Log

        #region Static

        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        public static void Log(string tag, string message)
        {
            Instance.LogMessage(tag, message);
        }

        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Log(string tag, string message, params object[] args)
        {
            Log(tag, string.Format(CultureInfo.InvariantCulture, message, args));
        }

        #endregion Static
    }
}
