# publish.ps1 — ポータブル配布物の発行スクリプト (spec v0.3追補3 §13.2)
#
# self-contained・単一ファイル EXE を発行し、ZIP と SHA256 を publish/ 配下に出力する。
#   publish/TokenOmamoriTool-vX.Y.Z-win-x64/            … 発行出力 (スモークテスト用にそのまま残す)
#   publish/TokenOmamoriTool-vX.Y.Z-win-x64.zip         … GitHub Releases 添付用
#   publish/TokenOmamoriTool-vX.Y.Z-win-x64.zip.sha256.txt … チェックサム
#
# バージョンは csproj の <Version> から取得する (git タグ vX.Y.Z と一致させること、§13.3)。
# 注意 (§13.2): WPF のため PublishTrimmed / ReadyToRun 系のオプションは絶対に足さないこと (壊れる)。
#
# 実行方法 (リポジトリルートで):
#   powershell -ExecutionPolicy Bypass -File tools\publish.ps1
#
# 発行後は §13.2 のスモークテストを必ず手動で行う:
#   1. EXE 起動、アイコン (ウィンドウ・タスクバー・トレイ) の表示
#   2. ja/en 切替 (ResourceDictionary の pack URI 解決)
#   3. settings.json が EXE と同じディレクトリに生成される
#   4. トレイ格納・復帰・終了
param(
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = 'Stop'

$csproj = Join-Path $RepoRoot 'TokenOmamoriTool\TokenOmamoriTool.csproj'
$csprojText = Get-Content $csproj -Raw
if ($csprojText -notmatch '<Version>([^<]+)</Version>') {
    throw "<Version> not found in $csproj"
}
$version = $Matches[1].Trim()
$name = "TokenOmamoriTool-v$version-win-x64"
$publishDir = Join-Path $RepoRoot 'publish'
$stageDir = Join-Path $publishDir $name

if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force -Confirm:$false }
New-Item -ItemType Directory -Force $stageDir | Out-Null

Write-Host "publishing $name ..."
dotnet publish $csproj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtraction=true `
    -o $stageDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }

# ZIP には EXE のみ入れる (§13.4: 同梱READMEは不要、pdb は配布しない)。
Get-ChildItem $stageDir -Filter '*.pdb' | Remove-Item -Force -Confirm:$false

$zipPath = Join-Path $publishDir "$name.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force -Confirm:$false }
Compress-Archive -Path (Join-Path $stageDir '*') -DestinationPath $zipPath

$hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
$shaPath = "$zipPath.sha256.txt"
"$hash  $name.zip" | Out-File $shaPath -Encoding ascii

Write-Host ""
Write-Host "done:"
Write-Host "  exe : $stageDir"
Write-Host "  zip : $zipPath"
Write-Host "  sha : $shaPath ($hash)"
Write-Host ""
Write-Host "next: run the §13.2 smoke test against $stageDir\TokenOmamoriTool.exe"
