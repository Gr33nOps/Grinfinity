param([Parameter(Mandatory=$true)][string]$Godot)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$stage=Join-Path $projectRoot ('builds/qa/rc4-stage-'+[Guid]::NewGuid().ToString('N'))
$output=Join-Path $projectRoot 'builds/windows-rc4'
New-Item -ItemType Directory -Force -Path $stage,$output | Out-Null
& robocopy $projectRoot $stage /E /XD .git .godot .vs builds .qa /XF '*.log' /NFL /NDL /NJH /NJS /NP | Out-Null
if($LASTEXITCODE -ge 8){throw 'Staging copy failed'}
Push-Location $stage
try {
    & dotnet build --disable-build-servers -m:1 -p:UseSharedCompilation=false
    if($LASTEXITCODE -ne 0){throw 'Compilation failed'}
    & $Godot --headless --path $stage --editor --import --quit
    if($LASTEXITCODE -ne 0){throw 'Import failed'}
    & $Godot --headless --path $stage --export-release 'Windows Desktop' (Join-Path $output 'Grinfinity.exe') --quit
    if($LASTEXITCODE -ne 0){throw 'Export failed'}
} finally {Pop-Location}
Write-Output "BUILD COMPLETE: $output"
