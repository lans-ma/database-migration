namespace Kata.Db.Console
{
    using Microsoft.Extensions.Logging;
    using NLog;
    using NLog.Extensions.Logging;
    using System;

    internal class ConsoleLoggerFactory : ILoggerFactory
    {
        private readonly NLogLoggerProvider _nlogLoggerProvider;

        public ConsoleLoggerFactory()
        {
            _nlogLoggerProvider = new NLogLoggerProvider();
        }

        public void AddProvider(ILoggerProvider provider)
        {
            // NLog integration does not require additional providers
            throw new NotSupportedException("Adding providers is not supported.");
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name)
        {
            return _nlogLoggerProvider.CreateLogger(name);
        }

        public void Dispose()
        {
            _nlogLoggerProvider.Dispose();
            LogManager.Shutdown();
        }
    }
}