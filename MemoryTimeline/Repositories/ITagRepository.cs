using MemoryTimeline.Models;

namespace MemoryTimeline.Repositories;

public interface ITagRepository
{
    Task<List<Tag>> GetAllAsync();
    Task<Tag?> GetByIdAsync(Guid id);
    Task<Tag?> GetByNameAsync(string name);
    Task AddAsync(Tag tag);
    Task UpdateAsync(Tag tag);
    Task DeleteAsync(Guid id);
    Task<List<Tag>> GetTagsForMemoryAsync(Guid memoryId);
    Task AddTagToMemoryAsync(Guid memoryId, Guid tagId);
    Task RemoveTagFromMemoryAsync(Guid memoryId, Guid tagId);
    Task RemoveAllTagsFromMemoryAsync(Guid memoryId);
}
