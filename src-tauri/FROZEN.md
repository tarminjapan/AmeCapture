# ⚠️ FROZEN — Legacy Tauri/Rust Implementation

This directory contains the **frozen legacy implementation** of AmeCapture (Rust + Tauri v2).

**Status:** Deprecated. No further development or bug fixes will be applied.

## Active Implementation

The active implementation is located at `src-dotnet/` (.NET MAUI + C#).

See the [Migration Guide](../documents/MigrationGuide.md) for transition details.

## Freeze Scope

| Item                                            | Status                             |
| ----------------------------------------------- | ---------------------------------- |
| `src-tauri/` (Rust backend)                     | Frozen — no changes accepted       |
| `src/` (React frontend)                         | Frozen — no changes accepted       |
| `index.html`, `vite.config.ts`, `tsconfig.json` | Frozen — no changes accepted       |
| `package.json` (Tauri scripts)                  | Retained for legacy reference only |

## Deletion Plan

| Phase    | Action                                                | Timeline                        |
| -------- | ----------------------------------------------------- | ------------------------------- |
| Phase 6a | Mark as frozen, add deprecation notices               | **Current**                     |
| Phase 6b | Remove from CI/CD build pipeline                      | After MAUI stable release       |
| Phase 6c | Delete `src-tauri/`, `src/`, and related config files | After 1 major version with MAUI |
| Phase 6d | Remove legacy npm dependencies from `package.json`    | Together with Phase 6c          |

## What is Preserved

- Database schema compatibility (identical `CREATE TABLE IF NOT EXISTS` SQL)
- Application identifier: `com.amecapture.app`
- Data directory structure (`originals/`, `edited/`, `thumbnails/`, `videos/`)

## Migration Reference Documents

- [DB Schema & C# Migration Compatibility](../documents/5.%20DB%E3%82%B9%E3%82%AD%E3%83%BC%E3%83%9E%E8%A7%A3%E6%9E%90%E3%81%A8C%23%E7%A7%BB%E8%A1%8C%E4%BA%92%E6%8F%9B%E6%96%B9%E9%87%9D.md)
- [Migration Guide](../documents/MigrationGuide.md)
