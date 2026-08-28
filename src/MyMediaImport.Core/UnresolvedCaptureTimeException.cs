namespace MyMediaImport.Core;

public sealed class UnresolvedCaptureTimeException : InvalidOperationException
{
    public UnresolvedCaptureTimeException(string message)
        : base(message)
    {
    }
}
