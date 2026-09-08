using System.Collections.Generic;

public static class GameText
{
    public const string Credits = "Yue / AI-assisted game development\nHero / Rigged robot by joney_lol (CC BY 3.0)\nModified rig motion, materials and attachments\nMotion / Quaternius (CC0)\nSound / Kenney (CC0)\nMusic / Subspace by Vitalezzz (CC0)\nFont / Noto Sans CJK (OFL)\nHero source: poly.pizza/m/BwjA6Thdzd\nLicense: creativecommons.org/licenses/by/3.0/";
    public static IEnumerable<string> Translations => chinese.Values;
    private static readonly Dictionary<string, string> chinese = new Dictionary<string, string>
    {
        { "VALKYR / BAY 07", "VALKYR / 07 号格纳库" },
        { "PRE-SORTIE LOADOUT", "战前装备整理" },
        { "Choose your primary weapon", "选择主武器" },
        { "Unequip", "卸下" },
        { "Full body", "查看全身" },
        { "Close-up", "近距离查看" },
        { "Drag to rotate / scroll to zoom", "拖动旋转查看 / 滚轮缩放" },
        { "Unarmed. Select a weapon to deploy.", "当前空手，请选择武器后出击。" },
        { "M7 equipped / automatic fire", "已装备 M7 / 连续自动射击" },
        { "M14 equipped / heavy piercing rounds", "已装备 M14 / 重型穿透射击" },
        { "RAIKEN equipped / close-range slash", "已装备巨剑 / 近战挥斩" },
        { "M7 / ASSAULT RIFLE\nFast, sustained fire", "M7 / 突击步枪\n快速、持续火力" },
        { "RAIKEN / GREATSWORD\nClose-range sweeping attacks", "RAIKEN / 巨剑\n近距离范围挥斩" },
        { "M14 / BATTLE RIFLE\nSlower, powerful piercing rounds", "M14 / 战斗步枪\n低射速、高伤害、穿透目标" },
        { "MECH TRIAL", "\u673a\u795e\u8bd5\u70bc" },
        { "SORTIE 01", "\u51fa\u51fb\u4efb\u52a1 01" },
        { "ROSE GOLD\nSENTINEL", "\u73ab\u7470\u91d1\n\u54e8\u5175" },
        { "SWIFT\nSENTINEL", "\u8f7b\u88c5\n\u54e8\u5175" },
        { Credits, "Yue / AI \u8f85\u52a9\u6e38\u620f\u5f00\u53d1\n\u4e3b\u89d2 / joney_lol\uff1aRigged robot (CC BY 3.0)\n\u6539\u52a8\uff1a\u52a8\u4f5c\u3001\u6750\u8d28\u548c\u6b66\u5668\u6302\u70b9\n\u52a8\u4f5c / Quaternius (CC0)\n\u97f3\u6548 / Kenney (CC0)\n\u97f3\u4e50 / Vitalezzz\uff1aSubspace (CC0)\n\u5b57\u4f53 / Noto Sans CJK (OFL)\n\u6765\u6e90\uff1apoly.pizza/m/BwjA6Thdzd\n\u6388\u6743\uff1acreativecommons.org/licenses/by/3.0/" },
        { "OUTER DECK  /  REACTOR  /  BOSS", "\u5916\u90e8\u7532\u677f / \u53cd\u5e94\u5806 / \u5b88\u536b\u8005" },
        { "DEPLOY", "\u5f00\u59cb\u4efb\u52a1" },
        { "DEMO", "\u5c55\u793a" },
        { "STANDARD", "\u6807\u51c6" },
        { "VETERAN", "\u7cbe\u82f1" },
        { "Demo", "\u5c55\u793a" },
        { "Cadet", "\u5c55\u793a" },
        { "Standard", "\u6807\u51c6" },
        { "Veteran", "\u7cbe\u82f1" },
        { "MISSION PAUSED", "\u4efb\u52a1\u6682\u505c" },
        { "Resume", "\u7ee7\u7eed" },
        { "Settings", "\u8bbe\u7f6e" },
        { "Return to hangar", "\u8fd4\u56de\u673a\u5e93" },
        { "Done", "\u5b8c\u6210" },
        { "SETTINGS", "\u8bbe\u7f6e" },
        { "Language", "\u8bed\u8a00" },
        { "Master", "\u603b\u97f3\u91cf" },
        { "Music", "\u97f3\u4e50" },
        { "Effects", "\u97f3\u6548" },
        { "Camera shake", "\u955c\u5934\u9707\u52a8" },
        { "Quality", "\u753b\u8d28" },
        { "Balanced", "\u5747\u8861" },
        { "High", "\u9ad8" },
        { "Credits", "\u5236\u4f5c\u4fe1\u606f" },
        { "SELECT UPGRADE", "\u9009\u62e9\u5f3a\u5316" },
        { "Install", "\u5b89\u88c5" },
        { "MISSION COMPLETE", "\u4efb\u52a1\u5b8c\u6210" },
        { "MISSION FAILED", "\u4efb\u52a1\u5931\u8d25" },
        { "Retry sector (1)", "\u91cd\u8bd5\u6218\u533a\uff081\uff09" },
        { "Retry Boss (1)", "\u91cd\u8bd5\u9996\u9886\uff081\uff09" },
        { "DASH", "\u51b2\u523a" },
        { "SLASH", "\u6325\u5200" },
        { "SALVO", "\u9f50\u5c04" },
        { "Kills", "\u51fb\u7834" },
        { "Hostiles", "\u654c\u673a" },
        { "Salvage", "\u56de\u6536\u7269" },
        { "Time", "\u7528\u65f6" },
        { "SYSTEMS", "\u7cfb\u7edf" },
        { "AMP", "\u706b\u529b" },
        { "RATE", "\u5c04\u901f" },
        { "SPLIT", "\u5206\u88c2" },
        { "PIERCE", "\u7a7f\u900f" },
        { "BLAST", "\u7206\u70b8" },
        { "ARMOR", "\u88c5\u7532" },
        { "REPAIR", "\u7ef4\u4fee" },
        { "Overclocked Trigger", "\u8d85\u9891\u6273\u673a" },
        { "Splitter Beam", "\u5206\u88c2\u5149\u675f" },
        { "Piercing Rounds", "\u7a7f\u7532\u5149\u675f" },
        { "Burst Core", "\u7206\u88c2\u6838\u5fc3" },
        { "Reactive Armor", "\u53cd\u5e94\u88c5\u7532" },
        { "Dash Capacitor", "\u51b2\u523a\u7535\u5bb9" },
        { "Nanorepair Loop", "\u7eb3\u7c73\u4fee\u590d" },
        { "Beam Amplifier", "\u5149\u675f\u589e\u5e45" },
        { "+25% beam fire rate.\n\nPairs with Piercing Rounds.", "\u5149\u675f\u5c04\u901f +25%\u3002\n\n\u642d\u914d\u7a7f\u7532\u5149\u675f\u3002" },
        { "3 beams at 55% damage each.\n\nEach beam carries Blast and Pierce.", "\u53d1\u5c04 3 \u9053\u5149\u675f\uff0c\u6bcf\u9053\u4f24\u5bb3\u4e3a 55%\u3002\n\n\u5747\u53ef\u89e6\u53d1\u7206\u70b8\u548c\u7a7f\u900f\u3002" },
        { "5 beams at 40% damage each.\n\nWider crowd coverage.", "\u53d1\u5c04 5 \u9053\u5149\u675f\uff0c\u6bcf\u9053\u4f24\u5bb3\u4e3a 40%\u3002\n\n\u6269\u5927\u7fa4\u4f53\u706b\u529b\u8986\u76d6\u3002" },
        { "+2 pierced enemies per shot.\n\nPairs with Overclocked Trigger.", "\u6bcf\u9053\u5149\u675f\u989d\u5916\u7a7f\u900f 2 \u4e2a\u654c\u4eba\u3002\n\n\u642d\u914d\u8d85\u9891\u6273\u673a\u3002" },
        { "Blast radius 1.45 m.\n\nFull direct damage; splash falls off.", "\u7206\u70b8\u534a\u5f84 1.45 \u7c73\u3002\n\n\u76f4\u51fb\u5168\u989d\u4f24\u5bb3\uff0c\u6e85\u5c04\u968f\u8ddd\u79bb\u8870\u51cf\u3002" },
        { "Blast radius 2.10 m.\n\nFull direct damage; splash falls off.", "\u7206\u70b8\u534a\u5f84 2.10 \u7c73\u3002\n\n\u76f4\u51fb\u5168\u989d\u4f24\u5bb3\uff0c\u6e85\u5c04\u968f\u8ddd\u79bb\u8870\u51cf\u3002" },
        { "+35 max HP.\n\nRepair the added armor immediately.", "\u6700\u5927\u751f\u547d +35\u3002\n\n\u7acb\u5373\u4fee\u590d\u65b0\u589e\u88c5\u7532\u3002" },
        { "+0.6 m dash distance.\n-0.18 s dash cooldown.", "\u51b2\u523a\u8ddd\u79bb +0.6 \u7c73\u3002\n\u51b2\u523a\u51b7\u5374 -0.18 \u79d2\u3002" },
        { "+1.2 HP per kill.\n\nRepair 24 HP now.", "\u6bcf\u6b21\u51fb\u7834\u56de\u590d 1.2 \u751f\u547d\u3002\n\n\u7acb\u5373\u4fee\u590d 24 \u751f\u547d\u3002" },
        { "+28% beam damage.\n\nAlso amplifies the missile salvo.", "\u5149\u675f\u4f24\u5bb3 +28%\u3002\n\n\u540c\u65f6\u589e\u5f3a\u5bfc\u5f39\u9f50\u5c04\u3002" },
        { "SECURE MAINTENANCE DECK", "\u6e05\u7406\u7ef4\u4fee\u7532\u677f" },
        { "BREAK THE FIRING LINE", "\u7a81\u7834\u706b\u529b\u5c01\u9501" },
        { "HOLD THE TRANSFER GATE", "\u5b88\u4f4f\u8f6c\u8fd0\u95f8\u95e8" },
        { "ISOLATE REACTOR FEED", "\u5207\u65ad\u53cd\u5e94\u5806\u4f9b\u7ed9" },
        { "CLEAR THE COOLANT RING", "\u6e05\u7406\u51b7\u5374\u73af" },
        { "BREACH WARDEN CONTROL", "\u7a81\u7834\u5b88\u536b\u63a7\u5236\u533a" },
        { "REACTOR WARDEN", "\u53cd\u5e94\u5806\u5b88\u536b" },
        { "INCOMING", "\u6b63\u5728\u63a5\u8fd1" },
        { "PHASE 1", "\u7b2c\u4e00\u9636\u6bb5" },
        { "PHASE 2", "\u7b2c\u4e8c\u9636\u6bb5" },
        { "PHASE I", "\u7b2c\u4e00\u9636\u6bb5" },
        { "PHASE II", "\u7b2c\u4e8c\u9636\u6bb5" },
        { "ARMORED I", "\u88c5\u7532\u72b6\u6001 I" },
        { "ARMORED II", "\u88c5\u7532\u72b6\u6001 II" },
        { "CORE EXPOSED", "\u6838\u5fc3\u66b4\u9732" },
        { "FAN VOLLEY", "\u6247\u5f62\u9f50\u5c04" },
        { "ARTILLERY STRIKE", "\u8fde\u7eed\u70ae\u51fb" },
        { "RAM CHARGE", "\u7a81\u8fdb\u51b2\u649e" },
        { "REINFORCEMENTS", "\u589e\u63f4\u6765\u88ad" },
        { "ENCOUNTER CLEAR", "\u906d\u9047\u6218\u5b8c\u6210" },
        { "DEMO\nEnemy armor 78%  /  Damage 65%\nField repair 20%  /  One continue", "\u5c55\u793a\n\u654c\u65b9\u88c5\u7532 78% / \u4f24\u5bb3 65%\n\u6218\u540e\u4fee\u590d 20% / \u53ef\u7eed\u5173\u4e00\u6b21" },
        { "STANDARD\nEnemy armor 100%  /  Damage 100%\nField repair 12%  /  No continue", "\u6807\u51c6\n\u654c\u65b9\u88c5\u7532 100% / \u4f24\u5bb3 100%\n\u6218\u540e\u4fee\u590d 12% / \u65e0\u7eed\u5173" },
        { "VETERAN\nEnemy armor 125%  /  Damage 120%\nField repair 8%  /  No continue", "\u7cbe\u82f1\n\u654c\u65b9\u88c5\u7532 125% / \u4f24\u5bb3 120%\n\u6218\u540e\u4fee\u590d 8% / \u65e0\u7eed\u5173" },
        { "Yue / AI-assisted game development\nHero / user-provided Meshy asset\nSound / Kenney (CC0)\nMusic / Subspace by Vitalezzz (CC0)\nFont / Noto Sans CJK (OFL)\nEngine / Unity 6", "Yue / AI \u8f85\u52a9\u6e38\u620f\u5f00\u53d1\n\u4e3b\u89d2 / \u7528\u6237\u63d0\u4f9b\u7684 Meshy \u6a21\u578b\n\u97f3\u6548 / Kenney\uff08CC0\uff09\n\u97f3\u4e50 / Vitalezzz\uff1aSubspace\uff08CC0\uff09\n\u5b57\u4f53 / Noto Sans CJK\uff08OFL\uff09\n\u5f15\u64ce / Unity 6" },
    };

    public static string T(string english)
    {
        if (string.IsNullOrEmpty(english)) return "";
        return GamePreferences.Chinese && chinese.TryGetValue(english, out var value) ? value : english;
    }

    public static string Progress(string english)
    {
        if (!GamePreferences.Chinese || string.IsNullOrEmpty(english)) return english;
        var parts = english.Split('/');
        for (int i = 0; i < parts.Length; i++) parts[i] = T(parts[i].Trim());
        return string.Join(" / ", parts)
            .Replace("SECTOR ", "\u6218\u533a ")
            .Replace("ENCOUNTER ", "\u906d\u9047\u6218 ")
            .Replace(" OF 3", " / 3")
            .Replace("REPAIR +", "\u4fee\u590d +");
    }
}
