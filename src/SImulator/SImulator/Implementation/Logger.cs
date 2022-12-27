using SImulator.ViewModel.PlatformSpecific;
using System.IO;

namespace SImulator.Implementation;

internal sealed class Logger : ILogger
{
    private StreamWriter _writer;

    private Logger()
    {

    }

    public static Logger Create(string? filename)
    {
        var logger = new Logger();

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
