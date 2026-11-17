using System.Globalization;
using System.Text.Json;
using System.Windows;

namespace MemoryTimeline.Views;

public partial class LocationPickerWindow : Window
{
    public string? SelectedLocationName { get; private set; }
    public double? SelectedLatitude { get; private set; }
    public double? SelectedLongitude { get; private set; }

    public LocationPickerWindow(string? locationName, double? latitude, double? longitude)
    {
        InitializeComponent();

        SelectedLocationName = locationName;
        SelectedLatitude = latitude;
        SelectedLongitude = longitude;

        TxtLocationName.Text = locationName ?? string.Empty;
        TxtLatitude.Text = latitude.HasValue ? latitude.Value.ToString("F6", CultureInfo.InvariantCulture) : string.Empty;
        TxtLongitude.Text = longitude.HasValue ? longitude.Value.ToString("F6", CultureInfo.InvariantCulture) : string.Empty;

        BtnCancel.Click += (s, e) => { DialogResult = false; Close(); };
        BtnConfirm.Click += OnConfirmClick;

        Loaded += LocationPickerWindow_Loaded;
    }

    private async void LocationPickerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await MapView.EnsureCoreWebView2Async();
            MapView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            var initialLat = SelectedLatitude ?? 7.8731; // Default Sri Lanka or center
            var initialLng = SelectedLongitude ?? 80.7718;
            var initialZoom = SelectedLatitude.HasValue ? 13 : 7;
            var hasMarker = SelectedLatitude.HasValue && SelectedLongitude.HasValue;

            var html = GenerateLeafletHtml(initialLat, initialLng, initialZoom, hasMarker);
            MapView.NavigateToString(html);
        }
        catch
        {
            // If WebView2 fails (e.g. runtime missing or offline error)
            OfflineNotice.Visibility = Visibility.Visible;
            MapView.Visibility = Visibility.Collapsed;
        }
    }

    private void OnWebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var rawJson = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(rawJson)) return;

            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("lat", out var latProp) && root.TryGetProperty("lng", out var lngProp))
            {
                var lat = latProp.GetDouble();
                var lng = lngProp.GetDouble();

                TxtLatitude.Text = lat.ToString("F6", CultureInfo.InvariantCulture);
                TxtLongitude.Text = lng.ToString("F6", CultureInfo.InvariantCulture);

                if (root.TryGetProperty("name", out var nameProp) && !string.IsNullOrWhiteSpace(nameProp.GetString()))
                {
                    if (string.IsNullOrWhiteSpace(TxtLocationName.Text))
                    {
                        TxtLocationName.Text = nameProp.GetString();
                    }
                }
            }
        }
        catch { }
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        SelectedLocationName = string.IsNullOrWhiteSpace(TxtLocationName.Text) ? null : TxtLocationName.Text.Trim();

        if (double.TryParse(TxtLatitude.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
            SelectedLatitude = lat;
        else
            SelectedLatitude = null;

        if (double.TryParse(TxtLongitude.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
            SelectedLongitude = lng;
        else
            SelectedLongitude = null;

        DialogResult = true;
        Close();
    }

    private string GenerateLeafletHtml(double lat, double lng, int zoom, bool hasMarker)
    {
        var latStr = lat.ToString("F6", CultureInfo.InvariantCulture);
        var lngStr = lng.ToString("F6", CultureInfo.InvariantCulture);
        var addMarkerJs = hasMarker
            ? $"var marker = L.marker([{latStr}, {lngStr}]).addTo(map);"
            : "var marker = null;";

        return $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
            <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
            <style>
                html, body, #map { height: 100%; width: 100%; margin: 0; padding: 0; }
            </style>
        </head>
        <body>
            <div id="map"></div>
            <script>
                var map = L.map('map').setView([{{latStr}}, {{lngStr}}], {{zoom}});
                L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    maxZoom: 19,
                    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                }).addTo(map);

                {{addMarkerJs}}

                map.on('click', function(e) {
                    var clickedLat = e.latlng.lat;
                    var clickedLng = e.latlng.lng;

                    if (marker) {
                        marker.setLatLng(e.latlng);
                    } else {
                        marker = L.marker(e.latlng).addTo(map);
                    }

                    // Reverse geocoding via Nominatim
                    fetch('https://nominatim.openstreetmap.org/reverse?format=json&lat=' + clickedLat + '&lon=' + clickedLng)
                        .then(function(res) { return res.json(); })
                        .then(function(data) {
                            var placeName = data.display_name || '';
                            if (window.chrome && window.chrome.webview) {
                                window.chrome.webview.postMessage(JSON.stringify({
                                    lat: clickedLat,
                                    lng: clickedLng,
                                    name: placeName
                                }));
                            }
                        })
                        .catch(function() {
                            if (window.chrome && window.chrome.webview) {
                                window.chrome.webview.postMessage(JSON.stringify({
                                    lat: clickedLat,
                                    lng: clickedLng,
                                    name: ''
                                }));
                            }
                        });
                });
            </script>
        </body>
        </html>
        """;
    }
}
