# AmeCapture — Release Notes

## v1.0.0 — .NET MAUI 正式版

### 概要

AmeCapture が Rust + Tauri v2 + React から **.NET MAUI (C#)** への完全移行を完了し、正式版としてリリースしました。

### 主要変更点

#### アーキテクチャ刷新

| 項目           | 旧 (Tauri/Rust)            | 新 (.NET MAUI)                |
| -------------- | -------------------------- | ----------------------------- |
| バックエンド   | Rust (Tauri v2)            | C# (.NET 10)                  |
| フロントエンド | React + TypeScript         | MAUI XAML + MVVM              |
| プロセスモデル | マルチプロセス (WebView)   | シングルプロセス (ネイティブ) |
| IPC            | Tauri IPC (JSON)           | 直接メソッド呼び出し          |
| 状態管理       | Zustand                    | CommunityToolkit.Mvvm         |
| 画像処理       | image + imageproc crate    | SkiaSharp                     |
| DB             | rusqlite                   | Microsoft.Data.Sqlite         |
| ログ           | tracing + tracing-appender | Serilog                       |

#### 機能（移行済み）

- **画面キャプチャ**: 全画面 / 範囲 / ウィンドウ（GDI+ BitBlt）
- **画像編集**: 矢印、矩形、テキスト、モザイク、クロップ、Undo/Redo
- **ワークスペース**: キャプチャ履歴の一覧表示・お気に入り・名前変更・エクスプローラー表示・クリップボードコピー
- **タグ管理**: タグ CRUD、アイテムへのタグ付け
- **システムトレイ**: キャプチャメニュー付きトレイ常駐、最小化でトレイに隠す
- **グローバルショートカット**: 範囲 / 全画面 / ウィンドウキャプチャのホットキー
- **クリップボード**: キャプチャ後の自動コピー
- **通知**: キャプチャ完了通知
- **設定管理**: ホットキー・保存先・画像形式の設定

#### 改善点

- **パフォーマンス**: WebView オーバーヘッドの排除によるネイティブ描画
- **メモリ管理**: SafeHandle による Win32 リソースの確実な解放
- **一貫性**: クリーンアーキテクチャによる層分離（Domain / Application / Infrastructure / App）
- **テスト**: xUnit によるドメイン・インテグレーションテスト
- **ログ**: Serilog による日次ローテーション・30日保持

### データ互換

- SQLite データベーススキーマは**変更なし**（同じ `CREATE TABLE IF NOT EXISTS`）
- Tauri 版と MAUI 版は同じ `amecapture.db` を共有可能
- ファイルストレージ構造（`originals/`, `edited/`, `thumbnails/`, `videos/`）は同一

### 動作環境

- Windows 10 version 1809 (17763) 以降
- .NET 10 Runtime

### リリース手順

1. `dotnet test src-dotnet/AmeCapture.Tests/` でテスト実行
2. `dotnet build src-dotnet/AmeCapture.App/ -c Release -f net10.0-windows10.0.19041.0` でリリースビルド
3. ビルド成果物を配布（unpackaged Win32 アプリケーション）
4. GitHub Release を作成し、バイナリを添付
5. リリースノートを GitHub Release に記載
6. `package.json` のバージョンを更新

---

## 過去のフェーズ

### Phase 1: 基盤構築

- プロジェクトスキャフォールディング
- CI/CD パイプライン設定
- SQLite スキーマ設計

### Phase 2: データ層実装

- Repository 実装（Workspace, Tag, Settings）
- StorageService 実装
- Settings DTO

### Phase 3: キャプチャ機能実装

- GDI+ BitBlt 画面キャプチャ
- キャプチャオーケストレーター
- サムネイル生成
- ウィンドウ列挙

### Phase 4: エディタ基盤実装

- SkiaSharp ベース画像アノテーション
- Arrow, Rectangle, Text, Mosaic, Crop
- Undo/Redo

### Phase 5: 周辺機能実装

- グローバルショートカット
- システムトレイ
- クリップボード
- 通知
- クリップボードへの画像コピー

### Phase 6: 切り替え・廃止

- MAUI 版を正式版へ昇格
- 旧 Tauri/Rust 実装の凍結
- 移行ガイド・リリースノート作成
