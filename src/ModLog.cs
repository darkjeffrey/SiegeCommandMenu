using System;
using System.IO;
using TaleWorlds.Library;

namespace SiegeCommandMenu
{
    /// <summary>
    /// Two small log files in the mod's folder (Modules\SiegeCommandMenu): errors are always
    /// written to error_log.txt, and debug_log.txt is only written when "Write debug log" is on.
    /// </summary>
    internal static class ModLog
    {
        private static string PathOf(string file)
        {
            return Path.Combine(BasePath.Name, "Modules", "SiegeCommandMenu", file);
        }

        /// <summary>True when the debug log option is on.</summary>
        public static bool DebugEnabled => Options.DebugLog;

        /// <summary>Appends a line to debug_log.txt (opened and closed each time, so it survives a crash).</summary>
        public static void Debug(Func<string> message)
        {
            if (!DebugEnabled)
                return;

            Append("debug_log.txt", message());
        }

        public static void Error(string where, Exception e)
        {
            Append("error_log.txt", where + "\r\n" + e + "\r\n");
        }

        private static void Append(string file, string text)
        {
            try
            {
                File.AppendAllText(PathOf(file), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + text + "\r\n");
            }
            catch (Exception)
            {
                // Logging must never break the game.
            }
        }
    }
}
