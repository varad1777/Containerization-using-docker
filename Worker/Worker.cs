using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.AverageCalculator
{
    public class AverageCalculatorWorker : BackgroundService
    {
        private readonly ILogger<AverageCalculatorWorker> _logger;
        private readonly IConfiguration _config;
        private IConnection? _connection;
        private IModel? _channel;
        private string _requestsQueue = "avg_requests";
        private string _resultsQueue = "avg_results";

        public AverageCalculatorWorker(ILogger<AverageCalculatorWorker> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMq:Host"] ?? "localhost",
                UserName = _config["RabbitMq:User"] ?? "guest",
                Password = _config["RabbitMq:Password"] ?? "guest",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _requestsQueue = _config["RabbitMq:RequestsQueue"] ?? "avg_requests";
            _resultsQueue = _config["RabbitMq:ResultsQueue"] ?? "avg_results";

            _channel.QueueDeclare(_requestsQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(_resultsQueue, durable: true, exclusive: false, autoDelete: false);
            _channel.BasicQos(0, 1, false);

            _logger.LogInformation("✅ Worker connected to RabbitMQ, listening on: {Queue}", _requestsQueue);
            return base.StartAsync(cancellationToken);
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                _logger.LogError("❌ RabbitMQ channel not initialized.");
                return; // plain return in async Task
            }

            // Wait 3 seconds before starting consumer (actually awaited)
            //try
            //{
            //    await Task.Delay(3000, stoppingToken).ConfigureAwait(false);
            //}
            //catch (OperationCanceledException)
            //{
            //    // canceled during startup delay
            //    _logger.LogInformation("Startup delay canceled.");
            //    return;
            //}

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                try
                {

                    _logger.LogInformation("🕐 Simulating slow processing...");
                    await Task.Delay(800, stoppingToken).ConfigureAwait(false);
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var request = JsonSerializer.Deserialize<AvgRequestMessage>(json);

                    if (request == null)
                    {
                        _logger.LogWarning("⚠️ Received null request");
                        _channel.BasicAck(ea.DeliveryTag, false);
                        return;
                    }

                    _logger.LogInformation("📥 Processing RequestId={RequestId}, AssetId={AssetId}, Column={Column}, User={User}",
                        request.RequestId, request.AssetId, request.ColumnName, request.UserId);

                    double? average = null;
                    string? error = null;

                    try
                    {
                        average = await CalculateAverageAsync(request.AssetId, request.ColumnName, stoppingToken);
                        if (average == null)
                            average = 0.0;
                    }
                    catch (Exception exCalc)
                    {
                        _logger.LogError(exCalc, "❌ Error calculating average for RequestId={RequestId}", request.RequestId);
                        error = exCalc.Message;
                        average = 0.0;
                    }

                    var result = new AvgResultMessage
                    {
                        RequestId = request.RequestId,
                        AssetId = request.AssetId,
                        ColumnName = request.ColumnName,
                        Average = average.Value,
                        UserId = request.UserId,
                        UserName = request.UserName,
                        CompletedAtUtc = DateTime.UtcNow,
                        Error = error
                    };

                    var resultJson = JsonSerializer.Serialize(result);
                    var resultBody = Encoding.UTF8.GetBytes(resultJson);
                    var props = _channel.CreateBasicProperties();
                    props.Persistent = true;

                    _channel.BasicPublish("", _resultsQueue, props, resultBody);

                    _logger.LogInformation("✅ Result sent: RequestId={RequestId}, Avg={Average}, User={User}",
                        request.RequestId, average, request.UserId);

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to process message - NACK without requeue");
                    try { _channel.BasicNack(ea.DeliveryTag, false, false); } catch { /* best effort */ }
                }
            };

            _channel.BasicConsume(_requestsQueue, autoAck: false, consumer: consumer);
            _logger.LogInformation("👂 Listening for calculation requests...");

            // Keep ExecuteAsync alive until cancellation. This avoids returning immediately.
            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown, just exit
            }

            // Method ends here — no Task.CompletedTask returned (async Task so just return)
        }



        private async Task<double?> CalculateAverageAsync(Guid assetId, string columnName, CancellationToken cancellationToken)
        {
            var connString = _config.GetConnectionString("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(connString);

            using var db = new AppDbContext(optionsBuilder.Options);

            if (columnName.Equals("Strength", StringComparison.OrdinalIgnoreCase))
            {
                var avg = await db.Signals
                    .AsNoTracking()
                    .Where(s => s.AssetId == assetId)
                    .Select(s => (double?)s.Strength)
                    .AverageAsync(cancellationToken);

                return avg ?? 0.0;
            }

            throw new NotSupportedException($"Column '{columnName}' is not supported for average calculation.");
        }

        public override void Dispose()
        {
            try { _channel?.Close(); } catch { }
            try { _connection?.Close(); } catch { }
            base.Dispose();
        }
    }

    public class AvgRequestMessage
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public Guid AssetId { get; set; }
        public string ColumnName { get; set; } = default!;
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class AvgResultMessage
    {
        public string RequestId { get; set; } = default!;
        public Guid AssetId { get; set; }
        public string ColumnName { get; set; } = default!;
        public double Average { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public string? Error { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Signal> Signals { get; set; }
    }

    public class Signal
    {
        public int Id { get; set; }
        public Guid AssetId { get; set; }
        public int Strength { get; set; }
    }
}
