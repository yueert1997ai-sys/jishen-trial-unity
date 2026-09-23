from pathlib import Path
root=Path(__file__).resolve().parents[2]
def edit(name,old,new):
 p=root/name;s=p.read_text(encoding='utf-8-sig');assert old in s,(name,old[:80]);p.write_text(s.replace(old,new),encoding='utf-8')
base='Assets/Scripts/'
edit(base+'Player/ValkyrComboProfile.cs','var profile=Resources.Load<ValkyrComboProfile>', 'if(P0CombatDemo.Enabled) return P0ComboProfile.Active;\n        var profile=Resources.Load<ValkyrComboProfile>')
edit(base+'Player/PlayerMeleeController.cs','public float MovementScale => IsAttacking && raiken != null ? (elapsed<CurrentStroke.contactEnd?0:.45f) : 1f;', '''public float MovementScale => !IsAttacking ? 1f : P0CombatDemo.Enabled ? (ComboStage<2?.82f:.55f) : (elapsed<CurrentStroke.contactEnd?0:.45f);
    public bool CanDashCancel => !P0CombatDemo.Enabled || !IsAttacking || ComboStage<2 || elapsed>=CurrentStroke.contactEnd+.10f;
    private float queueRemaining;
    private const float InputBuffer = .22f;''')
edit(base+'Player/PlayerMeleeController.cs','if(ComboStage>=2 || queuedNext || elapsed>CurrentStroke.duration-.15f)return false;\n            queuedNext=true;return true;', '''if(ComboStage>=2)return false;
            if(P0CombatDemo.Enabled && elapsed>CurrentStroke.linkTime+.065f)return false;
            queuedNext=true;queueRemaining=InputBuffer;return true;''')
edit(base+'Player/PlayerMeleeController.cs','float previous = elapsed;','if(queuedNext && P0CombatDemo.Enabled && (queueRemaining-=Time.deltaTime)<=0)queuedNext=false;\n        float previous = elapsed;')
edit(base+'Player/PlayerMeleeController.cs','elapsed = Mathf.Min(elapsed + Time.deltaTime, raiken != null ? CurrentStroke.duration : Duration);','''float stageEnd = raiken != null ? CurrentStroke.duration : Duration;
        // Land exactly on the shared key, even on a long frame; LateUpdate presents it before linking.
        if(raiken!=null && queuedNext && elapsed<CurrentStroke.linkTime)stageEnd=CurrentStroke.linkTime;
        elapsed = Mathf.Min(elapsed + Time.deltaTime, stageEnd);''')
edit(base+'Player/PlayerMeleeController.cs','float carry=Mathf.Min(.08f,elapsed-CurrentStroke.linkTime);','float carry=P0CombatDemo.Enabled?0:Mathf.Min(.08f,elapsed-CurrentStroke.linkTime);')
edit(base+'Player/PlayerMeleeController.cs','float amount=basis*scale;','float amount=basis*scale*(P0CombatDemo.Enabled?(ComboStage==2?1.6f:ComboStage==1?.95f:1):1);')
edit(base+'Player/PlayerMeleeController.cs','StrikeHit?.Invoke(target,amount);','''if(P0CombatDemo.Enabled && !target.IsDead)target.GetComponent<EnemyBase>()?.ReceiveMeleeImpact(forward,ComboStage==2);
        StrikeHit?.Invoke(target,amount);''')
edit(base+'Player/PlayerMeleeController.cs','public void ClearComboQueue(){queuedNext=false;}','public void ClearComboQueue(){queuedNext=false;queueRemaining=0;}')
edit(base+'Player/PlayerController.cs','private bool boostExhausted;','private bool boostExhausted;\n    private float dashBuffer;')
edit(base+'Player/PlayerController.cs','if (command.Dash) TryDash(command.Move);','''if(P0CombatDemo.Enabled)
        {
            dashBuffer=Mathf.Max(0,dashBuffer-deltaTime);
            if(command.Dash)dashBuffer=.18f;
            if(dashBuffer>0 && TryDash(command.Move))dashBuffer=0;
        }
        else if (command.Dash) TryDash(command.Move);''')
edit(base+'Player/PlayerController.cs','if (stats != null && !stats.TrySpendEnergy(25f)) return false;','if (!Melee.CanDashCancel) return false;\n        if (stats != null && !stats.TrySpendEnergy(25f)) return false;')
edit(base+'Player/PlayerController.cs','boostHeldTime = 0;','boostHeldTime = 0;\n        dashBuffer = 0;')
edit(base+'Player/PlayerWeaponStance.cs','private Damageable pendingSkill;','private Damageable pendingSkill;\n    private float slashBuffer;')
edit(base+'Player/PlayerWeaponStance.cs','bool alreadyRanged=State==WeaponStance.Ranged;','''if(P0CombatDemo.Enabled)
        {
            slashBuffer=Mathf.Max(0,slashBuffer-dt);
            if(slashBuffer<=0)pendingSlash=false;
            command.Skill=false;
        }
        bool alreadyRanged=State==WeaponStance.Ranged;''')
