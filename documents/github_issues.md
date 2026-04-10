# AmeCapture - GitHub Issues 登録用テキスト

このファイルの内容を使って GitHub Issues を手動登録してください。
Milestones と Labels は github_issues_setup.txt を参照してください。

---
---

# Issue #1: ソリューション初期構成を作成する

**Labels:** epic:foundation, type:setup, priority:highest
**Milestone:** M1: Foundation

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

---
---

# Issue #2: SQLite初期化とスキーマ適用を実装する

**Labels:** epic:foundation, type:data, priority:highest
**Milestone:** M1: Foundation

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

---
---

# Issue #3: ローカル保存ディレクトリ構造を実装する

**Labels:** epic:foundation, type:storage, priority:high
**Milestone:** M1: Foundation

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

---
---

# Issue #4: ワークスペース一覧画面を実装する

**Labels:** epic:workspace, type:feature, priority:highest
**Milestone:** M2: Workspace MVP

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

---
---

# Issue #5: ワークスペース詳細ペインを実装する

**Labels:** epic:workspace, type:feature, priority:high
**Milestone:** M2: Workspace MVP

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

---
---

# Issue #6: ワークスペースで削除・名前変更を実装する

**Labels:** epic:workspace, type:feature, priority:high
**Milestone:** M2: Workspace MVP

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

---
---

# Issue #7: お気に入り機能を実装する

**Labels:** epic:workspace, type:feature, priority:medium
**Milestone:** M2: Workspace MVP

## 概要

アイテムをお気に入り登録できるようにする。

## 完了条件（Acceptance Criteria）

- [ ] お気に入りON/OFFできる
- [ ] 一覧に状態が表示される
- [ ] 永続化される

---
---

# Issue #8: タグ付け機能を実装する

**Labels:** epic:workspace, type:feature, priority:medium
**Milestone:** M2: Workspace MVP

## 概要

アイテムにタグを付与し、後で絞り込めるようにする。

## 完了条件（Acceptance Criteria）

- [ ] 複数タグを付与できる
- [ ] タグで絞り込みできる
- [ ] タグが永続化される

---
---

# Issue #9: 全画面キャプチャを実装する

**Labels:** epic:capture, type:feature, priority:highest
**Milestone:** M3: Capture MVP

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

---
---

# Issue #10: 指定領域キャプチャを実装する

**Labels:** epic:capture, type:feature, priority:highest
**Milestone:** M3: Capture MVP

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

---
---

# Issue #11: ウィンドウキャプチャを実装する

**Labels:** epic:capture, type:feature, priority:high
**Milestone:** M3: Capture MVP

## 概要

アクティブウィンドウまたは選択ウィンドウ単位でキャプチャする。

## 完了条件（Acceptance Criteria）

- [ ] ウィンドウ単位で画像保存できる
- [ ] ワークスペースへ登録される
- [ ] 影や境界の扱いが破綻しない

---
---

# Issue #12: グローバルホットキーを実装する

**Labels:** epic:capture, type:feature, priority:highest
**Milestone:** M3: Capture MVP

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

---
---

# Issue #13: タスクトレイ常駐を実装する

**Labels:** epic:capture, type:feature, priority:high
**Milestone:** M3: Capture MVP

## 概要

タスクトレイからキャプチャ操作やメイン画面表示を行えるようにする。

## 完了条件（Acceptance Criteria）

- [ ] 最小化でトレイ常駐できる
- [ ] トレイメニューから範囲 / 全画面 / ウィンドウが起動できる
- [ ] 終了できる

---
---

# Issue #14: クリップボードコピーを実装する

**Labels:** epic:capture, type:feature, priority:high
**Milestone:** M3: Capture MVP

## 概要

画像をクリップボードへコピーし、TeamsやOffice等へ貼り付けられるようにする。

## 完了条件（Acceptance Criteria）

- [ ] キャプチャ直後にコピーできる
- [ ] ワークスペースから再コピーできる
- [ ] 一般的なWindowsアプリへ貼り付けできる

---
---

# Issue #15: 画像編集画面を実装する

**Labels:** epic:editor, type:feature, priority:high
**Milestone:** M4: Editor MVP

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

---
---

# Issue #16: 矢印ツールを実装する

**Labels:** epic:editor, type:feature, priority:highest
**Milestone:** M4: Editor MVP

## 完了条件（Acceptance Criteria）

- [ ] ドラッグで矢印を配置できる
- [ ] 保存結果に反映される
- [ ] 最低限の色/太さ設定ができる

---
---

