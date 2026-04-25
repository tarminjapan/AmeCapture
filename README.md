# AmeCapture

A local screen capture application built with **.NET MAUI (C#)** and **SkiaSharp** for image editing.

## Architecture

```text
AmeCapture/
├── src/                                  # Source projects
│   ├── AmeCapture.Domain/                # Domain entities (no dependencies)
│   │   └── Entities/
│   │       ├── WorkspaceItem.cs          # Capture item model
│   │       ├── Tag.cs                    # Tag model
│   │       ├── AppSettings.cs            # App settings
│   │       └── Annotation.cs            # Annotation hierarchy
│   ├── AmeCapture.Application/           # Interfaces & Models
│   │   ├── Interfaces/                   # Service contracts
│   │   ├── Models/                       # DTOs (CaptureResult, WindowInfo, etc.)
│   │   └── Messages/                     # Messenger messages
│   ├── AmeCapture.Infrastructure/        # Implementations (Windows)
│   │   ├── Database/                     # SQLite (Microsoft.Data.Sqlite)
│   │   ├── Repositories/                 # Data access
│   │   └── Services/                     # Capture, Editor, Tray, Shortcuts, etc.
│   └── AmeCapture.App/                   # MAUI UI Layer
│       ├── Views/                        # XAML pages
│       ├── ViewModels/                   # MVVM ViewModels
│       └── Platforms/                    # Platform-specific code
├── tests/                                # Test projects
│   └── AmeCapture.Tests/                 # xUnit tests
├── docs/                                 # Design documents
└── .github/                              # CI/CD workflows
```

## Tech Stack

- **.NET 10** with **MAUI** (Preview — 正式リリース後に安定版へ移行予定)
- **CommunityToolkit.Mvvm** for MVVM pattern
- **SkiaSharp** for image annotation rendering
- **Microsoft.Data.Sqlite** for SQLite database
- **Serilog** for structured logging
- **Win32 API** (P/Invoke) for screen capture (GDI+ BitBlt)

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (latest preview)
- **Windows 10 1809+** (for Win32 platform features)
- Visual Studio 2022 17.12+ or later with MAUI workload

### Build & Run

```bash
# Restore dependencies
dotnet restore AmeCapture.slnx

# Build the .NET MAUI application
dotnet build src/AmeCapture.App/AmeCapture.App.csproj -f net10.0-windows10.0.19041.0

# Run the application
dotnet run --project src/AmeCapture.App/AmeCapture.App.csproj -f net10.0-windows10.0.19041.0
```

### Run Tests

```bash
dotnet test tests/AmeCapture.Tests/
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
