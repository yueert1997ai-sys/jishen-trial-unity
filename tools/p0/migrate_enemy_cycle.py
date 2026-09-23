"""One-time exact edits from the archived V2 working tree; fails on unexpected source."""
from pathlib import Path
root=Path(__file__).resolve().parents[2]
p=root/'Assets/Scripts/Enemy/EnemyBase.cs'
s=p.read_text('utf-8-sig')
def replace(old,new,count=1):
    global s
    assert s.count(old)==count, (old,s.count(old))
    s=s.replace(old,new)
replace('    private bool attacking;', '''    private readonly EnemyAttackCycle attackCycle = new EnemyAttackCycle();
    private bool attacking => attackCycle.Phase==EnemyAttackPhase.Windup || attackCycle.Phase==EnemyAttackPhase.Commit;
    public EnemyAttackPhase AttackPhase => attackCycle.Phase;
    public int AttackGeneration => attackCycle.Generation;''')
replace('public bool IsInRecovery => Time.time<recoveryUntil;', 'public bool IsInRecovery => Time.time<recoveryUntil || attackCycle.Phase==EnemyAttackPhase.Recovery;')
replace('ClearAttackWarning();StopAllCoroutines();attacking=detonating=false;', 'CancelAttackWork();detonating=false;',2)
replace('''    private void ResetTactics()
    {evasionUntil=recoveryUntil=nextEvasionTime=0;EvasionsStarted=ShotsCommitted=0;evasionSide=1;}''','''    private void ResetTactics()
    {attackCycle.Cancel();evasionUntil=recoveryUntil=nextEvasionTime=0;EvasionsStarted=ShotsCommitted=0;evasionSide=1;}
    private void CancelAttackWork()
    {attackCycle.Cancel();ClearAttackWarning();StopAllCoroutines();}''')
replace('''        if(CombatSliceSettings.Enabled)ClearAttackWarning();
        StopAllCoroutines(); attacking = false; detonating = false;''', '        CancelAttackWork();detonating=false;')
replace('StopAllCoroutines();attacking=false;', 'CancelAttackWork();')
replace('''    private float attackStarted, attackDuration;
    public float AttackWindup => attacking ? Mathf.Clamp01((Time.time - attackStarted) / attackDuration) : 0f;''','''    private float attackDuration => attackCycle.Duration;
    public float AttackWindup => attackCycle.Progress(Time.time);''')
replace('''        if(CombatSliceSettings.Enabled)ClearAttackWarning();
        StopAllCoroutines();
        attacking = false;''','        CancelAttackWork();')
replace('''    private void Update()
    {''','''    private void Update()
    {
        attackCycle.Tick(Time.time);''')
replace('BeginWindup(0.38f);','int token=BeginWindup(CombatRules.Current.MeleeWindup);')
replace('''        attackWarning=null;
        Vector3 delta = target.position - center;''','''        attackWarning=null;
        if(!CommitAttack(token))yield break;
        Vector3 delta = target.position - center;''')
replace('''        attacking = false;
    }

    private IEnumerator RangedStrike()''','''        attackCycle.Complete(Time.time,CombatLoopV2.Enabled?CombatRules.Current.MeleeRecovery:0);
    }

    private IEnumerator RangedStrike()''')
replace('BeginWindup(UsesDirectionalArmor?.65f:.55f);','int token=BeginWindup(UsesDirectionalArmor?CombatRules.Current.EliteWindup:CombatRules.Current.RangedWindup,CommittedShotDirection);')
replace('''        if (damageable.IsDead) yield break;
        if(rifleMuzzle!=null)''','''        if(!CommitAttack(token))yield break;
        if(CombatLoopV2.Enabled)direction=attackCycle.Direction;
        if(rifleMuzzle!=null)''')
replace('''        attacking = false;
        ShotsCommitted++;
        if(UsesDirectionalArmor)recoveryUntil=Time.time+CombatLoopV2.EliteRecovery;''','''        attackCycle.Complete(Time.time,UsesDirectionalArmor?CombatLoopV2.EliteRecovery:CombatLoopV2.Enabled?CombatRules.Current.RangedRecovery:0);
        ShotsCommitted++;''')
replace('''    private void BeginWindup(float duration)
    {
        attacking = true;
        attackStarted = Time.time;
        attackDuration = duration;
    }''','''    private int BeginWindup(float duration, Vector3 direction=default)
    {
        return attackCycle.Begin(Time.time,duration,direction);
    }
    private bool CommitAttack(int token)
    {
        bool canAct=damageable!=null&&!damageable.IsDead&&target!=null
            && (GameManager.Instance==null || GameManager.Instance.IsCombatActive)
            && (Stability==null || !Stability.IsBroken) && meleeStagger<=0;
        return attackCycle.TryCommit(token,Time.time,canAct);
    }''')
replace('''        detonating = true;
        CombatFeedback.SpawnWarningDisc(transform.position, 2.2f, 0.55f, new Color(1f, 0.2f, 0.015f, 1f));''','''        detonating = true;
        int token=BeginWindup(.55f);
        TrackWarning(CombatEffects.Disc(transform.position, 2.2f, 0.55f, new Color(1f, 0.2f, 0.015f, 1f)));''')
replace('''        if (damageable != null && !damageable.IsDead)
        {
            Explode();''','''        if(CommitAttack(token))
        {
            Explode();''')
p.write_text(s,encoding='utf-8')
print('Enemy attack cycle migrated')
