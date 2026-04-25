# AmeCapture 移行ガイド — Tauri/Rust 版から .NET MAUI 版へ

## 1. 概要

本ドキュメントは、AmeCapture を Tauri/Rust 版から .NET MAUI 版へ移行するための手順書です。両バージョン間のデータ互換性について説明し、設定移行およびロールバック手順を記載します。

## 2. データ互換方針

### 2.1 自動互換（ユーザー操作不要）

| 項目                       | 方針     | 詳細                                                                                 |
| -------------------------- | -------- | ------------------------------------------------------------------------------------ |
| **SQLite スキーマ**        | 完全互換 | 同一 `CREATE TABLE IF NOT EXISTS` 定義。両バージョンで同じテーブル・インデックス構成 |
| **PRAGMA 設定**            | 完全互換 | `journal_mode=WAL`, `foreign_keys=ON` を両バージョンで使用                           |
| **画像ファイル形式**       | 完全互換 | PNG/JPG 形式は変更なし                                                               |
| **サムネイル命名規則**     | 完全互換 | `{name}_thumb.{ext}` 形式を両バージョンで使用                                        |
| **ファイルストレージ構造** | 完全互換 | `originals/`, `edited/`, `thumbnails/`, `videos/` の構成を維持                       |
| **アプリ識別子**           | 同一     | `com.amecapture.app`                                                                 |

### 2.2 手動対応が必要な項目

| 項目                   | 説明                                      | 対応方法                           |
| ---------------------- | ----------------------------------------- | ---------------------------------- |
| **データベースのパス** | 旧版と新版で DB ファイル位置が異なる      | 手動コピーまたはシンボリックリンク |
| **設定データ**         | 旧版は JSON ファイル + DB、新版は DB のみ | 手動移行（下記手順参照）           |
| **ログファイル**       | フォーマット・パスが変更                  | 移行不要（旧ログは残置）           |

### 2.3 非互換（仕様変更）

| 項目             | 旧版 (Tauri)             | 新版 (MAUI)                   |
| ---------------- | ------------------------ | ----------------------------- |
| ログフォーマット | tracing 構造化ログ       | Serilog テキスト形式          |
| アーキテクチャ   | マルチプロセス (WebView) | シングルプロセス (ネイティブ) |

## 3. データ保存場所

### 3.1 旧版 (Tauri/Rust) のパス

| データ         | パス                                            |
| -------------- | ----------------------------------------------- |
| データベース   | `%APPDATA%\com.amecapture.app\amecapture.db`    |
| 設定ファイル   | `%APPDATA%\com.amecapture.app\appsettings.json` |
| ログ           | `%APPDATA%\com.amecapture.app\logs\`            |
| キャプチャ画像 | `%USERPROFILE%\Pictures\AmeCapture\` (既定)     |

### 3.2 新版 (.NET MAUI) のパス

| データ       | パス                                             |
| ------------ | ------------------------------------------------ |
| データベース | `%LOCALAPPDATA%\AmeCapture\amecapture.db`        |
| ストレージ   | `%LOCALAPPDATA%\AmeCapture\data\`                |
| ログ         | `%LOCALAPPDATA%\AmeCapture\logs\amecapture-.log` |

## 4. 設定移行手順

### 4.1 旧版の設定を確認

旧版の設定は以下のいずれかに保存されています:

1. **appsettings.json**（ファイルベース）
   - 場所: `%APPDATA%\com.amecapture.app\appsettings.json`
   - 形式: JSON (`camelCase` キー)

2. **app_settings テーブル**（DB ベース）
   - 場所: `%APPDATA%\com.amecapture.app\amecapture.db`
   - キー: `save_path`, `image_format`, `start_minimized`, `hotkey_capture_region`, `hotkey_capture_fullscreen`, `hotkey_capture_window`

### 4.2 新版へのデータベース移行

新版と旧版は同じ SQLite スキーマを使用しています。旧版のデータベースを新版の場所にコピーすることで、すべてのキャプチャ履歴・タグ・設定を移行できます。

#### 手順

```powershell
# 1. 新版のデータディレクトリを作成
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\AmeCapture"

