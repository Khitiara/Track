using System.Runtime.InteropServices;

namespace Track.Ties.Threading;

public sealed partial class CoroutineManager : SynchronizationContext
{
    private readonly Time _time;

    private class CoroutineTaskScheduler : TaskScheduler
    {
        private readonly CoroutineManager   _manager;
        private          SendOrPostCallback _callback;

        private void SPostCallback(object? s) {
            if (s is Task task)
                TryExecuteTask(task); // with double-execute check because SC could be buggy
        }

        public CoroutineTaskScheduler(CoroutineManager manager) {
            _manager = manager;
            _callback = SPostCallback;
        }

        protected override IEnumerable<Task>? GetScheduledTasks() {
            return null;
        }
        
        

        protected override void QueueTask(Task task) {
            _manager.Post(_callback, task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
            if (SynchronizationContext.Current == _manager && _manager.IsOnContextThread) {
                return TryExecuteTask(task);
            }

            return false;
        }
    }

    internal struct QueuedOperation
    {
        public          float                 WaitTimer;
        public readonly ManualResetEventSlim? WaitHandle;
        public readonly SendOrPostCallback    Callback;
        public readonly object?               State;

        public QueuedOperation(SendOrPostCallback callback, object? state, float waitTimer = 0f,
            ManualResetEventSlim? waitHandle = null) {
            State = state;
            Callback = callback;
            WaitTimer = waitTimer;
            WaitHandle = waitHandle;
        }
    }

    private readonly object                _lock               = new();
    private          List<QueuedOperation> _queue              = new();
    private          List<QueuedOperation> _operations         = new();
    private          List<QueuedOperation> _shouldRunNextFrame = new();
    private          int                   _uiThreadId;

    public CoroutineManager(Time time) {
        _time = time;
        Scheduler = new CoroutineTaskScheduler(this);
    }


    public void Initialize() {
        _uiThreadId = Environment.CurrentManagedThreadId;
        SetSynchronizationContext(this);
        Current = this;
    }

    public void Update() {
        // Swap old and new queue;
        lock (_lock) {
            (_operations, _queue) = (_queue, _operations);
        }

        // Force span to be taken off stack here to avoid memory issues
        {
            Span<QueuedOperation> span = CollectionsMarshal.AsSpan(_operations);
            for (int index = 0; index < span.Length; index++) {
                ref QueuedOperation queuedOperation = ref span[index];
                if (queuedOperation.WaitTimer > 0) {
                    queuedOperation.WaitTimer -= _time.DeltaTime;
                    _shouldRunNextFrame.Add(queuedOperation);
                    continue;
                }

                queuedOperation.Callback(queuedOperation.State);
                queuedOperation.WaitHandle?.Set();
            }
        }

        (_operations, _shouldRunNextFrame) = (_shouldRunNextFrame, _operations);
        _shouldRunNextFrame.Clear();
    }

    internal void Enqueue(QueuedOperation operation) {
        lock (_lock) {
            _queue.Add(operation);
        }
    }

    public new static CoroutineManager? Current { get; private set; }

    public TaskScheduler Scheduler { get; }

    public bool IsOnContextThread => Environment.CurrentManagedThreadId == _uiThreadId;

    public static bool IsOnUiThread => Current?.IsOnContextThread ?? false;

    public override void Post(SendOrPostCallback d, object? state) {
        Enqueue(new QueuedOperation(d, state));
    }

    public override void Send(SendOrPostCallback d, object? state) {
        if (IsOnContextThread) {
            d(state);
        } else {
            ManualResetEventSlim waitHandle = new();
            Enqueue(new QueuedOperation(d, state, waitHandle: waitHandle));
            waitHandle.Wait();
        }

        base.Send(d, state);
    }
}