# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

Core features (monitoring, RTK/ccusage/claude-mem integration, install/uninstall UI, tray
residency, ja/en localization, donation flow, omamori visual theme) are implemented. All
requirements live in `docs/`:

- `docs/トークン節約ツール_仕様書_v0.2_1.md` — the spec (features, UI, scope).
- `docs/仕様書v0.2_調査結果補足_1.md` — follow-up research answering the spec's open questions
  (log paths, tool output formats, install methods). **Read both files together before implementing**
  — the second file overrides/resolves "要調査" (needs investigation) items in the first.
- `docs/トレイ常駐・カンパ導線_仕様追加_v0.3.md` — v0.3 additions: tray residency (§8) and
  donation prompts (§9), both implemented.
- `docs/多言語対応 仕様追加(v0.3追補).md` — §11 Japanese/English localization (implemented,
  including the §11.7 bilingual README.md).
- `docs/外見仕様追加(v0.3追補2).md` — §12 omamori visual theme: palette, tray/app icons, washi
  main-window styling (implemented).
- `docs/EXE化・配布仕様追加(v0.3追補3).md` — §13 portable EXE distribution: publish script,
  versioning, GitHub Releases, README distribution notes (implemented).

**These files in `docs/` are the authoritative spec.** If code, comments, or this CLAUDE.md ever
disagree with them, the `docs/` files win.

App display name: **「トークン節約お守りツール」**.

## What this tool is

A Windows WPF tray/overlay app ("Claude Code トークン節約お守りツール") that monitors a Claude Code
dev environment and helps install/measure token-saving tools: RTK, ccusage, claude-mem. Policy
constraints from the spec (do not violate these when implementing features):

- Never reduce the AI's own output/response quality to save tokens — only optimize I/O and memory
  layers.
- The tool itself must not consume tokens (no API calls for measurement).
- Explicitly out of scope: Caveman, kizami, Token Optimizer/Savior, Context Mode (see spec §4.3
  "導入しないもの" for reasons).

## Build & run

```powershell
dotnet build TokenOmamoriTool.sln -v q -nologo
dotnet run --project TokenOmamoriTool/TokenOmamoriTool.csproj
dotnet test -v q -nologo
```

Token-saving rule for development: **always pass `-v q -nologo` to `dotnet build` / `dotnet test`**.
When reporting build/test runs, output one line on success (OK / pass count) and full details only
on failure.

