using UnityEngine;

public enum EnemyAttackPhase { Ready, Windup, Commit, Recovery }

// Generation spans telegraph to emission. Interruption invalidates all unfinished work for that attack.
public sealed class EnemyAttackCycle
{
    public EnemyAttackPhase Phase { get; private set; }
    public int Generation { get; private set; }
    public float Duration { get; private set; }
    public Vector3 Direction { get; private set; }
    float started, recoveryUntil;
    int cancellation;
    public int Begin(float now, float windup, Vector3 direction)
    {
        Generation++;started=now;Duration=Mathf.Max(.01f,windup);Direction=direction.normalized;
        cancellation=CombatRuntime.ActionGeneration;
        Phase=EnemyAttackPhase.Windup;return Generation;
    }
    public float Progress(float now) => Phase==EnemyAttackPhase.Windup?Mathf.Clamp01((now-started)/Duration):0;
    public bool IsCommitted(int token) => token==Generation && cancellation==CombatRuntime.ActionGeneration && Phase==EnemyAttackPhase.Commit;
    public bool TryCommit(int token, float now, bool canAct)
    {
        if(token!=Generation || cancellation!=CombatRuntime.ActionGeneration || Phase!=EnemyAttackPhase.Windup || !canAct || now+.00001f<started+Duration)return false;
        Phase=EnemyAttackPhase.Commit;return true;
    }
    public void Complete(float now, float recovery)
    {if(Phase!=EnemyAttackPhase.Commit)return;recoveryUntil=now+Mathf.Max(0,recovery);Phase=recovery>0?EnemyAttackPhase.Recovery:EnemyAttackPhase.Ready;}
    public void Tick(float now)
    {if(Phase==EnemyAttackPhase.Recovery && now>=recoveryUntil)Phase=EnemyAttackPhase.Ready;}
    public void Cancel() { Generation++;Phase=EnemyAttackPhase.Ready;Duration=0;Direction=default;recoveryUntil=0; }
}
