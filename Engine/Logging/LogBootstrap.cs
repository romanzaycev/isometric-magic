using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace IsometricMagic.Engine.Logging
{
    public static class LogBootstrap
    {
        public static string? CurrentErrorLogPath { get; private set; }

        public static void Initialize(AppConfig config)
        {
            CurrentErrorLogPath = null;

            if (!config.LoggingEnabled)
            {
                LogManager.Configuration = null;
                return;
            }

            var runStamp = DateTime.Now.ToString(config.LoggingDateFormat);
            var nlogConfig = new LoggingConfiguration();

            if (config.LoggingAllEnabled)
            {
                var path = ResolvePath(config.LoggingAllPath, runStamp, "all");
                var target = CreateFileTarget("all_file", path, config.LoggingLayout);
                nlogConfig.AddTarget(target);
                nlogConfig.AddRule(LogLevel.Trace, LogLevel.Fatal, target);
            }

            if (config.LoggingWarnEnabled)
            {
                var path = ResolvePath(config.LoggingWarnPath, runStamp, "warn");
                var target = CreateFileTarget("warn_file", path, config.LoggingLayout);
                nlogConfig.AddTarget(target);
                nlogConfig.AddRule(LogLevel.Warn, LogLevel.Fatal, target);
            }

            if (config.LoggingErrorEnabled)
            {
                var path = ResolvePath(config.LoggingErrorPath, runStamp, "error");
                CurrentErrorLogPath = Path.GetFullPath(path);
                var target = CreateFileTarget("error_file", path, config.LoggingLayout);
                nlogConfig.AddTarget(target);
                nlogConfig.AddRule(LogLevel.Error, LogLevel.Fatal, target);
            }

            LogManager.Configuration = nlogConfig;
        }

        public static void Shutdown()
        {
            LogManager.Shutdown();
            CurrentErrorLogPath = null;
        }

        private static FileTarget CreateFileTarget(string targetName, string filePath, string layout)
        {
            EnsureDirectory(filePath);

            return new FileTarget(targetName)
            {
                FileName = filePath,
                Layout = layout,
                KeepFileOpen = false,
                ConcurrentWrites = true,
                Encoding = System.Text.Encoding.UTF8
            };
        }

        private static string ResolvePath(string template, string runStamp, string level)
        {
            var withDate = template.Replace("{date}", runStamp, StringComparison.OrdinalIgnoreCase);
            return withDate.Replace("{level}", level, StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureDirectory(string filePath)
        {
            var fullPath = Path.GetFullPath(filePath);
            var directory = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
