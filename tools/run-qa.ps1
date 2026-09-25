param([Parameter(Mandatory=$true)][string]$Godot)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$qaRoot = Join-Path $projectRoot 'builds/qa/isolated-project'
& dotnet build (Join-Path $projectRoot 'Grinfinity.csproj') --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed; refusing to run stale QA assembly' }
New-Item -ItemType Directory -Force -Path $qaRoot | Out-Null
& robocopy $projectRoot $qaRoot /E /XD .git .vs builds .qa 'NVIDIA Corporation' /XF '*.log' /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw 'QA copy failed' }
$config = Get-Content -LiteralPath (Join-Path $qaRoot 'project.godot') -Raw
$config = $config.Replace('config/name="Grinfinity"', 'config/name="Grinfinity-QA"')
Set-Content -LiteralPath (Join-Path $qaRoot 'project.godot') -Value $config -Encoding utf8
& $Godot --headless --path $qaRoot --max-fps 120 --quit-after 10800 res://tools/release_qa.tscn | Tee-Object -Variable qaOutput
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if (-not ($qaOutput -match 'QA RESULT: 0 failure\(s\)')) { throw 'QA did not complete successfully' }
exit 0
