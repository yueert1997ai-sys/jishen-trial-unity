using UnityEngine;

// Runtime-only tuning: the authored V7 asset and its validated blade mounting stay intact.
public static class P0ComboProfile
{
    private static ValkyrComboProfile active;
    public static ValkyrComboProfile Active
    {
        get
        {
            if(active!=null)return active;
            active=ScriptableObject.CreateInstance<ValkyrComboProfile>();
            active.name="P0 Fast Fast Heavy";
            active.continuousBladePath=true;
            active.strokes=Build();return active;
        }
    }
    static ValkyrComboProfile.Stroke[] Build()
    {
        var source=ValkyrComboProfile.PowerDefaults();
        var a=source[0].keys[5];
        var b=source[1].keys[6];
        var ready=ValkyrComboProfile.Ready;
        ValkyrComboProfile.Key At(ValkyrComboProfile.Key k,float t){k.time=t;return k;}
        // A small counter wind-up, then chest -> elbow -> wrist. No neutral key between cuts.
        var prep=ValkyrComboProfile.Blend(ready,source[0].keys[1],.38f);
        var first=new ValkyrComboProfile.Stroke{label="快 · 斜斩",contactStart=.075f,contactEnd=.19f,linkTime=.23f,duration=.42f,advance=.44f,hitHold=.045f,
            keys=new[]{At(ready,0),At(prep,.045f),At(source[0].keys[2],.075f),At(source[0].keys[3],.125f),At(source[0].keys[4],.185f),At(a,.23f),At(a,.29f),At(ready,.42f)}};
        // Reverse directly from the last blade location; remove the long open-handed grip flourish.
        var middle=source[1].keys[4];middle.forwardGrip=0;middle.open=0;
        b.forwardGrip=0;b.open=0;
        var loaded=a;loaded.chest.y+=14;loaded.waist.y+=8;loaded.hips.y+=4;
        var second=new ValkyrComboProfile.Stroke{label="快 · 反向回斩",contactStart=.035f,contactEnd=.15f,linkTime=.19f,duration=.39f,advance=.38f,hitHold=.040f,
            keys=new[]{At(a,0),At(loaded,.025f),At(middle,.09f),At(b,.15f),At(b,.19f),At(b,.25f),At(ready,.39f)}};
        // Second follow-through feeds the overhead load, retaining the proven heavy blade plane.
        var third=source[2];third.label="重 · 推进终结斩";third.contactStart=.24f;third.contactEnd=.40f;
        third.linkTime=third.duration=.80f;third.advance=1.25f;third.hitHold=.095f;third.movementScale=.55f;
        var heavyKeys=new ValkyrComboProfile.Key[third.keys.Length];
        for(int i=0;i<heavyKeys.Length;i++)
        {
            var k=third.keys[i];k.time*=.8f;
            // Keep one grip across the chain; a mid-combo blade flip is not needed for the P0 rhythm.
            k.forwardGrip=0;k.open=0;heavyKeys[i]=k;
        }
        // The descending finish crosses a broad front arc, with the chest pulling the shoulder through.
        heavyKeys[2].chest.y=58;heavyKeys[2].waist.y=38;heavyKeys[2].hips.y=24;
        // Compress the chassis into the load, then drive through the planted support leg.
        heavyKeys[2].pelvis.y-=.12f;
        heavyKeys[3].pelvis.y-=.10f;
        heavyKeys[3].blade=new Vector3(.74f,.45f,.50f).normalized;
        heavyKeys[3].chest.y=43;heavyKeys[3].waist.y=25;
        heavyKeys[4].wrist=new Vector3(.52f,2.55f,1.15f);
        heavyKeys[4].blade=new Vector3(.05f,-.20f,.98f).normalized;
        heavyKeys[5].wrist=new Vector3(-.32f,1.80f,.78f);
        heavyKeys[5].elbow=new Vector3(.05f,2.48f,.18f);
        heavyKeys[5].blade=new Vector3(-.82f,-.50f,.28f).normalized;
        heavyKeys[5].chest.y=-66;heavyKeys[5].waist.y=-43;heavyKeys[5].hips.y=-27;
        heavyKeys[6].wrist=new Vector3(-.28f,1.74f,.60f);
        heavyKeys[6].elbow=new Vector3(.03f,2.40f,.12f);
        heavyKeys[6].blade=new Vector3(-.73f,-.57f,-.30f).normalized;
        heavyKeys[6].chest.y=-60;heavyKeys[6].waist.y=-39;heavyKeys[6].hips.y=-24;
        heavyKeys[0]=At(b,0);heavyKeys[heavyKeys.Length-1]=At(ready,.80f);third.keys=heavyKeys;
        var strokes=new[]{first,second,third};
        // The leading foot lands before the blade can deal damage; keep it grounded through contact.
        foreach(var stroke in strokes)for(int i=0;i<stroke.keys.Length;i++)
        {
            var key=stroke.keys[i];
            if(key.time>=stroke.contactStart&&key.time<=stroke.contactEnd)
            {key.leftFoot.y=0;key.rightFoot.y=0;}
            stroke.keys[i]=key;
        }
        return strokes;
    }
}
