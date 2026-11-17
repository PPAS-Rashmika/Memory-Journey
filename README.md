# 📸 Memory Timeline – Personal Digital Memory Archive

**Memory Timeline** is a modern, offline-first personal digital memory archive desktop application for Windows. It allows users to organize their life experiences, important events, milestones, photographs, videos, geographical locations, and related Google Drive documents in one elegant personal hub.

---

## 🌟 Key Highlights & Design Direction

- **Architecture:** MVVM (Model-View-ViewModel) with clean separation of Concerns (Models, Data, Repositories, Services, ViewModels, Views, Converters, Themes).
- **Offline First:** All primary databases, settings, and media files are stored locally on the user's PC with zero cloud dependency.
- **Dark & Light Mode:** Glassmorphism-inspired UI with rounded cards, soft shadows, custom brushes, and modern typography.
- **Serpentine Winding Memory Line:** Interactive winding timeline featuring custom Bezier curve spine (`TimelineSpine`), glow connector nodes, left/right alternating cards, top media preview strip `[ ][ ][ ] 5+`, and classic view toggle.
- **Dedicated Cover / Preview Photo:** Choose a dedicated cover photo for any memory (from local files, local link, or cloud URL), with live thumbnail previews and a 1-click "Set as Preview" action.
- **Collapsible Sidebar:** Streamlined hideable navigation sidebar with keyboard shortcut (`Ctrl+B`) and an unobtrusive "Show Menu" restore bar.
- **Enhanced Calendar View:** Month view renders event titles under dates with milestone star icons, overflow counters (`+N more`), and an interactive selected date inspector.
- **Media Storage Flexibility:** Supports **Local Copy**, **Original Link**, and **Cloud Storage (Google Drive / Web URLs)** with thumbnail generation and missing-file relink capabilities.
- **Interactive Map:** Leaflet + OpenStreetMap interactive picker powered by WebView2, supporting manual and map-click coordinate acquisition with OpenStreetMap attribution.
- **Google Drive Integration:** Links memories to Google Drive documents and opens them in the default Windows web browser.

---

## 🛠️ Technology Stack

| Component | Technology |
|---|---|
| **Language** | C# 13 |
| **Framework** | .NET 10 LTS (`net10.0-windows`) |
| **Desktop UI** | Windows Presentation Foundation (WPF) |
| **Architecture** | MVVM via `CommunityToolkit.Mvvm` |
| **Database** | SQLite |
| **ORM** | Entity Framework Core 9 (`Microsoft.EntityFrameworkCore.Sqlite`) |
| **Dependency Injection** | `Microsoft.Extensions.DependencyInjection` & `Microsoft.Extensions.Hosting` |
| **Map Engine** | Leaflet.js + OpenStreetMap via `Microsoft.Web.WebView2` |
| **Media Player** | WPF `MediaElement` with controls & Windows default player fallback |
| **Image Engine** | WPF Native `BitmapImage` with automatic thumbnail generation |

---

## 📁 Project Structure

