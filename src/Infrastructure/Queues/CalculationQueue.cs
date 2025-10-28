using MyApp.Application.DTOs;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Infrastructure.Queues
{
    // NOTE: this uses ConcurrentQueue internally but exposes a controlled API.
    public class CalculationQueue : IDisposable
    {
        private readonly ConcurrentQueue<EnqueueRequest> _queue = new();
        private readonly SemaphoreSlim _itemsAvailable = new(0); // signals consumer(s)
        private readonly SemaphoreSlim? _capacitySemaphore;      // optional capacity/backpressure

        public CalculationQueue(int? capacity = null)
        {
            if (capacity.HasValue)
            {
                // initialize with capacity slots (producers wait when full)
                _capacitySemaphore = new SemaphoreSlim(capacity.Value, capacity.Value);
            }
        }

        /// <summary>
        /// Enqueue an item. If capacity was configured and the queue is full,
        /// this will wait until a slot frees up (honors cancellation).
        /// </summary>
        public async Task EnqueueAsync(EnqueueRequest item, CancellationToken cancellationToken = default)
        {
            if (_capacitySemaphore != null)
            {

                Console.WriteLine("=====================================");
                Console.WriteLine($"[Queue] Before Wait: CurrentCount={_capacitySemaphore.CurrentCount} (ThreadId={Environment.CurrentManagedThreadId})");
                var sw = Stopwatch.StartNew();
                await _capacitySemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                sw.Stop();
                Console.WriteLine($"[Queue] After Wait: Waited={sw.ElapsedMilliseconds}ms CurrentCount={_capacitySemaphore.CurrentCount} (ThreadId={Environment.CurrentManagedThreadId})");
                Console.WriteLine("=====================================");
            }

            _queue.Enqueue(item);
            _itemsAvailable.Release(); // signal consumer(s) that an item is available, that means telling consumer that the item is 
            // there in the queue 
        }

        /// <summary>
        /// Consumer waits for an item to be available (cancellable).
        /// </summary>
        public Task WaitForItemAsync(CancellationToken cancellationToken = default) =>
            _itemsAvailable.WaitAsync(cancellationToken);

        /// <summary>
        /// Try to dequeue an item. Returns false if queue empty.
        /// If the queue was bounded, we release a slot for producers.
        /// </summary>
        public bool TryDequeue(out EnqueueRequest? item)
        {
            if (_queue.TryDequeue(out item))
            {
                _capacitySemaphore?.Release(); // free a capacity slot
                return true;
            }

            item = null;
            return false;
        }

        public int Count => _queue.Count;

        public void Dispose()
        {
            _itemsAvailable?.Dispose();
            _capacitySemaphore?.Dispose();
        }
    }
}
