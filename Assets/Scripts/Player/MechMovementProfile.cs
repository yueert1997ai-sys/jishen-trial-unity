using UnityEngine;

// The motor remains the sole owner of travel; presentation reads the same profile.
public readonly struct MechMovementProfile
{
    public readonly float speed, dashDistance, dashSeconds, acceleration, braking, reversal;
    MechMovementProfile(float speed,float distance,float seconds,float acceleration,float braking,float reversal)
    {this.speed=speed;dashDistance=distance;dashSeconds=seconds;this.acceleration=acceleration;this.braking=braking;this.reversal=reversal;}
    public static MechMovementProfile For(HeroMech hero)=>hero==HeroMech.Nemesis
        ?new MechMovementProfile(13.5f,6.5f,.13f,200,240,260)
        :new MechMovementProfile(10.4f,5,.16f,90,125,135);
}
