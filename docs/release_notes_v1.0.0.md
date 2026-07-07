# トークン節約お守りツール v1.0.0

Claude Code の開発環境を見守り、トークン消費を抑えるための Windows 常駐型ツールです。
初回リリースです。

## 変更点

- **CLAUDE.md ヘルスチェック**: 行数・サイズ(既定 200行 / 25KB)を監視し、使用率をバー表示。70%超で警告色。
- **セッションログ監視**: ファイルサイズ(既定 5MB)を監視し、`/compact` 実行の境界を追跡。
- **RTK / ccusage / claude-mem 連携**: 節約実績・消費トークンの表示と、右クリックメニューからのワンクリック導入/削除。
- **トレイ常駐**: ×ボタンでトレイに格納(設定で無効化可)。健康状態に応じてアイコンの色が変化し、しきい値超過時はトースト通知。
- **多言語対応**: 右クリックメニューの「Language / 言語」から日本語 / English を切り替え可能(再起動不要)。
- **お守りモチーフ**: トレイアイコン・アプリアイコン・小窓の外見を、和の意匠(常盤色・琥珀色・生成り)で統一。
- **カンパ導線**: 累計節約トークンのマイルストーン到達時に、控えめな応援バナーを表示(任意)。

## 動作環境

- Windows 10 バージョン 1809 以降 / Windows 11
- .NET ランタイムのインストールは不要(自己完結型 EXE)
- ccusage 機能を利用する場合のみ Node.js 20+ が必要

## インストール方法

1. 下記の ZIP をダウンロード
2. 書き込み可能な任意のフォルダ(`Program Files` 以外を推奨)に展開
3. `TokenOmamoriTool.exe` を実行

初回実行時に Windows SmartScreen の警告が表示される場合があります。コード署名を行っていないための表示で、
「詳細情報」→「実行」で起動できます。詳細は README の「動作環境」節をご参照ください。

## 既知の問題

- RTK は Bash ツール経由のコマンドにのみ介入するため、Windows + .NET 系開発など PowerShell 中心の環境では
  節約効果が出にくいことがあります(README のトラブルシューティング節を参照)。
- `rtk discover` が Windows 環境でセッションログを検出しない場合があります(RTK 側の既知の制約)。
- GitHub Sponsors によるご支援は近日対応予定です。現在は Ko-fi のみご利用いただけます。

## SHA256

```
42ed75f8920d7472f06c23836de88cf3782337d15f9e2c50442ae7b24a594fd3  TokenOmamoriTool-v1.0.0-win-x64.zip
```

(添付の `TokenOmamoriTool-v1.0.0-win-x64.zip.sha256.txt` と同内容)

## 支援について

このツールが役に立ったら、[Ko-fi](https://ko-fi.com/lranslot) から開発を応援していただけると嬉しいです。
支援の有無で機能に差はありません。

---

# Token Omamori Tool v1.0.0

A Windows tray-resident companion for Claude Code that helps you keep token consumption under
control. This is the initial release.

## Changes

- **CLAUDE.md health check**: Monitors line count and file size (default 200 lines / 25 KB) with
  a usage bar; turns amber above 70%.
- **Session log monitoring**: Tracks session log size (default 5 MB) and tracks `/compact`
  boundaries.
- **RTK / ccusage / claude-mem integration**: Shows token savings and consumption, with one-click
  install/uninstall from the context menu.
- **Tray residency**: Closing the window hides it to the tray (configurable). The tray icon
  changes color with your environment's health and raises a toast notification when a threshold
  is crossed.
- **Multi-language support**: Switch between 日本語 / English from the "Language / 言語"
  context-menu item, with instant UI updates (no restart needed).
- **Omamori (charm) theme**: A cohesive Japanese-inspired look across the tray icon, app icon, and
  status window (tokiwa green, amber, and washi-paper tones).
- **Support banner**: An unobtrusive banner appears when your cumulative token savings hit a
  milestone (optional).

## Requirements

- Windows 10 version 1809 or later / Windows 11
- No .NET runtime installation required (self-contained EXE)
- Node.js 20+ only if you use the ccusage feature

## Installation

1. Download the ZIP below
2. Extract to any writable folder (avoid `Program Files`)
3. Run `TokenOmamoriTool.exe`

Windows SmartScreen may show a warning on first run, since this build isn't code-signed. Choose
"More info" → "Run anyway" to proceed. See the README's Requirements section for details.

## Known issues

- RTK only intercepts commands run through the Bash tool, so environments where Claude Code
  favors PowerShell (e.g. Windows + .NET development) may see little RTK savings — see the
  README's troubleshooting section.
- `rtk discover` may fail to detect session logs on Windows (a known RTK-side limitation).
- GitHub Sponsors support is coming soon. Only Ko-fi is available at this time.

## SHA256

```
42ed75f8920d7472f06c23836de88cf3782337d15f9e2c50442ae7b24a594fd3  TokenOmamoriTool-v1.0.0-win-x64.zip
```

(same as the attached `TokenOmamoriTool-v1.0.0-win-x64.zip.sha256.txt`)

## Support the project

If this tool helps you, consider supporting development via [Ko-fi](https://ko-fi.com/lranslot).
All features are free regardless of support.
