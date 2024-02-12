using SImulator.ViewModel.PlatformSpecific;
using System.IO;

namespace SImulator.Implementation;

internal sealed class GameLogger : IGameLogger
{
    private StreamWriter? _writer;

    private GameLogger()
    {

    }

    public static GameLogger Create(string? filename)
    {
        var logger = new GameLogger();

        if (filename != null)
        {
            logger._writer = new StreamWriter(filename) { AutoFlush = true };
        }

        return logger;
    }

    public void Write(string message, params object?[] args)
    {
        if (_writer == null)
        {
            return;
        }

        _writer.WriteLine(message, args);
    }

    public void Dispose()
    {
        if (_writer != null)
        {
            _writer.Dispose();
        }
    }
}
