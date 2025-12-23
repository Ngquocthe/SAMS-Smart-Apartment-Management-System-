namespace SAMS_BE.Interfaces.IRepository.Building
{
    public interface IScriptRepository
    {
        Task ExecuteSqlScriptAsync(string sqlScript);
    }
}
