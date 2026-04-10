# 1. GitHub Issue本文テンプレ付き Backlog

以下は **GitHub Issues / Azure DevOps / Backlog / Jira** のどれにも流用しやすいように、
**テンプレ → 実Issue例** の順で書いています。

## 1-1. Issue本文テンプレート

### テンプレ（Feature用）

```md
## 概要
<!-- 何を実装するIssueかを1〜3行で -->

## 背景
<!-- なぜ必要か -->

## 実装内容
- [ ]
- [ ]
- [ ]

## 非対象
-
-

## 完了条件（Acceptance Criteria）
- [ ]
- [ ]
- [ ]

## テスト観点
- [ ]
- [ ]
- [ ]

## 備考
<!-- 参考設計、依存Issue、注意点など -->
```

### テンプレ（Bugfix用）

```md
## 事象
<!-- 何が起きているか -->

## 再現手順
1.
2.
3.

## 期待結果
<!-- 本来どうあるべきか -->

## 原因候補
-
-

## 修正方針
- [ ]
- [ ]

## 完了条件
- [ ] 再現手順で不具合が発生しない
- [ ] 既存機能に影響がない
- [ ] 関連テストを追加または更新した
```

### テンプレ（Refactor用）

```md
## 目的
<!-- なぜリファクタするか -->

## 対象
-
-

## 方針
- [ ]
- [ ]

## 完了条件
- [ ] 振る舞いが変わらない
- [ ] 依存関係が明確になっている
- [ ] テストが通る
```

## 1-2. Milestone / Epic 構成

* **M1: Foundation**
* **M2: Workspace MVP**
* **M3: Capture MVP**
* **M4: Editor MVP**
* **M5: UX Enhancement**
* **M6: Video Capture**
* **M7: Advanced Features**

## 1-3. 実Issue一覧（MVP中心）

## Epic: Foundation

### Issue #1: ソリューション初期構成を作成する

**Labels**: `epic:foundation`, `type:setup`, `priority:highest`

```md
## 概要
WPF + MVVM + SQLite を前提に、アプリ全体のソリューション構成とプロジェクト雛形を作成する。

## 背景
後続のAI Agent実装で責務分離を崩さないため、最初にプロジェクト構成を固定する必要がある。

## 実装内容
- [ ] Solution を作成する
- [ ] App / Application / Domain / Infrastructure / Capture / Editor / Workspace / Shared / Tests を作成する
- [ ] DI の初期設定を行う
- [ ] 共通ログ出力の基盤を追加する
- [ ] appsettings.json の読み込みを実装する

## 非対象
- 実機能の詳細実装
- 動画録画
- OCR

## 完了条件（Acceptance Criteria）
- [ ] アプリが起動する
- [ ] DI で Service / Repository / ViewModel が解決できる
- [ ] ログファイルが出力される
- [ ] 設定値を読み込める

## テスト観点
- [ ] 初回起動で例外が出ない
- [ ] 設定未作成時でもデフォルト値で動作する
- [ ] ログ出力先が自動作成される
```

### Issue #2: SQLite初期化とスキーマ適用を実装する

**Labels**: `epic:foundation`, `type:data`, `priority:highest`

```md
## 概要
SQLite DB の初期作成と、workspace_items などの初期テーブル作成処理を実装する。

## 背景
ワークスペース機能を最初から必須とするため、DBスキーマを早期に固定したい。

## 実装内容
- [ ] DBファイル作成
- [ ] スキーマ適用処理
- [ ] マイグレーション方針の決定
- [ ] 初回起動時の自動初期化

## 非対象
- ORM導入の高度化
- DBマイグレーションUI

## 完了条件（Acceptance Criteria）
- [ ] 初回起動でDBが自動生成される
- [ ] 必須テーブルが作成される
- [ ] workspace_items に対して CRUD が動作する

## テスト観点
- [ ] DB未存在時に正常作成される
- [ ] 既存DBがある場合は再作成されない
- [ ] 外部キー制約が有効である
```

### Issue #3: ローカル保存ディレクトリ構造を実装する

**Labels**: `epic:foundation`, `type:storage`, `priority:high`

```md
## 概要
画像原本・編集済み・サムネイル・動画を保存するためのローカルディレクトリ構造を作成する。

## 背景
ワークスペース、サムネイル、編集済みファイルを扱うために保存ルールを統一する必要がある。

## 実装内容
- [ ] 保存ルートを決定する
- [ ] originals / edited / thumbnails / videos を自動作成する
- [ ] パス解決サービスを実装する

## 完了条件（Acceptance Criteria）
- [ ] アプリ起動時に必要なディレクトリが作成される
- [ ] 原本保存と編集済み保存の置き場が分離される
- [ ] サムネイルパスが一意に決まる
```

## Epic: Workspace MVP

### Issue #4: ワークスペース一覧画面を実装する

**Labels**: `epic:workspace`, `type:feature`, `priority:highest`

