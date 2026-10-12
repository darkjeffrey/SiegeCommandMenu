using System;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace SiegeCommandMenu
{
    /// <summary>
    /// Two small log files in the mod's folder (Modules\SiegeCommandMenu): errors are always
    /// written to error_log.txt, and debug_log.txt is written when "Write debug log" is on.
    /// With "Print debug info in game" on, debug lines also appear in the message feed.
    /// Files are opened and closed for every line, so they survive a native crash.
    /// </summary>
    internal static class ModLog
    {
        private static string PathOf(string file)
        {
            return Path.Combine(BasePath.Name, "Modules", "SiegeCommandMenu", file);
        }

        /// <summary>True when any debug output is on.</summary>
        public static bool DebugEnabled => Options.DebugLog || Options.DebugMessages;

        /// <summary>Writes a debug line. The text is only built when debug output is on.</summary>
        public static void Debug(Func<string> message)
        {
            if (!DebugEnabled)
                return;

            string text;
            try
            {
                text = message();
            }
            catch (Exception e)
            {
                text = "(debug text failed: " + e.Message + ")";
            }

            if (Options.DebugLog)
                Append("debug_log.txt", text);

            if (Options.DebugMessages)
                Show(text);
        }

        public static void Error(string where, Exception e)
        {
            Append("error_log.txt", where + "\r\n" + e + "\r\n");
            if (Options.DebugMessages)
                Show("ERROR in " + where + ": " + e.Message);
        }

        private static void Show(string text)
        {
            try
            {
                InformationManager.DisplayMessage(new InformationMessage("[SCM] " + text));
            }
            catch (Exception)
            {
            }
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
