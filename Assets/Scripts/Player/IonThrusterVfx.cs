using UnityEngine;
using UnityEngine.Rendering;

// GB4-inspired compressed exhaust: three bright shock cells and drifting ions,
// attached to the actual nozzle rather than a screen-space full-body glow.
[DefaultExecutionOrder(105)]
public sealed class IonThrusterVfx : MonoBehaviour
{
    PlayerController player;MechDashPresentation dash;ParticleSystem ions;
    LineRenderer[] cells=new LineRenderer[3];float budget;
    public bool Visible=>cells[0]!=null&&cells[0].enabled;
    public void Initialize(PlayerController p,MechDashPresentation d,Material material)
    {
        player=p;dash=d;
        for(int i=0;i<3;i++)
        {
            var line=new GameObject("Ion shock cell "+i).AddComponent<LineRenderer>();line.transform.SetParent(transform,false);
            line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=17;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.enabled=false;cells[i]=line;
        }
        ions=new GameObject("Blue ion wake").AddComponent<ParticleSystem>();ions.transform.SetParent(transform,false);
        ions.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ions.main;main.playOnAwake=false;main.maxParticles=96;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.35f;main.startSize=.09f;main.startSpeed=0;main.gravityModifier=0;
        var emission=ions.emission;emission.enabled=false;var shape=ions.shape;shape.enabled=false;
        var color=ions.colorOverLifetime;color.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.cyan,.2f),new GradientColorKey(new Color(.06f,.25f,1),1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(0,1)});color.color=g;
        var renderer=ions.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.velocityScale=.025f;renderer.lengthScale=2;renderer.shadowCastingMode=ShadowCastingMode.Off;
        ions.Play();
    }
    void LateUpdate()
    {
        if(player==null)return;var gm=GameManager.Instance;
        if(gm!=null&&gm.IsPaused)return;
        bool active=gm!=null&&gm.IsCombatActive&&!player.GetComponent<Damageable>().IsDead&&dash.Pulse>.3f;
        for(int i=0;i<3;i++)
        {
            var line=cells[i];line.enabled=active;if(!active)continue;
            float z=.35f+i*.78f,r=(.26f-i*.046f)*dash.Pulse;
            line.startWidth=line.endWidth= .06f*(1-i*.18f);line.startColor=line.endColor=new Color(.52f,.96f,1,.75f-i*.17f);
            for(int k=0;k<17;k++){float angle=k*Mathf.PI/8;line.SetPosition(k,transform.position+transform.forward*z+transform.right*(Mathf.Cos(angle)*r)+transform.up*(Mathf.Sin(angle)*r));}
        }
        if(!active){budget=0;if(gm==null||gm.Phase!=GamePhase.Combat)ions.Clear();return;}
        budget+=Time.deltaTime*70;int count=Mathf.Min(8,Mathf.FloorToInt(budget));budget-=count;
        for(int i=0;i<count;i++)
        {
            float phase=Time.time*35+i*2.4f;var radial=transform.right*Mathf.Cos(phase)+transform.up*Mathf.Sin(phase);
            ions.Emit(new ParticleSystem.EmitParams{position=transform.position+radial*.1f,velocity=transform.forward*7+radial*.9f,startColor=Color.cyan,startSize=.10f,startLifetime=.38f},1);
        }
    }
}
