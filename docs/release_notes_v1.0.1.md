# トークン節約お守りツール v1.0.1

表示の頑健性を改善したメンテナンスリリースです。

## 変更点

- **RTK / ccusage の表示改善**: 外部コマンドが一時的に失敗しても、当日の前回成功値を
  「(更新失敗)」付きで表示し続けるようにしました(従来は失敗文言に丸ごと置き換わって
  いました)。日付が変わると前日の値は破棄されます。
- **ccusage 実行のタイムアウトを 45秒 → 180秒 に延長**: ccusage の新バージョン公開直後、
  npx のダウンロードが完了せず「実行に失敗しました」が続く問題の恒久対策です。
- **GitHub Sponsors での支援に対応**: README とリポジトリの Sponsor ボタンから
  継続的な支援ができるようになりました。

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

## SHA256

```
d2537f91384b21400fcac4b632628ce5bbc4f37c20c0ef7cd592204cd0aa7d72  TokenOmamoriTool-v1.0.1-win-x64.zip
```

(添付の `TokenOmamoriTool-v1.0.1-win-x64.zip.sha256.txt` と同内容)

## 支援について

このツールが役に立ったら、[Ko-fi](https://ko-fi.com/lranslot)(単発)または
[GitHub Sponsors](https://github.com/sponsors/lranslot)(継続)から開発を応援していただけると嬉しいです。
支援の有無で機能に差はありません。

---

# Token Omamori Tool v1.0.1

A maintenance release that makes the status display more robust.

## Changes

- **More robust RTK / ccusage display**: When an external command fails transiently, the last
  successful value from the same day now stays visible with an "(update failed)" marker
  (previously the whole line was replaced by a failure message). Values from a previous day are
  discarded at the date rollover.
- **ccusage timeout extended from 45s to 180s**: A permanent fix for the failure loop right after
  a new ccusage version is published, where the npx download could never finish.
- **GitHub Sponsors support**: You can now support development via the repository's Sponsor
  button and the links in the README.

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

## SHA256

```
d2537f91384b21400fcac4b632628ce5bbc4f37c20c0ef7cd592204cd0aa7d72  TokenOmamoriTool-v1.0.1-win-x64.zip
```

(same as the attached `TokenOmamoriTool-v1.0.1-win-x64.zip.sha256.txt`)

## Support the project

If this tool helps you, consider supporting development via
[Ko-fi](https://ko-fi.com/lranslot) (one-time) or
[GitHub Sponsors](https://github.com/sponsors/lranslot) (recurring).
All features are free regardless of support.
