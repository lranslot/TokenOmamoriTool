# リリースノートテンプレート (spec v0.3追補3 §13.4)

GitHub Releases に貼る本文のテンプレート。日本語先・English後(READMEと同順)。
`vX.Y.Z` は csproj の `<Version>`・git タグと一致させること(§13.3)。
SHA256 は `tools/publish.ps1` が出力する `*.zip.sha256.txt` の値を転記する。

---

# vX.Y.Z

## 変更点

- (新機能・変更・修正を箇条書き。ユーザーに見える変化を先に)
-

## 動作環境

- Windows 10 バージョン 1809 以降 / Windows 11(x64)
- .NET ランタイム不要(自己完結型EXE)
- ccusage 機能を使う場合: Node.js 20+

## 既知の問題

- 初回実行時に SmartScreen の警告が出ます(コード署名なし)。「詳細情報」→「実行」で
  起動できます。詳細は [README](../README.md) を参照。
- Claude Code が PowerShell ツール経由でコマンドを実行した場合、RTK の節約が伸びない
  ことがあります(RTK 側の制約。README のトラブルシューティング参照)。
- (このリリース固有の既知の問題があれば追記)

## チェックサム

```
SHA256: (TokenOmamoriTool-vX.Y.Z-win-x64.zip.sha256.txt の値)
```

---

# vX.Y.Z (English)

## Changes

- (New features / changes / fixes, user-visible items first)
-

## Requirements

- Windows 10 version 1809 or later / Windows 11 (x64)
- No .NET runtime required (self-contained EXE)
- Node.js 20+ for the ccusage feature

## Known issues

- Windows SmartScreen shows a warning on first run (the app is not code-signed).
  Click "More info" → "Run anyway". See the [README](../README.md) for details.
- When Claude Code runs commands through its PowerShell tool, RTK savings may not grow
  (an RTK-side limitation — see the README troubleshooting section).
- (Add release-specific known issues here)

## Checksum

```
SHA256: (value from TokenOmamoriTool-vX.Y.Z-win-x64.zip.sha256.txt)
```
