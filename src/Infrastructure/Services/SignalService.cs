using Microsoft.EntityFrameworkCore;

using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;


namespace MyApp.Infrastructure.Services
{
    public class SignalService : ISignalService
    {

        private readonly AppDbContext _context;

        public SignalService(AppDbContext context)
        {
            _context = context;
        }


        public PagedResult<SignalDto> GetByAsset(Guid assetId, int page, int pageSize, string search)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var query = _context.Signals
                .AsNoTracking()
                .Where(s => s.AssetId == assetId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalized = search.Trim().ToLower();
                query = query.Where(s => s.Name.ToLower().Contains(normalized) ||
                                         s.Description.ToLower().Contains(normalized));
            }

            var total = query.Count();

            var items = query
                .OrderByDescending(s => s.Id) // stable ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SignalDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Strength = s.Strength,
                    AssetId = s.AssetId
                })
                .ToList();

            return new PagedResult<SignalDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public SignalDto GetById(Guid assetId, int signalId)
        {
            var s = _context.Signals
                .AsNoTracking()
                .FirstOrDefault(x => x.Id == signalId && x.AssetId == assetId);

            if (s == null) return null;

            return new SignalDto { Id = s.Id, Name = s.Name, Description = s.Description, Strength= s.Strength, AssetId = s.AssetId };
        }

        public SignalDto Create(Guid assetId, SignalDto dto, string userId)
        {
            // optionally validate asset ownership outside or inside service
            var asset = _context.Assets.FirstOrDefault(a => a.Id == assetId);
            if (asset == null) throw new KeyNotFoundException("Asset not found.");

            var signal = new Signal
            {
                Name = dto.Name,
                Description = dto.Description,
                Strength = dto.Strength,
                AssetId = assetId
            };

            _context.Signals.Add(signal);
            _context.SaveChanges();

            return new SignalDto { Id = signal.Id, Name = signal.Name, Description = signal.Description, Strength = signal.Strength, AssetId = signal.AssetId };
        }

        public SignalDto Update(Guid assetId, int signalId, SignalDto dto, string userId)
        {
            var signal = _context.Signals.FirstOrDefault(s => s.Id == signalId && s.AssetId == assetId);
            if (signal == null) return null;

            signal.Name = dto.Name;
            signal.Description = dto.Description;
            signal.Strength = dto.Strength;

            _context.SaveChanges();

            return new SignalDto { Id = signal.Id, Name = signal.Name, Description = signal.Description, Strength= signal.Strength, AssetId = signal.AssetId };
        }

        public bool Delete(Guid assetId, int signalId, string userId)
        {
            var signal = _context.Signals.FirstOrDefault(s => s.Id == signalId && s.AssetId == assetId);
            if (signal == null) return false;

            _context.Signals.Remove(signal);
            _context.SaveChanges();
            return true;
        }

    }
}
