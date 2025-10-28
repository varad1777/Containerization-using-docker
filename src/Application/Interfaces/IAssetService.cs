
using MyApp.Domain.Entities;


namespace MyApp.Application.Interfaces
{
    public interface IAssetService
    {
        Asset Create(Asset asset);
        Asset Update(Guid id, Asset asset);
        bool Delete(Guid id);
        Asset GetById(Guid id);
        IEnumerable<Asset> GetAll(string? userId = null);
       
    }
}
