using Godot;
using System.Collections.Generic;

// Tiny persistent progress/settings store (user://save.cfg).
public static class Save
{
    const string Path = "user://save.cfg";
    public static int Unlocked = 1;        // highest level the player may start
    public static bool Minimap = true;
    public static bool Muted = false;
    public static int Coins = 0;
    public static readonly HashSet<string> Owned = new HashSet<string>();
    static bool loaded;

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        try
        {
            var cf = new ConfigFile();
            if (cf.Load(Path) != Error.Ok) return;
            Unlocked = Mathf.Clamp(cf.GetValue("progress", "unlocked", 1).AsInt32(), 1, Cfg.MaxLevel);
            Minimap = cf.GetValue("settings", "minimap", true).AsBool();
            Muted = cf.GetValue("settings", "muted", false).AsBool();
            Coins = Mathf.Max(0, cf.GetValue("shop", "coins", 0).AsInt32());
            foreach (var id in cf.GetValue("shop", "owned", "").AsString().Split(',')) if (id != "") Owned.Add(id);
        }
        catch { }
    }

    public static void Store()
    {
        try
        {
            var cf = new ConfigFile();
            cf.Load(Path);
            cf.SetValue("progress", "unlocked", Unlocked);
            cf.SetValue("settings", "minimap", Minimap);
            cf.SetValue("settings", "muted", Muted);
            cf.SetValue("shop", "coins", Coins);
            cf.SetValue("shop", "owned", string.Join(",", Owned));
            cf.Save(Path);
        }
        catch { }
    }

    public static void Unlock(int level)
    {
        level = Mathf.Min(level, Cfg.MaxLevel);
        if (level > Unlocked) { Unlocked = level; Store(); }
    }
}
