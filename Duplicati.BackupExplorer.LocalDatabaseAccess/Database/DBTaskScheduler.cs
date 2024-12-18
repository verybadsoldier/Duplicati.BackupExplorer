using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duplicati.BackupExplorer.LocalDatabaseAccess.Database
{
    public class DBTaskScheduler(CancellationToken cancellationToken) : TaskScheduler
    {
        [ThreadStatic]
        private static bool _isExecuting;
        private readonly CancellationToken _cancellationToken = cancellationToken;

        private readonly BlockingCollection<Task> _taskQueue = [];

        public void Start()
        {
            new Thread(Run) { Name = "DB Thread" }.Start();
        }

        private void Run()
        {
            _isExecuting = true;
            try
            {
                foreach (var task in _taskQueue.GetConsumingEnumerable(_cancellationToken))
                {
                    TryExecuteTask(task);
                }
            }
            catch (OperationCanceledException) { }
            finally { _isExecuting = false; }
        }

        public void Complete() { _taskQueue.CompleteAdding(); }
        public Task Run(Action action)
        {
            return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, this);
        }

        protected override IEnumerable<Task>? GetScheduledTasks() { return null; }
        protected override void QueueTask(Task task)
        {
            try
            {
                _taskQueue.Add(task, _cancellationToken);
            }
            catch (OperationCanceledException) { }
        }
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (taskWasPreviouslyQueued) return false;
            // Check if it is on the right thread
            return _isExecuting && TryExecuteTask(task);
        }
    }
}
