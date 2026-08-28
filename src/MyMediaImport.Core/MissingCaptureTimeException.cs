namespace MyMediaImport.Core;

public sealed class MissingCaptureTimeException : InvalidOperationException
{
    public MissingCaptureTimeException(string message)
        : base(message)
    {
    }
}
