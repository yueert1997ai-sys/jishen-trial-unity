using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MobileTravelChecks
{
    static T Find<T>(string name) where T : Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.name == name);
    static PointerEventData Pointer(int id, RectTransform rect, Vector2 offset)
    {
        var canvas = rect.GetComponentInParent<Canvas>();
        return new PointerEventData(EventSystem.current) { pointerId = id, button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, rect.position) + offset * canvas.scaleFactor };
    }
    static Rect Bounds(RectTransform rect)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
    }
    public static IEnumerator Run(GameManager gm, PlayerController p, string output, Action<bool,string> check, Action<string> capture)
    {
        check(MobilePlatform.UsesTouch, "explicit batch fixture activates production touch path");
        var input = p.InputRouter;
        input.readKeyboard = true; // Mobile must reject desktop fallback even with the serialized default true.
        input.Clear();
        check(!input.ReadCommand().HasAim, "idle touch device does not invent mouse aim");
        for (int n=0;n<4;n++) yield return null;
        var controls = p.GetComponent<MobileControls>();
        Canvas.ForceUpdateCanvases();
        var move = controls.Joystick; var aim = controls.AimJoystick;
        check(move.gameObject.activeInHierarchy && aim.gameObject.activeInHierarchy, "both sticks visible in mobile combat");
        check(!Bounds(Find<RectTransform>("StatusPanel")).Overlaps(Bounds((RectTransform)move.transform)) &&
            !Bounds(Find<RectTransform>("ActionPanel")).Overlaps(Bounds((RectTransform)aim.transform)), "health and ammo panels clear both thumb controls");
        check(Find<Button>("PauseButton").GetComponent<RectTransform>().rect.height>=44, "mobile pause target has 44 unit height");
        var left = Pointer(10, (RectTransform)move.transform, Vector2.up * 44);
        var right = Pointer(20, (RectTransform)aim.transform, Vector2.right * 44);
        move.OnPointerDown(left); aim.OnPointerDown(right);
        var command = input.ReadCommand();
        check(command.Move.y > .95f && command.Fire && command.HasAim, "two independent fingers move and fire together");
        move.OnPointerUp(right);
        check(move.PointerId == 10, "other finger release does not release movement");
        aim.OnPointerUp(right);
        command = input.ReadCommand();
        check(command.Move.y > .95f && !command.Fire && !command.HasAim, "releasing aim stops fire without mouse fallback or lost movement");
        move.OnCancel(new BaseEventData(EventSystem.current));
        check(input.ReadCommand().Move == Vector2.zero && move.PointerId == int.MinValue, "cancel releases movement ownership");
        move.OnPointerDown(left); move.SendMessage("OnApplicationPause", true);
        check(input.ReadCommand().Move == Vector2.zero && move.PointerId == int.MinValue, "OS pause releases joystick pointer");

        var dash = Find<MobileActionButton>("DashButton");
        dash.GetComponent<Button>().interactable = true;
        var finger = Pointer(30, (RectTransform)dash.transform, Vector2.zero);
        dash.OnPointerDown(finger); command = input.ReadCommand();
        check(command.Dash && command.BoostHeld && dash.IsHoldingBoost, "touch dash queues dash and held boost");
        dash.OnPointerUp(right);
        check(dash.IsHoldingBoost, "unrelated finger cannot release boost");
        dash.OnCancel(new BaseEventData(EventSystem.current));
        check(!dash.IsHoldingBoost && !input.ReadCommand().BoostHeld, "pointer cancel releases held boost");
        dash.OnPointerDown(finger); dash.SendMessage("OnApplicationFocus", false);
        check(!dash.IsHoldingBoost && !input.ReadCommand().BoostHeld, "focus loss releases held boost");
        input.Clear();
        dash.OnPointerDown(finger); dash.SendMessage("OnApplicationPause", true);
        check(!dash.IsHoldingBoost && !input.ReadCommand().BoostHeld, "OS pause releases held boost");
        input.Clear();

        var lifecycle = Object.FindFirstObjectByType<MobileSessionLifecycle>();
        check(lifecycle != null, "mobile lifecycle installed automatically");
        GamePreferences.SetMusic(.37f);
        move.OnPointerDown(left); aim.OnPointerDown(right);
        lifecycle.SendMessage("OnApplicationPause", true);
        check(gm.IsPaused && !gm.CanPlayerControl && AudioListener.pause, "background pauses combat and audio");
        check(Mathf.Abs(PlayerPrefs.GetFloat("MechTrial.Slice.Music") - .37f) < .001f, "background flushes settings without closing settings panel");
        for(int n=0;n<3;n++)yield return null;
        command=input.ReadCommand();
        check(command.Move==Vector2.zero && !command.Fire && !command.BoostHeld, "background removes all held input");
        lifecycle.SendMessage("OnApplicationPause", false);
        check(gm.IsPaused, "returning from background waits for explicit resume");
        Find<Button>("ResumeButton").onClick.Invoke();
        check(!gm.IsPaused && !AudioListener.pause, "real resume button restores combat and audio");

        GamePreferences.SetBatterySaver(false);
        check(Application.targetFrameRate==60, "normal mobile target capped at 60");
        gm.settingsUI.Show(gm);yield return null;
        var battery=Find<Button>("MobileBatteryButton");
        battery.onClick.Invoke();
        check(GamePreferences.BatterySaver && Application.targetFrameRate==30 && PlayerPrefs.GetInt("MechTrial.Slice.BatterySaver")==1, "settings button enables and persists 30fps mode");
        Canvas.ForceUpdateCanvases();
        var panel=Find<RectTransform>("SettingsPanel");
        var fit=panel.GetComponent<MobileMenuFit>();
        check(fit!=null, "phone settings have safe-area fit");
        var safe=panel.GetComponentInParent<SafeAreaLayout>();safe.useScreen=false;
        foreach(var inset in new[]{new Rect(72,30,1510,710),new Rect(18,30,1510,710)})
        {
            safe.Apply(new Vector2(1600,740),inset);Canvas.ForceUpdateCanvases();fit.Fit();
            var parent=(RectTransform)panel.parent;
            check(panel.rect.height*panel.localScale.y<=parent.rect.height-15 && panel.rect.width*panel.localScale.x<=parent.rect.width-15, "settings including footer fit simulated phone notch / home safe area");
        }
        safe.useScreen=true;
        capture("production-mobile-settings.png");
        battery.onClick.Invoke();gm.settingsUI.Hide();
        check(!GamePreferences.BatterySaver && Application.targetFrameRate==60 && !gm.IsPaused, "close settings preserves selected mode and resumes previous combat state");

        var absorption=gm.equipmentLoop.Absorption;
        var pickup=new GameObject("MobileRecoveryFixture");pickup.transform.position=p.transform.position+Vector3.up*.65f;
        var module=GameObject.CreatePrimitive(PrimitiveType.Cube);module.transform.SetParent(pickup.transform,false);
        typeof(SliceAbsorption).GetMethod("Offer",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(absorption,new object[]{pickup,module,"e01_rifle"});
        for(int n=0;n<12;n++)yield return null;
        var recover=Find<Button>("MobileRecoverButton");
        check(recover.gameObject.activeInHierarchy && recover.interactable, "nearby recoverable equipment enables touch recovery");
        recover.onClick.Invoke();
        check(absorption.Busy && gm.equipmentLoop.Warehouse.Owns("e01_rifle"), "touch recovery starts real installation after committing local ownership");
        absorption.Cancel();absorption.ClearOffer();
        var restored=new SalvageWarehouse(gm.equipmentLoop.Warehouse.Path);
        check(restored.Owns("e01_rifle"), "equipment survives offline file reload after interrupted installation");
        for(int n=0;n<12;n++)yield return null;
        check(!recover.gameObject.activeSelf, "recovery button hides once no offer remains");
        capture("production-mobile-combat.png");
        gm.ExitPractice();for(int n=0;n<3;n++)yield return null;
        check(!move.gameObject.activeInHierarchy, "touch combat controls hide on return to hangar");
        GamePreferences.SetMusic(1);GamePreferences.Save();
    }
}