```md
## 概要
撮影済み画像や動画を一覧表示するワークスペース画面を作成する。

## 背景
このアプリは「キャプチャツール」ではなく「ワークスペース中心のキャプチャ管理ツール」として設計する。

## 実装内容
- [ ] サムネイル一覧表示
- [ ] 作成日時表示
- [ ] 種別表示（Image / Video）
- [ ] 選択状態の表示
- [ ] 空状態UIの表示

## 完了条件（Acceptance Criteria）
- [ ] DBから取得したアイテムが一覧表示される
- [ ] サムネイルが表示される
- [ ] データ0件時に空状態UIが表示される
- [ ] 一覧から1件選択できる

## テスト観点
- [ ] 100件程度でスクロール表示が破綻しない
- [ ] 画像と動画で表示崩れしない
- [ ] サムネイルがない場合でもフォールバック表示される
```

### Issue #5: ワークスペース詳細ペインを実装する

**Labels**: `epic:workspace`, `type:feature`, `priority:high`

```md
## 概要
選択したアイテムの詳細情報と操作ボタンを右ペインまたは下ペインに表示する。

## 実装内容
- [ ] プレビュー表示
- [ ] タイトル表示
- [ ] 作成日時表示
- [ ] 保存先パス表示
- [ ] コピー / 削除 / 保存先を開く ボタン

## 完了条件（Acceptance Criteria）
- [ ] 一覧選択と連動して詳細が切り替わる
- [ ] タイトル変更できる
- [ ] 保存先フォルダを開ける
```

### Issue #6: ワークスペースで削除・名前変更を実装する

**Labels**: `epic:workspace`, `type:feature`, `priority:high`

```md
## 概要
一覧からアイテムを削除したり、表示名を変更したりできるようにする。

## 実装内容
- [ ] 削除操作
- [ ] 名前変更操作
- [ ] DB更新
- [ ] 実ファイル整合性維持

## 完了条件（Acceptance Criteria）
- [ ] 削除時に一覧から消える
- [ ] DBから削除される
- [ ] 実ファイルも削除される（またはごみ箱移動の設計に従う）
- [ ] 名前変更が再起動後も保持される
```

### Issue #7: お気に入り機能を実装する

**Labels**: `epic:workspace`, `type:feature`, `priority:medium`

```md
## 概要
アイテムをお気に入り登録できるようにする。

## 完了条件（Acceptance Criteria）
- [ ] お気に入りON/OFFできる
- [ ] 一覧に状態が表示される
- [ ] 永続化される
```

### Issue #8: タグ付け機能を実装する

**Labels**: `epic:workspace`, `type:feature`, `priority:medium`

```md
## 概要
アイテムにタグを付与し、後で絞り込めるようにする。

## 完了条件（Acceptance Criteria）
- [ ] 複数タグを付与できる
- [ ] タグで絞り込みできる
- [ ] タグが永続化される
```

## Epic: Capture MVP

### Issue #9: 全画面キャプチャを実装する

**Labels**: `epic:capture`, `type:feature`, `priority:highest`

```md
## 概要
現在のデスクトップ全体を画像として保存する。

## 実装内容
- [ ] 全画面取得
- [ ] 原本保存
- [ ] サムネイル生成
- [ ] ワークスペース登録

## 完了条件（Acceptance Criteria）
- [ ] 全画面の画像が保存される
- [ ] 一覧に自動追加される
- [ ] サムネイルが生成される
```

### Issue #10: 指定領域キャプチャを実装する

**Labels**: `epic:capture`, `type:feature`, `priority:highest`

```md
## 概要
半透明オーバーレイ上でドラッグ選択した範囲を保存する。

## 実装内容
- [ ] 半透明オーバーレイ表示
- [ ] ドラッグで範囲選択
- [ ] Enter確定またはマウスアップ確定
- [ ] Escキャンセル
- [ ] 保存と登録

## 完了条件（Acceptance Criteria）
- [ ] 任意範囲をドラッグ選択できる
- [ ] 選択範囲のみが保存される
- [ ] Esc で中断できる
```

### Issue #11: ウィンドウキャプチャを実装する

**Labels**: `epic:capture`, `type:feature`, `priority:high`

```md
## 概要
アクティブウィンドウまたは選択ウィンドウ単位でキャプチャする。

## 完了条件（Acceptance Criteria）
- [ ] ウィンドウ単位で画像保存できる
- [ ] ワークスペースへ登録される
- [ ] 影や境界の扱いが破綻しない
```

### Issue #12: グローバルホットキーを実装する

**Labels**: `epic:capture`, `type:feature`, `priority:highest`

```md
## 概要
アプリが非アクティブでもホットキーからキャプチャを起動できるようにする。

## 例
- PrintScreen: 範囲キャプチャ
- Ctrl + PrintScreen: 全画面
- Alt + PrintScreen: ウィンドウ

## 完了条件（Acceptance Criteria）
- [ ] 非アクティブ時でも動作する
- [ ] ホットキー重複時の失敗が検知できる
- [ ] 各操作に応じたキャプチャが起動する
```

