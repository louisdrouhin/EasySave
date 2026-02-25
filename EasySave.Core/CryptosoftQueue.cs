namespace EasySave.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// Gère une queue FIFO pour les opérations CryptoSoft
// S'assure qu'une seule instance s'exécute à la fois (semaphore + timeout de 5 min)
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

    // Ajoute une opération CryptoSoft à la queue et attend son exécution
    // @param operation - type d'opération (encrypt/decrypt)
    // @param sourceFile - chemin du fichier source
    // @param password - mot de passe pour l'opération
    // @param targetDirectory - dossier de destination
    // @param errorLogType - type d'erreur à logger si problème
    // @param cryptosoftPath - chemin de l'exécutable CryptoSoft
    // @returns temps écoulé en ms si succès, code erreur négatif sinon
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

    // Traite la queue séquentiellement, une opération à la fois
    // @param cryptosoftPath - chemin de l'exécutable CryptoSoft
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

    // Exécute une commande CryptoSoft de manière synchrone
    // Lance l'exécutable avec les paramètres et attend le résultat avec timeout
    // @param operation - type d'opération
    // @param sourceFile - fichier source
    // @param password - mot de passe
    // @param targetDirectory - dossier cible
    // @param errorLogType - type d'erreur à logger
    // @param cryptosoftPath - chemin de l'exécutable
    // @returns temps écoulé en ms ou code erreur (-1 fichier manquant, -2 processus échec, -3 timeout, -999 erreur générale)
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

    // Récupère la taille actuelle de la queue
    // @returns nombre d'opérations en attente
    public int GetQueueSize()
    {
        lock (_queueLock)
        {
            return _queue.Count;
        }
    }

    // Vérifie si une opération CryptoSoft est actuellement en cours
    // @returns true si une opération s'exécute, false sinon
    public bool IsOperationRunning()
    {
        return _isRunning || _semaphore.CurrentCount == 0;
    }
}