```
MemoryTimeline/
├── Controls/
│   └── TimelineSpine.cs      # Custom FrameworkElement rendering serpentine Bezier curve spine & nodes
├── Models/
│   ├── Memory.cs             # Core memory entity (Title, Date, Location, GPS, Drive URL, Milestone, Favorite)
│   ├── MediaItem.cs          # Photo / video record with storage mode & thumbnail paths
│   ├── MediaType.cs          # Enum: Photo, Video
│   ├── StorageMode.cs        # Enum: Copied (App Storage), Original (Linked)
│   ├── Tag.cs                # Tag entity (Name, Color)
│   └── MemoryTag.cs          # Join entity for Many-to-Many Memory <-> Tag
├── Data/
│   ├── AppDbContext.cs       # EF Core SQLite DbContext with fluent configuration & cascade deletes
│   └── DatabaseInitializer.cs# Directory verification, EnsureCreated, and default tag seeding
├── Repositories/
│   ├── IMemoryRepository.cs  # CRUD, chronological sorting, search, milestones, favorites
│   ├── MemoryRepository.cs   # EF Core implementation
│   ├── IMediaRepository.cs   # Media CRUD, search, and type-based filtering
│   ├── MediaRepository.cs    # EF Core implementation
│   ├── ITagRepository.cs     # Tag management and association
│   └── TagRepository.cs      # EF Core implementation
├── Services/
│   ├── INavigationService.cs # Sidebar navigation coordination
│   ├── NavigationService.cs
│   ├── IThemeService.cs      # Dynamic Dark/Light ResourceDictionary swapping
│   ├── ThemeService.cs
│   ├── IMediaService.cs      # Import (copy/link), thumbnail generation, relinking, cache clearing
│   ├── MediaService.cs
│   ├── ISettingsService.cs   # Configuration, ZIP backup, JSON export, restore
│   ├── SettingsService.cs
│   ├── IGoogleDriveService.cs# URL validation and default browser launcher
│   └── GoogleDriveService.cs
├── ViewModels/
│   ├── MainViewModel.cs           # Shell navigation and theme toggle commands
│   ├── NavigationViewModelBase.cs
│   ├── DashboardViewModel.cs      # Stats, recent items, memories by year breakdown
│   ├── TimelineViewModel.cs       # Chronological timeline feed, search, milestone toggle, disk cleanup
│   ├── TimelineItemViewModel.cs   # Winding item wrapper with preview media strip & remaining counter
│   ├── AddEditMemoryViewModel.cs  # Add/Edit form, cover photo selection, cloud media staging, validation
│   ├── GalleryViewModel.cs        # Media filtering (type, year, location, favorite, query)
│   ├── CalendarViewModel.cs       # Year, Month, and Week views with date inspection
│   └── OtherViewModels.cs         # MilestonesViewModel, FavoritesViewModel, SettingsViewModel
├── Views/
│   ├── MainWindow.xaml            # Modern 220px sidebar with navigation and theme switcher
│   ├── DashboardView.xaml         # Statistic cards, memories by year, quick creation
│   ├── TimelineView.xaml          # Vertical timeline with date line, cards, and markers
│   ├── AddEditMemoryWindow.xaml   # Add/Edit memory dialog with media mode selector
│   ├── MemoryDetailWindow.xaml    # Full memory details, Drive link launcher, media grid
│   ├── LocationPickerWindow.xaml  # Interactive Leaflet + OpenStreetMap WebView2 picker
│   ├── GalleryView.xaml           # Media thumbnail grid with live filters
│   ├── MediaPreviewWindow.xaml    # Image viewer, video player, and missing-file relink
│   ├── CalendarView.xaml          # 3-mode calendar (Year, Month, Week)
│   ├── MilestonesView.xaml        # Curated milestone showcase with Drive links
│   ├── FavoritesView.xaml         # Favorited memories and media collection
│   └── SettingsView.xaml          # Theme, storage paths, cache cleaning, ZIP backup/restore
├── Converters/
│   └── Converters.cs              # Value converters (DateFormat, PathToImage, BoolToVisibility, etc.)
├── Themes/
│   ├── Dark.xaml                  # Dark glassmorphism color palette (#1A1A2E background)
│   └── Light.xaml                 # Clean light color palette (#F8F9FA background)
└── MemoryTimeline.csproj          # Project definition targeting net10.0-windows
```

---

## 🗄️ Database Design

The local SQLite database is stored by default at:
`%LOCALAPPDATA%\MemoryTimeline\memorytimeline.db`

### Entity Schema

