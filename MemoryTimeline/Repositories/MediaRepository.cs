using Microsoft.EntityFrameworkCore;
using MemoryTimeline.Data;
using MemoryTimeline.Models;

namespace MemoryTimeline.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly AppDbContext _db;
    public MediaRepository(AppDbContext db) => _db = db;

    public async Task<List<MediaItem>> GetAllAsync() =>
        await _db.MediaItems.Include(m => m.Memory).OrderByDescending(m => m.DateAdded).ToListAsync();

    public async Task<List<MediaItem>> GetByMemoryIdAsync(Guid memoryId) =>
        await _db.MediaItems.Where(m => m.MemoryId == memoryId).OrderBy(m => m.DateAdded).ToListAsync();

    public async Task<List<MediaItem>> GetPhotosAsync() =>
        await _db.MediaItems.Include(m => m.Memory)
                 .Where(m => m.MediaType == MediaType.Photo)
                 .OrderByDescending(m => m.DateAdded).ToListAsync();

    public async Task<List<MediaItem>> GetVideosAsync() =>
        await _db.MediaItems.Include(m => m.Memory)
                 .Where(m => m.MediaType == MediaType.Video)
                 .OrderByDescending(m => m.DateAdded).ToListAsync();

    public async Task<List<MediaItem>> GetFavoritesAsync() =>
        await _db.MediaItems.Include(m => m.Memory)
                 .Where(m => m.IsFavorite)
                 .OrderByDescending(m => m.DateAdded).ToListAsync();

    public async Task<MediaItem?> GetByIdAsync(Guid id) =>
        await _db.MediaItems.Include(m => m.Memory).FirstOrDefaultAsync(m => m.Id == id);

    public async Task AddAsync(MediaItem item)
    {
        _db.MediaItems.Add(item);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(MediaItem item)
    {
        _db.MediaItems.Update(item);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _db.MediaItems.FindAsync(id);
        if (item != null)
        {
            _db.MediaItems.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<int> GetPhotoCountAsync() =>
        await _db.MediaItems.CountAsync(m => m.MediaType == MediaType.Photo);

    public async Task<int> GetVideoCountAsync() =>
        await _db.MediaItems.CountAsync(m => m.MediaType == MediaType.Video);

    public async Task<List<MediaItem>> GetRecentPhotosAsync(int count = 8) =>
        await _db.MediaItems
                 .Where(m => m.MediaType == MediaType.Photo)
                 .OrderByDescending(m => m.DateAdded)
                 .Take(count).ToListAsync();

    public async Task<List<MediaItem>> SearchAsync(string query, MediaType? type = null, bool? favoritesOnly = null)
    {
        var q = _db.MediaItems.Include(m => m.Memory).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.ToLower();
            q = q.Where(m => m.FileName.ToLower().Contains(lower)
                          || (m.Memory != null && m.Memory.Title.ToLower().Contains(lower))
                          || (m.Memory != null && m.Memory.LocationName != null && m.Memory.LocationName.ToLower().Contains(lower)));
        }
        if (type.HasValue) q = q.Where(m => m.MediaType == type.Value);
        if (favoritesOnly == true) q = q.Where(m => m.IsFavorite);
        return await q.OrderByDescending(m => m.DateAdded).ToListAsync();
    }
}
