using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MyMediaImport.App;

internal sealed class SingleInstanceService : IDisposable
{
    private const int ActivationAttemptCount = 40;
    private const int ActivationRetryDelayMilliseconds = 50;
    private const int RestoreWindowCommand = 9;

    private readonly string _mutexName;
    private Mutex? _mutex;
    private bool _ownsMutex;
    private bool _isDisposed;

    public SingleInstanceService(string mutexName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mutexName);
        _mutexName = mutexName;
    }

    public bool TryAcquire()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (_mutex is not null)
        {
            throw new InvalidOperationException(
                "Single-instance ownership has already been checked.");
        }

        _mutex = new Mutex(true, _mutexName, out bool createdNew);
        _ownsMutex = createdNew;
        return createdNew;
    }

    public void ActivateExistingInstance()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        using Process currentProcess = Process.GetCurrentProcess();
        for (int attempt = 0; attempt < ActivationAttemptCount; attempt++)
        {
            nint windowHandle = FindExistingMainWindow(currentProcess);
            if (windowHandle != IntPtr.Zero)
            {
                if (IsIconic(windowHandle))
                {
                    ShowWindowAsync(windowHandle, RestoreWindowCommand);
                }

                SetForegroundWindow(windowHandle);
                return;
            }

            Thread.Sleep(ActivationRetryDelayMilliseconds);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_ownsMutex && _mutex is not null)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
        }

        _ownsMutex = false;
        _mutex?.Dispose();
        _mutex = null;
        _isDisposed = true;
    }

    private static nint FindExistingMainWindow(Process currentProcess)
    {
        Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
        try
        {
            foreach (Process process in processes)
            {
                try
                {
                    if (process.Id == currentProcess.Id
                        || process.SessionId != currentProcess.SessionId)
                    {
                        continue;
                    }

                    nint windowHandle = process.MainWindowHandle;
                    if (windowHandle != IntPtr.Zero)
                    {
                        return windowHandle;
                    }
                }
                catch (Exception exception) when (
                    exception is InvalidOperationException
                    or Win32Exception
                    or NotSupportedException)
                {
                }
            }

            return IntPtr.Zero;
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint windowHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(nint windowHandle, int command);
}
