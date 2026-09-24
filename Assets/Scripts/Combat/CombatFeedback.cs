using System.Collections;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Damageable))]
public class CombatFeedback : MonoBehaviour
{
    public Color hitColor = Color.white;
    public float flashDuration = 0.08f;

    private Damageable damageable;
    private Renderer[] renderers;
    private Coroutine flashRoutine;
    private MaterialPropertyBlock flashBlock;
    public HitPresentation LastPresentation {get;private set;}
    public int HitEvents {get;private set;}

    private void Awake()
    {
        damageable = GetComponent<Damageable>();
        flashBlock = new MaterialPropertyBlock();
    }

    private void Start()
    {
        CacheRenderers();
    }

    private void OnEnable()
    {
        if (damageable == null)
        {
            damageable = GetComponent<Damageable>();
        }

        if (damageable != null)
        {
            damageable.OnResolved += HandleResolved;
            damageable.OnDied += HandleDied;
        }
    }

    private void OnDisable()
    {
        if (damageable != null)
        {
            damageable.OnResolved -= HandleResolved;
            damageable.OnDied -= HandleDied;
        }
    }

    public static void EncounterCleared(Vector3 lastKill)
    {
        GameAudio.PlayAt(GameAudioCue.Wave,lastKill,.36f,1.15f);
        CombatEffects.Impact(lastKill+Vector3.up,new Color(.28f,.86f,1),.7f,true);
        Camera.main?.GetComponent<CameraFollow>()?.AddShake(.16f,.12f);
    }

    private void HandleResolved(Damageable target, DamageResult result)
    {
        if(!result.Applied)return;
        LastPresentation=result.Presentation;HitEvents++;
        ImpactAccentVfx.Get().Present(target,result);
        var info=result.ToDamageInfo();
        if(result.BrokeArmor)
        {
            GameAudio.PlayAt(GameAudioCue.ArmorBreak,target.AimCenter,.90f);
            OverdriveVfx.Break(target.AimCenter);
            if(GetComponent<EnemyBase>()!=null)
            {
                EnemyVfx.BreakBurst(target.AimCenter,new Color(1f,.5f,.12f));
                var direction=target.transform.position-info.SourcePosition;direction.y=0;
                (GetComponent<MechBladeHitReaction>()??gameObject.AddComponent<MechBladeHitReaction>()).Trigger(direction.normalized,true);
            }
            Camera.main?.GetComponent<CameraFollow>()?.AddShake(.22f,.14f);
        }
        HandleDamaged(target,info);
    }

    private void HandleDamaged(Damageable target, DamageInfo info)
    {
        CombatLabTelemetry.Feedback(target);
        if (GetComponent<E01SoldierMotion>() == null && flashRoutine == null && GetComponent<MechBladeHitReaction>() == null) flashRoutine = StartCoroutine(Flash());
        bool armorContact=GetComponent<E01SoldierMotion>()!=null&&info.HasContact;
        if(armorContact)
        {
            ArmorContactVfx.Get().Contact(target,info);
            EnemyVfx.MetalHit(info.ContactPoint,info.ContactNormal,info.HeavyImpact,true);
            if(info.HeavyImpact && !info.BrokeArmor)
                Camera.main?.GetComponent<CameraFollow>()?.AddShake(.14f,.09f);
            if(!info.HeavyImpact&&GetComponent<EnemyBase>()?.HitStaggerRemaining>0)
                Camera.main?.GetComponent<CameraFollow>()?.AddShake(.065f,.045f);
            if(info.MeleeStrike||info.BrokeArmor)SpawnDamageNumber(target.AimCenter+Vector3.up*.5f,info.Amount,info.BrokeArmor?Color.cyan:new Color(1,.78f,.35f));
            if(!info.MeleeStrike&&!info.BrokeArmor)GameAudio.PlayAt(info.HeavyImpact?GameAudioCue.ArmorClash:(info.ArmorDamage>0?GameAudioCue.Hit:GameAudioCue.HullHit),info.ContactPoint,.4f,Random.Range(.95f,1.05f));
            return;
        }
        if(info.HeavyImpact && !info.BrokeArmor && target.team!=0)
            Camera.main?.GetComponent<CameraFollow>()?.AddShake(.14f,.09f);
        Color pulseColor = target.team == 0 ? new Color(1f, 0.16f, 0.08f) : new Color(0.25f, 0.9f, 1f);
        if (info.Amount > 0)
        {
            Vector3 direction = Vector3.ProjectOnPlane(target.transform.position-info.SourcePosition,Vector3.up).normalized;
            if(direction.sqrMagnitude<.01f)direction=-target.transform.forward;
            // Emit on the visible armor surface instead of inside the chassis.
            var collider = target.GetComponent<Collider>();
            Vector3 outside = target.AimCenter - direction * (collider != null ? collider.bounds.extents.magnitude+1 : 1);
            Vector3 contact = collider != null ? collider.ClosestPoint(outside)-direction*.12f : target.AimCenter;
            OverdriveVfx.Hit(contact,-direction,info.HeavyImpact,info.DirectHit);
            if(target.team != 0)
                EnemyVfx.MetalHit(contact,-direction,info.HeavyImpact,false);
        }
        if(info.SourceObject!=null && info.SourceObject.name=="M7_KineticRound")pulseColor=new Color(1f,.76f,.40f);
        if(info.DirectHit)pulseColor=new Color(.4f,1,1);
        SpawnImpactPulse(transform.position + Vector3.up * 0.8f, pulseColor, target.team == 0 ? 0.5f : 0.3f);
        SpawnDamageNumber(transform.position + Vector3.up * (target.team == 0 ? 2.2f : 1.75f), info.Amount, pulseColor);
        bool sword=info.MeleeStrike;
        if(!sword)
        {
            if(target.team==0)GameAudio.Play(GameAudioCue.PlayerHit,.48f,info.HeavyImpact?.88f:.96f);
            else if(!info.BrokeArmor)GameAudio.PlayAt(info.HeavyImpact?GameAudioCue.MetalHitHeavy:GameAudioCue.MetalHitLight,target.AimCenter,.42f,Random.Range(.93f,1.08f));
        }

        // A hit that actually rocks the frame deserves a camera tick; the stagger
        // gate keeps rapid fire from turning this into constant vibration.
        if(target.team!=0 && !info.HeavyImpact && GetComponent<EnemyBase>()?.HitStaggerRemaining>0)
            Camera.main?.GetComponent<CameraFollow>()?.AddShake(.05f,.05f);

        if (target.team == 0)
        {
            CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (cameraFollow != null)
            {
                cameraFollow.AddShake(info.HeavyImpact ? .16f : .08f, info.HeavyImpact ? .16f : .10f);
            }
        }
    }

