using System.Runtime.InteropServices;

namespace DiscordMusicPlayer
{
    /// <summary>
    /// DS 2018-06-27: A static helper class for the windows api.
    /// </summary>
    internal static class Win32
    {
        /// <summary>
        /// The console exit types
        /// </summary>
        public enum ConsoleCtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        /// <summary>
        /// Sets the console exit handler
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="add"></param>
        /// <returns></returns>
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);

        /// <summary>
        /// The delegation for the console exit event
        /// </summary>
        /// <param name="ctrlType"></param>
        /// <returns></returns>
        public delegate bool ConsoleCtrlHandler(ConsoleCtrlTypes ctrlType);
    }
}
