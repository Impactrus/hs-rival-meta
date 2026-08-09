#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CCG.Editor
{
    [InitializeOnLoad]
    public static class ErrorLogger
    {
        private static readonly string logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "CCG_ConsoleLogs.txt");

        static ErrorLogger()
        {
            // Register log callback
            Application.logMessageReceived += HandleLog;
            
            // Clear log on editor launch / compile
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }

        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            // Only write Warnings, Errors, and Exceptions to keep file small
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Warning)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(logFilePath, true))
                    {
                        writer.WriteLine($"[{System.DateTime.Now:HH:mm:ss}] [{type}] {logString}");
                        if (type == LogType.Exception || type == LogType.Error)
                        {
                            writer.WriteLine(stackTrace);
                        }
                        writer.WriteLine(new string('-', 50));
                    }
                }
                catch
                {
                    // Fail silently to not cause loop errors
                }
            }
        }
    }
}
#endif
