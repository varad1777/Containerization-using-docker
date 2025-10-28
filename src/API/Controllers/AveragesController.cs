using Microsoft.AspNetCore.Mvc;
using MyApp.Application.DTOs;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Queues;

namespace MyApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AveragesController : Controller
    {
        private readonly CalculationQueue _queue;

        public AveragesController(CalculationQueue queue)
        {
            _queue = queue;
        }

        [HttpPost]
        public async Task<IActionResult >Enqueue([FromBody] EnqueueRequestDto req, CancellationToken cancellationToken)
        {

            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

            Console.WriteLine(userName);
            //var userId = "79432985-a38c-43f9-90ed-fc4555071250";


            if (req == null || string.IsNullOrWhiteSpace(req.ColumnName))
                return BadRequest("ColumnName is required.");   
            if (req == null || req.AssetId == Guid.Empty)
                return BadRequest("AssetId is required.");   

            EnqueueRequest e = new EnqueueRequest
            {
                ColumnName = req.ColumnName,   
                userId = userId,
                userName = userName,
                AssetId = req.AssetId
            };

            // while enquing we are awiting here, 
            //so the backpressure and concellation token works properly 
            await _queue.EnqueueAsync(e, cancellationToken).ConfigureAwait(false);


            return Accepted(new { Message = "Column queued for background calculation.", Column = req.ColumnName });
        }

    }
}
