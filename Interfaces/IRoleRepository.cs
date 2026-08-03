using HR_system.Models;
namespace HR_system.Interfaces
{
    public interface IRoleRepository
    {
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task<Role?> GetRoleByIdAsync(int id);
        Task AddAsync(Role role);

        Task updateAsync(Role role);

        Task DeleteAsync(int id);

    }
}