```
   ┌────────────────────────────────────────┐
   │                Memories                │
   ├────────────────────────────────────────┤
   │ Id (Guid, PK)                          │
   │ Title (nvarchar 200, Not Null)         │
   │ MemoryDate (datetime, Indexed)         │
   │ Description (nvarchar 2000)            │
   │ LocationName (nvarchar 200)            │
   │ Latitude (float, Nullable)             │
   │ Longitude (float, Nullable)            │
   │ GoogleDriveUrl (nvarchar 500)          │
   │ IsMilestone (boolean)                  │
   │ IsFavorite (boolean)                   │
   │ CreatedAt (datetime)                   │
   │ UpdatedAt (datetime)                   │
   └──────────────────┬─────────────────────┘
                      │ 1:N (Cascade Delete)
         ┌────────────┴───────────┐
         ▼                        ▼
┌──────────────────┐    ┌──────────────────┐
│    MediaItems    │    │    MemoryTags    │
├──────────────────┤    ├──────────────────┤
│ Id (Guid, PK)    │    │ MemoryId (FK)    │◄───┐ Composite PK
│ MemoryId (FK)    │    │ TagId (FK)       │◄───┘
│ FileName         │    └─────────┬────────┘
│ OriginalFilePath │              │ N:1
│ StoredFilePath   │              ▼
│ MediaType        │    ┌──────────────────┐
│ StorageMode      │    │       Tags       │
│ ThumbnailPath    │    ├──────────────────┤
│ IsFavorite       │    │ Id (Guid, PK)    │
│ DateAdded        │    │ Name (nvarchar)  │
└──────────────────┘    │ Color (nvarchar) │
                        └──────────────────┘
```

---

## 🚀 How to Build and Run

### Prerequisites
- Windows 10 / Windows 11 (64-bit)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 1. Build from Command Line
```powershell
# Navigate to the project directory
cd "c:\Users\rashm\Desktop\photo app\MemoryTimeline"

# Build Debug version
dotnet build

# Or build Release version
dotnet build -c Release
```

### 2. Run the Application
```powershell
# Run from dotnet CLI
dotnet run --project "c:\Users\rashm\Desktop\photo app\MemoryTimeline"

# Or launch the compiled executable directly:
& "c:\Users\rashm\Desktop\photo app\Publish\MemoryTimeline.exe"
```

---

## ✅ Acceptance Test Verification Matrix

| # | Test Scenario | Implementation & Verification Details | Status |
|---|---|---|---|
| **1** | Add memory with date, title, location, tags, description | Supported via `AddEditMemoryWindow`, validates required fields, saves to SQLite. | **PASSED** |
| **2** | Attach multiple photographs and videos | Multi-select file dialog supports JPG, PNG, WEBP, MP4, MOV, MKV, etc. | **PASSED** |
| **3** | Copy media file into application storage | Option A copies file into `%LOCALAPPDATA%\MemoryTimeline\Media\{MemoryId}\`. | **PASSED** |
| **4** | Attach file using original location | Option B records file path without copying bytes. | **PASSED** |
| **5** | Mark memory as a milestone | Checkbox marks milestone; renders star badge and gold border in timeline & Milestones view. | **PASSED** |
| **6** | Attach Google Drive link & open in browser | Link is validated and opens in default browser via `IGoogleDriveService.OpenInBrowser`. | **PASSED** |
| **7** | Add older memory after newer exists | Timeline queries `OrderBy(m => m.MemoryDate)`, placing oldest at top and newest at bottom. | **PASSED** |
| **8** | Change memory date & automatic repositioning | Editing date updates database and automatically places card in correct chronological spot. | **PASSED** |
| **9** | Open Year, Month, and Week calendar views | Three view modes implemented with clean tab switching and responsive grid layouts. | **PASSED** |
| **10**| Click calendar date to view memories | Inspects selected date on side panel, listing memory titles, location, and photos. | **PASSED** |
| **11**| Filter gallery by date, location, tags, type, favorites | ComboBox and CheckBox filters apply composite query to media catalog. | **PASSED** |
| **12**| Search for memory by title | Real-time search box filters timeline, gallery, and dashboard. | **PASSED** |
| **13**| Switch between Light and Dark themes | Instant ResourceDictionary theme switching in sidebar and Settings. | **PASSED** |
| **14**| Restart app and verify persistence | SQLite database preserves all data across app restarts. | **PASSED** |
| **15**| Test missing original file & Relink option | Missing file displays warning with a **Relink File...** button to update path in SQLite. | **PASSED** |

---

## 🔒 Security & Privacy Notice
- **100% Local**: No user data, images, or videos are uploaded to third-party clouds or external servers.
- **Offline Maps**: Offline fallback ensures the location coordinates and location names can always be manually entered even with no internet connection.
