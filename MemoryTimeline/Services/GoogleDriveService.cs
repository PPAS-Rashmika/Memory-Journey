using System.Diagnostics;

namespace MemoryTimeline.Services;

public interface IGoogleDriveService
{
    bool IsValidDriveUrl(string? url);
    void OpenInBrowser(string url);
}

public class GoogleDriveService : IGoogleDriveService
{
    private static readonly string[] ValidHosts = { "drive.google.com", "docs.google.com", "sheets.google.com", "slides.google.com" };

    public bool IsValidDriveUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return ValidHosts.Any(h => uri.Host.Equals(h, StringComparison.OrdinalIgnoreCase));
    }

    public void OpenInBrowser(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