edit(base+'Player/PlayerWeaponStance.cs','requireFireRelease = command.Fire;','requireFireRelease = !P0CombatDemo.Enabled && command.Fire;\n            slashBuffer=.25f;')
edit(base+'Player/PlayerWeaponStance.cs','else if ((pressed && !requireFireRelease) || command.Skill)','else if ((pressed && !requireFireRelease) || command.Skill || (P0CombatDemo.Enabled && command.Fire && !pendingSlash && !player.Melee.IsAttacking && State==WeaponStance.Sword))')
edit(base+'Player/PlayerWeaponStance.cs','dt / TransitionDuration','dt / (P0CombatDemo.Enabled?.09f:TransitionDuration)')
edit(base+'Player/PlayerWeaponStance.cs','{ wasFire = requireFireRelease', '{ slashBuffer=0; wasFire = requireFireRelease')
edit(base+'Equipment/PlayerLoadout.cs','Selected == PrimaryWeapon.Greatsword || Selected == PrimaryWeapon.Collection;', 'Selected == PrimaryWeapon.Greatsword || Selected == PrimaryWeapon.Collection || (P0CombatDemo.Enabled && Selected == PrimaryWeapon.M7);')
edit(base+'Player/LoadoutVisual.cs','if (WeaponObject != null)\n        {\n            if (shoulderMount != null)', '''if (WeaponObject != null)
        {
            if(P0CombatDemo.Enabled && loadout.CanUseSword && player.Stance.State!=WeaponStance.Ranged)
            {
                // This later pose writer must relinquish the arms to the complete melee pose.
                WeaponObject.SetActive(false);adapter.RefreshSockets();return;
            }
            WeaponObject.SetActive(true);
            if (shoulderMount != null)''')
edit(base+'Player/ValkyrMotionDriver.cs','if(attacking&&poseTime<.08f)m=ValkyrComboProfile.Blend(entryKey,m,Mathf.SmoothStep(0,1,poseTime/.08f));','''float entryDuration=P0CombatDemo.Enabled?.04f:.08f;
        if(attacking&&poseTime<entryDuration)m=ValkyrComboProfile.Blend(entryKey,m,Mathf.SmoothStep(0,1,poseTime/entryDuration));''')
edit(base+'Player/ValkyrMotionDriver.cs','Vector3 displacement=player.transform.position-lastPosition;displacement.y=0;lastPosition=player.transform.position;','''Vector3 displacement=player.transform.position-lastPosition;displacement.y=0;lastPosition=player.transform.position;
        if(P0CombatDemo.Enabled && player.Melee.IsAttacking)attackOrigin+=displacement;''')
edit(base+'Player/MechDashPresentation.cs','if (player.IsBoosting) pulse = Mathf.Max(pulse, .72f);','''if (player.IsBoosting) pulse = Mathf.Max(pulse, .72f);
        if(P0CombatDemo.Enabled && player.Melee.IsAttacking && player.Melee.ComboStage==2)
        {
            float t=player.Melee.AttackElapsed,s=player.Melee.CurrentStroke.contactStart;
            pulse=Mathf.Max(pulse,Mathf.Clamp01(1-Mathf.Abs(t-s)/.12f)*1.15f);
        }''')
edit(base+'Equipment/EquipmentLoop.cs','Owner.playerStats.SetBackpackBonuses(Backpack.dashDistance, Backpack.dashCooldown, Backpack.moveSpeed);','''Owner.playerStats.SetBackpackBonuses(P0CombatDemo.Enabled?0:Backpack.dashDistance, P0CombatDemo.Enabled?0:Backpack.dashCooldown, P0CombatDemo.Enabled?0:Backpack.moveSpeed);''')
edit(base+'Equipment/EquipmentLoop.cs','if (Backpack.id != "standard")','if (!P0CombatDemo.Enabled && Backpack.id != "standard")')
edit(base+'Equipment/EquipmentLoop.cs','public void AttachCarrier(EnemyBase enemy)\n    {','public void AttachCarrier(EnemyBase enemy)\n    {\n        if(P0CombatDemo.Enabled)return;')
edit(base+'Equipment/EquipmentLoop.cs','public void DropFrom(EnemyBase enemy)\n    {','public void DropFrom(EnemyBase enemy)\n    {\n        if(P0CombatDemo.Enabled)return;')
edit(base+'Enemy/EnemyBase.cs','private bool attacking;','''private bool attacking;
    private float meleeStagger;
    private Vector3 meleePush;
    public void ReceiveMeleeImpact(Vector3 direction,bool heavy)
    {
        meleeStagger=heavy?.26f:.09f;meleePush=direction*(heavy?7f:2f);
        StopAllCoroutines();attacking=false;
    }''')
edit(base+'Enemy/EnemyBase.cs','if (attacking) return;','''if(meleeStagger>0)
        {
            meleeStagger=Mathf.Max(0,meleeStagger-Time.deltaTime);
            if(navigation!=null && navigation.enabled && navigation.isOnNavMesh)
            {navigation.isStopped=true;navigation.Move(meleePush*Time.deltaTime);}
            return;
        }
        if (attacking) return;''')
edit(base+'Core/GameManager.cs','public void BeginRun()\n    {\n        if (PrepareRun())', '''public void BeginP0Combat()
    {
        if(!PrepareRun())return;
        stageManager.StopStage();Phase=GamePhase.Combat;arenaSector.ShowSector(1);
        playerStats.baseDashCooldown=.48f;playerStats.ResetStats();
        playerController.RestoreAt(new Vector3(0,.1f,-4));
        combatHUD.SetVisible(false);
    }
    public void BeginRun()
    {
        if(P0CombatDemo.Enabled){BeginP0Combat();return;}
        if (PrepareRun())''')
edit(base+'Core/CameraFollow.cs','float desiredSize = normalSize;','float desiredSize = P0CombatDemo.Enabled?9:normalSize;')
edit(base+'Core/CameraFollow.cs','? 38f : 60f;', '? 38f : P0CombatDemo.Enabled?75f:60f;')
print('P0 source edits applied')
