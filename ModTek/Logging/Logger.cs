using System;
using System.Diagnostics;
using System.IO;
using ModTek.Misc;

namespace ModTek.Logging
{
    internal static class Logger
    {
        private static readonly object lockObject = new();

        private static StreamWriter stream;
        internal static void LogInit()
        {
            lock (lockObject)
            {
                if (stream == null)
                {
                    Directory.CreateDirectory(FilePaths.TempModTekDirectory);
                    stream = File.CreateText(FilePaths.LogPath);
                    stream.AutoFlush = true;
                }
            }
        }

        internal static void LogIf(bool condition, string message)
        {
            if (condition)
            {
                Log(message);
            }
        }

        internal static void LogIf(bool condition, object obj)
        {
            if (condition)
            {
                Log($"Method {GetFullMethodName()} called with {obj}");
            }
        }

        internal static void LogIfSlow(Stopwatch sw, string id = null)
        {
            if (sw.ElapsedMilliseconds < 500)
            {
                return;
            }

            id ??= "Method " + GetFullMethodName();
            LogWithDate($"{id} took {sw.Elapsed}");
        }

        private static string GetFullMethodName()
        {
            var frame = new StackTrace().GetFrame(2);
            var method = frame.GetMethod();
            var methodName = method.Name;
            var className = method.ReflectedType?.FullName;
            var fullMethodName = className + "." + methodName;
            return fullMethodName;
        }

        internal static void LogWithDate(string message)
        {
            Log(DateTime.Now.ToLongTimeString() + " - " + message);
        }

        internal static void Log(string message = null, Exception e = null)
        {
            lock (lockObject)
            {
                message ??= "Method " + GetFullMethodName() + " called";
                stream.WriteLine(message);
                if (e != null)
                {
                    stream.WriteLine(e.ToString());
                }
            }
        }
    }
}
