using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Trollito.Common.Utilities
{
    public static class Logger
    {
        public static void Log(string message, LogLevel level = LogLevel.Information, ConsoleColor? color = null)
        {
            if (GameNetwork.IsServer)
            {
                LogToConsole(message, level, color);
            }
            else
            {
                LogToGame(message, level, color);
            }
        }

        private static void LogToConsole(string message, LogLevel level, ConsoleColor? color)
        {
            var consoleColor = color ?? GetConsoleColorForLogLevel(level);
            Debug.Print(message, 0, (Debug.DebugColor)consoleColor);
        }

        private static void LogToGame(string message, LogLevel level, ConsoleColor? color)
        {
            var infoColor = color ?? GetColorForLogLevel(level);
            var gameColor = infoColor.ToGameColor();
            InformationManager.DisplayMessage(new InformationMessage(message, gameColor));
        }

        private static ConsoleColor GetColorForLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Information:
                    return ConsoleColor.White;
                case LogLevel.Warning:
                    return ConsoleColor.Yellow;
                case LogLevel.Error:
                    return ConsoleColor.Red;
                default:
                    throw new ArgumentException("Invalid log level specified.");
            }
        }

        private static ConsoleColor GetConsoleColorForLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Information:
                    return ConsoleColor.Gray;
                case LogLevel.Warning:
                    return ConsoleColor.DarkYellow;
                case LogLevel.Error:
                    return ConsoleColor.DarkRed;
                default:
                    throw new ArgumentException("Invalid log level specified.");
            }
        }
    }

    public enum LogLevel
    {
        Information,
        Warning,
        Error
    }

    public static class ColorExtensions
    {
        public static Color ToGameColor(this ConsoleColor consoleColor)
        {
            switch (consoleColor)
            {
                case ConsoleColor.Black:
                    return new Color(0f, 0f, 0f);
                case ConsoleColor.DarkBlue:
                    return new Color(0f, 0f, 0.5f);
                case ConsoleColor.DarkGreen:
                    return new Color(0f, 0.5f, 0f);
                case ConsoleColor.DarkCyan:
                    return new Color(0f, 0.5f, 0.5f);
                case ConsoleColor.DarkRed:
                    return new Color(0.5f, 0f, 0f);
                case ConsoleColor.DarkMagenta:
                    return new Color(0.5f, 0f, 0.5f);
                case ConsoleColor.DarkYellow:
                    return new Color(0.5f, 0.5f, 0f);
                case ConsoleColor.Gray:
                    return new Color(0.75f, 0.75f, 0.75f);
                case ConsoleColor.DarkGray:
                    return new Color(0.5f, 0.5f, 0.5f);
                case ConsoleColor.Blue:
                    return new Color(0f, 0f, 1f);
                case ConsoleColor.Green:
                    return new Color(0f, 1f, 0f);
                case ConsoleColor.Cyan:
                    return new Color(0f, 1f, 1f);
                case ConsoleColor.Red:
                    return new Color(1f, 0f, 0f);
                case ConsoleColor.Magenta:
                    return new Color(1f, 0f, 1f);
                case ConsoleColor.Yellow:
                    return new Color(1f, 1f, 0f);
                case ConsoleColor.White:
                    return new Color(1f, 1f, 1f);
                default:
                    throw new ArgumentException("Invalid ConsoleColor specified.");
            }
        }
    }
}
