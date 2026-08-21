namespace HR_system.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string action, string module, string details);
    }
}