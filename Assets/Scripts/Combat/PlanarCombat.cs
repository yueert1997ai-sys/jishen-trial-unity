using UnityEngine;

// Height is presentation only in the top-down demo; aiming and collision share the XZ floor plane.
public static class PlanarCombat
{
    public const float Height=1.1f;
    public static Vector3 Point(Vector3 value){value.y=Height;return value;}
    public static Vector3 Direction(Vector3 value,Vector3 fallback)
    {value.y=0;if(value.sqrMagnitude<.0001f){value=fallback;value.y=0;}return value.sqrMagnitude>.0001f?value.normalized:Vector3.forward;}
    public static Quaternion Rotation(Vector3 from,Vector3 target,Vector3 fallback)
        => Quaternion.LookRotation(AimDirection(from,target,fallback),Vector3.up);
    public static Vector3 AimDirection(Vector3 from,Vector3 target,Vector3 forward)
    {
        var aim=Direction(target-from,forward);var facing=Direction(forward,Vector3.forward);
        return Vector3.Dot(aim,facing)>.05f?aim:facing;
    }
    public static bool FootprintHit(Collider shape,Vector3 origin,Vector3 direction,float distance,float width,out float at)
    {
        var bounds=shape.bounds;var offset=Point(bounds.center)-Point(origin);
        float radius=Mathf.Max(bounds.extents.x,bounds.extents.z)+width;
        float along=Vector3.Dot(offset,direction),side=offset.sqrMagnitude-along*along;
        at=0;if(side>radius*radius)return false;
        float half=Mathf.Sqrt(Mathf.Max(0,radius*radius-side));
        if(along+half<0)return false;
        at=Mathf.Max(0,along-half);return at<=distance;
    }
}
