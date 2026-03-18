using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BacklogManager.Services
{
    /// <summary>
    /// Service de queue d'écriture centralisée pour éviter les conflits multi-utilisateurs
    /// Toutes les écritures passent par une queue et sont exécutées séquentiellement
    /// </summary>
    public class DatabaseWriteQueue
    {
        private static readonly Lazy<DatabaseWriteQueue> _instance = new Lazy<DatabaseWriteQueue>(() => new DatabaseWriteQueue());
        public static DatabaseWriteQueue Instance => _instance.Value;

        private readonly ConcurrentQueue<WriteOperation> _queue;
        private readonly SemaphoreSlim _signal;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _processorTask;
        private bool _isRunning;

        private DatabaseWriteQueue()
        {
            _queue = new ConcurrentQueue<WriteOperation>();
            _signal = new SemaphoreSlim(0);
            _cancellationTokenSource = new CancellationTokenSource();
            _isRunning = true;
            
            // Démarrer le thread de traitement
            _processorTask = Task.Run(ProcessQueueAsync);
            
            LoggingService.Instance.LogInfo("DatabaseWriteQueue initialisée - Mode séquentiel activé");
        }

        /// <summary>
        /// Ajoute une opération d'écriture à la queue
        /// </summary>
        public Task<T> EnqueueWriteAsync<T>(Func<T> writeOperation, string operationName = "WriteOperation")
        {
            if (!_isRunning)
            {
                throw new InvalidOperationException("DatabaseWriteQueue est arrêtée");
            }

            var operation = new WriteOperation<T>(writeOperation, operationName);
            _queue.Enqueue(operation);
            _signal.Release(); // Signaler qu'une nouvelle opération est disponible
            
            return operation.CompletionSource.Task;
        }

        /// <summary>
        /// Ajoute une opération d'écriture void à la queue
        /// </summary>
        public Task EnqueueWriteAsync(Action writeOperation, string operationName = "WriteOperation")
        {
            return EnqueueWriteAsync<object>(() =>
            {
                writeOperation();
                return null;
            }, operationName);
        }

        /// <summary>
        /// Traite les opérations de la queue de manière séquentielle
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Attendre qu'une opération soit disponible
                    await _signal.WaitAsync(_cancellationTokenSource.Token);

                    // Traiter toutes les opérations disponibles
                    while (_queue.TryDequeue(out WriteOperation operation))
                    {
                        try
                        {
                            operation.Execute();
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.LogError($"Erreur lors de l'exécution de {operation.Name}", ex);
                            operation.SetException(ex);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal lors de l'arrêt
                    break;
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.LogError("Erreur dans ProcessQueueAsync", ex);
                }
            }

            LoggingService.Instance.LogInfo("DatabaseWriteQueue arrêtée");
        }

        /// <summary>
        /// Arrête la queue proprement
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();
            
            // Traiter les opérations restantes
            while (_queue.TryDequeue(out WriteOperation operation))
            {
                try
                {
                    operation.Execute();
                }
                catch (Exception ex)
                {
                    operation.SetException(ex);
                }
            }
        }

        /// <summary>
        /// Nombre d'opérations en attente
        /// </summary>
        public int PendingOperationsCount => _queue.Count;

        #region Nested Classes

        private abstract class WriteOperation
        {
            public string Name { get; }

            protected WriteOperation(string name)
            {
                Name = name;
            }

            public abstract void Execute();
            public abstract void SetException(Exception ex);
        }

        private class WriteOperation<T> : WriteOperation
        {
            private readonly Func<T> _operation;
            public TaskCompletionSource<T> CompletionSource { get; }

            public WriteOperation(Func<T> operation, string name) : base(name)
            {
                _operation = operation;
                // RunContinuationsAsynchronously empêche le deadlock quand un appelant
                // bloque le thread UI avec .GetAwaiter().GetResult()
                CompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public override void Execute()
            {
                try
                {
                    T result = _operation();
                    CompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    CompletionSource.SetException(ex);
                    throw;
                }
            }

            public override void SetException(Exception ex)
            {
                CompletionSource.SetException(ex);
            }
        }

        #endregion
    }
}
