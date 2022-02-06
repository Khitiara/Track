using System.Runtime.CompilerServices;

namespace Track.Ballast.Threading;

public sealed partial class CoroutineManager
{
    /// <summary>Creates an awaitable that asynchronously yields back to the game context when awaited.</summary>
    /// <returns>
    /// A context that, when awaited, will asynchronously transition back into the game context at the
    /// time of the await. 
    /// </returns>
    public CoroutineYieldOrDelayAwaitable Yield() => Delay(0);

    /// <summary>
    /// Creates an awaitable that asynchronously yields back to the game context after a time delay when awaited.
    /// </summary>
    /// <param name="delay">The time span to wait before completing the returned awaitable</param>
    /// <returns>, when awaited, will asynchronously transition back into the game context after the specified delay at the
    /// time of the await. </returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The <paramref name="delay"/> is less than -1 or greater than the maximum allowed timer duration.
    /// </exception>
    public CoroutineYieldOrDelayAwaitable Delay(TimeSpan delay) => Delay((float)delay.TotalSeconds);

    /// <summary>
    /// Creates an awaitable that asynchronously yields back to the game context after a time delay when awaited.
    /// </summary>
    /// <param name="delay">The time span to wait before completing the returned awaitable</param>
    /// <returns>, when awaited, will asynchronously transition back into the game context after the specified delay at the
    /// time of the await. </returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The <paramref name="delay"/> is less than -1 or greater than the maximum allowed timer duration.
    /// </exception>
    public CoroutineYieldOrDelayAwaitable Delay(float delay) => delay < -1
        ? throw new ArgumentOutOfRangeException(nameof(delay))
        : new CoroutineYieldOrDelayAwaitable(this, delay);

    /// <summary>Provides an awaitable context for switching into a game main-loop environment after an optional delay</summary>
    /// <remarks>The delay will be evaluated using the game's <see cref="Time"/> instance, rather than a thread, and is aware of game timescale</remarks>
    /// <remarks>This type is intended for compiler use only.</remarks>
    public readonly struct CoroutineYieldOrDelayAwaitable
    {
        private readonly CoroutineManager _manager;
        private readonly float            _delay;

        public CoroutineYieldOrDelayAwaitable(CoroutineManager manager, float delay) {
            _manager = manager;
            _delay = delay;
        }

        public DelayAwaiter GetAwaiter() => new(_manager, _delay);

        public readonly struct DelayAwaiter : ICriticalNotifyCompletion
        {
            /// <summary>Gets whether a yield is not required.</summary>
            /// <remarks>This property is intended for compiler user rather than use directly in code.</remarks>
            public bool IsCompleted => false; // yielding is always required for YieldAwaiter, hence false

            private readonly CoroutineManager   _manager;
            private readonly SendOrPostCallback _callback;
            private readonly float              _delay;

            public DelayAwaiter(CoroutineManager manager, float delay) {
                _manager = manager;
                _callback = RunAction;
                _delay = delay;
            }

            private static void RunAction(object? state) {
                ((Action)state!)();
            }

            public void OnCompleted(Action continuation) {
                _manager.Enqueue(new QueuedOperation(_callback, continuation, _delay));
            }

            public void UnsafeOnCompleted(Action continuation) {
                _manager.Enqueue(new QueuedOperation(_callback, continuation, _delay));
            }

            /// <summary>Ends the await operation.</summary>
            public void GetResult() { } // Nop. It exists purely because the compiler pattern demands it.
        }
    }
}