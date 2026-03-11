$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$runRoot = Join-Path $PSScriptRoot '.'
$docs = Join-Path $runRoot 'docs'
$modernized = Join-Path $runRoot 'modernized'
$logs = Join-Path $runRoot 'logs'

Write-Host 'Resetting agent-upgrades-v1 replay workspace...'

if (Test-Path $docs) { Get-ChildItem -Path $docs -Force | Remove-Item -Recurse -Force }
if (Test-Path $modernized) { Get-ChildItem -Path $modernized -Force | Remove-Item -Recurse -Force }
if (Test-Path $logs) { Get-ChildItem -Path $logs -Force | Remove-Item -Recurse -Force }

New-Item -Path $docs -ItemType Directory -Force | Out-Null
New-Item -Path $modernized -ItemType Directory -Force | Out-Null
New-Item -Path $logs -ItemType Directory -Force | Out-Null

$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
$note = @"
Replay reset completed: $timestamp
Legacy source (existing local repo): legacy/eShopLegacyMVCSolution
Use this prompt in Copilot Chat:
/dotnet10.modernize.agent-upgrades-v1 legacy/eShopLegacyMVCSolution
"@

$note | Set-Content -Path (Join-Path $logs 'replay-start.txt') -Encoding UTF8

Write-Host 'Replay workspace is ready.'
Write-Host 'Next: run /dotnet10.modernize.agent-upgrades-v1 legacy/eShopLegacyMVCSolution in Copilot Chat.'