### Issue #13: タスクトレイ常駐を実装する

**Labels**: `epic:capture`, `type:feature`, `priority:high`

```md
## 概要
タスクトレイからキャプチャ操作やメイン画面表示を行えるようにする。

## 完了条件（Acceptance Criteria）
- [ ] 最小化でトレイ常駐できる
- [ ] トレイメニューから範囲 / 全画面 / ウィンドウが起動できる
- [ ] 終了できる
```

### Issue #14: クリップボードコピーを実装する

**Labels**: `epic:capture`, `type:feature`, `priority:high`

```md
## 概要
画像をクリップボードへコピーし、TeamsやOffice等へ貼り付けられるようにする。

## 完了条件（Acceptance Criteria）
- [ ] キャプチャ直後にコピーできる
- [ ] ワークスペースから再コピーできる
- [ ] 一般的なWindowsアプリへ貼り付けできる
```

## Epic: Editor MVP

### Issue #15: 画像編集画面を実装する

**Labels**: `epic:editor`, `type:feature`, `priority:high`

```md
## 概要
画像に対して注釈やトリミングを行う編集画面を作成する。

## 実装内容
- [ ] 画像表示
- [ ] ズーム
- [ ] パン
- [ ] ツールバー
- [ ] 保存
- [ ] コピー

## 完了条件（Acceptance Criteria）
- [ ] 画像を開ける
- [ ] ツールバーから各ツールを選択できる
- [ ] 保存できる
```

### Issue #16: 矢印ツールを実装する

**Labels**: `epic:editor`, `type:feature`, `priority:highest`

```md
## 完了条件（Acceptance Criteria）
- [ ] ドラッグで矢印を配置できる
- [ ] 保存結果に反映される
- [ ] 最低限の色/太さ設定ができる
```

### Issue #17: テキストツールを実装する

**Labels**: `epic:editor`, `type:feature`, `priority:highest`

```md
## 完了条件（Acceptance Criteria）
- [ ] テキストを追加できる
- [ ] 文字サイズ変更ができる
- [ ] 保存結果に反映される
```

### Issue #18: 四角形ツールを実装する

**Labels**: `epic:editor`, `type:feature`, `priority:high`

```md
## 完了条件（Acceptance Criteria）
- [ ] 四角形を描画できる
- [ ] 輪郭色が設定できる
- [ ] 保存結果に反映される
```

### Issue #19: モザイクツールを実装する

**Labels**: `epic:editor`, `type:feature`, `priority:highest`

```md
## 完了条件（Acceptance Criteria）
- [ ] 指定領域へモザイク適用できる
- [ ] 保存結果に反映される
- [ ] 個人情報隠し用途として最低限実用的である
```

### Issue #20: トリミングを実装する

**Labels**: `epic:editor`, `type:feature`, `priority:high`

```md
## 完了条件（Acceptance Criteria）
- [ ] 任意範囲を切り抜ける
- [ ] 保存後に画像サイズが更新される
```

### Issue #21: Undo / Redo を実装する

**Labels**: `epic:editor`, `type:feature`, `priority:high`

```md
## 完了条件（Acceptance Criteria）
- [ ] 直前操作を取り消せる
- [ ] 再実行できる
- [ ] 矢印 / テキスト / 四角 / モザイク / トリミング に対応する
```

## 1-4. 推奨Sprint分割

### Sprint 1

* \#1 ソリューション初期構成
* \#2 SQLite初期化
* \#3 保存ディレクトリ構造
* \#4 ワークスペース一覧画面（枠だけでも）

### Sprint 2

* \#9 全画面キャプチャ
* \#10 指定領域キャプチャ
* \#14 クリップボードコピー
* \#4 一覧への自動反映

### Sprint 3

* \#11 ウィンドウキャプチャ
* \#12 グローバルホットキー
* \#13 タスクトレイ
* \#5 詳細ペイン
* \#6 削除・名前変更

### Sprint 4

* \#15 編集画面
* \#16 矢印
* \#17 テキスト
* \#18 四角形
* \#19 モザイク
* \#20 トリミング

### Sprint 5

* \#21 Undo / Redo
* \#7 お気に入り
* \#8 タグ
* UX改善系

# 2. SQLite の CREATE TABLE 文

以下は **MVP〜拡張初期** を見据えた SQLite スキーマです。
**画像/動画/タグ/設定/最近使った操作** まで含めています。

## 2-1. DDL 一式

