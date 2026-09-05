using UnityEngine;

public struct PlayerCommand
{
    public Vector2 Move;
    public bool Dash;
    public bool Skill;
}

[DisallowMultipleComponent]
public class PlayerInputRouter : MonoBehaviour
{
    public bool readKeyboard = true;
    private Vector2 touchMove;
    private bool dashQueued;
    private bool skillQueued;

    public void SetTouchMove(Vector2 move) { touchMove = Vector2.ClampMagnitude(move, 1f); }
    public void QueueDash() { dashQueued = true; }
    public void QueueSkill() { skillQueued = true; }

    public PlayerCommand ReadCommand()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.CanPlayerControl)
        {
            Clear();
            return default;
        }
        Vector2 move = touchMove;
        if (readKeyboard)
        {
            Vector2 keys = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            if (keys.sqrMagnitude > 0f) move = keys;
        }
        var command = new PlayerCommand
        {
            Move = Vector2.ClampMagnitude(move, 1f),
            Dash = dashQueued || (readKeyboard && Input.GetKeyDown(KeyCode.Space)),
            Skill = skillQueued || (readKeyboard && Input.GetKeyDown(KeyCode.E))
        };
        dashQueued = skillQueued = false;
        return command;
    }

    public void Clear()
    {
        touchMove = Vector2.zero;
        dashQueued = skillQueued = false;
    }

    private void OnDisable() { Clear(); }
    private void OnApplicationFocus(bool focused)
    {
        if (!focused) Suspend();
    }
    private void OnApplicationPause(bool paused)
    {
        if (paused) Suspend();
    }
    private void Suspend()
    {
        Clear();
        if (GameManager.Instance != null) GameManager.Instance.SetPaused(true);
    }
}
