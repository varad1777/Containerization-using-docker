
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
                // Check if asset with same name exists
                bool exists = _context.Assets.Any(a => a.Name.ToLower() == asset.Name.ToLower());

                if (exists)
                    throw new Exception($"Asset with name '{asset.Name}' already exists.");

                _context.Assets.Add(asset);
                _context.SaveChanges();
                return asset;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true)
            {
                throw new Exception($"Asset with name '{asset.Name}' already exists.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
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

        if (asset == null) return null;

        // Check if another asset already uses this name
        bool nameExists = _context.Assets
            .Any(a => a.Name.ToLower() == updatedAsset.Name.ToLower() && a.Id != id);

        if (nameExists)
            throw new Exception($"Another asset with the name '{updatedAsset.Name}' already exists.");

        // Update Asset properties
        asset.Name = updatedAsset.Name;
        asset.Description = updatedAsset.Description;

        _context.SaveChanges();

        return asset;
    }
    catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true)
    {
        throw new Exception($"Asset with the name '{updatedAsset.Name}' already exists.", ex);
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
