using MemoryTimeline.Models;

namespace MemoryTimeline.Repositories;

public interface IMemoryRepository
{
    Task<List<Memory>> GetAllAsync();
    Task<List<Memory>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<Memory?> GetByIdAsync(Guid id);
    Task<List<Memory>> GetFavoritesAsync();
    Task<List<Memory>> GetMilestonesAsync();
    Task<List<Memory>> SearchAsync(string query);
    Task AddAsync(Memory memory);
    Task UpdateAsync(Memory memory);
    Task DeleteAsync(Guid id);
    Task<int> GetTotalCountAsync();
    Task<int> GetMilestoneCountAsync();
    Task<List<Memory>> GetRecentAsync(int count = 5);
    Task<List<Memory>> GetByYearAsync(int year);
    Task<List<Memory>> GetByMonthAsync(int year, int month);
    Task<List<Memory>> GetByDateAsync(DateTime date);
}
