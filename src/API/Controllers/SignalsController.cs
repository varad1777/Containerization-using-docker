using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;
using MyApp.Infrastructure.Data;
using System.Security.Claims;

namespace MyApp.API.Controllers
{
    [ApiController]
    [Route("api/assets/{assetId:guid}/[controller]")]
    [Authorize]
    public class SignalsController : Controller
    {
        private readonly ISignalService _signalService;
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public SignalsController(ISignalService signalService, AppDbContext context, INotificationService notificationService)
        {
            _signalService = signalService;
            _context = context;
            _notificationService = notificationService;
        }

        // GET api/assets/{assetId}/signals?page=1&pageSize=10&search=...
        [HttpGet]
        public IActionResult GetAll(Guid assetId, int page = 1, int pageSize = 10, string search = null)
        {
            try
            {
                // Authorization: allow if asset belongs to user or user is admin
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                var asset = _context.Assets.FirstOrDefault(a => a.Id == assetId);
                if (asset == null) return NotFound($"Asset not found.");

                if (!isAdmin && asset.UserId != userId)
                    return Forbid();

                var result = _signalService.GetByAsset(assetId, page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/assets/{assetId}/signals/{id}
        [HttpGet("{id:int}")]
        public IActionResult GetById(Guid assetId, int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                var asset = _context.Assets.FirstOrDefault(a => a.Id == assetId);
                if (asset == null) return NotFound($"Asset {assetId} not found.");
                if (!isAdmin && asset.UserId != userId) return Forbid();

                var signal = _signalService.GetById(assetId, id);
                if (signal == null) return NotFound($"Signal {id} not found for asset {assetId}.");
                return Ok(signal);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST api/assets/{assetId}/signals
        [HttpPost]
        public async Task<IActionResult> Create(Guid assetId, [FromBody] SignalDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                var asset = _context.Assets.FirstOrDefault(a => a.Id == assetId);
                if (asset == null) return NotFound($"Asset {assetId} not found.");
                if (!isAdmin && asset.UserId != userId) return Forbid();

                var created = _signalService.Create(assetId, dto, userId);

                // Optional: send notification
                await _notificationService.SendToAllAsync($"{User.Identity?.Name} created signal '{created.Name}' for asset '{asset.Name}'", User.Identity?.Name ?? "Unknown");

                return CreatedAtAction(nameof(GetById), new { assetId = assetId, id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to create signal: {ex.Message}");
            }
        }

        // PUT api/assets/{assetId}/signals/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(Guid assetId, int id, [FromBody] SignalDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                var asset = _context.Assets.FirstOrDefault(a => a.Id == assetId);
                if (asset == null) return NotFound($"Asset {assetId} not found.");
                if (!isAdmin && asset.UserId != userId) return Forbid();

                var updated = _signalService.Update(assetId, id, dto, userId);
                if (updated == null) return NotFound($"Signal {id} not found.");

                await _notificationService.SendToAllAsync($"{User.Identity?.Name} updated signal '{updated.Name}'", User.Identity?.Name ?? "Unknown");

                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to update signal: {ex.Message}");
            }
        }

        // DELETE api/assets/{assetId}/signals/{id}
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(Guid assetId, int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isAdmin = User.IsInRole("Admin");

                var asset = _context.Assets.FirstOrDefault(a => a.Id == assetId);
                if (asset == null) return NotFound($"Asset {assetId} not found.");
                if (!isAdmin && asset.UserId != userId) return Forbid();

                var ok = _signalService.Delete(assetId, id, userId);
                if (!ok) return NotFound($"Signal {id} not found.");

                await _notificationService.SendToAllAsync($"{User.Identity?.Name} deleted signal '{id}'", User.Identity?.Name ?? "Unknown");

                return Ok("Deleted");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to delete signal: {ex.Message}");
            }
        }
    }
}
