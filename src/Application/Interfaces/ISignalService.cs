

using MyApp.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Application.Interfaces
{
    public interface ISignalService
    {
        PagedResult<SignalDto> GetByAsset(Guid assetId, int page, int pageSize, string search);
        SignalDto GetById(Guid assetId, int signalId);
        SignalDto Create(Guid assetId, SignalDto dto, string userId);
        SignalDto Update(Guid assetId, int signalId, SignalDto dto, string userId);
        bool Delete(Guid assetId, int signalId, string userId);
    }
}
