using UnityEngine;

public class RunManager : MonoBehaviour
{
    public float StartTime { get; private set; }
    public bool IsRunning { get; private set; }
    private float finalElapsedTime;

    public float ElapsedTime
    {
        get { return IsRunning ? Time.time - StartTime : finalElapsedTime; }
    }

    public void BeginRun()
    {
        StartTime = Time.time;
        finalElapsedTime = 0f;
        IsRunning = true;
    }

    public void EndRun()
    {
        if (!IsRunning) return;
        finalElapsedTime = Time.time - StartTime;
        IsRunning = false;
    }

    public void ResumeRun()
    {
        StartTime = Time.time - finalElapsedTime;
        IsRunning = true;
    }
}
