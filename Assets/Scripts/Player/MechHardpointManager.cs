using System.Collections.Generic;
using UnityEngine;

public class MechHardpointManager : MonoBehaviour
{
    public Transform visualRoot;
    public Transform hardpointsRoot;

    private readonly Dictionary<string, Transform> sockets = new Dictionary<string, Transform>();

    public static readonly string[] SocketNames =
    {
        "HeadSocket",
        "ChestSocket",
        "BackSocket",
        "LeftShoulderSocket",
        "RightShoulderSocket",
        "LeftHandSocket",
        "RightHandSocket",
        "LeftArmSocket",
        "RightArmSocket",
        "WaistSocket"
    };

    private static readonly Dictionary<string, Vector3> DefaultSocketPositions = new Dictionary<string, Vector3>
    {
        { "HeadSocket", new Vector3(0f, 3.18f, 0.18f) },
        { "ChestSocket", new Vector3(0f, 2.58f, 0.5f) },
        { "BackSocket", new Vector3(0f, 2.58f, -0.72f) },
        { "LeftShoulderSocket", new Vector3(-0.86f, 2.8f, 0.1f) },
        { "RightShoulderSocket", new Vector3(0.86f, 2.8f, 0.1f) },
        { "LeftHandSocket", new Vector3(-1.05f, 1.78f, 0.62f) },
        { "RightHandSocket", new Vector3(1.05f, 1.78f, 0.62f) },
        { "LeftArmSocket", new Vector3(-1.02f, 2.05f, 0.28f) },
        { "RightArmSocket", new Vector3(1.02f, 2.05f, 0.28f) },
        { "WaistSocket", new Vector3(0f, 1.72f, 0.02f) }
    };

    private void Awake()
    {
        EnsureDefaultHardpoints();
    }

    public void EnsureDefaultHardpoints()
    {
        if (visualRoot == null)
        {
            visualRoot = transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                visualRoot = new GameObject("VisualRoot").transform;
                visualRoot.SetParent(transform, false);
            }
        }

        if (hardpointsRoot == null)
        {
            hardpointsRoot = transform.Find("Hardpoints");
            if (hardpointsRoot == null)
            {
                hardpointsRoot = new GameObject("Hardpoints").transform;
                hardpointsRoot.SetParent(transform, false);
            }
        }

        sockets.Clear();
        for (int i = 0; i < SocketNames.Length; i++)
        {
            string socketName = SocketNames[i];
            Transform socket = hardpointsRoot.Find(socketName);
            if (socket == null)
            {
                socket = new GameObject(socketName).transform;
                socket.SetParent(hardpointsRoot, false);
                socket.localPosition = DefaultSocketPositions[socketName];
                socket.localRotation = Quaternion.identity;
            }

            sockets[socketName] = socket;
        }
    }

    public Transform GetSocket(string socketName)
    {
        EnsureDefaultHardpoints();
        if (!string.IsNullOrEmpty(socketName) && sockets.ContainsKey(socketName))
        {
            return sockets[socketName];
        }

        return sockets["ChestSocket"];
    }

    public Transform GetSocketForEquipment(EquipmentType equipmentType)
    {
        switch (equipmentType)
        {
            case EquipmentType.RightHandWeapon:
                return GetSocket("RightHandSocket");
            case EquipmentType.LeftHandWeapon:
                return GetSocket("LeftHandSocket");
            case EquipmentType.LeftShoulder:
                return GetSocket("LeftShoulderSocket");
            case EquipmentType.RightShoulder:
                return GetSocket("RightShoulderSocket");
            case EquipmentType.Backpack:
                return GetSocket("BackSocket");
            case EquipmentType.Shield:
                return GetSocket("LeftArmSocket");
            case EquipmentType.ChestCore:
                return GetSocket("ChestSocket");
            default:
                return GetSocket("ChestSocket");
        }
    }
}
