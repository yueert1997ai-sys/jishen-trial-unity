param([switch]$CheckOnly, [switch]$Verify)
$ErrorActionPreference = 'Stop'
try {
    $projectRoot = Split-Path $PSScriptRoot -Parent
    $release = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'current.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $build = [IO.Path]::GetFullPath((Join-Path $projectRoot $release.build))
    $buildsRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Builds')) + '\'
    if (!$build.StartsWith($buildsRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid playable build path.' }
    $exe = Join-Path $build 'MECH_TRIAL_P0.exe'
    if (!(Test-Path -LiteralPath $exe)) { throw 'Playable build is missing. Restore the build or publish another tested version.' }
    $profile = Join-Path $PSScriptRoot 'UserData\profile.json'
    $gameArgs = @('-combatSlice', '-sliceView', 'C', '-screen-fullscreen', '0', '-screen-width', '1600', '-screen-height', '900')
    if ($CheckOnly) {
        @{ version=$release.version; executable=$exe; profile=$profile; arguments=$gameArgs } | ConvertTo-Json
        exit 0
    }
    New-Item -ItemType Directory -Path (Split-Path $profile -Parent) -Force | Out-Null
    if ($Verify) {
        $proof = Join-Path $PSScriptRoot ('Verification\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
        New-Item -ItemType Directory -Path $proof -Force | Out-Null
        $profile = Join-Path $proof 'profile.json'
        $gameArgs += @('-batchmode', '-noaudio', '-p0Check', '-combatAudioCheck', '-logFile', ('"' + (Join-Path $proof 'player.log') + '"'))
    }
    $env:MECH_EQUIPMENT_PROFILE = $profile
    if ($Verify) {
        $process = Start-Process -FilePath $exe -ArgumentList $gameArgs -WorkingDirectory $build -WindowStyle Hidden -PassThru -Wait
        $result = Get-Content -LiteralPath (Join-Path $proof 'quick-check.txt') -Raw
        if ($process.ExitCode -ne 0 -or $result -notmatch 'P0_CHECK code=0 errors=0') { throw "Launcher verification failed: $proof" }
        Write-Output "PASS: $proof"
    } else {
        Start-Process -FilePath $exe -ArgumentList $gameArgs -WorkingDirectory $build -WindowStyle Normal
    }
} catch {
    if ($CheckOnly -or $Verify) { Write-Error $_; exit 1 }
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show($_.Exception.Message, 'Jishen Trial launcher') | Out-Null
    exit 1
}
