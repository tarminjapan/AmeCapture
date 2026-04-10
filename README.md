# AmeCapture

Windows向けのローカル完結型スクリーンキャプチャアプリ。
Screenpressoライクですが、クラウド連携は実装しません。
ワークスペース機能による撮影画像の管理・編集に特化しています。

## 前提

- Windows 10 / 11
- .NET 9 SDK
- SQLite

## ソリューション構成

```text
AmeCapture.sln
 ├─ AmeCapture.App                 // WPFアプリ本体 (View / ViewModel)
 ├─ AmeCapture.Application         // UseCase, Serviceインターフェース, DTO
 ├─ AmeCapture.Domain              // Entity, Enum, ValueObject
 ├─ AmeCapture.Infrastructure      // SQLite, FileStorage, Logging, OS連携
 ├─ AmeCapture.Capture             // 画面キャプチャ関連
 ├─ AmeCapture.Editor              // 画像編集関連
 ├─ AmeCapture.Workspace           // ワークスペース関連
 ├─ AmeCapture.Shared              // 共通ユーティリティ
 └─ AmeCapture.Tests               // テスト (xUnit)
```

## コマンド

### 復元

```shell
dotnet restore AmeCapture.sln
```

### ビルド

```shell
dotnet build AmeCapture.sln
```

### リリースビルド

```shell
dotnet build AmeCapture.sln -c Release
```

### 実行

```shell
dotnet run --project AmeCapture.App
```

### テスト

```shell
dotnet test AmeCapture.Tests
```

### テスト（詳細出力）

```shell
dotnet test AmeCapture.Tests --verbosity normal
```

## 保存先

```text
%LocalAppData%/AmeCapture/
 ├─ data/
 │   ├─ originals/      // キャプチャ原本
 │   ├─ edited/         // 編集済み画像
 │   ├─ thumbnails/     // サムネイル
 │   └─ videos/         // 動画（将来）
 ├─ db/                 // SQLiteデータベース
 ├─ logs/               // ログファイル
 └─ settings/           // 設定ファイル
```

## 技術スタック

| 項目 | 技術 |
| - | - |
| UI | WPF (.NET 9) |
| アーキテクチャ | MVVM |
| DB | SQLite |
| 画像処理 | SkiaSharp（予定） |
| ログ | Serilog（予定） |
| テスト | xUnit |
| DI | Microsoft.Extensions.DependencyInjection |

## ライセンス

このプロジェクトのライセンスについては LICENSE ファイルを参照してください。