```sql
PRAGMA foreign_keys = ON;

-- ----------------------------
-- workspace_items
-- ----------------------------
CREATE TABLE IF NOT EXISTS workspace_items (
    id TEXT PRIMARY KEY,
    item_type TEXT NOT NULL CHECK (item_type IN ('image', 'video')),
    title TEXT,
    original_path TEXT NOT NULL,
    current_path TEXT NOT NULL,
    thumbnail_path TEXT,
    file_extension TEXT,
    mime_type TEXT,
    width INTEGER,
    height INTEGER,
    duration_ms INTEGER,
    file_size_bytes INTEGER,
    is_favorite INTEGER NOT NULL DEFAULT 0 CHECK (is_favorite IN (0, 1)),
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    deleted_at TEXT,
    metadata_json TEXT
);

CREATE INDEX IF NOT EXISTS idx_workspace_items_created_at
    ON workspace_items(created_at DESC);

CREATE INDEX IF NOT EXISTS idx_workspace_items_item_type
    ON workspace_items(item_type);

CREATE INDEX IF NOT EXISTS idx_workspace_items_is_favorite
    ON workspace_items(is_favorite);

CREATE INDEX IF NOT EXISTS idx_workspace_items_title
    ON workspace_items(title);

-- ----------------------------
-- tags
-- ----------------------------
CREATE TABLE IF NOT EXISTS tags (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_tags_name
    ON tags(name);

-- ----------------------------
-- workspace_item_tags
-- ----------------------------
CREATE TABLE IF NOT EXISTS workspace_item_tags (
    workspace_item_id TEXT NOT NULL,
    tag_id TEXT NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    PRIMARY KEY (workspace_item_id, tag_id),
    FOREIGN KEY (workspace_item_id) REFERENCES workspace_items(id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_workspace_item_tags_tag_id
    ON workspace_item_tags(tag_id);

-- ----------------------------
-- app_settings
-- ----------------------------
CREATE TABLE IF NOT EXISTS app_settings (
    key TEXT PRIMARY KEY,
    value TEXT,
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

-- ----------------------------
-- capture_presets
-- ----------------------------
CREATE TABLE IF NOT EXISTS capture_presets (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    capture_mode TEXT NOT NULL CHECK (
        capture_mode IN ('region', 'fullscreen', 'window', 'last_region', 'delayed')
    ),
    image_format TEXT NOT NULL CHECK (image_format IN ('png', 'jpg', 'webp')),
    delay_seconds INTEGER NOT NULL DEFAULT 0,
    include_cursor INTEGER NOT NULL DEFAULT 0 CHECK (include_cursor IN (0, 1)),
    auto_copy_to_clipboard INTEGER NOT NULL DEFAULT 1 CHECK (auto_copy_to_clipboard IN (0, 1)),
    save_to_workspace INTEGER NOT NULL DEFAULT 1 CHECK (save_to_workspace IN (0, 1)),
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

-- ----------------------------
-- recent_actions
-- ----------------------------
CREATE TABLE IF NOT EXISTS recent_actions (
    id TEXT PRIMARY KEY,
    action_type TEXT NOT NULL,
    target_item_id TEXT,
    payload_json TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (target_item_id) REFERENCES workspace_items(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_recent_actions_created_at
    ON recent_actions(created_at DESC);

CREATE INDEX IF NOT EXISTS idx_recent_actions_target_item_id
    ON recent_actions(target_item_id);

-- ----------------------------
-- editor_documents
-- 将来の非破壊編集用
-- ----------------------------
CREATE TABLE IF NOT EXISTS editor_documents (
    id TEXT PRIMARY KEY,
    workspace_item_id TEXT NOT NULL,
    document_json TEXT NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (workspace_item_id) REFERENCES workspace_items(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_editor_documents_workspace_item_id
    ON editor_documents(workspace_item_id);

-- ----------------------------
-- trigger: updated_at 更新
-- ----------------------------
CREATE TRIGGER IF NOT EXISTS trg_workspace_items_updated_at
AFTER UPDATE ON workspace_items
FOR EACH ROW
BEGIN
    UPDATE workspace_items
    SET updated_at = datetime('now')
    WHERE id = OLD.id;
END;

CREATE TRIGGER IF NOT EXISTS trg_app_settings_updated_at
AFTER UPDATE ON app_settings
FOR EACH ROW
BEGIN
    UPDATE app_settings
    SET updated_at = datetime('now')
    WHERE key = OLD.key;
END;

CREATE TRIGGER IF NOT EXISTS trg_capture_presets_updated_at
AFTER UPDATE ON capture_presets
FOR EACH ROW
BEGIN
    UPDATE capture_presets
    SET updated_at = datetime('now')
    WHERE id = OLD.id;
END;

CREATE TRIGGER IF NOT EXISTS trg_editor_documents_updated_at
AFTER UPDATE ON editor_documents
FOR EACH ROW
BEGIN
    UPDATE editor_documents
    SET updated_at = datetime('now')
    WHERE id = OLD.id;
END;
```

## 2-2. 初期設定データ例

```sql
INSERT OR IGNORE INTO app_settings (key, value) VALUES
('storage.root', ''),
('capture.default_format', 'png'),
('capture.auto_copy_to_clipboard', 'true'),
('capture.include_cursor', 'false'),
('app.start_minimized', 'false'),
('app.minimize_to_tray', 'true'),
('workspace.sort_by', 'created_at_desc');

INSERT OR IGNORE INTO capture_presets (
    id, name, capture_mode, image_format, delay_seconds, include_cursor, auto_copy_to_clipboard, save_to_workspace
) VALUES
('preset-region-default', 'Region Default', 'region', 'png', 0, 0, 1, 1),
('preset-fullscreen-default', 'Fullscreen Default', 'fullscreen', 'png', 0, 0, 1, 1),
('preset-window-default', 'Window Default', 'window', 'png', 0, 0, 1, 1);
```

