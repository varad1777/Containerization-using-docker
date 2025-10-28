using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyApp.Application.DTOs;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.Queues;
using MyApp.Infrastructure.RTC;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace MyApp.Infrastructure.Services
{
    public class AverageCalculationBackgroundService : BackgroundService
    {
        private readonly CalculationQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<AverageCalculationBackgroundService> _logger;

        public AverageCalculationBackgroundService(
            CalculationQueue queue,
            IServiceScopeFactory scopeFactory,
            IHubContext<NotificationHub> hubContext,
            ILogger<AverageCalculationBackgroundService> logger
           )
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("AverageCalculationBackgroundService started.");
            _logger.LogInformation("AverageCalculationBackgroundService started.");

            // NOTE: The CalculationQueue used here must expose:
            //  - Task WaitForItemAsync(CancellationToken ct)
            //  - bool TryDequeue(out EnqueueRequest request)
            //
            // If your current CalculationQueue is still the simple `ConcurrentQueue<EnqueueRequest>`
            // (i.e. public class CalculationQueue : ConcurrentQueue<EnqueueRequest> { })
            // replace it with the wrapper we discussed earlier that adds signaling (SemaphoreSlim).
            //
            // The original (older) polling approach you had:
            // if (_queue.TryDequeue(out EnqueueRequest queueData)) { ... }
            // else { await Task.Delay(500, stoppingToken); }
            //
            // is kept below as comments in places where it was previously used.

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    
                    
                    // Wait efficiently until an item is available or cancellation requested.
                    // Previously you used:
                    //    if (_queue.TryDequeue(out EnqueueRequest queueData)) { ... } else { await Task.Delay(500, stoppingToken); }
                    // The new pattern waits for a signal from the producer and avoids busy polling.
                    await _queue.WaitForItemAsync(stoppingToken).ConfigureAwait(false);

                    // Drain loop — process all currently available items without blocking
                    while (_queue.TryDequeue(out EnqueueRequest queueData))
                    {
                        Console.WriteLine("i am background service and i am running");
                        await Task.Delay(500, stoppingToken).ConfigureAwait(false);
                        Console.WriteLine($"Dequeued column name: {queueData.ColumnName}");
                        Console.WriteLine($"Dequeued user id: {queueData.userId}");
                        Console.WriteLine($"Dequeued request: AssetId={queueData.AssetId} Column={queueData.ColumnName} User={queueData.userId}",
                            queueData.AssetId, queueData.ColumnName, queueData.userName);

                        // simulate a small processing delay if you still want it (optional)
                        // await Task.Delay(1000, stoppingToken);

                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        double? average = null;
                        string? error = null;

                        try
                        {
                            // only Strength supported
                            if (queueData.ColumnName.Equals("Strength", StringComparison.OrdinalIgnoreCase))
                            {
                                // make Average robust when there are no rows by using nullable cast and default
                                average = await db.Signals
                                    .Where(x => x.AssetId == queueData.AssetId)
                                    .Select(x => (double?)x.Strength)
                                    .AverageAsync(stoppingToken)
                                    .ConfigureAwait(false);

                                // AverageAsync on empty sequence returns InvalidOperationException for non-nullable,
                                // but we used nullable cast so it returns null if no elements.
                                if (average == null)
                                    average = 0.0;

                                Console.WriteLine($"Calculated average Strength: {average}");
                                Console.WriteLine($"Calculated average Strength: Average for AssetId={queueData.AssetId}",
                                    average, queueData.AssetId);
                            }
                            else
                            {
                                error = $"Column '{queueData.ColumnName}' not supported for average calculation.";
                                Console.WriteLine(error);
                                

                                // Optionally create a notification for unsupported column and continue
                                // continue;
                            }

                            // Create notification and user notification entries and save
                            var notification = new Notification
                            {
                                Message = $"The Average for column {queueData.ColumnName} is {average}",
                                CreatedBy = queueData.userName,
                                CreatedAt = DateTime.UtcNow
                            };

                            db.Notifications.Add(notification);
                            await db.SaveChangesAsync(stoppingToken).ConfigureAwait(false);

                            var userNotification = new UserNotification
                            {
                                UserId = queueData.userId,
                                NotificationId = notification.Id,
                                IsRead = false,
                                // set other fields if your entity requires them
                            };

                            db.UserNotifications.Add(userNotification);
                            await db.SaveChangesAsync(stoppingToken).ConfigureAwait(false);

                            // Send via SignalR to that user only
                            if (!string.IsNullOrEmpty(queueData.userId))
                            {
                                // Keep your existing invocation shape
                                await _hubContext.Clients.User(queueData.userId).SendAsync(
                                    "ReceiveNotification",
                                    notification.Id,
                                    notification.CreatedBy,
                                    notification.Message,
                                    notification.CreatedAt,
                                    cancellationToken: stoppingToken
                                ).ConfigureAwait(false);
                            }
                            else
                            {
                                _logger.LogWarning("queueData.userId was null or empty for AssetId={AssetId}", queueData.AssetId);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Propagate cancellation and exit loops gracefully
                            throw;
                        }
                        catch (Exception ex)
                        {
                            // Keep the original behavior of catching and delaying, but also log the exception.
                            _logger.LogError(ex, "Error processing queue item for AssetId={AssetId} Column={Column}",
                                queueData.AssetId, queueData.ColumnName);

                            // Optional: persist failed request to a dead-letter table, increment retry counter, etc.

                            // small backoff to avoid tight retry loops for transient errors
                            try
                            {
                                await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { throw; }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // graceful stop requested
                    break;
                }
                catch (Exception ex)
                {
                    // If WaitForItemAsync or any other unexpected error occurred, log and delay slightly
                    _logger.LogError(ex, "Unexpected error in AverageCalculationBackgroundService main loop.");
                    try
                    {
                        await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            Console.WriteLine("AverageCalculationBackgroundService stopped.");
            _logger.LogInformation("AverageCalculationBackgroundService stopped.");
        }
    }
}