# Issue #17: テキストツールを実装する

**Labels:** epic:editor, type:feature, priority:highest
**Milestone:** M4: Editor MVP

## 完了条件（Acceptance Criteria）

- [ ] テキストを追加できる
- [ ] 文字サイズ変更ができる
- [ ] 保存結果に反映される

---
---

# Issue #18: 四角形ツールを実装する

**Labels:** epic:editor, type:feature, priority:high
**Milestone:** M4: Editor MVP

## 完了条件（Acceptance Criteria）

- [ ] 四角形を描画できる
- [ ] 輪郭色が設定できる
- [ ] 保存結果に反映される

---
---

# Issue #19: モザイクツールを実装する

**Labels:** epic:editor, type:feature, priority:highest
**Milestone:** M4: Editor MVP

## 完了条件（Acceptance Criteria）

- [ ] 指定領域へモザイク適用できる
- [ ] 保存結果に反映される
- [ ] 個人情報隠し用途として最低限実用的である

---
---

# Issue #20: トリミングを実装する

**Labels:** epic:editor, type:feature, priority:high
**Milestone:** M4: Editor MVP

## 完了条件（Acceptance Criteria）

- [ ] 任意範囲を切り抜ける
- [ ] 保存後に画像サイズが更新される

---
---

# Issue #21: Undo / Redo を実装する

**Labels:** epic:editor, type:feature, priority:high
**Milestone:** M4: Editor MVP

## 完了条件（Acceptance Criteria）

- [ ] 直前操作を取り消せる
- [ ] 再実行できる
- [ ] 矢印 / テキスト / 四角 / モザイク / トリミング に対応する

---
---

# Issue #22: 遅延キャプチャを実装する

**Labels:** epic:ux, type:feature, priority:medium
**Milestone:** M5: UX Enhancement

## 完了条件（Acceptance Criteria）

- [ ] 3秒/5秒/10秒などから選べる
- [ ] カウントダウン後にキャプチャされる

---
---

# Issue #23: 前回と同じ領域で再撮影を実装する

**Labels:** epic:ux, type:feature, priority:high
**Milestone:** M5: UX Enhancement

## 完了条件（Acceptance Criteria）

- [ ] 前回領域を保持する
- [ ] ワンクリックで同じ範囲を再撮影できる

---
---

# Issue #24: 保存形式設定を実装する

**Labels:** epic:ux, type:feature, priority:medium
**Milestone:** M5: UX Enhancement

## 完了条件（Acceptance Criteria）

- [ ] PNG/JPG/WebPから選択できる
- [ ] 次回起動後も設定が保持される

---
---

# Issue #25: 画面録画（MP4）を実装する

**Labels:** epic:video, type:feature, priority:medium
**Milestone:** M6: Video Capture

## 完了条件（Acceptance Criteria）

- [ ] 録画開始/停止ができる
- [ ] MP4で保存される
- [ ] ワークスペースへ登録される

---
---

# Issue #26: マイク音声録音を実装する

**Labels:** epic:video, type:feature, priority:medium
**Milestone:** M6: Video Capture

---
---

# Issue #27: OCRを実装する

**Labels:** epic:advanced, type:feature, priority:low
**Milestone:** M7: Advanced Features

---
---

# Issue #28: スクロールキャプチャを実装する

**Labels:** epic:advanced, type:feature, priority:low
**Milestone:** M7: Advanced Features

---
---

# Issue #29: 外部エディタ連携を実装する

**Labels:** epic:advanced, type:feature, priority:low
**Milestone:** M7: Advanced Features

---
---

# Sprint 分割（参考）

## Sprint 1 (M1: Foundation)

- #1 ソリューション初期構成
- #2 SQLite初期化
- #3 保存ディレクトリ構造
- #4 ワークスペース一覧画面（枠だけでも）

## Sprint 2 (M3: Capture MVP)

- #9 全画面キャプチャ
- #10 指定領域キャプチャ
- #14 クリップボードコピー
- #4 一覧への自動反映

## Sprint 3 (M2+M3: Workspace + Capture)

- #11 ウィンドウキャプチャ
- #12 グローバルホットキー
- #13 タスクトレイ
- #5 詳細ペイン
- #6 削除・名前変更

## Sprint 4 (M4: Editor MVP)

- #15 編集画面
- #16 矢印
- #17 テキスト
- #18 四角形
- #19 モザイク
- #20 トリミング

## Sprint 5 (M4+M5: Editor + UX)

- #21 Undo / Redo
- #7 お気に入り
- #8 タグ
- UX改善系