## 2-3. MVPで最低限使うSQL例

### 一覧取得

```sql
SELECT
    id,
    item_type,
    title,
    current_path,
    thumbnail_path,
    created_at,
    is_favorite
FROM workspace_items
WHERE deleted_at IS NULL
ORDER BY datetime(created_at) DESC;
```

### 1件追加

```sql
INSERT INTO workspace_items (
    id,
    item_type,
    title,
    original_path,
    current_path,
    thumbnail_path,
    file_extension,
    mime_type,
    width,
    height,
    file_size_bytes,
    metadata_json
) VALUES (
    @Id,
    @ItemType,
    @Title,
    @OriginalPath,
    @CurrentPath,
    @ThumbnailPath,
    @FileExtension,
    @MimeType,
    @Width,
    @Height,
    @FileSizeBytes,
    @MetadataJson
);
```

### 論理削除（おすすめ）

```sql
UPDATE workspace_items
SET deleted_at = datetime('now')
WHERE id = @Id;
```

# 3. WPF のソリューション雛形（クラス一覧付き）

ここは **WPF + MVVM + SQLite + ローカル完結** を前提に、
**AI Agentが迷いにくい構造**にしてあります。

## 3-1. ソリューション構成

```text
AmeCaptureWorkspace.sln
 ├─ AmeCapture.App
 ├─ AmeCapture.Application
 ├─ AmeCapture.Domain
 ├─ AmeCapture.Infrastructure
 ├─ AmeCapture.Capture
 ├─ AmeCapture.Editor
 ├─ AmeCapture.Workspace
 ├─ AmeCapture.Shared
 └─ AmeCapture.Tests
```

## 3-2. 各プロジェクトの役割

### 1) `AmeCapture.App`

**役割**

* WPFアプリ本体
* View / ViewModel
* DI初期化
* Window管理
* タスクトレイ
* 画面遷移

**フォルダ構成**

```text
AmeCapture.App
 ├─ App.xaml
 ├─ App.xaml.cs
 ├─ Bootstrapper/
 │   ├─ ServiceCollectionExtensions.cs
 │   └─ AppHostBuilder.cs
 ├─ Views/
 │   ├─ MainWindow.xaml
 │   ├─ MainWindow.xaml.cs
 │   ├─ WorkspaceView.xaml
 │   ├─ WorkspaceView.xaml.cs
 │   ├─ EditorWindow.xaml
 │   ├─ EditorWindow.xaml.cs
 │   ├─ SettingsWindow.xaml
 │   ├─ SettingsWindow.xaml.cs
 │   ├─ RegionOverlayWindow.xaml
 │   └─ RegionOverlayWindow.xaml.cs
 ├─ ViewModels/
 │   ├─ MainWindowViewModel.cs
 │   ├─ WorkspaceViewModel.cs
 │   ├─ WorkspaceItemViewModel.cs
 │   ├─ EditorViewModel.cs
 │   ├─ SettingsViewModel.cs
 │   └─ RegionOverlayViewModel.cs
 ├─ Commands/
 │   ├─ RelayCommand.cs
 │   └─ AsyncRelayCommand.cs
 ├─ Converters/
 │   ├─ BoolToVisibilityConverter.cs
 │   ├─ NullToVisibilityConverter.cs
 │   └─ ItemTypeToIconConverter.cs
 ├─ Behaviors/
 │   └─ DragDropBehavior.cs
 ├─ Resources/
 │   ├─ Colors.xaml
 │   ├─ Styles.xaml
 │   └─ Icons/
 └─ Services/
     ├─ WindowNavigationService.cs
     └─ DialogService.cs
```

### 2) `AmeCapture.Application`

**役割**

* UseCase
* Serviceインターフェース
* DTO
* Command / Query
* アプリケーションロジック

**フォルダ構成**

