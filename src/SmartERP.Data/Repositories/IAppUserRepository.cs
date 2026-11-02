using SmartERP.Data.Models;

namespace SmartERP.Data.Repositories;

public interface IAppUserRepository
{
    Task<AppUser?> GetByUsernameAsync(string username);
    Task<AppUser?> GetByIdAsync(double id);
    Task CreateAsync(AppUser user);
}