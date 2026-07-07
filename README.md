# トークン節約お守りツール (Token Omamori Tool)

*日本語 | [English](#english)*

**⬇️ ダウンロード: [最新版はこちら](https://github.com/lranslot/TokenOmamoriTool/releases/latest)**

Claude Code の開発環境を見守り、トークン消費を抑えるための Windows 常駐型ツールです。
CLAUDE.md やセッションログの肥大化を検知し、RTK・ccusage・claude-mem といった
節約系ツールの導入/削除・利用状況の確認をワンストップで行えます。

詳細な仕様は [`docs/`](docs/) 以下を参照してください。コードと仕様書の内容が
食い違う場合は `docs/` の内容が正となります。

## 方針

- AI自身の応答品質を落として節約することはしない(I/O・メモリ層の最適化のみ)
- ツール自体はトークンを消費しない(測定のためにAPIを呼ばない)
- Caveman / kizami / Token Optimizer・Savior / Context Mode は導入しない(理由は仕様書§4.3)

## 機能一覧

- **CLAUDE.md ヘルスチェック** — 行数・サイズを定期チェックし、上限(既定 200行 / 25KB)に
  対する使用率をバー表示。70%超で警告色。
- **セッションログ監視** — ファイルサイズを監視し、上限(既定 5MB)に対する使用率をバー表示。
  `/compact` 実行の境界を追跡。
- **RTK 連携** — `rtk gain` を解析し本日分/累計の節約トークン数を表示。右クリックメニューから
  インストール/アンインストール可能。
- **ccusage 連携** — 本日の消費トークン数を表示(Node.js 20+ が必要)。表示される金額($)は
  ccusage によるAPI従量課金換算の参考値で、定額プラン(Pro/Max等)の場合は実際の請求額では
  ありません。
- **claude-mem 連携** — 導入状態の確認とインストール/アンインストール。導入時は低トークン設定を
  自動適用。
- **トレイ常駐** — ×ボタンで閉じるとトレイに格納(設定で無効化可)。トレイアイコンは
  健康状態で色が変わり、ホバーでサマリーをツールチップ表示。しきい値超過時はトースト通知。
- **言語切替** — 右クリックメニューの「Language / 言語」から日本語 / English を選択可能。
- **CLAUDE.md ダイエット指示のコピー / /compact のコピー** — 肥大化検知時にワンクリックで
  対処用テキストをクリップボードへ。

ウィンドウは画面右下に固定表示される小さなステータスパネル(幅300px)です。

## 効く環境・効かない環境

本ツールの監視機能(CLAUDE.md・セッションログの健康チェック)は環境を問わず動作します。
環境に依存するのは主に RTK 連携による節約効果です。

| 環境 | 監視 | RTK節約 | 総合 |
| --- | :-: | :-: | --- |
| Web系(Node/TS等)・Bashツール経由 | ◎ | ◎ | フル性能 |
| Ruby / Python 系 | ◎ | ○〜◎ | かなり効く |
| .NET / WPF(PowerShell中心) | ◎ | × | 監視・記憶が主 |
| 小規模・短期・使い捨てプロジェクト | △ | △ | 恩恵は薄い |

- 冗長な出力(`git status`/`diff`、テストログ、依存インストール)が多い大きめのリポジトリほど
  RTK の節約が伸びます。
- 長期プロジェクトほど claude-mem の記憶層が効きます。

.NET 系で RTK が効きにくい理由と対処の詳細は[トラブルシューティング](#トラブルシューティング)を
参照してください。

## 右クリックメニューの説明

メインウィンドウの右クリックとトレイアイコンの右クリックで、**完全に同じ構成**のメニューが
表示されます。上から順に:

1. **CLAUDE.mdダイエット指示をコピー**
   CLAUDE.md の使用率が70%未満の間はグレーアウトして無効。70%以上になると有効化され、
   クリックするとdocsへの分割を促す指示文(現在の行数・サイズを埋め込み)をクリップボードに
   コピーします。
2. **CLAUDE.mdのフォルダを開く**
   監視対象プロジェクトが1つならそのままエクスプローラーを開きます。複数ある場合は
   サブメニューにプロジェクト名の一覧が表示されます。
3. **/compact をコピー**
   セッションログの使用率が70%以上の場合に警告色になります。クリックで `/compact` を
   クリップボードにコピーします。
4. **ツール管理**(サブメニュー)
   - **RTKをインストール / アンインストール** — 現在の導入状態に応じて表示が切り替わります。
   - **claude-memをインストール / アンインストール** — 同様に導入状態で切り替わります。
     `claude` CLI が見つからない場合は「claude CLIが見つかりません」と表示され無効化されます。

   いずれも選択すると手順を1ステップずつ実行する専用ウィンドウが開きます(失敗時は
   失敗したステップから再試行できます)。
5. **Language / 言語**(サブメニュー)
   「日本語」「English」から選択します。選択中の言語にチェックが付き、切り替えは再起動なしで
   UI全体に即時反映されます。このサブメニューのラベルだけは、誤って切り替えても戻れるよう
   常に日英併記です。
6. **設定**
   設定ウィンドウを開きます。詳細は次項。
7. **説明**
   ヘルプウィンドウを開きます(末尾にカンパページへのリンクがあります)。
8. **☕ 開発を応援**
   カンパページを既定ブラウザで開きます。
9. **終了**(セパレータの下)
   アプリを完全終了します。ウィンドウ側・トレイ側どちらの「終了」も、トレイ格納ではなく
   完全終了です(×ボタンはトレイ格納)。

このほか、RTKの累計節約トークン数が 10万 / 100万 / 1000万 に到達すると、小窓内に一度だけ
応援バナーが表示されます(閉じたマイルストーンは二度と表示されません。通知トーストは出ません)。

## 設定項目の意味

設定は `settings.json`(実行ファイルと同じディレクトリ)に保存され、起動時に読み込まれます。
ファイルが存在しない場合は、このリポジトリ自身(`.sln` があるディレクトリ)を監視対象とする
デフォルト設定が自動生成されます。

右クリックメニューの「設定」から開ける設定ウィンドウでは、以下の項目を編集できます。

| 設定ウィンドウの項目 | settings.json 上のキー | 既定値 | 意味 |
| --- | --- | --- | --- |
| CLAUDE.md 行数上限 | `instructionFile.maxLines` | 200 | CLAUDE.md がこの行数を超えると警告対象 |
| CLAUDE.md サイズ上限 (KB) | `instructionFile.maxSizeKB` | 25 | CLAUDE.md がこのサイズを超えると警告対象 |
| セッションログ上限 (MB) | `sessionLog.maxSizeBytes`(MB単位で表示) | 5 | セッションログがこのサイズを超えると警告対象 |
| 監視間隔 (秒) | `pollIntervalSeconds` | 5 | CLAUDE.md・セッションログのチェック間隔 |
| 外部コマンド間隔 (秒) | `externalTools.pollIntervalSeconds` | 60 | `rtk gain` / `ccusage` を再実行する間隔 |
| ×ボタンでトレイに格納する | `tray.hideOnClose` | true | false にすると×ボタンで従来どおり終了 |
| 警告をトースト通知する | `tray.toastOnWarning` | true | false にするとアイコン色の変化のみ |

その他、設定ウィンドウには表示されない項目:

- `language`(`"ja"` / `"en"`) — 表示言語。右クリックメニューの「Language / 言語」から
  変更します。初回起動時はOSのUI言語から自動判定されます。
- `targetAi`(既定 `"claude"`) — 監視対象のAIツール。将来の複数AI対応用で、v1では常に `claude`。
- `instructionFile.path`(既定 `"CLAUDE.md"`) — 監視対象ファイル名。
- `sessionLog.configDirOverride` — セッションログの設定ディレクトリを変更したい場合に指定。
- `projects` — 監視対象プロジェクトの一覧(`name` と `path` のペア)。
- `tray.firstHideNoticeShown` — 初回トレイ格納時の案内を表示済みかどうか(内部フラグ)。
- `donation.shownMilestones` — 表示済みの応援バナーのマイルストーン(内部フラグ)。

設定ウィンドウの「初期値に戻す」ボタンで、上の表の項目を既定値にリセットできます
(言語と内部フラグはリセットされません)。

## インストール(配布版)

1. [GitHub Releases](../../releases) から `TokenOmamoriTool-vX.Y.Z-win-x64.zip` をダウンロード
2. **書き込み可能なフォルダ**(例: ドキュメント配下や任意の作業フォルダ)に展開
3. `TokenOmamoriTool.exe` を実行

- `Program Files` 配下への展開は**非推奨**です(設定を EXE と同じ場所の `settings.json` に
  保存するため、書き込みできず動作しません)。
- 自己完結型 EXE のため、.NET ランタイムのインストールは不要です。

### 初回実行時の SmartScreen 警告について

本アプリはコード署名をしていないため、初回実行時に Windows SmartScreen の警告
(「Windows によって PC が保護されました」)が表示されることがあります。
**「詳細情報」→「実行」** で起動できます。ソースコードは本リポジトリで全て公開されており、
`tools/publish.ps1` で同一の EXE を自分でビルドして再現することもできます。

### セキュリティソフトの誤検知について

自己完結型の単一ファイル EXE は、まれにセキュリティソフトに誤検知されることがあります。
その場合は展開先フォルダを除外設定に追加するか、リポジトリから自前でビルドしてください。

## 動作環境

- Windows 10 バージョン 1809 以降 / Windows 11
- .NET 8 ランタイム(自己完結型EXEを使う場合は不要)
- ccusage 機能を使う場合: Node.js 20+

## トラブルシューティング

### RTK の節約が伸びない / セッションが検出されない(Windows)

- Claude Code がコマンドを **PowerShell ツール経由**で実行した場合、RTK のフック
  (Bash ツールのみ対応)は介入できないため、節約トークン数が伸びないことがあります。
- `rtk discover` が Windows でセッションを検出しない事象を確認しています。

いずれも RTK 側の制約であり、本ツールの表示(`rtk gain` の解析結果)は正確です。
特に Windows + .NET 系の開発では Claude Code がコマンドを PowerShell 経由で実行しやすく、
RTK(Bash ツールのみ対応)の効果が出にくくなります。`dotnet` コマンドの出力を
`-v q -nologo` で絞るなど、指示ファイル(CLAUDE.md)側での節約が有効です。

## ビルド・実行

```powershell
dotnet build TokenOmamoriTool.sln
dotnet run --project TokenOmamoriTool/TokenOmamoriTool.csproj
dotnet test
```

対象フレームワーク: `net8.0-windows10.0.17763.0`(WPF, nullable有効)。

## 支援について ☕

このツールが役に立ったら、開発を応援していただけると嬉しいです。

- **Ko-fi(単発の投げ銭)**: https://ko-fi.com/lranslot
- **GitHub Sponsors**: <!-- TODO: https://github.com/sponsors/XXXX -->

### Ko-fi での支援方法(3ステップ)

1. 上の Ko-fi リンクを開く
2. 金額を選ぶ(コーヒー1杯分から)
3. カード情報を入力して送信 — アカウント登録は不要です

ページは英語ですが、操作は金額を選んでカードを入力するだけです。
<!-- TODO: スクリーンショット追加 -->

いただいた支援は開発・維持の励みになります。支援の有無で機能に差はありません。

---

# English

**⬇️ Download: [Latest release](https://github.com/lranslot/TokenOmamoriTool/releases/latest)**

**Token Omamori Tool** ("omamori" = a Japanese protective charm) is a Windows tray-resident
companion for Claude Code that helps you keep token consumption under control. It watches for
CLAUDE.md and session-log bloat, and gives you one-stop install/uninstall and status monitoring
for token-saving tools: RTK, ccusage, and claude-mem.

Detailed specifications live under [`docs/`](docs/) (Japanese). If the code and the docs ever
disagree, the docs win.

## Principles

- Never save tokens by degrading the AI's own responses — only I/O and memory-layer optimization
- The tool itself consumes zero tokens (no API calls for measurement)
- Deliberately out of scope: Caveman, kizami, Token Optimizer/Savior, Context Mode (see spec §4.3)

## Features

- **CLAUDE.md health check** — monitors line count and file size against limits
  (default 200 lines / 25 KB) with a usage bar; turns amber above 70%.
- **Session log monitoring** — tracks session log size against a limit (default 5 MB),
  `/compact`-boundary aware.
- **RTK integration** — parses `rtk gain` to show today's and cumulative token savings;
  install/uninstall from the context menu.
- **ccusage integration** — shows today's token consumption (requires Node.js 20+). The dollar
  amount shown is a reference value computed by ccusage at pay-as-you-go API rates — on a
  flat-rate plan (Pro/Max etc.) it is not your actual bill.
- **claude-mem integration** — status check and install/uninstall, with low-token settings
  applied automatically on install.
- **Tray residency** — closing the window hides it to the tray (configurable). The tray icon
  changes color with your environment's health, shows a summary tooltip on hover, and raises
  a toast notification when a threshold is crossed.
- **Language switching** — 日本語 / English, from the "Language / 言語" context-menu item.
- **One-click clipboard helpers** — copy a CLAUDE.md "diet" instruction or the `/compact`
  command when bloat is detected.

The UI is a small status panel (300 px wide) pinned to the bottom-right of your screen.

## Which environments benefit most

The monitoring features (CLAUDE.md and session-log health checks) work in every environment.
What varies by environment is mainly how much the RTK integration can save.

| Environment | Monitoring | RTK savings | Overall |
| --- | :-: | :-: | --- |
| Web stacks (Node/TS etc.) via the Bash tool | ◎ | ◎ | Full benefit |
| Ruby / Python stacks | ◎ | ○–◎ | Strong benefit |
| .NET / WPF (PowerShell-centric) | ◎ | × | Monitoring & memory focused |
| Small, short-lived, throwaway projects | △ | △ | Little benefit |

- The larger the repository and the noisier its output (`git status`/`diff`, test logs,
  dependency installs), the more RTK saves.
- The longer the project lives, the more claude-mem's memory layer pays off.

For why RTK struggles on .NET stacks and what to do about it, see
[Troubleshooting](#troubleshooting).

## The right-click menu

Right-clicking the main window and right-clicking the tray icon show **exactly the same menu**.
Top to bottom:

1. **Copy CLAUDE.md diet instructions**
   Grayed out while CLAUDE.md usage is below 70%. Once it reaches 70%, clicking copies an
   instruction (with the current line count and size embedded) asking to move details out
   into `docs/`.
2. **Open CLAUDE.md folder**
   Opens Explorer directly when one project is monitored; with several projects, a submenu
   lists them by name.
3. **Copy /compact**
   Turns to a warning color when session-log usage reaches 70%. Clicking copies `/compact`
   to the clipboard.
4. **Manage tools** (submenu)
   - **Install / Uninstall RTK** — the label follows the current install state.
   - **Install / Uninstall claude-mem** — likewise; if the `claude` CLI cannot be found the
     item shows "claude CLI not found" and is disabled.

   Either one opens a dedicated window that runs the steps one by one (on failure you can
   retry from the failed step).
5. **Language / 言語** (submenu)
   Pick 日本語 or English. The current language is check-marked and the whole UI switches
   immediately, no restart needed. This submenu's label alone is always bilingual, so you can
   find your way back after switching by mistake.
6. **Settings**
   Opens the settings window — see the next section.
7. **Help**
   Opens the help window (with a donation-page link at the bottom).
8. **☕ Support development**
   Opens the donation page in your default browser.
9. **Exit** (below a separator)
   Fully quits the app. The Exit item in both menus is a real quit, not hide-to-tray
   (the × button is what hides to the tray).

Additionally, when RTK's cumulative savings reach 100K / 1M / 10M tokens, a one-time support
banner appears inside the status panel (a closed milestone never reappears; no toast is shown).

## Settings explained

Settings are stored in `settings.json` next to the executable and loaded at startup. If the
file is missing, a default is generated that monitors this repository itself (the directory
containing the `.sln`).

The settings window (right-click → Settings) edits the following:

| Settings window item | settings.json key | Default | Meaning |
| --- | --- | --- | --- |
| CLAUDE.md line limit | `instructionFile.maxLines` | 200 | CLAUDE.md warns above this line count |
| CLAUDE.md size limit (KB) | `instructionFile.maxSizeKB` | 25 | CLAUDE.md warns above this size |
| Session log limit (MB) | `sessionLog.maxSizeBytes` (shown in MB) | 5 | Session log warns above this size |
| Polling interval (seconds) | `pollIntervalSeconds` | 5 | How often CLAUDE.md / session logs are checked |
| External command interval (seconds) | `externalTools.pollIntervalSeconds` | 60 | How often `rtk gain` / `ccusage` are re-run |
| Hide to tray on close | `tray.hideOnClose` | true | When false, × quits the app instead |
| Show warning toast notifications | `tray.toastOnWarning` | true | When false, only the tray icon changes color |

Keys not shown in the settings window:

- `language` (`"ja"` / `"en"`) — display language; change it via the "Language / 言語"
  context-menu item. Detected from the OS UI language on first run.
- `targetAi` (default `"claude"`) — the monitored AI tool; reserved for future multi-AI
  support, always `claude` in v1.
- `instructionFile.path` (default `"CLAUDE.md"`) — the monitored file name.
- `sessionLog.configDirOverride` — set to override the session-log config directory.
- `projects` — the monitored projects (pairs of `name` and `path`).
- `tray.firstHideNoticeShown` — whether the one-time hide-to-tray notice was shown (internal).
- `donation.shownMilestones` — support-banner milestones already shown (internal).

The settings window's "Reset to defaults" button resets the items in the table above
(the language and the internal flags are left alone).

## Installation (release build)

1. Download `TokenOmamoriTool-vX.Y.Z-win-x64.zip` from [GitHub Releases](../../releases)
2. Extract it to a **writable folder** (e.g. somewhere under Documents, or any working folder)
3. Run `TokenOmamoriTool.exe`

- Extracting under `Program Files` is **not recommended** — the app stores its settings in a
  `settings.json` next to the EXE, which is not writable there.
- The EXE is self-contained; no .NET runtime install is needed.

### About the SmartScreen warning on first run

This app is not code-signed, so Windows SmartScreen may show a "Windows protected your PC"
warning the first time you run it. Click **"More info" → "Run anyway"** to start it. The full
source code is public in this repository, and you can reproduce the exact same EXE yourself
with `tools/publish.ps1`.

### About antivirus false positives

Self-contained single-file EXEs are occasionally flagged by antivirus software by mistake.
If that happens, add the extracted folder to your exclusion list, or build from source.

## Requirements

- Windows 10 version 1809 or later / Windows 11
- .NET 8 runtime (not needed with the self-contained EXE)
- Node.js 20+ for the ccusage feature

## Troubleshooting

### RTK savings not growing / sessions not detected (Windows)

- When Claude Code runs commands through its **PowerShell tool**, RTK's hook (which only
  supports the Bash tool) cannot intercept them, so the saved-token number may not grow.
- We have confirmed cases where `rtk discover` does not detect sessions on Windows.

Both are limitations on RTK's side — the numbers this tool displays (parsed from `rtk gain`)
are accurate.
Windows + .NET development is especially affected, because Claude Code tends to run commands
through its PowerShell tool there, where RTK (Bash-tool only) cannot help. Saving tokens via
the instruction file instead works well — e.g. trimming `dotnet` output with `-v q -nologo`.

## Build & run

```powershell
dotnet build TokenOmamoriTool.sln
dotnet run --project TokenOmamoriTool/TokenOmamoriTool.csproj
dotnet test
```

Target framework: `net8.0-windows10.0.17763.0` (WPF, nullable enabled).

## Support the project ☕

If this tool saves you tokens (and money), consider supporting development:

- **Ko-fi (one-time tip)**: https://ko-fi.com/lranslot
- **GitHub Sponsors**: <!-- TODO: https://github.com/sponsors/XXXX -->

Support is entirely optional — all features are free for everyone.