Target framework: `net8.0-windows10.0.17763.0` (both projects — required by the toast library,
don't lower it back to plain `net8.0-windows`), WPF (`UseWPF=true`), nullable enabled. Unit tests
(`TokenOmamoriTool.Tests`, xUnit) cover the pure logic in `Services/` (path encoding, CLAUDE.md
threshold checks, session log threshold/exclusion checks, `/compact` boundary tracking, `rtk gain`/
ccusage output parsing, warning-toast edge detection, localization key-set/fallback checks) — not
the WPF UI, and not the process-spawning `*Runner` classes. Tests that assert display strings must
call `TestLocalization.UseJapanese()` first (the language is process-global; all tests run as ja).

Monitoring config lives in `TokenOmamoriTool/settings.json`, read from `AppContext.BaseDirectory`
at startup. If missing, `Services/SettingsLoader` generates a default that self-monitors this repo
(finds the `.sln` directory) so a first run has real data to show.

## Architecture notes from the spec

Detailed implementation notes (confirmed real-machine behavior, gotchas, exact commands/URLs) live in
`docs/実装メモ_アーキテクチャ詳細.md` — read that file before touching any of the areas below. Summary:

- **Multi-AI design from day one**: even though v1 only targets Claude (`targetAI=claude`), all
  config should be structured to allow other AIs later (see spec §4.5 for the `config.json` shape).
  Don't hardcode Claude-specific paths/logic where a config lookup would do.
- **Session log monitoring** (done) — bloat detection via file size/mtime, `/compact`-aware via
  `Services/CompactBoundaryTracker`. Details: 実装メモ「セッションログ監視」「`/compact`検出」.
- **CLAUDE.md health check** (done) — flags >200 lines or >25KB. Details: 実装メモ「CLAUDE.md
  ヘルスチェック」.
- **RTK integration** (done) — `Services/RtkGainRunner`/`RtkGainParser`, install/uninstall via
  `Services/RtkInstaller`/`RtkUninstaller`, daily baseline tracking via
  `Services/RtkDailyBaselineTracker`/`RtkDailyDisplayBuilder` (shows 本日/累計 split, never writes to
  RTK's own data). Details incl. the critical `--git` install gotcha: 実装メモ「RTK統合」
  「RTK 本日分/累計の内訳表示」「RTK (Services/RtkInstaller/RtkUninstaller)」.
- **claude-mem integration** (done) — `Services/ClaudeMemInstaller`/`ClaudeMemUninstaller`/
  `ClaudeMemStatusChecker`, low-token settings written to `~/.claude-mem/settings.json`. Details:
  実装メモ「claude-mem統合」「claude-mem (...)」.
- **ccusage integration** (done) — shells out to `npx ccusage@latest daily --json`, requires
  Node.js 20+. Details: 実装メモ「ccusage統合」.
- **Install/uninstall UI** (done) — right-click `ContextMenu` on `MainWindow`, no setup-wizard
  button; menu order: CLAUDE.mdダイエット指示コピー → CLAUDE.mdを開く → /compactをコピー →
  ツール管理 → 設定 → 説明. Details incl. the `ContextMenu` lifecycle gotchas and the color-linked
  warning items: 実装メモ「Install/uninstall UI」.
- **UI shape**: small status window anchored bottom-right (300px wide), progress-bar checklist for
  CLAUDE.md size / session bloat, labeled 節約(RTK)/本日消費(ccusage) totals. Main window size never
  changes.
- **Tray residency** (done, v0.3 §8) — H.NotifyIcon.Wpf (pinned 2.3.2: 2.4.x ships no net8 asset),
  × hides to tray, window and tray share one `BuildContextMenu` (identical items, both ending in a
  full-quit「終了」), 2-state
  omamori icon via URI-backed .ico (see §12 bullet below; assigning a `RenderTargetBitmap` to
  `IconSource` crashes the app — see memo), edge-triggered 70% warning toasts via `Services/WarningEdgeDetector` +
  Microsoft.Toolkit.Uwp.Notifications (this package is why the TFM is `net8.0-windows10.0.17763.0`).
  Details: 実装メモ「トレイ常駐」.
- **Donation flow** (done, v0.3 §9) — pure milestone logic in `Services/DonationMilestones` (also
  holds the single `DonatePageUrl` placeholder constant — swap it there when the real URL exists),
  in-window banner (300px width unchanged), 「☕ 開発を応援」 menu item + help-window link, shown
  milestones persisted in `donation.shownMilestones`. All donation code paths are try/catch-isolated
  from monitoring (§9.4). Details: 実装メモ「カンパ導線」.
- **Localization** (done, v0.3追補 §11) — `Resources/Strings.ja.xaml`/`Strings.en.xaml` +
  `Services/LocalizationService` (`T`/`F` in code, `{DynamicResource}` in XAML, live switch via the
  Language / 言語 submenu, `language` key in settings.json). **Never hardcode a user-visible string;
  add the key to BOTH dictionaries** (a unit test enforces key-set equality). **When adding/changing
  a UI string, run the app in English and check it fits — especially in the 300px-wide main
  window**: prefer rewriting the en string shorter (natural UI English, not literal translation)
  over widening anything; the sub-windows auto-size (`SizeToContent` + `MaxWidth`/`MaxHeight` +
  wrap), the main window stays 300px and ellipsizes (`CharacterEllipsis` + full-text ToolTip) as a
  last resort. Gotchas (init order, test injection without WPF): 実装メモ「多言語対応」.
- **Omamori visual theme** (done, v0.3追補2 §12) — color values live in `OmamoriTheme.cs` (single
  source; `Resources/Theme.xaml` only re-exposes them to XAML via `x:Static` under the §12.2 keys —
  deliberate, so converter unit tests run without a WPF `Application`), shape in `OmamoriShape.cs`
  (24×24 path data: pouch/loop/knot). `tools/generate-icons.ps1` regenerates
  `Assets/omamori.ico`/`omamori-warning.ico` (16/32/48/256) from those two files — **rerun it and
  commit the .ico whenever a color or the shape changes**; never edit the .ico or hardcode a
  status/background color in a Window. Tray = 2-state .ico swap (GeneratedIconSource can't draw
  custom geometry), main window = washi bg + rounded accent inner border + status-linked mini
  omamori in the in-window title strip (window `Icon` stays green per §12.4). Gotchas (UTF-8 BOM,
  PS byte[] unrolling → CS7065, ico entry formats): 実装メモ「外見・お守りモチーフ」.
- **EXE distribution** (done, v0.3追補3 §13) — portable ZIP, published by `tools/publish.ps1`
  (reads the csproj `<Version>` — keep it in sync with the git tag and ZIP name; **never add
  PublishTrimmed/ReadyToRun**, WPF breaks). The 5 WPF native `*_cor3.dll`s cannot be bundled into
  the single file and must ship next to the EXE — the ZIP contains all 6 files. Help window shows
  the version via `Services/AppVersionInfo` (assembly metadata, unit-tested — no hardcoded version
  strings anywhere). Release notes template: `docs/release_note_template.md`. After every publish, run
  the §13.2 smoke test manually. Details: 実装メモ「EXE化・配布」.
- Suggested build order per the spec: (1) read-only monitoring — done, (2) `rtk gain`/
  `ccusage --json` parsing and display — done, (3) install/uninstall — done, (4) v0.3 §8 tray
  residency — done, (5) v0.3追補 §11 localization — done, (6) v0.3 §9 donation flow — done,
  (7) v0.3追補2 §12 omamori visual theme — done, (8) v0.3追補3 §13 EXE distribution — done.
