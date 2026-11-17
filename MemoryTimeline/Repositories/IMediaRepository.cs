using MemoryTimeline.Models;

namespace MemoryTimeline.Repositories;

public interface IMediaRepository
{
    Task<List<MediaItem>> GetAllAsync();
    Task<List<MediaItem>> GetByMemoryIdAsync(Guid memoryId);
    Task<List<MediaItem>> GetPhotosAsync();
    Task<List<MediaItem>> GetVideosAsync();
    Task<List<MediaItem>> GetFavoritesAsync();
    Task<MediaItem?> GetByIdAsync(Guid id);
    Task AddAsync(MediaItem item);
    Task UpdateAsync(MediaItem item);
    Task DeleteAsync(Guid id);
    Task<int> GetPhotoCountAsync();
    Task<int> GetVideoCountAsync();
    Task<List<MediaItem>> GetRecentPhotosAsync(int count = 8);
    Task<List<MediaItem>> SearchAsync(string query, MediaType? type = null, bool? favoritesOnly = null);
}
