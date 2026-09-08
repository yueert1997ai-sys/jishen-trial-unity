using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HangarPresentation : MonoBehaviour
{
    private GameObject room;
    private readonly List<Light> hiddenLights = new List<Light>();
    private Color ambient;
    private float yaw = 20, pitch = 9, distance = 7.4f;
    private Vector2 previous;
    private bool dragging;
    public void Show(GameManager gm)
    {
        var armory = gm.playerController.GetComponent<PlayerLoadout>().Armory;
        if (armory == null || armory.roomPrefab == null) return;
        if (room == null) room = Instantiate(armory.roomPrefab);
        room.SetActive(true);
        hiddenLights.Clear();
        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (!light.transform.IsChildOf(room.transform) && light.enabled) { hiddenLights.Add(light); light.enabled = false; }
        ambient = RenderSettings.ambientLight; RenderSettings.ambientLight = new Color(.25f,.29f,.35f);
        if (gm.arenaSector != null) { gm.arenaSector.maintenance.SetActive(false); gm.arenaSector.reactor.SetActive(false); }
        gm.playerController.RestoreAt(new Vector3(0,.38f,0));
        yaw = 20; pitch = 9; distance = 7.4f;
    }
    public void Hide()
    {
        if (room != null) room.SetActive(false);
        foreach (var light in hiddenLights) if (light != null) light.enabled = true;
        hiddenLights.Clear(); RenderSettings.ambientLight = ambient;
        dragging = false;
    }
    public void Closeup(bool close) { distance = close ? 3.3f : 7.4f; }
    public void UpdateCamera(Camera camera, Transform target)
    {
        bool active = GameManager.Instance != null && !GameManager.Instance.IsPaused && !GameManager.Instance.settingsUI.IsVisible;
        Vector2 pointer = Input.mousePosition;
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (active && Input.GetMouseButtonDown(0) && !overUI) { previous = pointer; dragging = true; }
        if (!Input.GetMouseButton(0) || !active) dragging = false;
        if (dragging)
        {
            Vector2 delta = pointer - previous; previous = pointer;
            yaw -= delta.x * .22f; pitch = Mathf.Clamp(pitch + delta.y * .12f, -5, 25);
        }
        if (active && !overUI) distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * .35f, 3.3f, 8.5f);
        float y = yaw * Mathf.Deg2Rad, p = pitch * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Sin(y)*Mathf.Cos(p),Mathf.Sin(p),Mathf.Cos(y)*Mathf.Cos(p));
        Vector3 right = new Vector3(Mathf.Cos(y),0,-Mathf.Sin(y));
        Vector3 center = target.position + Vector3.up * (distance < 4 ? 2.92f : 1.8f) - right * (distance < 4 ? .38f : .7f);
        camera.orthographic = false; camera.fieldOfView = 38; camera.rect = new Rect(0,0,1,1);
        camera.transform.position = center + direction * distance;
        camera.transform.LookAt(center);
    }
}
