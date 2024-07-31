using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    internal static class Logger
    {
        public enum LogLevel : int
        {
            Debug = 0,
            Trace = 1,
            Info = 2,
            Warn = 3,
            Error = 4
        }

        public static LogLevel MinLevel { get; set; } = LogLevel.Trace;

        private static Mutex mutex = new Mutex();
        private static Stopwatch sw = Stopwatch.StartNew();

        private static string FormatTimestamp()
        {
            return sw.Elapsed.ToString("c");
        }

        [DebuggerStepThrough]
        public static void Debug(string message)
        {
            if (MinLevel > LogLevel.Debug) return;
            if (mutex.WaitOne())
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"{FormatTimestamp()} [DEBUG] {message}");
                Console.ForegroundColor = ConsoleColor.White;
                mutex.ReleaseMutex();
            }
        }

        [DebuggerStepThrough]
        public static void Trace(string message)
        {
            if (MinLevel > LogLevel.Trace) return;
            if (mutex.WaitOne())
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"{FormatTimestamp()} [TRACE] {message}");
                Console.ForegroundColor = ConsoleColor.White;
                mutex.ReleaseMutex();
            }
        }

        [DebuggerStepThrough]
        public static void Info(string message)
        {
            if (MinLevel > LogLevel.Info) return;
            if (mutex.WaitOne())
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"{FormatTimestamp()} [INFO] {message}");
                Console.ForegroundColor = ConsoleColor.White;
                mutex.ReleaseMutex();
            }
        }

        [DebuggerStepThrough]
        public static void Warn(string message)
        {
            if (MinLevel > LogLevel.Warn) return;
            if (mutex.WaitOne())
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"{FormatTimestamp()} [WARN] {message}");
                Console.ForegroundColor = ConsoleColor.White;
                mutex.ReleaseMutex();
            }
        }

        [DebuggerStepThrough]
        public static void Error(string message)
        {
            if (MinLevel > LogLevel.Error) return;
            if (mutex.WaitOne())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{FormatTimestamp()} [ERROR] {message}");
                Console.ForegroundColor = ConsoleColor.White;
                mutex.ReleaseMutex();
            }
        }
    }
}
