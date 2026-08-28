using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace MyMediaImport.Windows;

internal sealed class WpdReadStream(IStream stream, WpdSession session) : Stream
{
    private IStream? _stream = stream;
    private WpdSession? _session = session;

    public override bool CanRead => _stream is not null;
    public override bool CanSeek => _stream is not null;
    public override bool CanWrite => false;

    public override long Length
    {
        get
        {
            EnsureNotDisposed().Stat(out STATSTG statistics, 1);
            return statistics.cbSize;
        }
    }

    public override long Position
    {
        get => Seek(0, SeekOrigin.Current);
        set => Seek(value, SeekOrigin.Begin);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (offset > buffer.Length - count)
        {
            throw new ArgumentException("Offset and count exceed the buffer length.");
        }

        byte[] target = offset == 0 ? buffer : new byte[count];
        nint bytesReadPointer = Marshal.AllocCoTaskMem(sizeof(int));
        try
        {
            EnsureNotDisposed().Read(target, count, bytesReadPointer);
            int bytesRead = Marshal.ReadInt32(bytesReadPointer);
            if (offset != 0)
            {
                Array.Copy(target, 0, buffer, offset, bytesRead);
            }

            return bytesRead;
        }
        finally
        {
            Marshal.FreeCoTaskMem(bytesReadPointer);
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        nint newPositionPointer = Marshal.AllocCoTaskMem(sizeof(long));
        try
        {
            EnsureNotDisposed().Seek(offset, (int)origin, newPositionPointer);
            return Marshal.ReadInt64(newPositionPointer);
        }
        finally
        {
            Marshal.FreeCoTaskMem(newPositionPointer);
        }
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            WpdSession.ReleaseComObject(_stream);
            _stream = null;
            _session?.Dispose();
            _session = null;
        }

        base.Dispose(disposing);
    }

    private IStream EnsureNotDisposed() =>
        _stream ?? throw new ObjectDisposedException(nameof(WpdReadStream));
}
