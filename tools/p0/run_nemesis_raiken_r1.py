from pathlib import Path
import os, sys, subprocess, datetime, hashlib, json, shutil

round_root=Path(r'D:\project-mecha-design\MECH ROUGE\handoff\GameplayLoop_V1')
root=Path(r'E:\SteamLibrary\JishenBuildWork\GB4Motion_R1\Project')
build=round_root/'Builds/Nemesis_Raiken_R1'
evidence=round_root/'AuditEvidence/nemesis-raiken-r1/release'
evidence.mkdir(parents=True,exist_ok=True)
env=os.environ.copy()
env.update(MECH_LOOP_V2_BUILD=str(build),MECH_LOOP_V2_EVIDENCE=str(evidence),MECH_LOOP_V2_VERSION='nemesis-raiken-r1',TEMP=str(round_root/'work/nemesis-raiken-r1'),TMP=str(round_root/'work/nemesis-raiken-r1'))
flags={'raiken':'-nemesisRaikenCheck','cannon':'-valkyrCannonCheck','imported':'-importedMotionCheck','aiui':'-aiUiCheck','lunar':'-lunarBasinCheck','arena':'-arenaTacticsCheck','terrain':'-p0TerrainAudit','punch':'-combatPunchCheck','punchperf':'-combatPunchPerformance','vfx':'-nemesisDroneVfxCheck','nemesis':'-nemesisCheck','foundation':'-foundationCheck','tactics':'-tacticsCheck','impact':'-enemyImpactCheck','melee':'-meleeFeelCheck','beam':'-beamVfxCheck','performance':'-nemesisPerformance','drones':'-nemesisDronesCheck','heroes':'-heroSelectionCheck','loopv2':'-loopV2Check'}
startup=subprocess.STARTUPINFO();startup.dwFlags|=subprocess.STARTF_USESHOWWINDOW;startup.wShowWindow=0
modes=sys.argv[1:] or ['heroes']
for mode in modes:
    runenv=env.copy()
    if mode in ('build','rebuild'):
        hero_path='Assets/Resources/Hangar/EXTRACTED_NEMESIS.prefab'
        hero_before=hashlib.sha256((round_root/hero_path).read_bytes()).hexdigest()
        # Canonical edits stay on D:; reuse the existing imported E: Library.
        source_folders=['Assets/Scripts','Assets/Editor','Assets/Resources/Hangar','Assets/Resources/GB4Motion','Assets/Scenes','Assets/Art/NemesisRaikenR1','Assets/Art/ValkyrCannonR1','ProjectSettings','Packages']
        for folder in source_folders:
            src=round_root/folder
            if src.exists(): shutil.copytree(src,root/folder,dirs_exist_ok=True)
        for name in ['Assets/Art/NemesisRaikenR1.meta','Assets/Art/ValkyrCannonR1.meta','Assets/Resources/Hangar/NEMESIS_RAIKEN.prefab.meta']:
            if (round_root/name).exists(): shutil.copy2(round_root/name,root/name)
        snapshot={p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for folder in source_folders for p in (root/folder).rglob('*') if p.is_file()}
        (evidence/'source-snapshot.json').write_text(json.dumps(snapshot,indent=2),encoding='utf8')
        out=evidence
        method='NemesisRaikenIntegration.Build' if mode=='build' else 'CombatLoopV2Build.Build'
        args=['D:/Editor/6000.3.18f1/Editor/Unity.exe','-batchmode','-quit','-projectPath',str(root),'-executeMethod',method,'-logFile',str(out/'unity-build.log')]
    else:
        out=evidence/(mode+'-'+datetime.datetime.now().strftime('%Y%m%d-%H%M%S'));out.mkdir()
        runenv.update(MECH_EQUIPMENT_PROFILE=str(out/'profile.json'),MECH_LOOP_V2_EVIDENCE=str(out))
        args=[str(build/'MECH_TRIAL_P0.exe'),'-batchmode','-noaudio','-p0Check','-combatSeed','9172026','-screen-width','1600','-screen-height','900','-logFile',str(out/'player.log')]
        if mode in flags:args+=['-combatSlice',flags[mode]]
        elif mode=='regression':args+=['-combatSlice']
        elif mode=='fullplay':args+=['-fullDemo']
        elif mode=='natural':args+=['-combatSlice','-naturalCheck']
        elif mode=='fullrun':args+=['-fullDemo','-fullPlayCheck']
        else:raise ValueError(mode)
        if mode=='beam':args.remove('-p0Check');args.remove('-combatSlice')
    print('START',mode,flush=True)
    result=subprocess.run(args,cwd=root,env=runenv,startupinfo=startup)
    record={'args':args,'exit':result.returncode,'build':str(build)}
    if mode not in ('build','rebuild'):record['assembly_sha256']=hashlib.sha256((build/'MECH_TRIAL_P0_Data/Managed/Assembly-CSharp.dll').read_bytes()).hexdigest()
    (out/'run.json').write_text(json.dumps(record,indent=2),encoding='utf8')
    print(mode,result.returncode,str(out),flush=True)
    if result.returncode:sys.exit(result.returncode)
    if mode in ('build','rebuild'):
        assert hashlib.sha256((round_root/hero_path).read_bytes()).hexdigest()==hero_before, 'NEMESIS prefab changed during build; preserve concurrent edits and resync before promotion.'
        for folder in ['Assets/Art/NemesisRaikenR1']:
            shutil.copytree(root/folder,round_root/folder,dirs_exist_ok=True)
        for name in ['Assets/Art/NemesisRaikenR1.meta','Assets/Resources/Hangar/NEMESIS_RAIKEN.prefab','Assets/Resources/Hangar/NEMESIS_RAIKEN.prefab.meta','Assets/Resources/Hangar/EXTRACTED_NEMESIS.prefab']:
            shutil.copy2(root/name,round_root/name)
        manifest=[dict(path=p.relative_to(build).as_posix(),size=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in build.rglob('*') if p.is_file() and 'UserData' not in p.parts]
        (out/'build-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