```text
AmeCapture.Application
 ├─ Interfaces/
 │   ├─ ICaptureService.cs
 │   ├─ IWorkspaceRepository.cs
 │   ├─ ITagRepository.cs
 │   ├─ ISettingsRepository.cs
 │   ├─ IThumbnailService.cs
 │   ├─ IFileStorageService.cs
 │   ├─ IClipboardService.cs
 │   ├─ IGlobalHotkeyService.cs
 │   ├─ ITrayIconService.cs
 │   └─ IImageEditorService.cs
 ├─ DTOs/
 │   ├─ CaptureResultDto.cs
 │   ├─ WorkspaceItemDto.cs
 │   └─ TagDto.cs
 ├─ UseCases/
 │   ├─ Capture/
 │   │   ├─ CaptureFullScreenUseCase.cs
 │   │   ├─ CaptureRegionUseCase.cs
 │   │   └─ CaptureWindowUseCase.cs
 │   ├─ Workspace/
 │   │   ├─ GetWorkspaceItemsUseCase.cs
 │   │   ├─ RenameWorkspaceItemUseCase.cs
 │   │   ├─ DeleteWorkspaceItemUseCase.cs
 │   │   ├─ ToggleFavoriteUseCase.cs
 │   │   └─ AddTagToWorkspaceItemUseCase.cs
 │   └─ Editor/
 │       ├─ SaveEditedImageUseCase.cs
 │       └─ CopyImageToClipboardUseCase.cs
 ├─ Commands/
 │   ├─ CaptureRegionCommand.cs
 │   ├─ DeleteWorkspaceItemCommand.cs
 │   └─ RenameWorkspaceItemCommand.cs
 └─ Queries/
     ├─ GetWorkspaceItemsQuery.cs
     └─ GetWorkspaceItemDetailQuery.cs
```

### 3) `AmeCapture.Domain`

**役割**

* エンティティ
* Enum
* ValueObject
* ドメインルール

**フォルダ構成**

```text
AmeCapture.Domain
 ├─ Entities/
 │   ├─ WorkspaceItem.cs
 │   ├─ Tag.cs
 │   ├─ CapturePreset.cs
 │   ├─ AppSetting.cs
 │   └─ EditorDocument.cs
 ├─ Enums/
 │   ├─ WorkspaceItemType.cs
 │   ├─ CaptureMode.cs
 │   └─ AnnotationType.cs
 ├─ ValueObjects/
 │   ├─ FilePath.cs
 │   ├─ ImageSize.cs
 │   └─ CaptureRegion.cs
 └─ Exceptions/
     └─ DomainValidationException.cs
```

**主要クラス例**

```csharp
public enum WorkspaceItemType
{
    Image = 1,
    Video = 2
}

public class WorkspaceItem
{
    public Guid Id { get; set; }
    public WorkspaceItemType ItemType { get; set; }
    public string? Title { get; set; }
    public string OriginalPath { get; set; } = string.Empty;
    public string CurrentPath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? MetadataJson { get; set; }
}
```

### 4) `AmeCapture.Infrastructure`

**役割**

* SQLite実装
* ファイル保存
* ログ
* 設定
* OS連携
* Hotkey / Tray

**フォルダ構成**

```text
AmeCapture.Infrastructure
 ├─ Persistence/
 │   ├─ SqliteConnectionFactory.cs
 │   ├─ SqliteInitializer.cs
 │   └─ SqlScripts/
 │       ├─ 001_initial.sql
 │       └─ seed.sql
 ├─ Repositories/
 │   ├─ WorkspaceRepository.cs
 │   ├─ TagRepository.cs
 │   └─ SettingsRepository.cs
 ├─ Storage/
 │   ├─ FileStorageService.cs
 │   ├─ ThumbnailService.cs
 │   └─ PathResolver.cs
 ├─ Logging/
 │   └─ SerilogConfigurator.cs
 ├─ Configuration/
 │   ├─ AppSettings.cs
 │   └─ SettingsLoader.cs
 ├─ Clipboard/
 │   └─ ClipboardService.cs
 ├─ Interop/
 │   ├─ NativeMethods.cs
 │   ├─ GlobalHotkeyService.cs
 │   ├─ TrayIconService.cs
 │   └─ WindowHandleService.cs
 └─ Imaging/
     └─ ImageMetadataReader.cs
```

### 5) `AmeCapture.Capture`

**役割**

* スクリーンショット取得
* オーバーレイ選択
* ウィンドウ列挙
* マルチディスプレイ対応

**フォルダ構成**

```text
AmeCapture.Capture
 ├─ Services/
 │   ├─ ScreenCaptureService.cs
 │   ├─ RegionSelectionService.cs
 │   └─ WindowCaptureService.cs
 ├─ Models/
 │   ├─ CaptureRequest.cs
 │   ├─ CaptureResult.cs
 │   └─ WindowInfo.cs
 ├─ Interop/
 │   ├─ MonitorInterop.cs
 │   └─ WindowInterop.cs
 └─ Utilities/
     └─ ScreenCoordinateConverter.cs
```

### 6) `AmeCapture.Editor`

**役割**

* 画像編集
* 注釈ツール
* Undo / Redo
* レンダリング
* エクスポート

**フォルダ構成**

```text
AmeCapture.Editor
 ├─ Models/
 │   ├─ EditDocument.cs
 │   ├─ AnnotationBase.cs
 │   ├─ ArrowAnnotation.cs
 │   ├─ TextAnnotation.cs
 │   ├─ RectangleAnnotation.cs
 │   ├─ MosaicAnnotation.cs
 │   └─ CropOperation.cs
 ├─ Tools/
 │   ├─ IEditorTool.cs
 │   ├─ ArrowTool.cs
 │   ├─ TextTool.cs
 │   ├─ RectangleTool.cs
 │   ├─ MosaicTool.cs
 │   └─ CropTool.cs
 ├─ History/
 │   ├─ IUndoableAction.cs
 │   ├─ UndoRedoManager.cs
 │   ├─ AddAnnotationAction.cs
 │   └─ CropImageAction.cs
 ├─ Rendering/
 │   ├─ EditorRenderer.cs
 │   └─ SkiaImageEditorService.cs
 └─ Services/
     └─ ImageEditorService.cs
```

