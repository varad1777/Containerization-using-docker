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
        private readonly ConnectionFactory _factory;
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

            _factory = new ConnectionFactory
            {
                HostName = _config["RabbitMq:Host"] ?? "localhost",
                UserName = _config["RabbitMq:User"] ?? "guest",
                Password = _config["RabbitMq:Password"] ?? "guest",
                DispatchConsumersAsync = true
            };

            _resultsQueue = _config["RabbitMq:ResultsQueue"] ?? "avg_results";
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Use synchronous create for simplicity and immediate exception if RabbitMQ unavailable.
                _connection = _factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(queue: _resultsQueue, durable: true, exclusive: false, autoDelete: false);
                _channel.BasicQos(0, 1, false);

                _logger.LogInformation("AverageResultListener started and listening to {Queue}", _resultsQueue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create RabbitMQ connection/channel in StartAsync");
                throw;
            }

            await base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                throw new InvalidOperationException("RabbitMQ channel is not initialized. Make sure StartAsync completed successfully.");
            }

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
                            result.RequestId,        // id (you can also use notification.Id if you want DB id)
                            result.UserId,           // createdBy
                            messageText,             // message
                            result.CompletedAtUtc,   // createdAt
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
                    try { _channel.BasicNack(ea.DeliveryTag, false, false); } catch { }
                }
            };



            // Start consuming (non-blocking). Keep consumerTag so we can cancel on shutdown.
            _consumerTag = _channel.BasicConsume(queue: _resultsQueue, autoAck: false, consumer: consumer);

            // Register cancellation to close channel/connection gracefully.
            stoppingToken.Register(() =>
            {
                try
                {
                    if (_channel != null && !string.IsNullOrEmpty(_consumerTag))
                    {
                        _channel.BasicCancel(_consumerTag);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while cancelling RabbitMQ consumer during shutdown.");
                }
            });

            // Consumer callbacks run on the background — returning a completed task is fine here.
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }
            base.Dispose();
        }
    }

    // make sure this model exists somewhere accessible (or replace with your actual type)

}
