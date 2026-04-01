using NLog;

namespace IsometricMagic.Engine.Diagnostics
{
    public interface IEngineLogger
    {
        void Trace(string message);
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Error(Exception exception, string message);
        void Fatal(string message);
        void Fatal(Exception exception, string message);
    }

    public static class Log
    {
        public static IEngineLogger For<T>()
        {
            return new EngineLogger(LogManager.GetLogger(typeof(T).FullName ?? typeof(T).Name));
        }

        public static IEngineLogger Get(string name)
        {
            return new EngineLogger(LogManager.GetLogger(name));
        }

        private sealed class EngineLogger : IEngineLogger
        {
            private readonly Logger _logger;

            public EngineLogger(Logger logger)
            {
                _logger = logger;
            }

            public void Trace(string message) => _logger.Trace(message);
            public void Debug(string message) => _logger.Debug(message);
            public void Info(string message) => _logger.Info(message);
            public void Warn(string message) => _logger.Warn(message);
            public void Error(string message) => _logger.Error(message);
            public void Error(Exception exception, string message) => _logger.Error(exception, message);
            public void Fatal(string message) => _logger.Fatal(message);
            public void Fatal(Exception exception, string message) => _logger.Fatal(exception, message);
        }
    }
}
