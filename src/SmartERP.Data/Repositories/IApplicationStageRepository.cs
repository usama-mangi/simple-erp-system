using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

// Configurable application pipeline stages (Choice B: configurable via lookup table).
// Provides read access to stages; writes go through the seed migration.
public interface IApplicationStageRepository
{
    Task<IEnumerable<ApplicationStage>> GetAllStagesAsync();
    Task<ApplicationStage?> GetStageByIdAsync(decimal id);
    Task<ApplicationStage?> GetStageByTitleAsync(string title);
}
