using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public struct PlayerCommand
{
    public long Identity;
    public float InputTime;
    public int Cancellation;
    public Vector2 Move;
    public bool Dash;
    public bool BoostHeld;
    public bool Skill;
    public bool Melee;
    public bool Fire;
    public bool HasAim;
    public Vector3 AimPoint;
}

[DisallowMultipleComponent]
public class PlayerInputRouter : MonoBehaviour
{
    // Opt-in command replay can run in an unfocused Player without touching the desktop.
    public static bool AllowUnfocusedReplay { get; internal set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetReplay() { AllowUnfocusedReplay = false; }
    public bool readKeyboard = true;
    private Vector2 touchMove;
    private bool dashQueued;
    private bool skillQueued;
    private bool meleeQueued, touchFire, boostHeld;
    private Vector2 touchAim;
    private Camera aimCamera;
    private bool mouseFire;
    private long sequence;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private PointerEventData mousePointer;
    private EventSystem pointerSystem;

    public void SetTouchMove(Vector2 move) { touchMove = Vector2.ClampMagnitude(move, 1f); }
    public void QueueDash() { dashQueued = true; }
    public void SetBoostHeld(bool value) { boostHeld = value; }
    public void QueueSkill() { skillQueued = true; }
    public void QueueMelee() { meleeQueued = true; }
    public void SetTouchAim(Vector2 aim, bool held)
    {
        touchAim = Vector2.ClampMagnitude(aim, 1f);
        touchFire = held && touchAim.sqrMagnitude > 0.001f;
    }

    public PlayerCommand ReadCommand()
    {
        var gm = GameManager.Instance;
        if (gm != null && !gm.CanPlayerControl)
        {
            Clear();
            return default;
        }
        Vector2 move = touchMove;
        bool overUI = false;
        if (readKeyboard && Input.touchCount == 0 && EventSystem.current != null)
        {
            if (pointerSystem != EventSystem.current)
            {
                pointerSystem = EventSystem.current;
                mousePointer = new PointerEventData(pointerSystem);
            }
            mousePointer.position = Input.mousePosition;
            uiHits.Clear();
            pointerSystem.RaycastAll(mousePointer, uiHits);
            overUI = uiHits.Count > 0;
        }
        if (!Input.GetMouseButton(0)) mouseFire = false;
        if (readKeyboard && Input.GetMouseButtonDown(0) && Input.touchCount == 0 && !overUI) mouseFire = true;
        if (readKeyboard)
        {
            Vector2 keys = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            if (keys.sqrMagnitude > 0f) move = keys;
        }
        var command = new PlayerCommand
        {
            Identity=++sequence, InputTime=CombatRuntime.SimulationTime, Cancellation=CombatRuntime.ActionGeneration,
            Move = Vector2.ClampMagnitude(move, 1f),
            Dash = dashQueued || (readKeyboard && Input.GetKeyDown(KeyCode.Space)),
            BoostHeld = boostHeld || (readKeyboard && Input.GetKey(KeyCode.Space)),
            Skill = skillQueued || (readKeyboard && Input.GetKeyDown(KeyCode.E)),
            Melee = meleeQueued || (readKeyboard && Input.GetKeyDown(KeyCode.Q)),
            HasAim = touchFire,
            Fire = touchFire,
            AimPoint = transform.position + new Vector3(touchAim.x, 0, touchAim.y).normalized * 14f + Vector3.up * 1.1f
        };
        if (readKeyboard && Input.touchCount == 0 && !touchFire
            && !overUI)
        {
            if (aimCamera == null) aimCamera = Camera.main;
            if (aimCamera != null && new Plane(Vector3.up, transform.position + Vector3.up * 1.1f)
                .Raycast(aimCamera.ScreenPointToRay(Input.mousePosition), out float distance))
            {
                command.HasAim = true;
                command.AimPoint = aimCamera.ScreenPointToRay(Input.mousePosition).GetPoint(distance);
                command.Fire = mouseFire;
                command.Melee |= Input.GetMouseButtonDown(1);
            }
        }
        dashQueued = skillQueued = meleeQueued = false;
        return command;
    }

    public void Clear()
    {
        touchMove = Vector2.zero;
        touchAim = Vector2.zero;
        touchFire = false;
        boostHeld = false;
        mouseFire = false;
        dashQueued = skillQueued = meleeQueued = false;
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
        if (!AllowUnfocusedReplay && GameManager.Instance != null) GameManager.Instance.SetPaused(true);
    }
}
