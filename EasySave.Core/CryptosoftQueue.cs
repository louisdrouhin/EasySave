namespace EasySave.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Manages a single-instance queue for CryptoSoft operations.
/// Ensures only one CryptoSoft process runs at a time using FIFO ordering.
/// </summary>
public class CryptosoftQueue
{
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly Queue<CryptosoftOperation> _queue = new Queue<CryptosoftOperation>();
    private readonly object _queueLock = new object();
    private readonly TimeSpan _operationTimeout = TimeSpan.FromMinutes(5);
    private volatile bool _isRunning = false;

    public struct CryptosoftOperation
    {
        public string Operation { get; set; }
        public string SourceFile { get; set; }
        public string Password { get; set; }
        public string TargetDirectory { get; set; }
        public string ErrorLogType { get; set; }
        public TaskCompletionSource<long> ResultTaskSource { get; set; }
    }

    /// <summary>
    /// Enqueues a CryptoSoft operation and waits for its execution.
    /// </summary>
    public async Task<long> EnqueueOperationAsync(
        string operation,
        string sourceFile,
        string password,
        string targetDirectory,
        string errorLogType,
        string cryptosoftPath)
    {
        var tcs = new TaskCompletionSource<long>();
        var cryptoOp = new CryptosoftOperation
        {
            Operation = operation,
            SourceFile = sourceFile,
            Password = password,
            TargetDirectory = targetDirectory,
            ErrorLogType = errorLogType,
            ResultTaskSource = tcs
        };

        lock (_queueLock)
        {
            _queue.Enqueue(cryptoOp);
        }

        // Start processing if not already running
        if (!_isRunning)
        {
            _ = ProcessQueueAsync(cryptosoftPath);
        }

        // Wait for this operation's result with timeout
        try
        {
            var task = tcs.Task;
            if (await Task.WhenAny(task, Task.Delay(_operationTimeout)) == task)
            {
                return await task;
            }
            else
            {
                tcs.TrySetResult(-3); // Timeout error code
                return -3;
            }
        }
        catch (Exception)
        {
            return -999; // General error
        }
    }

    /// <summary>
    /// Processes the queue sequentially, one operation at a time.
    /// </summary>
    private async Task ProcessQueueAsync(string cryptosoftPath)
    {
        _isRunning = true;

        try
        {
            while (true)
            {
                CryptosoftOperation operation;

                lock (_queueLock)
                {
                    if (_queue.Count == 0)
                    {
                        _isRunning = false;
                        break;
                    }

                    operation = _queue.Dequeue();
                }

                // Acquire semaphore to ensure only one CryptoSoft runs at a time
                await _semaphore.WaitAsync();
                try
                {
                    long result = ExecuteCryptosoftCommand(
                        operation.Operation,
                        operation.SourceFile,
                        operation.Password,
                        operation.TargetDirectory,
                        operation.ErrorLogType,
                        cryptosoftPath
                    );

                    operation.ResultTaskSource.TrySetResult(result);
                }
                finally
                {
                    _semaphore.Release();
                }

                // Small delay between operations to prevent resource contention
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            _isRunning = false;
            // If there are pending operations, mark them as failed
            lock (_queueLock)
            {
                while (_queue.Count > 0)
                {
                    var pendingOp = _queue.Dequeue();
                    pendingOp.ResultTaskSource.TrySetResult(-999);
                }
            }
        }
    }

    /// <summary>
    /// Executes a CryptoSoft command synchronously.
    /// </summary>
    private long ExecuteCryptosoftCommand(
        string operation,
        string sourceFile,
        string password,
        string targetDirectory,
        string errorLogType,
        string cryptosoftPath)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (!File.Exists(cryptosoftPath))
            {
                return -1;
            }

            var arguments = $"{operation} \"{sourceFile}\" \"{password}\" \"{targetDirectory}\"";

            var processInfo = new ProcessStartInfo
            {
                FileName = cryptosoftPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                {
                    return -2;
                }

                // Wait for exit with timeout
                if (!process.WaitForExit((int)_operationTimeout.TotalMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch { }
                    return -3; // Timeout
                }

                stopwatch.Stop();

                // Return the elapsed time on success, or negative exit code on failure
                if (process.ExitCode != 0)
                {
                    return -Math.Abs(process.ExitCode);
                }

                return stopwatch.ElapsedMilliseconds;
            }
        }
        catch (Exception)
        {
            return -999;
        }
    }

    /// <summary>
    /// Gets the current queue size (for monitoring purposes).
    /// </summary>
    public int GetQueueSize()
    {
        lock (_queueLock)
        {
            return _queue.Count;
        }
    }

    /// <summary>
    /// Checks if a CryptoSoft operation is currently running.
    /// </summary>
    public bool IsOperationRunning()
    {
        return _isRunning || _semaphore.CurrentCount == 0;
    }
}
