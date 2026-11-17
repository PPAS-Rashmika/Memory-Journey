using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace MemoryTimeline.Data;

public class DatabaseInitializer
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext context, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Ensure the directory exists using the connection string
            var connectionString = _context.Database.GetConnectionString() ?? string.Empty;
            // Parse "Data Source=path" from connection string
            var dataSourcePrefix = "Data Source=";
            var dbPath = string.Empty;
            if (connectionString.Contains(dataSourcePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var idx = connectionString.IndexOf(dataSourcePrefix, StringComparison.OrdinalIgnoreCase);
                dbPath = connectionString.Substring(idx + dataSourcePrefix.Length).Split(';')[0].Trim();
            }

            if (!string.IsNullOrEmpty(dbPath))
            {
                var dir = Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
            }

            // Create database and tables if they don't exist
            await _context.Database.EnsureCreatedAsync();

            // Seed default tags if none exist
            if (!await _context.Tags.AnyAsync())
            {
                _context.Tags.AddRange(
                    new Models.Tag { Id = Guid.NewGuid(), Name = "Family", Color = "#E94560" },
                    new Models.Tag { Id = Guid.NewGuid(), Name = "Travel", Color = "#4A90D9" },
                    new Models.Tag { Id = Guid.NewGuid(), Name = "Milestone", Color = "#F5A623" },
                    new Models.Tag { Id = Guid.NewGuid(), Name = "Career", Color = "#50C878" },
                    new Models.Tag { Id = Guid.NewGuid(), Name = "Celebration", Color = "#9B59B6" },
                    new Models.Tag { Id = Guid.NewGuid(), Name = "Education", Color = "#3498DB" }
                );
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Database initialized successfully at: {DbPath}", dbPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database.");
            throw;
        }
    }
}
