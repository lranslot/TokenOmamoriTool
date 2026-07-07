# EXE化・配布 仕様追加(v0.3追補3)

docs/ 配下の他の仕様追加と同様に正とする。§番号は既存仕様の続きとする。

## 13. EXE化・配布

### 13.1 基本方針
- 配布形態は「ポータブル型」とする:インストーラーなし、ZIPを展開して EXE を
  実行するだけ。settings.json は従来どおり EXE と同じディレクトリに保存する
  (この設計はポータブル配布と相性が良いので変更しない)。
- 配布物は self-contained・単一ファイル EXE を第一とする(.NETランタイム不要)。
  サイズが大きくなる(150MB前後)ことは許容する。

### 13.2 発行(publish)設定
- `tools/publish.ps1` として発行スクリプトをリポジトリに追加する。内容:
  - `dotnet publish` / Release / `win-x64` / `--self-contained true`
  - `PublishSingleFile=true`、`IncludeNativeLibrariesForSelfExtraction=true`
  - WPF のため **Trimming(PublishTrimmed)と ReadyToRun の圧縮系オプションは
    使用しない**(WPFはトリミング非対応。壊れる)
  - 出力を `publish/` 配下にまとめ、`TokenOmamoriTool-vX.Y.Z-win-x64.zip` に圧縮
  - ZIP の SHA256 を計算してテキストファイルに出力
- 発行後の EXE で以下を必ずスモークテストする(単一ファイル化で壊れやすい点):
  - 起動し、アイコン(ウィンドウ・タスクバー・トレイ)が正しく表示される
  - ja/en 切替が動く(ResourceDictionary の pack URI 解決の確認)
  - settings.json が EXE と同じディレクトリに生成される
  - トレイ格納・復帰・終了が動く

### 13.3 バージョン管理
- csproj に `Version`(例 1.0.0)を設定し、以後リリースごとに更新する。
- ヘルプウィンドウにバージョン番号を表示する(アセンブリ情報から取得。
  ハードコード禁止)。
- Git のタグ `vX.Y.Z` とリリースZIP名・csproj の Version を一致させる。

### 13.4 GitHub Releases での配布
- リリースごとに添付するもの:
  - `TokenOmamoriTool-vX.Y.Z-win-x64.zip`(EXE + 同梱README的なテキストは不要、
    リポジトリREADMEへ誘導)
  - SHA256 チェックサム
- リリースノートは日英併記(READMEと同じ「日本語先・English後」の順)。
- リリースノートのテンプレート(変更点・動作環境・既知の問題)を
  `docs/release_note_template.md` として用意する。

### 13.5 README への追記(配布関連)
README の日英両方に以下を追記する:

1. **インストール手順**: GitHub Releases から ZIP をダウンロード → 書き込み可能な
   フォルダ(例: ドキュメント配下や任意の作業フォルダ)に展開 → EXE を実行。
   `Program Files` 配下は settings.json が書けないため非推奨と明記。
2. **SmartScreen の説明**: 本アプリはコード署名をしていないため、初回実行時に
   Windows SmartScreen の警告が出る。「詳細情報」→「実行」で起動できる旨と、
   ソースが公開されておりビルドも再現可能である旨を記載する。
3. **セキュリティソフトの誤検知**: self-contained 単一ファイル EXE は
   まれに誤検知されることがある。その場合の対処(除外設定 or 自前ビルド)を
   一言案内する。
4. **RTKに関する既知の制約(Windows)**: Claude Code がコマンドを PowerShell
   ツール経由で実行した場合、RTK のフック(Bashツールのみ対応)は介入できず
   節約が伸びないことがある。また `rtk discover` が Windows でセッションを
   検出しない事象を確認している。これらは RTK 側の制約であり本ツールの
   表示は正確である旨を記載する(トラブルシュート節)。

### 13.6 やらないこと(v1)
- コード署名(証明書の費用対効果が個人開発の初期段階に見合わない。
  将来の検討課題として README に含めない)
- インストーラー(MSI/MSIX)化
- 自動アップデート機構
- winget / Microsoft Store への登録(反応を見てから検討)

### 13.7 テスト方針(追加分)
- publish.ps1 の実行と §13.2 のスモークテストは手動で行う(自動テスト対象外)。
- バージョン表示がアセンブリ情報から取得されていることは、可能なら
  ロジックを切り出して単体テストしてよい(必須ではない)。
