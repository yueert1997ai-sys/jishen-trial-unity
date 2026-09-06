using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum SalvageSlot { Weapon, Backpack }

public sealed class SalvageGear
{
    public string id, name, english, description, englishDescription, source, englishSource;
    public SalvageSlot slot;
    public int projectiles = 1, pierce, missiles = 4;
    public float damage = 18f, interval = .18f, spread, dashDistance, dashCooldown, moveSpeed, skillCooldown = 10f;
    public Color color = new Color(.24f, .72f, .92f);
    public string Title => GamePreferences.Chinese ? name : english;
    public string Description => GamePreferences.Chinese ? description : englishDescription;
    public string Source => GamePreferences.Chinese ? source : englishSource;

    public static readonly SalvageGear[] All = {
        new SalvageGear { id = "e01_rifle", name = "E-01 制式步枪", english = "E-01 service rifle", damage = 22f, interval = .22f,
            description = "稳定的单发连射。击败白色步枪兵后吸收入库。", englishDescription = "Steady automatic fire. Recover from E-01 rifle soldiers.",
            source = "击败 E-01 步枪兵后吸收", englishSource = "Absorb from E-01 soldiers", color = new Color(.65f,.85f,1f) },
        new SalvageGear { id = "pulse", name = "脉冲炮", english = "Pulse cannon", description = "连续精准射击，适合稳定输出。", englishDescription = "Accurate, continuous fire.", source = "初始装备", englishSource = "Starter equipment" },
        new SalvageGear { id = "scatter", name = "裂扇散射炮", english = "Fan cannon", projectiles = 5, damage = 9f, interval = .46f, spread = 18f,
            description = "一次发射 5 发，近距离扇面压制。", englishDescription = "Five-shot fan. Strong at close range.", source = "已有收藏可用 · 新来源暂未开放", englishSource = "Existing collection usable; new source pending", color = new Color(.95f, .68f, .3f) },
        new SalvageGear { id = "lance", name = "贯穿重炮", english = "Lance cannon", damage = 42f, interval = .48f, pierce = 3,
            description = "低射速重击，可额外贯穿 3 个目标。", englishDescription = "Heavy shot. Pierces 3 extra targets.", source = "已有收藏可用 · 新来源暂未开放", englishSource = "Existing collection usable; new source pending", color = new Color(.52f, .67f, 1f) },
        new SalvageGear { id = "standard", slot = SalvageSlot.Backpack, name = "标准推进背包", english = "Standard pack",
            description = "标准冲刺，4 发导弹齐射。", englishDescription = "Standard dash. Four-missile salvo.", source = "初始装备", englishSource = "Starter equipment" },
        new SalvageGear { id = "vector", slot = SalvageSlot.Backpack, name = "矢量疾行背包", english = "Vector pack", dashDistance = 1.6f, dashCooldown = .35f, moveSpeed = .6f,
            description = "冲刺 +1.6m，冷却 -0.35s，移速 +0.6。", englishDescription = "Dash +1.6m, cooldown -0.35s, speed +0.6.", source = "已有收藏可用 · 新来源暂未开放", englishSource = "Existing collection usable; new source pending", color = new Color(.32f, .9f, .7f) },
        new SalvageGear { id = "salvo", slot = SalvageSlot.Backpack, name = "蜂群火力背包", english = "Swarm pack", missiles = 8, skillCooldown = 12f,
            description = "8 发导弹齐射，技能冷却 12s。", englishDescription = "Eight-missile salvo. 12s skill cooldown.", source = "已有收藏可用 · 新来源暂未开放", englishSource = "Existing collection usable; new source pending", color = new Color(.94f, .57f, .32f) }
    };
    public static SalvageGear Find(string id) => Array.Find(All, gear => gear.id == id);
}

// This file contains ownership and equipment selection only. Run buffs never enter it.
[Serializable]
public sealed class SalvageProfile
{
    public int version = 1;
    public List<string> owned = new List<string> { "pulse", "standard" };
    public string weapon = "pulse", backpack = "standard";
}

public sealed class SalvageWarehouse
{
    public SalvageProfile Profile { get; private set; }
    public string Path { get; }
    public string Error { get; private set; }
    public SalvageWarehouse(string path)
    {
        Path = path;
        Profile = Read(path) ?? Read(path + ".bak") ?? new SalvageProfile();
        Profile.owned = Profile.owned.FindAll(id => SalvageGear.Find(id) != null);
        Profile.owned = new List<string>(new HashSet<string>(Profile.owned));
        foreach (string id in new[] { "pulse", "standard" }) if (!Owns(id)) Profile.owned.Add(id);
        if (!Owns(Profile.weapon) || SalvageGear.Find(Profile.weapon).slot != SalvageSlot.Weapon) Profile.weapon = "pulse";
        if (!Owns(Profile.backpack) || SalvageGear.Find(Profile.backpack).slot != SalvageSlot.Backpack) Profile.backpack = "standard";
    }
    private static SalvageProfile Read(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var value = JsonUtility.FromJson<SalvageProfile>(File.ReadAllText(path));
            return value != null && value.version == 1 && value.owned != null ? value : null;
        }
        catch (Exception) { return null; }
    }
    public bool Owns(string id) => id != null && Profile.owned.Contains(id);
    public bool Acquire(string id)
    {
        if (SalvageGear.Find(id) == null) return false;
        if (Owns(id)) return true;
        var next = JsonUtility.FromJson<SalvageProfile>(JsonUtility.ToJson(Profile));
        next.owned.Add(id);
        return Save(next);
    }
    public bool Equip(string id)
    {
        var gear = SalvageGear.Find(id);
        if (gear == null || !Owns(id)) return false;
        var next = JsonUtility.FromJson<SalvageProfile>(JsonUtility.ToJson(Profile));
        if (gear.slot == SalvageSlot.Weapon) next.weapon = id;
        else next.backpack = id;
        return Save(next);
    }
    private bool Save(SalvageProfile next)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path)));
            string temp = Path + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(next, true));
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
            if (File.Exists(Path)) File.Replace(temp, Path, Path + ".bak");
            else File.Move(temp, Path);
            Profile = next;
            Error = null;
            return true;
        }
        catch (Exception ex) { Error = ex.Message; return false; }
    }
}
