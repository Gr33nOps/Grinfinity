# Plays whole runs with tools/survival_bot.gd in an isolated copy of the project,
# so bot runs never write to real saves, leaderboards or unlocks.
# Example: ./tools/run-bot.ps1 -Godot <path to Godot console exe> -Runs 3
param([Parameter(Mandatory=$true)][string]$Godot, [int]$Runs = 1, [int]$MaxSeconds = 1500, [switch]$Immortal, [double]$Skill = 0.7, [string]$Shots = "", [string]$ShotDir = "", [switch]$Windowed)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$botRoot = Join-Path $projectRoot 'builds/qa/bot-project'
& dotnet build (Join-Path $projectRoot 'Grinfinity.csproj') --no-restore | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
New-Item -ItemType Directory -Force -Path $botRoot | Out-Null
& robocopy $projectRoot $botRoot /E /XD .git .vs builds .qa 'NVIDIA Corporation' /XF '*.log' /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw 'Bot copy failed' }
$config = Get-Content -LiteralPath (Join-Path $botRoot 'project.godot') -Raw
$config = $config.Replace('config/name="Grinfinity"', 'config/name="Grinfinity-Bot"')
Set-Content -LiteralPath (Join-Path $botRoot 'project.godot') -Value $config -Encoding utf8
$env:GRIN_RUNS = "$Runs"; $env:GRIN_MAX = "$MaxSeconds"; $env:GRIN_SKILL = "$Skill"
$env:GRIN_IMMORTAL = if ($Immortal) { '1' } else { '' }
$env:GRIN_SHOTS = $Shots; $env:GRIN_SHOT_DIR = $ShotDir
if ($Windowed) { & $Godot --path $botRoot --fixed-fps 60 --windowed --resolution 1600x900 res://tools/survival_bot.tscn }
else { & $Godot --headless --path $botRoot --fixed-fps 60 res://tools/survival_bot.tscn }