# 2. 旧版のデータベースをコピー
Copy-Item "$env:APPDATA\com.amecapture.app\amecapture.db" "$env:LOCALAPPDATA\AmeCapture\amecapture.db"

# 3. （オプション）旧版のキャプチャ画像の保存先が既定以外の場合、
#    新版の設定で保存先を変更するか、データを新しい場所にコピー
```

### 4.3 設定の個別移行（データベースコピーをしない場合）

1. 新版 AmeCapture を起動（自動的に DB とディレクトリが作成される）
2. 新版の設定画面で以下を旧版と同じ値に設定:

| 設定項目             | 旧版のキー                  | 新版の項目 | 既定値                           |
| -------------------- | --------------------------- | ---------- | -------------------------------- |
| 保存先パス           | `save_path`                 | 保存先     | `%LOCALAPPDATA%\AmeCapture\data` |
| 画像形式             | `image_format`              | 画像形式   | `png`                            |
| 範囲キャプチャ       | `hotkey_capture_region`     | ホットキー | `Ctrl+Shift+S`                   |
| 全画面キャプチャ     | `hotkey_capture_fullscreen` | ホットキー | `Ctrl+Shift+F`                   |
| ウィンドウキャプチャ | `hotkey_capture_window`     | ホットキー | `Ctrl+Shift+W`                   |

### 4.4 キャプチャ画像の移行

旧版のキャプチャ画像を新版のストレージに移行する場合:

```powershell
# 1. 新版のストレージディレクトリを作成（存在しない場合）
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\AmeCapture\data\originals"
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\AmeCapture\data\edited"
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\AmeCapture\data\thumbnails"
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\AmeCapture\data\videos"

# 2. 旧版のキャプチャ画像を新版にコピー（保存先が異なる場合）
# 例: 旧版の保存先が Pictures\AmeCapture の場合
Copy-Item -Recurse "$env:USERPROFILE\Pictures\AmeCapture\originals\*" "$env:LOCALAPPDATA\AmeCapture\data\originals\"
Copy-Item -Recurse "$env:USERPROFILE\Pictures\AmeCapture\edited\*" "$env:LOCALAPPDATA\AmeCapture\data\edited\"
Copy-Item -Recurse "$env:USERPROFILE\Pictures\AmeCapture\thumbnails\*" "$env:LOCALAPPDATA\AmeCapture\data\thumbnails\"
```

**注意**: データベースをコピーした場合、`workspace_items` テーブルの `original_path`, `current_path`, `thumbnail_path` は旧パスのままです。新しいストレージにファイルを移動した場合は、DB 内のパスも更新する必要があります。ファイルを元の場所に残すか、新しい場所にコピーして DB のパスを更新してください。

#### DB 内パスの更新例

ファイルを新しいストレージに移動した場合、SQLite ツール（`sqlite3` コマンドなど）で以下の SQL を実行してパスを更新します:

```sql
-- 旧パスのプレフィックスと新パスのプレフィックスを定義して一括更新
-- 例: C:\Users\<user>\Pictures\AmeCapture → C:\Users\<user>\AppData\Local\AmeCapture\data

UPDATE workspace_items
SET original_path = REPLACE(original_path,
    'C:\Users\<ユーザー名>\Pictures\AmeCapture',
    'C:\Users\<ユーザー名>\AppData\Local\AmeCapture\data')
WHERE original_path LIKE 'C:\Users\<ユーザー名>\Pictures\AmeCapture%';

UPDATE workspace_items
SET current_path = REPLACE(current_path,
    'C:\Users\<ユーザー名>\Pictures\AmeCapture',
    'C:\Users\<ユーザー名>\AppData\Local\AmeCapture\data')
