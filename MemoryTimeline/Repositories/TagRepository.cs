using Microsoft.EntityFrameworkCore;
using MemoryTimeline.Data;
using MemoryTimeline.Models;

namespace MemoryTimeline.Repositories;

public class TagRepository : ITagRepository
{
    private readonly AppDbContext _db;
    public TagRepository(AppDbContext db) => _db = db;

    public async Task<List<Tag>> GetAllAsync() =>
        await _db.Tags.OrderBy(t => t.Name).ToListAsync();

    public async Task<Tag?> GetByIdAsync(Guid id) =>
        await _db.Tags.FindAsync(id);

    public async Task<Tag?> GetByNameAsync(string name) =>
        await _db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());

    public async Task AddAsync(Tag tag)
    {
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tag tag)
    {
        _db.Tags.Update(tag);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var tag = await _db.Tags.FindAsync(id);
        if (tag != null) { _db.Tags.Remove(tag); await _db.SaveChangesAsync(); }
    }

    public async Task<List<Tag>> GetTagsForMemoryAsync(Guid memoryId) =>
        await _db.MemoryTags
                 .Where(mt => mt.MemoryId == memoryId)
                 .Select(mt => mt.Tag!)
                 .Where(t => t != null)
                 .ToListAsync();

    public async Task AddTagToMemoryAsync(Guid memoryId, Guid tagId)
    {
        var exists = await _db.MemoryTags.AnyAsync(mt => mt.MemoryId == memoryId && mt.TagId == tagId);
        if (!exists)
        {
            _db.MemoryTags.Add(new MemoryTag { MemoryId = memoryId, TagId = tagId });
            await _db.SaveChangesAsync();
        }
    }

    public async Task RemoveTagFromMemoryAsync(Guid memoryId, Guid tagId)
    {
        var mt = await _db.MemoryTags.FindAsync(memoryId, tagId);
        if (mt != null) { _db.MemoryTags.Remove(mt); await _db.SaveChangesAsync(); }
    }

    public async Task RemoveAllTagsFromMemoryAsync(Guid memoryId)
    {
        var tags = _db.MemoryTags.Where(mt => mt.MemoryId == memoryId);
        _db.MemoryTags.RemoveRange(tags);
        await _db.SaveChangesAsync();
    }
}
