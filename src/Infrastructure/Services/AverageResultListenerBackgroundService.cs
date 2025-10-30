using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyApp.Application.DTOs.MyApp.Messaging;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.RTC;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Infrastructure.Services
{
    public class AverageResultListenerBackgroundService : BackgroundService
    {
        private readonly IConfiguration _config;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<AverageResultListenerBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private IConnection? _connection;
        private IModel? _channel;
        private ConnectionFactory? _factory;
        private readonly string _resultsQueue;
        private string? _consumerTag;

        public AverageResultListenerBackgroundService(
            IConfiguration config,
            IHubContext<NotificationHub> hubContext,
            IServiceProvider serviceProvider,
            ILogger<AverageResultListenerBackgroundService> logger)
        {
            _config = config;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Use docker-friendly default "rabbitmq"
            _factory = new ConnectionFactory
            {
                HostName = _config["RabbitMq:Host"] ?? _config["RabbitMq__Host"] ?? "rabbitmq",
                UserName = _config["RabbitMq:User"] ?? _config["RabbitMq__User"] ?? "guest",
                Password = _config["RabbitMq:Password"] ?? _config["RabbitMq__Password"] ?? "guest",
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _resultsQueue = _config["RabbitMq:ResultsQueue"] ?? _config["RabbitMq__ResultsQueue"] ?? "avg_results";
        }

        // Keep StartAsync lightweight and non-throwing
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AverageResultListenerBackgroundService starting. RabbitMQ host: {Host}, queue: {Queue}",
                _factory?.HostName, _resultsQueue);
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_factory == null)
            {
                _logger.LogError("ConnectionFactory not initialized. Exiting listener.");
                return;
            }

            int attempt = 0;
            TimeSpan delay = TimeSpan.FromSeconds(5);
            const int maxAttemptsBeforeLongerBackoff = 6;

            // Retry loop: keep attempting to connect until stopped
            while (!stoppingToken.IsCancellationRequested)
            {
                attempt++;
                try
                {
                    _logger.LogInformation("Attempting to connect to RabbitMQ (attempt {Attempt})...", attempt);
                    _connection = _factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    // Idempotent queue declaration
                    _channel.QueueDeclare(queue: _resultsQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
                    _channel.BasicQos(0, 1, false);

                    _logger.LogInformation("Connected to RabbitMQ on host {Host} and declared queue {Queue}.", _factory.HostName, _resultsQueue);

                    // Start consumer
                    var consumer = new AsyncEventingBasicConsumer(_channel);
                    consumer.Received += async (sender, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var json = Encoding.UTF8.GetString(body);
                            _logger.LogInformation("📨 Received message from queue {Queue}: {Json}", _resultsQueue, json);

                            var result = JsonSerializer.Deserialize<AvgResultMessage>(json);

                            if (result == null)
                            {
                                _logger.LogWarning("⚠️ Failed to deserialize message: {Json}", json);
                                _channel.BasicAck(ea.DeliveryTag, false);
                                return;
                            }

                            _logger.LogInformation("✅ Parsed AvgResultMessage: RequestId={RequestId}, UserId={UserId}, Column={Column}, Avg={Average}",
                                result.RequestId, result.UserId, result.ColumnName, result.Average);

                            // --- Save to DB ---
                            using var scope = _serviceProvider.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                            var notification = new Notification
                            {
                                Message = $"The Average for column {result.ColumnName} is {result.Average}",
                                CreatedBy = result.UserId ?? "system",
                                CreatedAt = DateTime.UtcNow
                            };

                            db.Notifications.Add(notification);
                            await db.SaveChangesAsync(stoppingToken);

                            if (!string.IsNullOrEmpty(result.UserId))
                            {
                                var userNotification = new UserNotification
                                {
                                    UserId = result.UserId,
                                    NotificationId = notification.Id,
                                    IsRead = false
                                };

                                db.UserNotifications.Add(userNotification);
                                await db.SaveChangesAsync(stoppingToken);

                                _logger.LogInformation("💾 Notification saved for user {UserId}", result.UserId);
                            }

                            // --- Send via SignalR ---
                            if (!string.IsNullOrEmpty(result.UserId))
                            {
                                var messageText = $"The Average for column {result.ColumnName} is {result.Average}";
                                _logger.LogInformation("📤 Sending notification to user {UserId} via SignalR", result.UserId);

                                await _hubContext.Clients.User(result.UserId).SendAsync(
                                   "ReceiveNotification",
                                    result.RequestId,
                                    result.UserId,
                                    messageText,
                                    result.CompletedAtUtc,
                                    cancellationToken: stoppingToken
                                );

                                _logger.LogInformation("✅ SignalR notification sent to user {UserId}", result.UserId);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ Skipped SignalR send — missing UserId for RequestId={RequestId}", result.RequestId);
                            }

                            _channel.BasicAck(ea.DeliveryTag, false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Error processing avg result message. NACKing (no requeue).");
                            try { _channel?.BasicNack(ea.DeliveryTag, false, false); } catch { }
                        }
                    };

                    // Start consuming and store consumer tag
                    _consumerTag = _channel.BasicConsume(queue: _resultsQueue, autoAck: false, consumer: consumer);

                    // Connected and consumer running — break retry loop
                    break;
                }
                catch (BrokerUnreachableException brEx)
                {
                    _logger.LogWarning(brEx, "RabbitMQ unreachable on attempt {Attempt}. Will retry after {Delay}.", attempt, delay);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _logger.LogError(ex, "Unexpected error while connecting to RabbitMQ (attempt {Attempt}). Will retry after {Delay}.", attempt, delay);
                }

                // Wait with cancellation support
                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Stopping token requested while waiting to retry RabbitMQ connection.");
                    return;
                }

                // Backoff growth
                if (attempt % maxAttemptsBeforeLongerBackoff == 0)
                    delay = TimeSpan.FromSeconds(30);
                else
                    delay = delay + TimeSpan.FromSeconds(5);
            }

            // Keep the service alive until cancellation; consumer callbacks handle messages
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        public override void Dispose()
        {
            try { if (!string.IsNullOrEmpty(_consumerTag) && _channel != null) _channel.BasicCancel(_consumerTag); } catch { }
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }
            base.Dispose();
        }
    }
}
