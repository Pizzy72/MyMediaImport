namespace MyMediaImport.Core;

public sealed class CaptureTimeResolutionException : InvalidOperationException
{
    public CaptureTimeResolutionException(string message)
        : base(message)
    {
    }
}
