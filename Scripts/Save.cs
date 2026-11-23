using Godot;

// Tiny persistent progress/settings store (user://save.cfg).
public static class Save
{
    const string Path = "user://save.cfg";
    public static int Unlocked = 1;        // highest level the player may start
    public static bool Minimap = true;
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
