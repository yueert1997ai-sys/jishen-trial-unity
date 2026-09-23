using UnityEngine;

public enum BufferedCombatAction { Dash, Slash, Combo, Support, Primary, Count }

// One bounded slot per action. Time is the combat clock, not wall time or render count.
// Payload belongs to the press: releasing movement before a buffered dash cannot retarget it.
public sealed class CombatActionQueue
{
    public struct Request
    {
        public bool Pending, Moving;
        public long Identity;
        public int Cancellation;
        public float PressedAt, Expires;
        public Vector3 Direction;
    }
    long nextIdentity;
    readonly Request[] requests = new Request[(int)BufferedCombatAction.Count];
    public int Generation { get; private set; }
    public void Enqueue(BufferedCombatAction action, float now, float lifetime, Vector3 direction = default, bool moving = false)
    {
        requests[(int)action] = new Request { Pending = true, Identity=++nextIdentity,
            Cancellation=CombatRuntime.ActionGeneration, PressedAt=now,
            Expires = now + Mathf.Clamp(lifetime,0,.5f), Direction = direction, Moving=moving };
    }
    public bool TryPeek(BufferedCombatAction action, float now, out Vector3 direction)
    {
        int index=(int)action;
        if(requests[index].Pending && requests[index].Cancellation==CombatRuntime.ActionGeneration && now <= requests[index].Expires + .000001f)
        { direction=requests[index].Direction; return true; }
        requests[index]=default; direction=default; return false;
    }
    public bool Pending(BufferedCombatAction action, float now) => TryPeek(action,now,out _);
    public bool TryRead(BufferedCombatAction action,float now,out Request request)
    { bool valid=TryPeek(action,now,out _);request=requests[(int)action];return valid; }
    public bool WasMoving(BufferedCombatAction action) => requests[(int)action].Moving;
    public void Extend(BufferedCombatAction action, float dt)
    { ref var request=ref requests[(int)action];if(request.Pending)request.Expires=Mathf.Min(request.PressedAt+.5f,request.Expires+Mathf.Max(0,dt)); }
    public void Cancel(BufferedCombatAction action) { requests[(int)action]=default; }
    public void Clear() { System.Array.Clear(requests,0,requests.Length); Generation++; }
}
