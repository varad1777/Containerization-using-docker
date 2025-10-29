using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyApp.Application.DTOs.MyApp.Messaging;
using MyApp.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace MyApp.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AveragesController : Controller
    {
        private readonly IRabbitMqPublisher _publisher;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AveragesController> _logger;

        public AveragesController(IRabbitMqPublisher publisher, IConfiguration configuration, ILogger<AveragesController> logger)
        {
            _publisher = publisher;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Enqueue([FromBody] EnqueueRequestDto req, CancellationToken cancellationToken)
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            if (req == null || string.IsNullOrWhiteSpace(req.ColumnName))
                return BadRequest("ColumnName is required.");
            if (req == null || req.AssetId == Guid.Empty)
                return BadRequest("AssetId is required.");

            var message = new AvgRequestMessage
            {
                AssetId = req.AssetId,
                ColumnName = req.ColumnName,
                UserId = userId,
                UserName = userName
            };

            string requestsQueue = _configuration["RabbitMq:RequestsQueue"] ?? "avg_requests";
            //await _publisher.PublishAsync(message, requestsQueue, cancellationToken);

            try
            {
                await _publisher.PublishAsync(message, requestsQueue, cancellationToken).ConfigureAwait(false);
                return Accepted(new { Message = "Column queued for background calculation.", Column = req.ColumnName });
            }
            catch (QueueFullException)
            {
                _logger.LogWarning("Queue full when publishing avg request for AssetId={AssetId}", req.AssetId);
                return StatusCode(429, new { Message = "Queue is full. Try again later." });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Publish canceled for AssetId={AssetId}", req.AssetId);
                return StatusCode(503, new { Message = "Request canceled." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish avg request for AssetId={AssetId}", req.AssetId);
                return StatusCode(503, new { Message = "Failed to enqueue request." });
            }
        }
    }

    public class EnqueueRequestDto
    {
        public Guid AssetId { get; set; }
        public string ColumnName { get; set; } = default!;
    }

}
