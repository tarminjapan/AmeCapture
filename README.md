# AmeCapture

A local screen capture application built with **.NET MAUI (C#)** and **SkiaSharp** for image editing.

## Architecture

```text
AmeCapture/
├── src-dotnet/                          # .NET MAUI Application (primary)
│   ├── AmeCapture.Domain/               # Domain entities (no dependencies)
│   │   └── Entities/
│   │       ├── WorkspaceItem.cs         # Capture item model
│   │       ├── Tag.cs                   # Tag model
│   │       ├── AppSettings.cs           # App settings
│   │       └── Annotation.cs           # Annotation hierarchy
│   ├── AmeCapture.Application/          # Interfaces & Models
│   │   ├── Interfaces/                  # Service contracts
│   │   ├── Models/                      # DTOs (CaptureResult, WindowInfo, etc.)
│   │   └── Messages/                    # Messenger messages
│   ├── AmeCapture.Infrastructure/       # Implementations (Windows)
│   │   ├── Database/                    # SQLite (Microsoft.Data.Sqlite)
│   │   ├── Repositories/                # Data access
│   │   └── Services/                    # Capture, Editor, Tray, Shortcuts, etc.
│   ├── AmeCapture.App/                  # MAUI UI Layer
│   │   ├── Views/                       # XAML pages
│   │   ├── ViewModels/                  # MVVM ViewModels
│   │   └── Platforms/                   # Platform-specific code
│   └── AmeCapture.Tests/                # xUnit tests
├── src-tauri/                           # [FROZEN] Legacy Rust + Tauri v2 (deprecated)
├── src/                                 # [FROZEN] Legacy React + TypeScript frontend (deprecated)
├── documents/                           # Design & migration documents
└── scripts/                             # Build utilities
```

## Tech Stack

### Primary (.NET MAUI)

- **.NET 10** with **MAUI**
- **CommunityToolkit.Mvvm** for MVVM pattern
- **SkiaSharp** for image annotation rendering
- **Microsoft.Data.Sqlite** for SQLite database
- **Serilog** for structured logging
- **Win32 API** (P/Invoke) for screen capture (GDI+ BitBlt)

### Legacy (Frozen)

- **Rust** + **Tauri v2** (backend) — see `src-tauri/`
- **React 19** + **TypeScript 5.8** (frontend) — see `src/`

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (latest preview)
- **Windows 10 1809+** (for Win32 platform features)
- Visual Studio 2022 17.12+ or later with MAUI workload

### Build & Run

```bash
# Build the .NET MAUI application
dotnet build src-dotnet/AmeCapture.App/AmeCapture.App.csproj -f net10.0-windows10.0.19041.0

# Run the application
dotnet run --project src-dotnet/AmeCapture.App/AmeCapture.App.csproj -f net10.0-windows10.0.19041.0
```

### Run Tests

```bash
dotnet test src-dotnet/AmeCapture.Tests/
```

### Legacy (Tauri/Rust)

The legacy Tauri/Rust application is frozen. See [Migration Guide](documents/MigrationGuide.md) for details.

```bash
# Legacy dev server (requires Node.js + Rust)
npm run tauri dev
```

## Features

- [x] Full screen capture (GDI+ BitBlt)
- [x] Region capture
- [x] Window capture
- [x] Capture history (workspace)
- [x] Image annotation (arrow, rectangle, text, mosaic, crop)
- [x] Undo/Redo
- [x] Global hotkeys
- [x] Clipboard copy
- [x] System tray with minimize-to-tray
- [x] Tag management
- [x] Settings management

## License

See [LICENSE](LICENSE) for details.
