
using Microsoft.EntityFrameworkCore;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;


namespace MyApp.Infrastructure.Services
{
    public class AssetService : IAssetService
    {
        private readonly AppDbContext _context;

        public AssetService(AppDbContext context)
        {
            _context = context;
        }

        public Asset Create(Asset asset)
        {
            try
            {

                _context.Assets.Add(asset);
                _context.SaveChanges();
                return asset;
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while creating the asset: " + ex.Message, ex);
            }
        }

        public Asset? Update(Guid id, Asset updatedAsset)
        {
            try
            {
                // Load existing Asset including Signals
                var asset = _context.Assets
                    .Include(a => a.Signals)
                    .FirstOrDefault(a => a.Id == id);
                // firstORDefault return the first, that satisfied the condition,
                // if no element was found then return the default value 

                if (asset == null) return null;

                // Update Asset properties
                asset.Name = updatedAsset.Name;
                asset.Description = updatedAsset.Description;

                //// Remove all existing signals
                //_context.Signals.RemoveRange(asset.Signals);

                //// Add new signals from DTO
                //asset.Signals = updatedAsset.Signals.Select(s => new Signal
                //{
                //    Name = s.Name,
                //    Description = s.Description,
                //    AssetId = asset.Id
                //}).ToList();

                _context.SaveChanges();

                return asset;
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while updating the asset: " + ex.Message, ex);
            }
        }


        public bool Delete(Guid id)
        {

            try
            {

                var asset = _context.Assets.Include(a => a.Signals).FirstOrDefault(a => a.Id == id);
                if (asset == null) return false;

                _context.Assets.Remove(asset);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while creating the asset: " + ex.Message, ex);
            }
        }


        public Asset GetById(Guid id)
        {
            try
            {
                return _context.Assets.Include(a => a.Signals).FirstOrDefault(a => a.Id == id);
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while creating the asset: " + ex.Message, ex);
            }
        }



        public IEnumerable<Asset> GetAll(string? userId = null)
        {
            try
            {
                var query = _context.Assets
                    .Include(a => a.User)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(a => a.UserId == userId);
                }

                var assets = query.ToList();

                // Remove sensitive information
                foreach (var asset in assets)
                {
                    if (asset.User != null)
                    {
                        asset.User.PasswordHash = null;
                        asset.User.SecurityStamp = null;
                        asset.User.ConcurrencyStamp = null;
                    }
                }

                return assets;
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while retrieving the assets: " + ex.Message, ex);
            }
        }


    }
}