### 7) `AmeCapture.Workspace`

**役割**

* ワークスペース一覧ロジック
* フィルタ / ソート / 検索
* お気に入り / タグ

**フォルダ構成**

```text
AmeCapture.Workspace
 ├─ Services/
 │   ├─ WorkspaceQueryService.cs
 │   ├─ WorkspaceFilterService.cs
 │   └─ WorkspaceCommandService.cs
 ├─ Models/
 │   ├─ WorkspaceFilter.cs
 │   └─ WorkspaceSortOption.cs
 └─ Utilities/
     └─ WorkspaceGroupingHelper.cs
```

### 8) `AmeCapture.Shared`

**役割**

* 共通ユーティリティ
* Result型
* 例外
* 拡張メソッド

```text
AmeCapture.Shared
 ├─ Results/
 │   ├─ Result.cs
 │   └─ ResultT.cs
 ├─ Exceptions/
 │   └─ AppException.cs
 ├─ Extensions/
 │   ├─ StringExtensions.cs
 │   └─ DateTimeExtensions.cs
 └─ Utilities/
     └─ Guard.cs
```

### 9) `AmeCapture.Tests`

**役割**

* Unit Test
* Repository Test
* UseCase Test

```text
AmeCapture.Tests
 ├─ Application/
 ├─ Infrastructure/
 ├─ Capture/
 ├─ Editor/
 └─ Workspace/
```

## 3-3. MVPで最初に作るべきクラス一覧

### 絶対最初に必要

* `App.xaml`
* `MainWindow.xaml`
* `MainWindowViewModel.cs`
* `WorkspaceView.xaml`
* `WorkspaceViewModel.cs`
* `WorkspaceItemViewModel.cs`
* `SqliteInitializer.cs`
* `SqliteConnectionFactory.cs`
* `WorkspaceRepository.cs`
* `FileStorageService.cs`
* `ThumbnailService.cs`
* `ScreenCaptureService.cs`
* `CaptureRegionUseCase.cs`
* `CaptureFullScreenUseCase.cs`
* `ClipboardService.cs`
* `RelayCommand.cs`
* `AsyncRelayCommand.cs`

### 次点

* `GlobalHotkeyService.cs`
* `TrayIconService.cs`
* `EditorWindow.xaml`
* `EditorViewModel.cs`
* `ArrowTool.cs`
* `TextTool.cs`
* `MosaicTool.cs`
* `UndoRedoManager.cs`

# 4. AI Agentに段階投入するための実装プロンプト集

ここがかなり重要です。
AI Agentに雑に「全部作って」だと崩れやすいので、**段階ごとに小さく閉じた価値**で依頼します。

以下は **コピペ用プロンプト** です。

## 4-1. 共通の最上位プロンプト

```text
あなたはC# / WPF / MVVM / SQLite に詳しいシニアエンジニアです。
Windows向けのローカル完結型スクリーンキャプチャアプリを実装します。

前提:
- WPF
- .NET 8
- MVVM
- SQLite
- ローカルファイル保存
- クラウド連携なし
- ワークスペース機能を最初から必須とする
- Screenpressoライクだが、MVPでは必要最小限の機能から段階実装する

重要ルール:
- 1回の出力で大きく作りすぎない
- 必ず責務を分離する
- UI / ViewModel / Application / Infrastructure の責務を混ぜない
- 先に interface を定義し、後から実装クラスを追加する
- 生成コードにはファイルパスごとの出力を含める
- 各ステップで、作成ファイル一覧 / 実装内容 / 次のステップを明記する
- 可能なら最小限のテストも追加する
```

## 4-2. Sprint 1 用プロンプト（基盤 + ワークスペース土台）

```text
以下を実装してください。

ゴール:
- WPFアプリを起動できる
- SQLite初期化できる
- ワークスペース一覧画面の骨組みが表示できる
- workspace_items を読み込める構造を作る

今回の作業対象:
1. Solution / Project 構成
2. DI初期化
3. SQLite 初期化
4. workspace_items テーブルのスキーマ適用
5. MainWindow / WorkspaceView の雛形
6. WorkspaceRepository
7. WorkspaceViewModel
8. ダミーデータまたは実DBから一覧表示

出力形式:
- 作成ファイル一覧
- 各ファイルのコード
- 必要な NuGet パッケージ
- 実行手順
- 次にやるべきタスク
```

## 4-3. Sprint 2 用プロンプト（キャプチャMVP）

