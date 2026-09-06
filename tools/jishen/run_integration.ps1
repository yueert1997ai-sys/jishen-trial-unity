param([ValidateSet('Build','Validate','Release')][string]$Mode = 'Validate')
$ErrorActionPreference = 'Stop'
$jishenRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$jishenUnity = 'D:/Editor/6000.3.18f1/Editor/Unity.exe'
$jishenOutput = Join-Path $jishenRoot 'AuditEvidence/jishen'
New-Item -ItemType Directory -Force -Path $jishenOutput | Out-Null
function Invoke-JishenUnity([string]$Method, [string]$Log, [bool]$Quit) {
    $jishenArguments = @('-batchmode', '-projectPath', ('"' + $jishenRoot + '"'), '-executeMethod', $Method,
        '-logFile', ('"' + (Join-Path $jishenOutput $Log) + '"'))
    if ($Quit) { $jishenArguments += '-quit' }
    $jishenProcess = Start-Process -FilePath $jishenUnity -ArgumentList $jishenArguments -WindowStyle Hidden -PassThru
    $jishenProcess.WaitForExit()
    if ($jishenProcess.ExitCode -ne 0) { throw "$Method failed: $($jishenProcess.ExitCode). See $Log" }
}
switch ($Mode) {
    'Build' { Invoke-JishenUnity 'JishenHeroIntegration.Build' 'integration.log' $true }
    'Validate' {
        Invoke-JishenUnity 'ManualCombatAudit.Run' 'manual-final.log' $false
        Invoke-JishenUnity 'HeroVisualAudit.Run' 'visual-final.log' $false
    }
    'Release' { Invoke-JishenUnity 'JishenHeroIntegration.BuildRelease' 'release.log' $true }
}
