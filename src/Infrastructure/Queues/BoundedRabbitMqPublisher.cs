using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyApp.Application.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Infrastructure.Queues
{
    public class BoundedRabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<BoundedRabbitMqPublisher> _logger;
        private readonly string _requestsQueue;
        private readonly string _resultsQueue;
        private readonly int _maxQueueLength;
        private readonly TimeSpan _publisherWaitTimeout;

        public BoundedRabbitMqPublisher(IConfiguration config, ILogger<BoundedRabbitMqPublisher> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = config["RabbitMq:Host"] ?? "localhost",
                UserName = config["RabbitMq:User"] ?? "guest",
                Password = config["RabbitMq:Password"] ?? "guest"
            };

            int attempt = 0;
            const int maxAttempts = 10;

            while (true)
            {
                try
                {
                    attempt++;
                    _logger.LogInformation("🐇 Attempting to connect to RabbitMQ (attempt {Attempt})...", attempt);

                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    break; // success
                }catch(BrokerUnreachableException ex)
                {
                    if (attempt >= maxAttempts)
                    {
                        _logger.LogCritical(ex, "❌ RabbitMQ not reachable after {MaxAttempts} attempts. Giving up.", maxAttempts);
                        throw; // let app fail — RabbitMQ might be essential
                    }

                    _logger.LogWarning(ex, "⚠️ RabbitMQ unreachable (attempt {Attempt}). Retrying in 5 seconds...", attempt);
                    Thread.Sleep(5000);
                }
            }
          

            _requestsQueue = config["RabbitMq:RequestsQueue"] ?? "avg_requests";
            _resultsQueue = config["RabbitMq:ResultsQueue"] ?? "avg_results";

            // Read bounds from config (fall back to defaults)
            if (!int.TryParse(config["RabbitMq:MaxQueueLength"], out _maxQueueLength))
                _maxQueueLength = 2;

            if (!int.TryParse(config["RabbitMq:PublisherWaitTimeoutSeconds"], out var secs))
                secs = 1;
            _publisherWaitTimeout = TimeSpan.FromSeconds(secs);

            DeclareQueue(_requestsQueue);
            DeclareQueue(_resultsQueue);

            _logger.LogInformation("✅ RabbitMQ connected. Queues declared: {RequestsQueue}, {ResultsQueue} (maxLen={MaxLen})",
                _requestsQueue, _resultsQueue, _maxQueueLength);
        }

        private void DeclareQueue(string queueName)
        {
            _channel.QueueDeclare(
                queue: queueName,
                durable: true, // if true queue service will restart
                exclusive: false, // queue will use only by one connection and deleted when that connection closes
                autoDelete: false, // if true queue is deleted when last consumer unsubscribes
                arguments: null // no extra option like max length n all 
            );
        }

        /// <summary>
        /// Publish with a bounded check: it will check queue length and wait (poll) until there is space or timeout.
        /// Throws QueueFullException if the queue remains full until timeout.
        /// </summary>
        public async Task PublishAsync<T>(T message, string? routingKey = null, CancellationToken cancellationToken = default)
        {
            var targetQueue = routingKey ?? _requestsQueue;

            // quick path: if max length is <= 0 treat as unbounded, we are not applying any limit
            if (_maxQueueLength <= 0)
            {
                DoPublish(message, targetQueue);
                return;
            }

            var started = DateTime.UtcNow; // start point for timeout measurement
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // passive declare returns queue info without changing it
                    var declareOk = _channel.QueueDeclarePassive(targetQueue); // getting the metadata of the queue
                    var messageCount = declareOk.MessageCount; // number of messages in the queue

                    _logger.LogInformation("-----------------------------------------------");
                    _logger.LogInformation($" (count={messageCount}).");
                        _logger.LogInformation("-----------------------------------------------");


                    if (messageCount < _maxQueueLength)
                    {
                        // there is space — publish now
                        DoPublish(message, targetQueue);
                        return;
                    }

                    // queue is full — check timeout
                    var elapsed = DateTime.UtcNow - started;
                    if (elapsed >= _publisherWaitTimeout)
                    {

                        _logger.LogInformation("-----------------------------------------------");
                        _logger.LogWarning("Queue {Queue} is full (count={Count}, max={Max}). Timeout reached after {Timeout}s.", targetQueue, messageCount, _maxQueueLength, _publisherWaitTimeout.TotalSeconds);
                        throw new QueueFullException($"Queue {targetQueue} is full (count={messageCount}).");
                    }

                    // wait a short while before re-checking (small backoff)
                    var delayMs = Math.Min(250, (int)(_publisherWaitTimeout - elapsed).TotalMilliseconds);
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404)
                {
                    // queue doesn't exist (shouldn't happen since we declare in ctor) => declare and then continue
                    _logger.LogWarning(ex, "Queue {Queue} did not exist when checking. Declaring it now.", targetQueue);
                    DeclareQueue(targetQueue);
                }
                catch (OperationInterruptedException ex)
                {
                    // other RabbitMQ error — rethrow
                    _logger.LogError(ex, "RabbitMQ error while checking queue length for {Queue}", targetQueue);
                    throw;
                }
            }

            // cancellation requested
            throw new OperationCanceledException(cancellationToken);
        }

        private void DoPublish<T>(T message, string targetQueue)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json); // rabbit mq needs byte array

            var props = _channel.CreateBasicProperties();
            props.Persistent = true; // message will be persisted to disk, so when msg broker restarts it is not lost

            _channel.BasicPublish(
                exchange: "",
                routingKey: targetQueue,
                basicProperties: props,
                body: body
            );

            _logger.LogInformation("📤 Message published to {RoutingKey} (size={Bytes})", targetQueue, body.Length);
        }


        // Dispose pattern to clean up RabbitMQ connection and channel
        // if DI container closed or object is disposed
        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
