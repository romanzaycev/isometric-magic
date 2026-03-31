using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace IsometricMagic.Engine.Diagnostics
{
    public static class FatalErrorReporter
    {
        public const int ExitCodeDialogWithLog = 10;
        public const int ExitCodeDialogWithoutLog = 11;
        public const int ExitCodeConsoleWithLog = 12;
        public const int ExitCodeConsoleWithoutLog = 13;

        private static int _shown;

        public static int Report(Exception? exception, string? errorLogPath)
        {
            var message = BuildMessage(exception, errorLogPath);
            var hasLogPath = !string.IsNullOrWhiteSpace(errorLogPath);

            if (Interlocked.CompareExchange(ref _shown, 1, 0) == 0)
            {
                var shown = TryShowDialog("Isometric Magic - Fatal Error", message);
                if (!shown)
                {
                    Console.Error.WriteLine(message);
                }

                return shown
                    ? (hasLogPath ? ExitCodeDialogWithLog : ExitCodeDialogWithoutLog)
                    : (hasLogPath ? ExitCodeConsoleWithLog : ExitCodeConsoleWithoutLog);
            }

            Console.Error.WriteLine(message);
            return hasLogPath ? ExitCodeConsoleWithLog : ExitCodeConsoleWithoutLog;
        }

        public static int ReportNonException(object? exceptionObject, string? errorLogPath)
        {
            var exceptionText = exceptionObject?.ToString() ?? "<null>";
            var wrapped = new InvalidOperationException(
                $"Unhandled non-exception object: {exceptionText}");
            return Report(wrapped, errorLogPath);
        }

        private static string BuildMessage(Exception? exception, string? errorLogPath)
        {
            var builder = new StringBuilder();
            builder.AppendLine("The game has encountered a fatal error and needs to close.");
            builder.AppendLine();
            builder.AppendLine($"Log file: {GetLogPathDisplay(errorLogPath)}");
            builder.AppendLine();
            builder.AppendLine("Exception:");
            builder.AppendLine(FormatException(exception));
            return builder.ToString();
        }

        private static string GetLogPathDisplay(string? errorLogPath)
        {
            return string.IsNullOrWhiteSpace(errorLogPath) ? "unavailable" : errorLogPath;
        }

        private static string FormatException(Exception? exception)
        {
            if (exception is null)
            {
                return "<unknown exception>";
            }

#if DEBUG
            return exception.ToString();
#else
            var builder = new StringBuilder();
            var current = exception;
            var depth = 0;

            while (current is not null)
            {
                var prefix = depth == 0 ? string.Empty : $"Inner[{depth}]: ";
                builder.AppendLine($"{prefix}{current.GetType().FullName}: {current.Message}");
                current = current.InnerException;
                depth++;
            }

            return builder.ToString().TrimEnd();
#endif
        }

        private static bool TryShowDialog(string title, string message)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    return TryShowWindowsMessageBox(title, message);
                }

                if (OperatingSystem.IsLinux())
                {
                    return TryShowLinuxMessageBox(title, message);
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool TryShowWindowsMessageBox(string title, string message)
        {
            const uint mbOk = 0x00000000;
            const uint mbIconError = 0x00000010;
            const uint mbSetForeground = 0x00010000;

            var result = MessageBoxW(IntPtr.Zero, message, title, mbOk | mbIconError | mbSetForeground);
            return result != 0;
        }

        private static bool TryShowLinuxMessageBox(string title, string message)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY")))
            {
                return false;
            }

            return TryRunGuiTool("zenity", "--error", "--title", title, "--text", message)
                   || TryRunGuiTool("kdialog", "--title", title, "--error", message)
                   || TryRunGuiTool("xmessage", "-center", "-title", title, message);
        }

        private static bool TryRunGuiTool(string fileName, params string[] args)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                foreach (var arg in args)
                {
                    process.StartInfo.ArgumentList.Add(arg);
                }

                var started = process.Start();
                if (!started)
                {
                    return false;
                }

                process.WaitForExit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
    }
}
