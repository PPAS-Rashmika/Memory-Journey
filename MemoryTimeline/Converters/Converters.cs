using System.Collections;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MemoryTimeline.Models;
using MemoryTimeline.ViewModels;

namespace MemoryTimeline.Converters;

/// <summary>Converts null/empty string to Visibility.Collapsed</summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isVisible = !string.IsNullOrWhiteSpace(value as string);
        if (parameter as string == "invert") isVisible = !isVisible;
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>
/// Converts bool, int, collection, or object to Visibility.
/// If value is int: Visible if > 0.
/// If value is ICollection: Visible if Count > 0.
/// If value is bool: Visible if true.
/// Parameter="invert" reverses the logic.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = false;

        if (value is bool bv)
        {
            b = bv;
        }
        else if (value is int iv)
        {
            b = iv > 0;
        }
        else if (value is long lv)
        {
            b = lv > 0;
        }
        else if (value is ICollection coll)
        {
            b = coll.Count > 0;
        }
        else if (value != null)
        {
            b = true;
        }

        if (parameter as string == "invert") b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        v is Visibility vis && vis == Visibility.Visible;
}

/// <summary>Converts null to Visibility.Collapsed</summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNotNull = value != null;
        if (parameter as string == "invert") isNotNull = !isNotNull;
        return isNotNull ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>
/// Converts a local file path, cloud URL, Google Drive URL, or MediaItem to a BitmapImage.
/// </summary>
public class PathToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? path = null;
        if (value is MediaItem mi)
        {
            path = mi.EffectiveDisplayPath;
        }
        else if (value is PendingMediaItem pmi)
        {
            path = pmi.EffectivePreviewPath;
        }
        else if (value is string s)
        {
            path = s;
        }

        if (string.IsNullOrWhiteSpace(path)) return null;

        // Try extracting Google Drive thumbnail if applicable
        path = CloudUrlHelper.ResolveImageUrl(path);
        if (string.IsNullOrWhiteSpace(path)) return null;

        try
        {
            // Web / Cloud URL (HTTP / HTTPS)
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(path);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.DecodePixelWidth = 400;
                img.EndInit();
                return img;
            }

            // Local File
            if (!File.Exists(path)) return null;

            var localImg = new BitmapImage();
            localImg.BeginInit();
            localImg.UriSource = new Uri(path);
            localImg.CacheOption = BitmapCacheOption.OnLoad;
            localImg.DecodePixelWidth = 400;
            localImg.EndInit();
            localImg.Freeze();
            return localImg;
        }
        catch
        {
            return null;
        }
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>Formats DateTime as a friendly date string</summary>
public class DateFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
        {
            var fmt = parameter as string ?? "MMMM d, yyyy";
            return dt.ToString(fmt);
        }
        return string.Empty;
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>Converts MediaType enum to a display icon string</summary>
public class MediaTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is MediaType mt && mt == MediaType.Video ? "🎬" : "🖼️";
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>Returns true if value equals parameter</summary>
public class EqualsToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == parameter?.ToString();
    public object ConvertBack(object v, Type t, object p, CultureInfo c) =>
        (v is bool b && b) ? p : Binding.DoNothing;
}

/// <summary>Returns Visible if value equals parameter, otherwise Collapsed</summary>
public class EqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool equals = string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);
        return equals ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>Converts IsMilestone bool to star emoji</summary>
public class MilestoneToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && b ? "⭐" : string.Empty;
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>Converts count to "No items" or count string</summary>
public class CountToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            var noun = parameter as string ?? "item";
            return count == 0 ? $"No {noun}s" : $"{count} {noun}{(count == 1 ? "" : "s")}";
        }
        return string.Empty;
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>Converts IsFavorite bool to colored brush</summary>
public class FavoriteToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool b && b
            ? new SolidColorBrush(Color.FromRgb(233, 69, 96))
            : new SolidColorBrush(Color.FromRgb(100, 100, 120));
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>Truncates long text</summary>
public class TruncateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string ?? string.Empty;
        var maxLen = parameter is string s && int.TryParse(s, out var n) ? n : 120;
        return text.Length <= maxLen ? text : text.Substring(0, maxLen) + "…";
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>Converts StorageMode or URL to a badge description (e.g., ☁️ Cloud, 📁 Local)</summary>
public class StorageModeToBadgeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StorageMode sm)
        {
            return sm switch
            {
                StorageMode.Cloud => "☁️ Cloud",
                StorageMode.Copied => "📁 Local Copy",
                _ => "🔗 Local Link"
            };
        }
        if (value is MediaItem mi)
        {
            if (mi.IsCloud) return "☁️ Cloud";
            if (mi.StorageMode == StorageMode.Copied) return "📁 Local Copy";
            return "🔗 Local Link";
        }
        if (value is string s && (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                  s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            return "☁️ Cloud";
        }
        return "📁 Local";
    }
    public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
}

/// <summary>
/// Helper to extract thumbnail URLs from cloud and Google Drive URLs.
/// </summary>
public static class CloudUrlHelper
{
    public static string? ResolveImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        if (url.Contains("drive.google.com", StringComparison.OrdinalIgnoreCase))
        {
            var id = ExtractGoogleDriveId(url);
            if (!string.IsNullOrEmpty(id))
            {
                return $"https://drive.google.com/thumbnail?id={id}&sz=w800";
            }
        }

        return url;
    }

    public static string? ExtractGoogleDriveId(string url)
    {
        try
        {
            if (url.Contains("/d/"))
            {
                var match = Regex.Match(url, @"/d/([a-zA-Z0-9_-]+)");
                if (match.Success) return match.Groups[1].Value;
            }
            if (url.Contains("id="))
            {
                var match = Regex.Match(url, @"[?&]id=([a-zA-Z0-9_-]+)");
                if (match.Success) return match.Groups[1].Value;
            }
        }
        catch { }
        return null;
    }
}