WHERE current_path LIKE 'C:\Users\<ユーザー名>\Pictures\AmeCapture%';

UPDATE workspace_items
SET thumbnail_path = REPLACE(thumbnail_path,
    'C:\Users\<ユーザー名>\Pictures\AmeCapture',
    'C:\Users\<ユーザー名>\AppData\Local\AmeCapture\data')
WHERE thumbnail_path LIKE 'C:\Users\<ユーザー名>\Pictures\AmeCapture%';

-- 更新結果を確認
SELECT id, original_path, current_path, thumbnail_path FROM workspace_items LIMIT 10;
```

> `<ユーザー名>` は実際の Windows ユーザー名に置き換えてください。PowerShell では `$env:USERPROFILE` で確認できます。

## 5. ロールバック手順

新版から旧版に戻す必要がある場合の手順です。

### 5.1 旧版へのロールバック

```powershell
# 1. 新版 AmeCapture を終了

# 2. 新版のデータをバックアップ
Compress-Archive -Path "$env:LOCALAPPDATA\AmeCapture" -DestinationPath "$env:USERPROFILE\Desktop\AmeCapture_MAUI_backup.zip"

# 3. 旧版 (Tauri) をインストール・起動
# 旧版は独自のデータディレクトリ (%APPDATA%\com.amecapture.app\) を使用するため、
# 新版で作成されたデータは影響を受けません

# 4. 旧版でキャプチャしたデータはそのまま旧版の DB に残っています
```

### 5.2 データベースのロールバック

新版で作成されたキャプチャデータを旧版に戻す場合:

1. 新版の DB (`%LOCALAPPDATA%\AmeCapture\amecapture.db`) を SQLite ツールで開く
2. 新版で追加されたレコードをエクスポート（SQL 形式）
3. 旧版の DB (`%APPDATA%\com.amecapture.app\amecapture.db`) にインポート
4. ファイルパスが異なる場合は、`workspace_items` テーブルのパス列を更新

### 5.3 並行稼働

旧版と新版は**異なるデータディレクトリ**を使用するため、一時的に両方をインストールして並行稼働が可能です。ただし、同じ SQLite ファイルを同時に開くことは推奨されません（WAL ロックの競合）。

## 6. 移行後の確認項目

- [ ] 新版が正常に起動すること
- [ ] キャプチャ履歴が表示されること（DB 移行時）
- [ ] ホットキーが正常に動作すること
- [ ] システムトレイに常駐すること
- [ ] 各種キャプチャ（全画面/範囲/ウィンドウ）が動作すること
- [ ] クリップボードへのコピーが動作すること
- [ ] 画像編集が動作すること
- [ ] タグ管理が動作すること

## 7. 旧版のアンインストール

移行が完了し、旧版が不要になった場合:

1. 旧版 AmeCapture (Tauri) をアンインストール
2. 旧版のデータをバックアップ後に削除（オプション）:

```powershell
# バックアップ
Compress-Archive -Path "$env:APPDATA\com.amecapture.app" -DestinationPath "$env:USERPROFILE\Desktop\AmeCapture_Tauri_backup.zip"

# 削除（バックアップ後に実行）
Remove-Item -Recurse -Force "$env:APPDATA\com.amecapture.app"
```

3. 旧版のキャプチャ画像（`Pictures\AmeCapture\` など）は必要に応じて残置または削除

## 8. 参考ドキュメント

- [DB スキーマ解析と C# 移行互換方針](5.%20DB%E3%82%B9%E3%82%AD%E3%83%BC%E3%83%9E%E8%A7%A3%E6%9E%90%E3%81%A8C%23%E7%A7%BB%E8%A1%8C%E4%BA%92%E6%8F%9B%E6%96%B9%E9%87%9D.md)
- [リリースノート](ReleaseNotes.md)
- [旧版の凍結に関する通知](../src-tauri/FROZEN.md)
