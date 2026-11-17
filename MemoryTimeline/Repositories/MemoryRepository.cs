using Microsoft.EntityFrameworkCore;
using MemoryTimeline.Data;
using MemoryTimeline.Models;

namespace MemoryTimeline.Repositories;

public class MemoryRepository : IMemoryRepository
{
    private readonly AppDbContext _db;
    public MemoryRepository(AppDbContext db) => _db = db;

    private IQueryable<Memory> BaseQuery() =>
        _db.Memories
           .Include(m => m.Tags).ThenInclude(mt => mt.Tag)
           .Include(m => m.MediaItems);

    public async Task<List<Memory>> GetAllAsync() =>
        await BaseQuery().OrderBy(m => m.MemoryDate).ToListAsync();

    public async Task<List<Memory>> GetByDateRangeAsync(DateTime from, DateTime to) =>
        await BaseQuery()
              .Where(m => m.MemoryDate >= from && m.MemoryDate <= to)
              .OrderBy(m => m.MemoryDate).ToListAsync();

    public async Task<Memory?> GetByIdAsync(Guid id) =>
        await BaseQuery().FirstOrDefaultAsync(m => m.Id == id);

    public async Task<List<Memory>> GetFavoritesAsync() =>
        await BaseQuery().Where(m => m.IsFavorite).OrderByDescending(m => m.MemoryDate).ToListAsync();

    public async Task<List<Memory>> GetMilestonesAsync() =>
        await BaseQuery().Where(m => m.IsMilestone).OrderBy(m => m.MemoryDate).ToListAsync();

    public async Task<List<Memory>> SearchAsync(string query)
    {
        var q = query.ToLower();
        return await BaseQuery()
            .Where(m => m.Title.ToLower().Contains(q)
                     || (m.Description != null && m.Description.ToLower().Contains(q))
                     || (m.LocationName != null && m.LocationName.ToLower().Contains(q))
                     || m.Tags.Any(mt => mt.Tag != null && mt.Tag.Name.ToLower().Contains(q)))
            .OrderBy(m => m.MemoryDate).ToListAsync();
    }

    public async Task AddAsync(Memory memory)
    {
        _db.Memories.Add(memory);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Memory memory)
    {
        memory.UpdatedAt = DateTime.UtcNow;
        _db.Memories.Update(memory);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var memory = await _db.Memories.FindAsync(id);
        if (memory != null)
        {
            _db.Memories.Remove(memory);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalCountAsync() => await _db.Memories.CountAsync();
    public async Task<int> GetMilestoneCountAsync() => await _db.Memories.CountAsync(m => m.IsMilestone);

    public async Task<List<Memory>> GetRecentAsync(int count = 5) =>
        await BaseQuery().OrderByDescending(m => m.CreatedAt).Take(count).ToListAsync();

    public async Task<List<Memory>> GetByYearAsync(int year) =>
        await BaseQuery().Where(m => m.MemoryDate.Year == year).OrderBy(m => m.MemoryDate).ToListAsync();

    public async Task<List<Memory>> GetByMonthAsync(int year, int month) =>
        await BaseQuery()
              .Where(m => m.MemoryDate.Year == year && m.MemoryDate.Month == month)
              .OrderBy(m => m.MemoryDate).ToListAsync();

    public async Task<List<Memory>> GetByDateAsync(DateTime date) =>
        await BaseQuery()
              .Where(m => m.MemoryDate.Date == date.Date)
              .OrderBy(m => m.MemoryDate).ToListAsync();
}
