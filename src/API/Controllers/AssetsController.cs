
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.RTC;
using System.Security.Claims;


namespace EF_core_assignment.Controllers
{
    [ApiController] // line tell that, the action serve as a API
    [Route("api/[controller]")]
    //[Authorize(Roles = "User")]
    [Authorize] // check user authenticated becore entering into the actions 
    public class AssetsController : ControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;


        public AssetsController(IAssetService assetService, AppDbContext context, IHubContext<NotificationHub> hubContext, INotificationService notificationService)
        {
            _assetService = assetService;
            _hubContext = hubContext;
            _context = context;
            _notificationService = notificationService;
        }




        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                IEnumerable<Asset> assets;

                if (isAdmin)
                {
                    assets = _assetService.GetAll();
                }
                else
                {
                    assets = _assetService.GetAll(userId); // filter at DB level
                }

                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("{id}")]
        public IActionResult GetById(Guid id)
        {
            try
            {
                var asset = _assetService.GetById(id);
                if (asset == null) return NotFound($"Asset with id {id} not found.");
                return Ok(asset);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AssetDto dto)
            // form body just tells that, the data has to take from the body..
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (dto == null)
                {
                    return BadRequest("Device Data Cannot be null..");
                }



                foreach (var claim in User.Claims)
                {
                    Console.WriteLine($"{claim.Type} : {claim.Value}");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token.");

                var asset = new Asset
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    UserId = userId
                    //Signals = dto.Signals.Select(s => new Signal
                    //{
                    //    Name = s.Name,
                    //    Description = s.Description
                    //}).ToList()
                };




                var created = _assetService.Create(asset);



                // Notification
                var userName = User.Identity?.Name ?? "Unknown";
                await _notificationService.SendToAllAsync(
                $"{userName} created asset '{asset.Name}'", 

                User.Identity?.Name ?? "Unknown"
                    );


                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to create asset: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] AssetDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var updatedAsset = new Asset
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Signals = dto.Signals.Select(s => new Signal
                    {
                        Name = s.Name,
                        Description = s.Description
                    }).ToList()
                };

                var updated = _assetService.Update(id, updatedAsset);
                if (updated == null) return NotFound($"Asset with id {id} not found.");


                // Notification
                var userName = User.Identity?.Name ?? "Unknown";
                await _notificationService.SendToAllAsync(
               $"{userName} updated asset '{updated.Name}'",
               userName
                     );

                return Ok(updated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to update asset: {ex.Message}");
            }
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                if (!_assetService.Delete(id)) return NotFound($"Asset with id {id} not found.");

                var userName = User.Identity?.Name ?? "Unknown";
                await _notificationService.SendToAllAsync(
                $"{userName} deleted asset with id '{id}'",
                userName
                    );

                return Ok("Deleted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to delete asset: {ex.Message}");
            }
        }
    }
}