    private void HandleDied(Damageable target)
    {
        OverdriveVfx.Death(target.AimCenter);
        if(target.team!=0&&GetComponent<E01SoldierMotion>()!=null)
        {
            MechDeathVfx.Play(target);
            return;
        }
        Color burstColor = target.team == 0 ? new Color(0.1f, 0.75f, 1f) : new Color(1f, 0.18f, 0.06f);
        bool armor=GetComponent<E01SoldierMotion>()!=null;
        if(armor)ArmorContactVfx.Get().Death(target,target.LastHit);
        else SpawnDeathBurst(transform.position + Vector3.up * 0.75f, burstColor, target.team == 0 ? 1.4f : 0.75f);
        if(target.team != 0)
            EnemyVfx.DeathBurst(transform.position + Vector3.up * 0.9f, burstColor, target.GetComponent<BossController>() != null ? 2.2f : 1f);
        GameAudio.PlayAt(GameAudioCue.Death,target.AimCenter,target.team==0?.65f:.42f,Random.Range(.92f,1.04f));

        CameraFollow cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cameraFollow != null)
        {
            cameraFollow.AddShake(target.team == 0 ? 0.45f : 0.12f, target.team == 0 ? 0.45f : 0.12f);
        }
    }

    private IEnumerator Flash()
    {
        CacheRenderers();
        var saved = new MaterialPropertyBlock[renderers.Length];
        for (int i=0;i<renderers.Length;i++)
        {
            if(renderers[i]==null)continue;
            saved[i]=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(saved[i]);
            renderers[i].GetPropertyBlock(flashBlock);
            flashBlock.SetColor("_EmissionColor", new Color(1f,.45f,.16f)*.8f);
            renderers[i].SetPropertyBlock(flashBlock);
        }
        yield return new WaitForSeconds(.045f);
        for(int i=0;i<renderers.Length;i++)
            if(renderers[i]!=null)renderers[i].SetPropertyBlock(saved[i]);

        flashRoutine = null;
    }

    private void CacheRenderers()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true)
                .Where(r=>r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
        }
    }

    public void RefreshVisuals()
    {
        if(flashRoutine!=null){StopCoroutine(flashRoutine);flashRoutine=null;}
        renderers=null;CacheRenderers();
    }

    public static void SpawnGroundLine(Vector3 origin, Vector3 direction, float length, float width, float duration, Color color)
    {
        CombatEffects.Line(origin, direction, length, width, duration, color);
    }

    public static void SpawnWarningDisc(Vector3 position, float radius, float duration, Color color)
    {
        CombatEffects.Disc(position, radius, duration, color);
    }

    public static void SpawnImpactPulse(Vector3 position, Color color, float scale)
    {
        CombatEffects.Impact(position, color, scale);
    }

    private static void SpawnDamageNumber(Vector3 position, float amount, Color color)
    {
        CombatEffects.Number(position, amount, color);
    }

    private static void SpawnDeathBurst(Vector3 position, Color color, float scale)
    {
        CombatEffects.Impact(position, color, scale, true);
    }
}
