using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace DataConnector.Test.Utils
{
    public class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly ConcurrentDictionary<string, XUnitLogger> _loggers = new();

        public XUnitLoggerProvider(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new XUnitLogger(_outputHelper, name));
        }

        public void Dispose() => _loggers.Clear();
    }

    public class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly string _categoryName;

        public XUnitLogger(ITestOutputHelper outputHelper, string categoryName)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter != null)
            {
                var message = formatter(state, exception);
                _outputHelper.WriteLine($"[{logLevel}] {_categoryName}: {message}");
            }
        }
    }
}