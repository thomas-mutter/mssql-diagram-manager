using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Mutter.Tools.SqlServer.DiagramManager.Tests;

public class XunitLogger<T>
    : IDisposable, ILogger<T> where T : class
{
    public static ITestOutputHelper? Output { get; private set; }

    public static void Register(ITestOutputHelper testOutput) => Output = testOutput;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => this;

    public void Dispose()
    {
        // nothing to dispose
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string message = formatter(state, exception);
        Output?.WriteLine(message);
    }
}
