$ErrorActionPreference = 'Stop'
$assetDirectory = $PSScriptRoot
$blenderExecutable = 'C:/Program Files/Blender Foundation/Blender 5.2/blender.exe'
$unityExecutable = 'D:/Editor/6000.3.18f1/Editor/Unity.exe'
Push-Location -LiteralPath $assetDirectory
try {
    foreach ($script in @('assemble_colored.py','render_assembly.py','build_game_candidate.py','verify_export.py')) {
        & $blenderExecutable --background --threads 4 --python-exit-code 1 --python $script *> (Join-Path $assetDirectory ('logs/' + $script + '.log'))
        if ($LASTEXITCODE -ne 0) { throw "Blender failed: $script / $LASTEXITCODE" }
    }
    & 'C:/Python313/python.exe' 'prepare_unity_review.py'
    if ($LASTEXITCODE -ne 0) { throw 'Unity input preparation failed' }
    & 'C:/Python313/python.exe' 'make_review_sheets.py'
    $unityArguments = @('-batchmode','-projectPath',('"'+$assetDirectory+'/UnityImportReview"'),'-executeMethod','JishenAssemblyReview.Build','-quit','-logFile',('"'+$assetDirectory+'/logs/unity_import.log"'))
    $unityProcess = Start-Process -FilePath $unityExecutable -ArgumentList $unityArguments -WindowStyle Hidden -PassThru -Wait
    if ($unityProcess.ExitCode -ne 0) { throw "Unity import failed: $($unityProcess.ExitCode)" }
} finally { Pop-Location }
