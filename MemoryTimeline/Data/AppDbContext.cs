using Microsoft.EntityFrameworkCore;
using MemoryTimeline.Models;

namespace MemoryTimeline.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Memory> Memories => Set<Memory>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<MemoryTag> MemoryTags => Set<MemoryTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Memory configuration
        modelBuilder.Entity<Memory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.LocationName).HasMaxLength(200);
            entity.Property(e => e.GoogleDriveUrl).HasMaxLength(500);
        });

        // MediaItem configuration
        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(260);
            entity.Property(e => e.OriginalFilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.StoredFilePath).HasMaxLength(500);
            entity.Property(e => e.ThumbnailPath).HasMaxLength(500);
            entity.Property(e => e.MediaType).HasConversion<string>();
            entity.Property(e => e.StorageMode).HasConversion<string>();

            entity.HasOne(e => e.Memory)
                  .WithMany(m => m.MediaItems)
                  .HasForeignKey(e => e.MemoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Tag configuration
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Color).HasMaxLength(20);
        });

        // MemoryTag (join table) — composite key
        modelBuilder.Entity<MemoryTag>(entity =>
        {
            entity.HasKey(e => new { e.MemoryId, e.TagId });

            entity.HasOne(e => e.Memory)
                  .WithMany(m => m.Tags)
                  .HasForeignKey(e => e.MemoryId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                  .WithMany(t => t.Memories)
                  .HasForeignKey(e => e.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
