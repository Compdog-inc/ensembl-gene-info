using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    internal static class Logger
    {
        public enum LogLevel:int
        {
            Debug = 0,
            Trace = 1,
            Info = 2,
            Warn = 3,
            Error = 4
        }

        public static LogLevel MinLevel { get; set; } = LogLevel.Trace;

        public static void Debug(string message)
        {
            if (MinLevel > LogLevel.Debug) return;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[DEBUG] {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Trace(string message)
        {
            if (MinLevel > LogLevel.Trace) return;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[TRACE] {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Info(string message)
        {
            if (MinLevel > LogLevel.Info) return;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"[INFO] {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Warn(string message)
        {
            if (MinLevel > LogLevel.Warn) return;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($"[WARN] {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void Error(string message)
        {
            if (MinLevel > LogLevel.Error) return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
