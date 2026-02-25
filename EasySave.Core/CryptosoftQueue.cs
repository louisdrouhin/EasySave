namespace EasySave.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// Manages FIFO queue for CryptoSoft operations
// Ensures only one instance runs at a time (semaphore + 5 min timeout)
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

    // Adds CryptoSoft operation to queue and waits for execution
    // @param operation - operation type (encrypt/decrypt)
    // @param sourceFile - source file path
    // @param password - password for operation
    // @param targetDirectory - destination folder
    // @param errorLogType - error type to log if issue
    // @param cryptosoftPath - path to CryptoSoft executable
    // @returns elapsed time in ms if success, negative error code otherwise
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

        if (!_isRunning)
        {
            _ = ProcessQueueAsync(cryptosoftPath);
        }

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

    // Processes queue sequentially, one operation at a time
    // @param cryptosoftPath - path to CryptoSoft executable
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

                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            _isRunning = false;
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

    // Executes CryptoSoft command synchronously
    // Launches executable with parameters and waits for result with timeout
    // @param operation - operation type
    // @param sourceFile - source file
    // @param password - password
    // @param targetDirectory - target folder
    // @param errorLogType - error type to log
    // @param cryptosoftPath - path to executable
    // @returns elapsed time in ms or error code (-1 missing file, -2 process fail, -3 timeout, -999 general error)
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

    // Gets current queue size
    // @returns number of pending operations
    public int GetQueueSize()
    {
        lock (_queueLock)
        {
            return _queue.Count;
        }
    }

    // Checks if CryptoSoft operation is currently running
    // @returns true if operation is running, false otherwise
    public bool IsOperationRunning()
    {
        return _isRunning || _semaphore.CurrentCount == 0;
    }
}