```text
既存のWPF + MVVM + SQLite構成に対して、キャプチャMVPを追加してください。

ゴール:
- 全画面キャプチャ
- 指定領域キャプチャ
- 保存
- サムネイル生成
- ワークスペース登録
- クリップボードコピー

実装対象:
1. ICaptureService
2. ScreenCaptureService
3. CaptureFullScreenUseCase
4. CaptureRegionUseCase
5. RegionOverlayWindow
6. IFileStorageService / FileStorageService
7. IThumbnailService / ThumbnailService
8. IClipboardService / ClipboardService
9. キャプチャ後に workspace_items へ登録する処理
10. ワークスペース一覧の自動更新

要件:
- 保存先は originals / thumbnails を使う
- ViewModel から直接 DB を触らない
- キャンセル時は何も保存しない
- Esc キーで領域選択を中断できる

出力形式:
- 変更対象ファイル一覧
- 新規追加ファイル一覧
- 各ファイルのコード
- 実装上の注意点
- テスト観点
```

## 4-4. Sprint 3 用プロンプト（ホットキー + トレイ + 詳細ペイン）

```text
既存アプリに以下を追加してください。

ゴール:
- グローバルホットキーでキャプチャ起動
- タスクトレイ常駐
- ワークスペース詳細ペイン
- 削除 / 名前変更

実装対象:
1. IGlobalHotkeyService / GlobalHotkeyService
2. ITrayIconService / TrayIconService
3. MainWindow の最小化時トレイ動作
4. WorkspaceDetail 表示用 ViewModel
5. RenameWorkspaceItemUseCase
6. DeleteWorkspaceItemUseCase
7. 保存先フォルダを開く機能

要件:
- PrintScreen = 範囲キャプチャ
- Ctrl + PrintScreen = 全画面
- Alt + PrintScreen = ウィンドウキャプチャ（未実装ならTODOでも可）
- 削除時はDBと実ファイルの整合性を保つ
- 名前変更は再起動後も保持される

出力形式:
- 変更ファイル一覧
- 追加ファイル一覧
- 各コード
- Windows特有の注意点
```

## 4-5. Sprint 4 用プロンプト（編集MVP）

```text
既存アプリに画像編集機能のMVPを追加してください。

ゴール:
- 画像編集画面を開ける
- 矢印
- テキスト
- 四角形
- モザイク
- トリミング
- 保存

実装対象:
1. EditorWindow
2. EditorViewModel
3. EditDocument
4. AnnotationBase
5. ArrowTool
6. TextTool
7. RectangleTool
8. MosaicTool
9. CropTool
10. ImageEditorService
11. SaveEditedImageUseCase

要件:
- 元画像は originals に残す
- 編集後は edited に保存する
- workspace_items.current_path を更新する
- 将来の Undo / Redo を考慮した構造にする
- 描画と保存処理の責務を分離する

出力形式:
- 追加/変更ファイル一覧
- 各コード
- 実装上の制約
- 今回未対応の点
```

## 4-6. Sprint 5 用プロンプト（Undo/Redo + UX改善）

```text
既存の編集機能に対して、Undo / Redo と基本UX改善を実装してください。

ゴール:
- Undo / Redo
- お気に入り
- タグ
- 保存形式設定
- 前回領域の再利用用下地

実装対象:
1. UndoRedoManager
2. IUndoableAction
3. AddAnnotationAction
4. CropImageAction
5. ToggleFavoriteUseCase
6. TagRepository
7. AddTagToWorkspaceItemUseCase
8. AppSettings の読み書き
9. 保存形式設定UI

要件:
- 直前操作を取り消せる
- 少なくとも注釈追加とトリミングを履歴化する
- 設定変更は app_settings に永続化する
```

## 4-7. Bugfix 用プロンプト

```text
以下の不具合を修正してください。

不具合:
[ここに事象を書く]

前提:
- WPF
- MVVM
- SQLite
- AmeCaptureWorkspace 構成

修正方針:
- 原因分析
- 最小修正
- 既存設計を崩さない
- 影響範囲を明示する

出力形式:
1. 原因分析
2. 修正対象ファイル
3. 修正コード
4. 再発防止策
5. テスト観点
```

## 4-8. Refactor 用プロンプト

```text
以下のコードをリファクタしてください。

目的:
- 責務分離の改善
- テストしやすさ向上
- ViewModel肥大化の解消

対象:
[対象クラスやコードを貼る]

条件:
- 振る舞いは変えない
- interface 抽出を優先する
- 依存方向を整理する
- 変更理由をコメントで説明する

出力形式:
1. 問題点
2. 改善方針
3. 変更コード
4. 追加すべきテスト
```

## 4-9. テスト追加用プロンプト

```text
以下の実装に対するテストを追加してください。

対象:
[対象コード]

前提:
- xUnit
- 可能な限り unit test を優先
- Infrastructure 依存は最小化

必要な観点:
- 正常系
- 異常系
- 境界値
- 永続化の整合性

出力形式:
1. テスト観点一覧
2. テストコード
3. モック方針
```
