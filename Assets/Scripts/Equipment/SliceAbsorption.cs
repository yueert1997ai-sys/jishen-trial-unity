using UnityEngine;

// Shared acquisition transaction. Ownership commits first; installation commits only at Power completion.
[DefaultExecutionOrder(140)]
public sealed class SliceAbsorption : MonoBehaviour
{
    public enum Step { Idle, Pull, Catch, Lock, Power }
    public Step Phase { get; private set; }
    public bool Busy => Phase != Step.Idle;
    public bool Installed { get; private set; }
    public bool CarrierAssigned { get; private set; }
    public float Progress => Mathf.Clamp01(elapsed / duration);
    public float CatchPulse => Busy ? Mathf.Max(0,1-Mathf.Abs(Progress-.55f)/.12f) : 0;
    public bool Offering => pickup!=null && !Busy;
    public GameObject Pickup => pickup;
    public GameObject Module => module;
    public bool CanAbsorb => !Busy && pickup!=null && VisibleNearby();
    private EquipmentLoop loop;
    private PlayerController player;
    private GameObject pickup,module;
    private Transform hand;
    private Vector3 start,initialScale;
    private Quaternion initialRotation;
    private float elapsed,duration;
    private LineRenderer tether;
    private readonly RaycastHit[] hits=new RaycastHit[32];
    private Renderer[] armor;
    private MaterialPropertyBlock[] saved;
    private readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
    private string notice;
    private float noticeUntil;
    public void Initialize(EquipmentLoop owner)
    {
        loop=owner;player=owner.Owner.playerController;
        player.Dashed+=OnDash;player.GetComponent<Damageable>().OnDamaged+=OnHit;
    }
    public void Attach(EnemyBase enemy)
    {
        if(CarrierAssigned||enemy.TrainingTarget||enemy.kind!=EnemyKind.Ranged)return;
        if(CombatRuntime.Run?.Mode==CombatMode.FullDemo && loop.Owner.CompletedEncounters!=1)return;
        var soldier=enemy.GetComponent<E01SoldierMotion>();
        if(soldier==null||soldier.rifle==null)return;
        var carrier=enemy.GetComponent<SalvageCarrier>()??enemy.gameObject.AddComponent<SalvageCarrier>();
        carrier.gear=SalvageGear.Find("e01_rifle");carrier.visual=soldier.rifle.gameObject;
        CarrierAssigned=true;
    }
    public void Drop(EnemyBase enemy)
    {
        var carrier=enemy.GetComponent<SalvageCarrier>();
        if(enemy.TrainingTarget||Installed||pickup!=null||carrier==null||carrier.visual==null)return;
        pickup=new GameObject("Slice_E01_Recovery");pickup.transform.position=enemy.transform.position+Vector3.up*.65f;
        module=carrier.visual;module.transform.SetParent(pickup.transform,true);
        module.transform.localPosition=Vector3.zero;module.transform.localRotation=Quaternion.Euler(0,35,12);
        var soldier=enemy.GetComponent<E01SoldierMotion>();if(soldier!=null)soldier.rifle=null;
        foreach(var c in module.GetComponentsInChildren<Collider>())c.enabled=false;
        initialScale=module.transform.localScale;
        armor=module.GetComponentsInChildren<Renderer>();saved=new MaterialPropertyBlock[armor.Length];
        for(int i=0;i<armor.Length;i++){saved[i]=new MaterialPropertyBlock();armor[i].GetPropertyBlock(saved[i]);}
        Notice("步枪脱落 · 靠近后按 F 牵引装配");
    }
    private bool VisibleNearby()
    {
        if(loop==null||!loop.Owner.IsCombatActive||loop.Owner.IsPaused||player.GetComponent<Damageable>().IsDead)return false;
        Vector3 delta=pickup.transform.position-player.transform.position;delta.y=0;
        if(delta.sqrMagnitude>5.5f*5.5f)return false;
        Vector3 origin=player.transform.position+Vector3.up*1.3f;
        Vector3 ray=pickup.transform.position+Vector3.up*.3f-origin;
        int n=Physics.RaycastNonAlloc(origin,ray.normalized,hits,ray.magnitude,~0,QueryTriggerInteraction.Ignore);
        if(n==hits.Length)return false;
        for(int i=0;i<n;i++)if(hits[i].collider.GetComponentInParent<Damageable>()==null)return false;
        return true;
    }
    public bool TryBegin()
    {
        if(!CanAbsorb||player.IsDashing)return false;
        hand=player.GetComponent<MechHardpointManager>()?.GetSocket("RightHandSocket");
        if(hand==null||Resources.Load<GameObject>("E01/Rifle")==null)return false;
        bool known=loop.Warehouse.Owns("e01_rifle");
        if(!loop.Warehouse.Acquire("e01_rifle")){Notice("保存失败 · 武器保留，可再次按 F");return false;}
        loop.RecordAcquisition(!known);
        start=pickup.transform.position;initialRotation=module.transform.rotation;
        elapsed=0;duration=known?.65f:1.05f;Phase=Step.Pull;
        player.Melee.CancelAttack();player.Stance.BeginAbsorptionPose();
        tether=pickup.GetComponent<LineRenderer>()??pickup.AddComponent<LineRenderer>();
        tether.sharedMaterial=SalvageModuleVisual.Glow;tether.positionCount=2;tether.startWidth=.025f;tether.endWidth=.008f;
        tether.startColor=new Color(.3f,.85f,1,.8f);tether.endColor=new Color(.5f,.95f,1,.4f);tether.enabled=true;
        GameAudio.Play(GameAudioCue.SalvagePull,.3f);
        return true;
    }
    private void Update()
    {
        if(loop==null||loop.Owner.IsPaused)return;
        if(Busy&&(!loop.Owner.IsCombatActive||player.GetComponent<Damageable>().IsDead)){Cancel();return;}
        if(Busy)elapsed+=Time.deltaTime;
    }
    private void LateUpdate()
    {
        if(loop==null||loop.Owner.IsPaused||pickup==null)return;
        if(!Busy)
        {
            bool ready=CanAbsorb;
            for(int i=0;i<armor.Length;i++)if(armor[i]!=null)
            {armor[i].GetPropertyBlock(block);block.SetColor("_EmissionColor",ready?new Color(.12f,.35f,.45f):new Color(.035f,.07f,.09f));armor[i].SetPropertyBlock(block);}
            return;
        }
        float t=Progress;
        Step next=t<.50f?Step.Pull:t<.68f?Step.Catch:t<.84f?Step.Lock:Step.Power;
        if(next!=Phase)
        {
            Phase=next;
            if(next==Step.Catch)GameAudio.Play(GameAudioCue.SalvageCatch,.60f);
            if(next==Step.Lock)GameAudio.Play(GameAudioCue.SalvageLock,.65f);
            if(next==Step.Power)GameAudio.Play(GameAudioCue.SalvageReady,.50f);
        }
        float travel=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.14f,.50f,t));
        pickup.transform.position=Vector3.Lerp(start,hand.position,travel)+Vector3.up*(Mathf.Sin(travel*Mathf.PI)*.65f);
        Quaternion target=Quaternion.LookRotation(player.AimDirection,Vector3.up);
        module.transform.rotation=Quaternion.Slerp(initialRotation,target,Mathf.SmoothStep(0,1,t/.68f));
        module.transform.localScale=Vector3.Lerp(initialScale,Vector3.one*1.25f,Mathf.SmoothStep(0,1,t/.68f));
        tether.SetPosition(0,pickup.transform.position);tether.SetPosition(1,hand.position);tether.enabled=t<.52f;
        for(int i=0;i<armor.Length;i++)if(armor[i]!=null)
        {armor[i].GetPropertyBlock(block);block.SetColor("_EmissionColor",new Color(.12f,.45f,.7f)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.84f,1,t)));armor[i].SetPropertyBlock(block);}
        if(t<1)return;
        RestoreArmor();Installed=true;
        loop.InstallRecovered("e01_rifle");
        player.GetComponent<E01PlayerRifle>().Adopt(module);
        module=null;Destroy(pickup);pickup=null;Phase=Step.Idle;
        player.Stance.FinishAbsorptionPose();Notice("E-01 步枪就绪 · 左键开火 · 斩舰刀保留");
    }
    private void RestoreArmor(){if(armor!=null)for(int i=0;i<armor.Length;i++)if(armor[i]!=null)armor[i].SetPropertyBlock(saved[i]);}
    private void OnHit(Damageable target,DamageInfo hit){if(Busy)Cancel();}
    private void OnDash(Vector3 direction){if(Busy)Cancel();}
    public void Cancel()
    {
        if(!Busy)return;
        RestoreArmor();Phase=Step.Idle;elapsed=0;
        if(tether!=null)tether.enabled=false;
        if(pickup!=null){pickup.transform.position=start;module.transform.localScale=initialScale;module.transform.rotation=initialRotation;}
        player.Stance.ResetStance();GameAudio.StopSalvage();
        if(loop.Owner.IsCombatActive)GameAudio.Play(GameAudioCue.SalvageCancel,.27f);
        Notice("装配已取消 · 原枪保留 · 靠近按 F 重试");
    }
    public void ResetRun()
    {
        ClearOffer();Installed=false;CarrierAssigned=false;noticeUntil=0;
    }
    public void ClearOffer()
    {
        Cancel();
        if(pickup!=null)Destroy(pickup);pickup=module=null;
    }
    public bool CollectWithoutInstalling()
    {
        if(Busy)return false;
        if(pickup==null)return true;
        bool fresh=!loop.Warehouse.Owns("e01_rifle");
        if(!loop.Warehouse.Acquire("e01_rifle")){Notice("保存失败 · 请重试继续，武器仍保留");return false;}
        loop.RecordAcquisition(fresh);ClearOffer();return true;
    }
    private void Notice(string message){notice=message;noticeUntil=Time.time+2.5f;}
    public string StatusText=>Busy?(Phase==Step.Pull?"牵引中":Phase==Step.Catch?"接住":Phase==Step.Lock?"机械锁定":"能量启动")+" · 空格取消":Time.time<noticeUntil?notice:null;
    private void OnDestroy(){if(player!=null){player.Dashed-=OnDash;player.GetComponent<Damageable>().OnDamaged-=OnHit;}if(pickup!=null)Destroy(pickup);}
}
