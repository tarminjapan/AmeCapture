# AmeCapture

A local screen capture application built with **Rust + Tauri v2** and **React + TypeScript**.

## Architecture

```text
AmeCapture/
├── src/                        # Frontend (React + TypeScript)
│   ├── main.tsx               # Entry point
│   ├── App.tsx                # Root component
│   ├── pages/                 # Page components
│   │   ├── WorkspacePage.tsx  # Capture history browser
│   │   ├── EditorPage.tsx     # Image annotation editor
│   │   └── SettingsPage.tsx   # Application settings
│   ├── components/            # Reusable UI components
│   │   ├── ThumbnailGrid.tsx  # Image thumbnail grid
│   │   ├── Toolbar.tsx        # Main toolbar
│   │   ├── SearchBar.tsx      # Search/filter bar
│   │   └── DetailPanel.tsx    # Item detail panel
│   ├── stores/                # Zustand state stores
│   ├── hooks/                 # Custom React hooks
│   ├── types/                 # TypeScript type definitions
│   └── lib/                   # Utility functions
├── src-tauri/                  # Backend (Rust + Tauri v2)
│   ├── src/
│   │   ├── main.rs            # Rust entry point
│   │   ├── lib.rs             # Tauri app setup & plugin registration
│   │   ├── commands/          # Tauri IPC command handlers
│   │   │   ├── capture.rs     # Capture commands
│   │   │   ├── workspace.rs   # Workspace CRUD commands
│   │   │   ├── editor.rs      # Editor commands
│   │   │   └── settings.rs    # Settings commands
│   │   ├── capture/           # Screen capture logic (Win32 API)
│   │   ├── workspace/         # Workspace item management
│   │   ├── editor/            # Image editing tools
│   │   ├── db/                # SQLite database (rusqlite)
│   │   ├── config/            # App configuration
│   │   └── utils/             # Error types & utilities
│   ├── Cargo.toml             # Rust dependencies
│   └── tauri.conf.json        # Tauri configuration
├── package.json                # Node.js dependencies
├── vite.config.ts              # Vite build config
└── tsconfig.json               # TypeScript config
```

## Tech Stack

### Frontend

- **React 19** + **TypeScript 5.8**
- **Tailwind CSS 4** for styling
- **Zustand** for state management
- **Lucide React** for icons
- **Vite 6** for build tooling

### Backend

- **Rust** (Edition 2021)
- **Tauri v2** for desktop app framework
- **rusqlite** for SQLite database
- **image** crate for image processing
- **Win32 API** (via `windows` crate) for screen capture

### Plugins

- `tauri-plugin-clipboard-manager` - Clipboard operations
- `tauri-plugin-global-shortcut` - Global hotkeys
- `tauri-plugin-notification` - System notifications
- `tauri-plugin-store` - Persistent key-value store

## Getting Started

### Prerequisites

- [Node.js](https://nodejs.org/) (v18+)
- [Rust](https://www.rust-lang.org/tools/install) (v1.70+)
- [Tauri Prerequisites](https://v2.tauri.app/start/prerequisites/)

### Install Dependencies

```bash
# Install frontend dependencies
npm install

# Build Rust dependencies (first time takes a while)
cd src-tauri && cargo build
```

### Development

```bash
# Start dev server (frontend + backend with hot reload)
npm run tauri dev
```

### Build

```bash
# Build production app
npm run tauri build
```

## Features (MVP)

- [x] Full screen capture
- [x] Region capture
- [x] Window capture
- [x] Capture history (workspace)
- [x] Image annotation (arrow, rectangle, mosaic)
- [x] Undo/Redo
- [x] Global hotkeys
- [x] Clipboard copy
- [x] Settings management

## License

See [LICENSE](LICENSE) for details.
