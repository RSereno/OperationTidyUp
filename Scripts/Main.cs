using Godot;
using System;
using System.Collections.Generic;

public enum GameMode { Title, Playing, Paused, Won }
public enum Tool { Shovel, Mop, Scraper, Spray }

// Game controller for Level 1: player, carrying, bins, puddles, hidden poops, radioactive poop, slipping, Buba's panic.
public partial class Main : Node3D
{
    // ------------------------------------------------------------ state (read by the HUD, Buba and the touch controls)
    public GameMode Mode = GameMode.Title;
    public int Phase = 1;                       // 1 = calm, 2 = Buba panics
    public float Clock;
    public int Cleaned, RegDumped, FinalType;
    public Vector2 DropSpot;
    public bool HasCarry, CarryGlow, CarrySpecial, CarrySmall;
    public int SpecialTotal = 1, SpecialDone;   // Buba's special drop is the last 10%: 1 golden poop, or a puddle + little poops after it explodes
    public int PanicAt = Cfg.RegTotal;          // Buba panics when this many regular messes are cleaned (random: 60%..90%)
    bool finalDropped;
    float panicEndT = -1f;
    public int DropsLeft;                       // special drops Buba still has to make
    public int TotalNow { get { return Cfg.RegTotal + SpecialTotal; } }
    public int CarryType, CarryCount = 1;
    public Tool ToolSel = Tool.Shovel;
    public bool IsTouch;
    public bool ShowMinimap = true;
    public MainMenu Menu, ShopMenu;
    float menuT;

    public readonly List<Poop> Poops = new List<Poop>();
    public readonly List<Puddle> Puddles = new List<Puddle>();
    public List<Bin> Bins;
    public readonly List<Vector2> CleanedSpots = new List<Vector2>();

    public CharacterBody3D Player;
    public Buba Dog;
    public Hud Ui;
    public TouchControls Touch;
    public Sfx Audio;
    public float Yaw, Pitch = -0.15f;
    public float FlashT;

    public Poop Target, LumpT;
    public Puddle PuddleT;
    public readonly List<Crust> Crusts = new List<Crust>();
    public Crust CrustT;
    public bool Scrubbing, Scraping;
    public float ScrapeP;                       // progress (0..1) of the crust being scraped, for the HUD
    float scrapeSnd;
    bool dirtyWarned;
    public Tap TapObj;
    public float Water = 1f;          // clean water left in the bucket (0..1)
    public bool Filling;              // holding the mop under the tap
    float emptyMsgCd, fillSnd;

    Camera3D cam;
    float fvx, fvz, slipT, slipImm, slowT, bumpCd, shake, swingT, coolT, bob, mopT, scrubSnd, peek = 1f, actionBuf, msgT;
    Vector2 mv;
    int runId;
    Node3D shovel, mop, scraper, spray, carried;
    public int Charges, RunCoins;
    bool twistDone;
    // level 4 boss
    public Ermelinda Boss;
    public int Strikes, StopsDone;
    public float GraceLeft;
    float bossDropT, bossFailT;
    readonly MeshInstance3D[] carriedBody = new MeshInstance3D[3];

    class Particle { public MeshInstance3D M; public StandardMaterial3D Mat; public Vector3 V; public float Life, Max; }
    readonly List<Particle> parts = new List<Particle>();

    public Vector2 PP { get { return new Vector2(Player.Position.X, Player.Position.Z); } }
    Vector2 Fwd { get { return new Vector2(-Mathf.Sin(Yaw), -Mathf.Cos(Yaw)); } }

    public float Rand(float a, float b) { return a + GD.Randf() * (b - a); }

    // ------------------------------------------------------------ setup
    public override void _Ready()
    {
        Loc.Init();
        DisplayServer.WindowSetTitle(Loc.T("title"));
        IsTouch = DisplayServer.IsTouchscreenAvailable();

        var env = new Godot.Environment();
        env.BackgroundMode = Godot.Environment.BGMode.Color;
        bool garden = Cfg.Lvl.No == 2, kitchen = Cfg.Lvl.No == 3;
        env.BackgroundColor = Cfg.Hex(garden ? "#9fd8ff" : (kitchen ? "#fbe3a8" : "#f4d9e6"));
        env.AmbientLightSource = Godot.Environment.AmbientSource.Color;
        env.AmbientLightColor = Colors.White;
        env.AmbientLightEnergy = garden ? 0.88f : 0.78f;
        var we = new WorldEnvironment(); we.Environment = env; AddChild(we);
        var sun = new DirectionalLight3D(); sun.RotationDegrees = new Vector3(-55f, 35f, 0f); sun.LightEnergy = garden ? 0.85f : 0.45f; AddChild(sun);

        if (garden) Garden.Build(this, out Bins); else if (kitchen) Kitchen.Build(this, out Bins); else Room.Build(this, out Bins);
        float tx = Cfg.Lvl.TapX;
        TapObj = Tap.Create(tx); AddChild(TapObj);
        Room.Solid(this, tx - 0.65f, 5.5f, tx + 1.25f, 6.4f);     // sink + bucket footprint (collider + minimap + spawn avoidance)
        BuildPlayer();
        Dog = Buba.Create(); AddChild(Dog);
        if (Cfg.Lvl.Boss) { Boss = Ermelinda.Create(); Boss.AttachTo(this); }
        Audio = new Sfx(); AddChild(Audio);
        Ui = new Hud(); AddChild(Ui);
        Ui.Init(this);
        Touch = new TouchControls(); Ui.AddChild(Touch);
        Touch.Init(this);

        Loc.Changed += OnLang;
        GetViewport().SizeChanged += OnResize;
        OnResize();
        Save.Load();
        ShowMinimap = Save.Minimap;
        Audio.Muted = AudioServer.IsBusMute(0);
        Reset(GameMode.Title);
        if (Cfg.AutoStart) { Cfg.AutoStart = false; StartPressed(); }   // coming from "Next level" / level select
        else { Menu = new MainMenu(); AddChild(Menu); Menu.Init(this); }
    }

    // the scene is reloaded between levels: drop the handlers on things that outlive this node
    public override void _ExitTree()
    {
        Loc.Changed -= OnLang;
        var vp = GetViewport();
        if (vp != null) vp.SizeChanged -= OnResize;
    }

    // from the menu: start level n (reloads the scene when it is a different level)
    public void PlayLevel(int n)
    {
        n = Mathf.Clamp(n, 1, Cfg.MaxLevel);
        if (n == Cfg.Lvl.No)
        {
            if (Menu != null) { Menu.Close(); Menu = null; }
            StartPressed();
            return;
        }
        Cfg.SetLevel(n);
        Cfg.AutoStart = true;
        GetTree().ReloadCurrentScene();
    }

    // shop from the pause screen or the victory panel
    public void OpenShop()
    {
        if (ShopMenu != null && IsInstanceValid(ShopMenu)) return;
        ShopMenu = new MainMenu(); AddChild(ShopMenu); ShopMenu.Init(this, true);
    }

    // from the pause screen
    public void ToMenu()
    {
        Cfg.AutoStart = false;
        GetTree().ReloadCurrentScene();
    }

    public void NextLevel()
    {
        if (Cfg.Lvl.No >= Cfg.MaxLevel) return;
        Cfg.SetLevel(Cfg.Lvl.No + 1);
        Cfg.AutoStart = true;
        GetTree().ReloadCurrentScene();
    }

    // from the title screen
    public void SelectLevel(int n)
    {
        Cfg.SetLevel(n);
        Cfg.AutoStart = false;
        GetTree().ReloadCurrentScene();
    }

    void BuildPlayer()
    {
        Player = new CharacterBody3D();
        Player.MotionMode = CharacterBody3D.MotionModeEnum.Floating;
        Player.CollisionLayer = 4; Player.CollisionMask = 1 | 2;
        var cs = new CollisionShape3D();
        var cap = new CapsuleShape3D(); cap.Radius = Cfg.PlayerR; cap.Height = 1.6f;
        cs.Shape = cap; cs.Position = new Vector3(0f, 0.8f, 0f);
        Player.AddChild(cs);
        AddChild(Player);

        cam = new Camera3D();
        cam.Position = new Vector3(0f, Cfg.Eye, 0f);
        cam.Near = 0.05f; cam.Far = 60f; cam.Fov = 72f;
        Player.AddChild(cam);
        cam.Current = true;

        // shovel (first person)
        shovel = new Node3D(); cam.AddChild(shovel);
        Top(Gfx.Cyl(shovel, 0.025f, 0.025f, 0.95f, Cfg.Hex("#b98b52"), Vector3.Zero, 1f, false, 8), 10);
        Top(Gfx.Box(shovel, new Vector3(0.2f, 0.26f, 0.03f), Cfg.Hex("#9aa7b5"), new Vector3(0f, -0.55f, 0f)), 10);
        Top(Gfx.Box(shovel, new Vector3(0.22f, 0.04f, 0.05f), Cfg.Hex("#7d8996"), new Vector3(0f, -0.43f, 0f)), 10);
        Top(Gfx.Box(shovel, new Vector3(0.14f, 0.03f, 0.03f), Cfg.Hex("#b98b52"), new Vector3(0f, 0.47f, 0f)), 10);
        Top(Gfx.Sphere(shovel, 0.06f, Cfg.Hex("#ffd23f"), new Vector3(0f, 0.3f, 0f), 1f, false, 10), 10);
        Top(Gfx.Sphere(shovel, 0.06f, Cfg.Hex("#ffd23f"), new Vector3(0f, 0.05f, 0f), 1f, false, 10), 10);
        shovel.Scale = Vector3.One * 0.55f;

        // what sits on the shovel while carrying
        carried = new Node3D(); carried.Position = new Vector3(0f, -0.55f, -0.03f); carried.Scale = Vector3.One * 0.7f; carried.Visible = false;
        shovel.AddChild(carried);
        var col = Cfg.Types[0].Body;
        carriedBody[0] = Top(Gfx.Sphere(carried, 0.26f, col[0], new Vector3(0f, 0.05f, 0f), 1f, false, 12), 11); carriedBody[0].Scale = new Vector3(1f, 0.55f, 1f);
        carriedBody[1] = Top(Gfx.Sphere(carried, 0.2f, col[1], new Vector3(0f, 0.2f, 0f), 1f, false, 12), 12); carriedBody[1].Scale = new Vector3(1f, 0.6f, 1f);
        carriedBody[2] = Top(Gfx.Sphere(carried, 0.13f, col[2], new Vector3(0f, 0.34f, 0f), 1f, false, 10), 13); carriedBody[2].Scale = new Vector3(1f, 0.65f, 1f);
        Top(Gfx.Sphere(carried, 0.045f, Colors.White, new Vector3(-0.06f, 0.24f, 0.14f), 1f, false, 8), 14);
        Top(Gfx.Sphere(carried, 0.045f, Colors.White, new Vector3(0.06f, 0.24f, 0.14f), 1f, false, 8), 14);

        // mop (first person)
        mop = new Node3D(); cam.AddChild(mop);
        Top(Gfx.Cyl(mop, 0.025f, 0.025f, 0.95f, Cfg.Hex("#b98b52"), Vector3.Zero, 1f, false, 8), 10);
        Top(Gfx.Box(mop, new Vector3(0.34f, 0.07f, 0.14f), Cfg.Hex("#8892a0"), new Vector3(0f, -0.5f, 0f)), 10);
        for (int i = -3; i <= 3; i++)
            Top(Gfx.Box(mop, new Vector3(0.04f, 0.2f, 0.1f), Cfg.Hex(i % 2 != 0 ? "#e9f1fb" : "#c9dcf2"), new Vector3(i * 0.05f, -0.62f, 0f)), 10);
        Top(Gfx.Sphere(mop, 0.06f, Cfg.Hex("#ffd23f"), new Vector3(0f, 0.3f, 0f), 1f, false, 10), 10);
        Top(Gfx.Sphere(mop, 0.06f, Cfg.Hex("#ffd23f"), Vector3.Zero, 1f, false, 10), 10);
        mop.Scale = Vector3.One * 0.55f; mop.Visible = false;

        // scraper (first person): short handle and a wide flat blade
        scraper = new Node3D(); cam.AddChild(scraper);
        Top(Gfx.Cyl(scraper, 0.025f, 0.025f, 0.8f, Cfg.Hex("#b98b52"), Vector3.Zero, 1f, false, 8), 10);
        Top(Gfx.Box(scraper, new Vector3(0.3f, 0.03f, 0.04f), Cfg.Hex("#8892a0"), new Vector3(0f, -0.4f, 0f)), 10);
        Top(Gfx.Box(scraper, new Vector3(0.3f, 0.2f, 0.015f), Cfg.Hex("#c4ccd6"), new Vector3(0f, -0.5f, 0f)), 10);
        Top(Gfx.Sphere(scraper, 0.06f, Cfg.Hex("#ffd23f"), new Vector3(0f, 0.3f, 0f), 1f, false, 10), 10);
        Top(Gfx.Sphere(scraper, 0.06f, Cfg.Hex("#ffd23f"), Vector3.Zero, 1f, false, 10), 10);
        scraper.Scale = Vector3.One * 0.55f; scraper.Visible = false;
        // detergent spray bottle (first person)
        spray = new Node3D(); cam.AddChild(spray);
        Top(Gfx.Cyl(spray, 0.07f, 0.08f, 0.3f, Cfg.Hex("#5ec8e8"), Vector3.Zero, 1f, false, 12), 10);
        Top(Gfx.Cyl(spray, 0.05f, 0.07f, 0.06f, Cfg.Hex("#ffffff"), new Vector3(0f, 0.18f, 0f), 1f, false, 12), 10);
        Top(Gfx.Box(spray, new Vector3(0.05f, 0.05f, 0.16f), Cfg.Hex("#ff5c8a"), new Vector3(0f, 0.24f, -0.06f)), 10);
        Top(Gfx.Box(spray, new Vector3(0.03f, 0.1f, 0.03f), Cfg.Hex("#ff5c8a"), new Vector3(0f, 0.12f, -0.09f)), 10);
        spray.Scale = Vector3.One * 0.9f; spray.Visible = false;
    }

    static MeshInstance3D Top(MeshInstance3D mi, int priority) { Gfx.OnTop(mi, priority); return mi; }

    void OnLang()
    {
        DisplayServer.WindowSetTitle(Loc.T("title"));
        foreach (var b in Bins) b.Relabel();
        if (TapObj != null) TapObj.Relabel();
        Room.RelabelPortrait();
        Garden.Relabel(); Kitchen.Relabel();
        if (Boss != null) Boss.Relabel();
        Ui.Relabel();
    }

    void OnResize()
    {
        var win = GetWindow();
        Vector2I size = win.Size;
        float f = Mathf.Clamp(Mathf.Min(size.X, size.Y) / 600f, 0.8f, 2.5f);
        if (Mathf.Abs(win.ContentScaleFactor - f) > 0.01f) win.ContentScaleFactor = f;
        bool portrait = size.Y > size.X;
        cam.KeepAspect = portrait ? Camera3D.KeepAspectEnum.Width : Camera3D.KeepAspectEnum.Height;
        cam.Fov = portrait ? 80f : 72f;
    }

    // ------------------------------------------------------------ new game
    public void Reset(GameMode mode)
    {
        runId++;
        foreach (var p in Poops) p.QueueFree();
        Poops.Clear();
        foreach (var q in Puddles) q.QueueFree();
        Puddles.Clear();
        foreach (var cr in Crusts) cr.QueueFree();
        Crusts.Clear();
        foreach (var pt in parts) pt.M.QueueFree();
        parts.Clear();
        Phase = 1; Clock = 0f; Cleaned = 0; RegDumped = 0; CleanedSpots.Clear();
        SpecialTotal = Cfg.Lvl.Drops; SpecialDone = 0; finalDropped = false; panicEndT = -1f; CarrySpecial = false; CarrySmall = false;
        DropsLeft = 0; dirtyWarned = false; Scraping = false; ScrapeP = 0f; CrustT = null;
        PanicAt = Mathf.Clamp(Mathf.RoundToInt(Cfg.RegTotal * Rand(Cfg.Lvl.PanicMin, Cfg.Lvl.PanicMax)), 1, Cfg.RegTotal);   // random moment, level 1: ~63%..90%
        Charges = Cfg.SprayMax; twistDone = false; RunCoins = 0; CarryCount = 1;
        Strikes = 0; StopsDone = 0; bossFailT = 0f; GraceLeft = Cfg.Lvl.BossGrace; bossDropT = Cfg.Lvl.BossGrace + Rand(2f, 4f);
        if (Boss != null) Boss.ResetState();
        if (Cfg.Lvl.Boss) PanicAt = int.MaxValue;
        HasCarry = false; ToolSel = Tool.Shovel; Scrubbing = false; Filling = false; Water = 1f; emptyMsgCd = 0f;
        Yaw = 0f; Pitch = -0.15f;
        Player.Position = new Vector3(0f, 0f, 4.6f);
        fvx = fvz = 0f; slipT = slipImm = slowT = bumpCd = shake = swingT = coolT = mopT = 0f; peek = 1f; actionBuf = 0f; msgT = 0f; FlashT = 0f;
        DropSpot = new Vector2(0f, -4f);
        Dog.ResetState();
        SpawnAll();
        Ui.Panic(false);
        Ui.HideVictory();
        Ui.HideMsg();
        Audio.SetMode(mode == GameMode.Playing ? "calm" : "off");
        Mode = mode;
        if (mode == GameMode.Playing && Cfg.Lvl.Boss) Say(Loc.T("intro4", ("n", ((int)Cfg.Lvl.BossGrace).ToString())), 5f);
        else if (mode == GameMode.Playing) Say(Loc.T(Cfg.Lvl.No > 1 ? "intro" + Cfg.Lvl.No : "intro", ("k", IsTouch ? Loc.T("key_tap_btn") : Loc.T("key_space"))), 4f);
    }

    // first start or resume
    public void StartPressed()
    {
        if (bossFailT > 0f) return;
        if (ShopMenu != null && IsInstanceValid(ShopMenu)) return;
        if (Mode == GameMode.Title) { Reset(GameMode.Playing); }
        else if (Mode == GameMode.Paused) { Mode = GameMode.Playing; Audio.SetMode(Phase == 2 ? "panic" : "calm"); }
        else return;
        Ui.HideOverlay();
        if (!IsTouch) Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public void RestartPressed()
    {
        Reset(GameMode.Playing);
        Ui.HideOverlay();
        if (!IsTouch) Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public void Pause()
    {
        if (Mode != GameMode.Playing) return;
        Mode = GameMode.Paused;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        Audio.SetMode("off");
        if (Touch != null) Touch.Reset();
        Ui.ShowOverlay(true);
    }

    public override void _Notification(int what)
    {
        // 1005 = WM_WINDOW_FOCUS_OUT, 2017 = APPLICATION_FOCUS_OUT, 2015 = APPLICATION_PAUSED (mobile): pause automatically
        if ((what == 1005 || what == 2017 || what == 2015) && Ui != null) Pause();
    }

    // ------------------------------------------------------------ spawning
    void SpawnAll()
    {
        var kinds = new List<int>();
        for (int i = 0; i < Cfg.NormalTypes; i++) for (int k = 0; k < Cfg.Lvl.PerColor; k++) kinds.Add(i);
        for (int i = kinds.Count - 1; i > 0; i--) { int j = (int)(GD.Randi() % (uint)(i + 1)); int t = kinds[i]; kinds[i] = kinds[j]; kinds[j] = t; }
        Vector2 pl = new Vector2(0f, 4.6f), dog = Dog.P;

        Func<float, float, float, bool> free = (x, z, gap) =>
        {
            foreach (var o in Poops) if (new Vector2(o.Position.X, o.Position.Z).DistanceTo(new Vector2(x, z)) < gap) return false;
            foreach (var o in Crusts) if (o.P.DistanceTo(new Vector2(x, z)) < gap) return false;
            return new Vector2(x, z).DistanceTo(pl) >= 3.5f && new Vector2(x, z).DistanceTo(dog) >= 2.2f;
        };
        Func<int> pop = () => { int v = kinds[kinds.Count - 1]; kinds.RemoveAt(kinds.Count - 1); return v; };

        // one hiding under a rug (a squirming lump gives it away)
        bool green = GD.Randf() < 0.4f;
        for (int i = 0; i < 300; i++)
        {
            float x, z;
            Vector4 rc = Cfg.Lvl.RugCircle, rr = Cfg.Lvl.RugRect;
            if (green) { float a = Rand(0f, Mathf.Tau), r = Rand(0f, rc.Z); x = rc.X + Mathf.Cos(a) * r; z = rc.Y + Mathf.Sin(a) * r; }
            else { x = Rand(rr.X, rr.Z); z = Rand(rr.Y, rr.W); }
            if (Room.InFurniture(x, z, 0.55f) || !free(x, z, 1f)) continue;
            AddPoop(x, z, false, pop(), "rug", green ? Cfg.Lvl.RugCircleColor : Cfg.Lvl.RugRectColor);
            break;
        }
        // one hiding under the coffee table (look down at it, or follow the stink)
        for (int i = 0; i < 100; i++)
        {
            float x = Cfg.Table.X + Rand(-0.75f, 0.75f), z = Cfg.Table.Y + Rand(-0.2f, 0.2f);
            if (!free(x, z, 1f)) continue;
            AddPoop(x, z, false, pop(), "table", Colors.White);
            break;
        }
        // dried mess stuck to the floor (scraper): each one is one of the poops
        for (int ci = 0; ci < Cfg.Lvl.Crusts && kinds.Count > 0; ci++)
        {
            for (int i = 0; i < 2000; i++)
            {
                float x = Rand(-7.5f, 7.5f), z = Rand(-5.2f, 5.2f);
                if (Room.InFurniture(x, z, 0.9f) || !free(x, z, Mathf.Max(1.6f, 2.4f - i / 300f))) continue;
                var cr = Crust.Create(x, z, pop()); AddChild(cr); Crusts.Add(cr);
                break;
            }
        }
        int tries = 0; float gapv = 2.3f;
        while (kinds.Count > 0 && tries++ < 8000)
        {
            if (tries % 1000 == 0) gapv = Mathf.Max(1.3f, gapv - 0.2f);
            float x = Rand(-7.8f, 7.8f), z = Rand(-5.3f, 5.3f);
            if (Room.InFurniture(x, z, 0.9f) || !free(x, z, gapv)) continue;
            AddPoop(x, z, false, pop(), "", Colors.White);
        }
        // the radioactive one
        for (int i = 0; i < 3000; i++)
        {
            float x = Rand(-7.5f, 7.5f), z = Rand(-5f, 5f);
            if (Room.InFurniture(x, z, 1.1f) || !free(x, z, Mathf.Max(1.3f, 2.6f - i / 400f))) continue;
            AddPoop(x, z, false, Cfg.Rad, "", Colors.White);
            break;
        }
        // the puddles
        for (int pi = 0; pi < Cfg.Lvl.Puddles; pi++)
        {
            for (int i = 0; i < 4000; i++)
            {
                float x = Rand(-7.5f, 7.5f), z = Rand(-5f, 5f);
                bool apart = true;
                foreach (var o in Puddles) if (o.P.DistanceTo(new Vector2(x, z)) < Mathf.Max(2.2f, 3.4f - i / 500f)) apart = false;
                if (!apart || Room.InFurniture(x, z, Mathf.Max(0.9f, 1.4f - i / 1500f)) || !free(x, z, Mathf.Max(1.5f, 2.4f - i / 400f))) continue;
                var q = Puddle.Create(x, z, Cfg.Lvl.PuddleR0, Cfg.Lvl.PuddleMax); AddChild(q); Puddles.Add(q);
                break;
            }
        }
    }

    Poop AddPoop(float x, float z, bool glow, int type, string hidden, Color rug, bool special = false, bool small = false)
    {
        var p = Poop.Create(type, glow, x, z, hidden, rug);
        p.Special = special;
        if (small) p.MakeSmall();
        AddChild(p);
        Poops.Add(p);
        return p;
    }

    public Vector2 RandomWaypoint(Vector2 from)
    {
        for (int i = 0; i < 40; i++)
        {
            var p = new Vector2(Rand(-Cfg.RoomX + 1f, Cfg.RoomX - 1f), Rand(-Cfg.RoomZ + 1f, Cfg.RoomZ - 1f));
            if (from.DistanceTo(p) > 4f && !Room.InFurniture(p.X, p.Y, 0.6f)) return p;
        }
        return new Vector2(0f, 4f);
    }

    // ------------------------------------------------------------ messages / feedback
    public void Say(string text, float secs) { msgT = secs; Ui.ShowMsg(text); }
    void Haptic(int ms) { if (IsTouch) Input.VibrateHandheld(ms); }

    public void Burst(float x, float y, float z, Color[] colors, int n, float speed, float life, float size)
    {
        for (int i = 0; i < n; i++)
        {
            var col = colors[(int)(GD.Randi() % (uint)colors.Length)];
            var mat = Gfx.Make(col, 1f, true);
            mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            var mi = new MeshInstance3D();
            var sm = new SphereMesh(); float s = Rand(size * 0.5f, size);
            sm.Radius = s; sm.Height = s * 2f; sm.RadialSegments = 6; sm.Rings = 4;
            mi.Mesh = sm; mi.MaterialOverride = mat; mi.Position = new Vector3(x, y, z);
            AddChild(mi);
            float a = Rand(0f, Mathf.Tau), v = Rand(speed * 0.3f, speed);
            parts.Add(new Particle { M = mi, Mat = mat, V = new Vector3(Mathf.Cos(a) * v, Rand(1.5f, 4.5f), Mathf.Sin(a) * v), Life = Rand(life * 0.6f, life), Max = life });
        }
    }

    void UpdateParticles(float dt)
    {
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            var p = parts[i];
            p.Life -= dt;
            if (p.Life <= 0f) { p.M.QueueFree(); parts.RemoveAt(i); continue; }
            p.V.Y -= 9f * dt;
            var pos = p.M.Position + p.V * dt;
            if (pos.Y < 0.03f) { pos.Y = 0.03f; p.V.X *= 0.5f; p.V.Z *= 0.5f; p.V.Y = 0f; }
            p.M.Position = pos;
            var c = p.Mat.AlbedoColor; c.A = Mathf.Clamp(p.Life / p.Max * 1.5f, 0f, 1f); p.Mat.AlbedoColor = c;
        }
    }

    // ------------------------------------------------------------ input
    public override void _Input(InputEvent e)
    {
        if (e is InputEventScreenTouch && !IsTouch) { IsTouch = true; Ui.Relabel(); }
        if (e is InputEventMouseMotion mm)
        {
            if (Mode == GameMode.Playing && Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                Yaw -= mm.ScreenRelative.X * 0.0022f;
                Pitch = Mathf.Clamp(Pitch - mm.ScreenRelative.Y * 0.0022f, -1.4f, 1.4f);
            }
            return;
        }
        if (e is InputEventKey k && k.Pressed && !k.Echo)
        {
            switch (k.Keycode)
            {
                case Key.Space: if (Mode == GameMode.Playing) actionBuf = 0.25f; break;
                case Key.Key1: SetTool(Tool.Shovel); break;
                case Key.Key2: SetTool(Tool.Mop); break;
                case Key.Key3: SetTool(Tool.Scraper); break;
                case Key.Key4: SetTool(Tool.Spray); break;
                case Key.F: CycleTool(); break;
                case Key.M: Audio.ToggleMute(); break;
                case Key.N: ShowMinimap = !ShowMinimap; Save.Minimap = ShowMinimap; Save.Store(); break;
                case Key.L: Loc.Toggle(); break;
                case Key.T: IsTouch = !IsTouch; Ui.Relabel(); if (IsTouch) Input.MouseMode = Input.MouseModeEnum.Visible; break;   // debug: preview the touch UI on desktop
                case Key.Escape: Pause(); break;
                case Key.Enter: if (Mode == GameMode.Won) RestartPressed(); break;
                case Key.R: if (Mode == GameMode.Won) RestartPressed(); break;
            }
        }
    }

    public bool ActionHeld() { return Input.IsKeyPressed(Key.Space) || (Touch != null && Touch.ActHeld); }
    public void PressAction() { if (Mode == GameMode.Playing) actionBuf = 0.25f; }
    public void AddPitch(float d) { Pitch = Mathf.Clamp(Pitch + d, -1.4f, 1.4f); }

    public void CycleTool()
    {
        if (ToolSel == Tool.Shovel) SetTool(Tool.Mop);
        else if (ToolSel == Tool.Mop) SetTool(Cfg.Lvl.Scraper ? Tool.Scraper : Tool.Shovel);
        else if (ToolSel == Tool.Scraper) SetTool(Cfg.Lvl.Spray > 0 ? Tool.Spray : Tool.Shovel);
        else SetTool(Tool.Shovel);
    }

    public void SetTool(Tool t)
    {
        if (Mode != GameMode.Playing || ToolSel == t) return;
        if (t == Tool.Scraper && !Cfg.Lvl.Scraper) return;
        if (t == Tool.Spray && Cfg.Lvl.Spray <= 0) return;
        if (t != Tool.Shovel && HasCarry) { Say(Loc.T("handsfull"), 2f); return; }
        ToolSel = t; Audio.Swish();
        string k = IsTouch ? Loc.T("the_button") : Loc.T("key_space");
        if (t == Tool.Mop) Say(Loc.T("mopready", ("k", k)), 2f);
        else if (t == Tool.Scraper) Say(Loc.T("scraperready", ("k", k)), 2f);
        else if (t == Tool.Spray) Say(Loc.T("sprayready", ("k", k)), 2f);
        else Say(Loc.T("shovelready"), 2f);
    }

    // ------------------------------------------------------------ main loop
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        if (Mode == GameMode.Paused || Mode == GameMode.Title) { Dog.Tick(dt, this); return; }

        Clock += dt;
        if (msgT > 0f) { msgT -= dt; if (msgT <= 0f) Ui.HideMsg(); }
        if (shake > 0f) shake -= dt;
        if (FlashT > 0f) FlashT -= dt;
        if (swingT > 0f) swingT -= dt;
        UpdateParticles(dt);
        if (Mode == GameMode.Playing) UpdatePlayer(dt);
        else { Target = null; LumpT = null; PuddleT = null; CrustT = null; Scrubbing = false; Scraping = false; }
        if (Mode == GameMode.Playing) TickSpecial(dt);
        if (Mode == GameMode.Playing && Boss != null) BossTick(dt);
        if (Mode == GameMode.Playing && Cfg.Lvl.No == 3 && !twistDone && Phase == 1 && RegDumped * 2 >= Cfg.RegTotal)
        {
            twistDone = true; Audio.Bark(); shake = 0.4f;
            Say(Loc.T("twist"), 4.5f);
        }
        AnimateWorld(dt);
        Dog.Tick(dt, this);
    }

    void UpdatePlayer(float dt)
    {
        float ix = 0f, iz = 0f;
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) iz += 1f;
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) iz -= 1f;
        if (Input.IsKeyPressed(Key.E)) ix += 1f;
        if (Input.IsKeyPressed(Key.Q)) ix -= 1f;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) Yaw += 1.9f * dt;
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) Yaw -= 1.9f * dt;
        if (Touch != null) { Yaw -= Touch.MoveX * 2.2f * dt; iz += Touch.MoveZ; }   // joystick: up/down walks, left/right turns

        bool sliding = slipT > 0f;
        float len = Mathf.Sqrt(ix * ix + iz * iz);
        Vector3 move = Vector3.Zero;
        if (len > 0f)
        {
            if (len > 1f) { ix /= len; iz /= len; }
            var fwd = new Vector3(-Mathf.Sin(Yaw), 0f, -Mathf.Cos(Yaw));
            var right = new Vector3(Mathf.Cos(Yaw), 0f, -Mathf.Sin(Yaw));
            float sp = Cfg.WalkSpeed * (HasCarry ? Cfg.CarrySlow * (1f - 0.06f * (CarryCount - 1)) : 1f) * (slowT > 0f ? 0.5f : 1f) * (sliding ? 0.25f : 1f);
            Vector3 dir = fwd * iz + right * ix;
            move = dir * sp;
            mv = new Vector2(dir.X, dir.Z);
            bob += dt * 9f;
        }
        else mv = Vector2.Zero;

        Player.Rotation = new Vector3(0f, Yaw, 0f);
        Player.Velocity = move + new Vector3(fvx, 0f, fvz);
        Player.MoveAndSlide();
        // knockback / slide momentum fades out
        float k = Mathf.Exp(-(sliding ? 2.4f : 7f) * dt);
        fvx *= k; fvz *= k;
        if (Mathf.Sqrt(fvx * fvx + fvz * fvz) < 0.05f) { fvx = 0f; fvz = 0f; }
        if (slipT > 0f) slipT -= dt;
        if (slipImm > 0f) slipImm -= dt;
        if (slowT > 0f) slowT -= dt;
        if (bumpCd > 0f) bumpCd -= dt;
        foreach (var q in Puddles) q.R = Mathf.Min(q.Max, q.R + Cfg.Lvl.PuddleGrow * dt);   // puddles keep spreading

        Vector2 pp = PP;
        bool held = ActionHeld();
        bool mopping = ToolSel == Tool.Mop && held;

        // slippery dirt: puddles and the lump over a hidden poop
        if (!Cfg.AntiSlip && slipT <= 0f && slipImm <= 0f && !mopping)
        {
            foreach (var q in Puddles) if (pp.DistanceTo(q.P) < q.R * 0.9f) { Slip("puddle"); break; }
        }
        foreach (var p in Poops.ToArray())
        {
            if (p.Hidden == "rug" && pp.DistanceTo(p.P) < 0.45f)
            {
                RevealPoop(p, "step");
                if (!Cfg.AntiSlip && slipT <= 0f && slipImm <= 0f) Slip("squish");
            }
        }

        // peeking under the coffee table: look steeply down at it and the top turns see-through
        {
            Vector2 td = Cfg.Table - pp; float tdl = td.Length();
            Vector2 fw = Fwd;
            bool facing = tdl > 0.001f && td.Dot(fw) / tdl > 0.3f;
            bool peekNow = tdl < 3.6f && facing && (Pitch < -0.7f || (IsTouch && tdl < 2.8f));
            peek += ((peekNow ? 0.2f : 1f) - peek) * Mathf.Min(1f, dt * 10f);
            foreach (var fm in Room.TableFade) { var c = fm.C; c.A *= peek; fm.M.AlbedoColor = c; }
            if (peekNow)
            {
                foreach (var p in Poops.ToArray())
                {
                    if (p.Hidden != "table") continue;
                    Vector2 d = p.P - pp; float dl = d.Length();
                    if (dl < 3.2f && d.Dot(fw) / (dl > 0.001f ? dl : 1f) > 0.5f) RevealPoop(p, "table");
                }
            }
        }

        // scrubbing a puddle with the mop
        Scrubbing = false; Filling = false;
        if (emptyMsgCd > 0f) emptyMsgCd -= dt;
        if (ToolSel == Tool.Mop && held && !HasCarry)
        {
            var q = PuddleInReach();
            bool atTap = TapInReach();
            if (q != null && Water <= 0f && !atTap)
            {
                if (emptyMsgCd <= 0f) { emptyMsgCd = 3f; Audio.Buzz(); Say(Loc.T("bucket_empty"), 2.6f); }
            }
            else if (atTap && (q == null || Water <= 0f) && Water < 1f)
            {
                // refill: hold the mop under the tap
                Filling = true; mopT += dt; fillSnd -= dt;
                Water = Mathf.Min(1f, Water + 0.45f * dt);
                if (fillSnd <= 0f)
                {
                    fillSnd = 0.25f; Audio.Scrub();
                    Burst(TapObj.Position.X + Rand(-0.1f, 0.1f), 0.95f, TapObj.Position.Z - 0.15f, new[] { Cfg.Hex("#bfe6f7"), Colors.White }, 3, 0.5f, 0.5f, 0.035f);
                }
                if (Water > 0.6f) dirtyWarned = false;
                if (Water >= 1f) Say(Loc.T("bucket_full"), 1.8f);
            }
            else if (q != null)
            {
                Scrubbing = true; mopT += dt; scrubSnd -= dt;
                // level 2: the water gets filthy, so the mop works worse the less clean water is left
                float eff = Cfg.Lvl.DirtyWater ? 0.3f + 0.7f * Water : 1f;
                Water = Mathf.Max(0f, Water - (Cfg.Lvl.DirtyWater ? 0.2f : 0.17f) * (Shop.Has("bucket") ? 0.55f : 1f) * dt);
                q.R -= 0.5f * eff * dt;
                if (Cfg.Lvl.DirtyWater && Water < 0.3f && !dirtyWarned) { dirtyWarned = true; Say(Loc.T("dirty_warn"), 3f); }
                if (scrubSnd <= 0f)
                {
                    scrubSnd = 0.22f; Audio.Scrub(); Haptic(8);
                    Burst(q.Position.X + Rand(-q.R, q.R) * 0.5f, 0.1f, q.Position.Z + Rand(-q.R, q.R) * 0.5f, new[] { Cfg.Hex("#bfe6f7"), Colors.White }, 3, 0.5f, 0.5f, 0.04f);
                }
                if (q.R <= 0.22f) CleanPuddle(q);
            }
        }

        // scraping dried mess with the scraper: hold for 2 s
        Scraping = false;
        Crust scrapeTarget = null;
        if (ToolSel == Tool.Scraper && held && !HasCarry)
        {
            scrapeTarget = CrustInReach();
            if (scrapeTarget != null)
            {
                Scraping = true; mopT += dt; scrapeSnd -= dt;
                scrapeTarget.Progress += dt / (scrapeTarget.Soft ? 0.7f : 2f);
                if (scrapeSnd <= 0f)
                {
                    scrapeSnd = 0.18f; Audio.Scrub(); Haptic(10);
                    Burst(scrapeTarget.Position.X + Rand(-0.3f, 0.3f), 0.08f, scrapeTarget.Position.Z + Rand(-0.3f, 0.3f), new[] { Cfg.Hex("#7a5532"), Cfg.Hex("#5e4126") }, 3, 0.8f, 0.4f, 0.035f);
                }
                if (scrapeTarget.Progress >= 1f) { ScrapeP = 1f; FinishCrust(scrapeTarget); scrapeTarget = null; Scraping = false; }
            }
        }
        ScrapeP = (Scraping && scrapeTarget != null) ? Mathf.Min(1f, scrapeTarget.Progress) : 0f;
        foreach (var cr in Crusts) if (cr != scrapeTarget && cr.Progress > 0f) cr.Progress = Mathf.Max(0f, cr.Progress - dt * 0.8f);   // it sticks again if you stop

        if (coolT > 0f) coolT -= dt;
        if (actionBuf > 0f)
        {
            actionBuf -= dt;
            if (coolT <= 0f) { actionBuf = 0f; if (ToolSel == Tool.Shovel) Act(); else if (ToolSel == Tool.Spray) DoSpray(); }
        }
        Target = HasCarry ? null : FindTarget();
        LumpT = HasCarry ? null : FindLump();
        PuddleT = HasCarry ? null : PuddleInReach();
        CrustT = HasCarry ? null : CrustInReach();
    }

    // visuals that run at the physics rate: poops, stink, lumps, pulses, bins, puddles
    void AnimateWorld(float dt)
    {
        Vector2 pp = PP;
        foreach (var p in Poops)
        {
            if (p.Lump != null)
            {
                float w = 1f + Mathf.Sin(Clock * 5f + p.Seed) * 0.08f;
                p.Lump.Scale = new Vector3(w, 0.32f * (1f + Mathf.Sin(Clock * 7f + p.Seed) * 0.18f), 0.8f / w);
            }
            Vector2 d = pp - p.P;
            p.Visual.Rotation = new Vector3(0f, Mathf.Atan2(d.X, d.Y), 0f);
            float wob = 1f + Mathf.Sin(Clock * 4f + p.Seed) * 0.025f;
            float hl = (p == Target ? 1.18f : 1f) * p.Size;
            if (p.Special && p.Glow && p.Fuse > 0f) hl *= 1f + 0.16f * Mathf.Sin(Clock * (12f + (5f - p.Fuse) * 7f));   // throbs faster as it is about to blow
            p.Visual.Scale = new Vector3(hl / wob, hl * wob, hl / wob);
            for (int i = 0; i < 3; i++)
            {
                float ph = Mathf.PosMod(Clock * 0.6f + i / 3f + p.Seed, 1f);
                p.Puffs[i].Position = new Vector3(Mathf.Sin(ph * 9f + i * 2f) * 0.1f + (i - 1) * 0.12f, 0.7f + ph * 0.7f, Mathf.Cos(ph * 7f + i) * 0.1f);
                p.Puffs[i].Scale = Vector3.One * (1.2f - ph * 0.6f);
            }
            if (p.Halo != null) p.Halo.Scale = Vector3.One * (1f + Mathf.Sin(Clock * 6f) * 0.12f);
            if (p.Aura != null) p.Aura.Scale = Vector3.One * (1f + Mathf.Sin(Clock * 6f) * 0.12f);
        }
        foreach (var q in Puddles) q.ApplyScale(1f + Mathf.Sin(Clock * 2f + q.Seed) * 0.015f);
        foreach (var cr in Crusts) cr.Animate(Clock);

        // radioactive pulses
        foreach (var p in Poops.ToArray())
        {
            if (p.Type != Cfg.Rad || p.Ring == null) continue;
            p.Pt += dt;
            float ph = Mathf.PosMod(p.Pt, Cfg.PulseT) / Cfg.PulseLife;
            int id = (int)(p.Pt / Cfg.PulseT);
            if (ph < 1f)
            {
                float r = 0.3f + ph * Cfg.PulseR;
                p.Ring.Visible = true; p.Ring.Scale = Vector3.One * r;
                var c = p.RingMat.AlbedoColor; c.A = 0.85f * (1f - ph); p.RingMat.AlbedoColor = c;
                if (Mode == GameMode.Playing && p.ZapId != id && Mathf.Abs(pp.DistanceTo(p.P) - r) < 0.35f) { p.ZapId = id; Zap(p); }
            }
            else p.Ring.Visible = false;
        }

        foreach (var b in Bins)
        {
            bool on = HasCarry && CarryType == b.Type && Mode == GameMode.Playing;
            b.Marker.Visible = on;
            if (!on) continue;
            float w = Mathf.Sin(Clock * 5f) * 0.07f;
            b.MarkerLabel.Position = new Vector3(0f, 2.25f + w, 0f);
            b.MarkerCone.Position = new Vector3(0f, 1.7f + w, 0f);
            b.MarkerRing.Scale = Vector3.One * (1f + 0.08f * Mathf.Sin(Clock * 6f));
        }
        if (TapObj != null)
            TapObj.Update(Clock, Water, Mode == GameMode.Playing && ToolSel == Tool.Mop && Water < 0.3f && !HasCarry, Filling);
    }

    // camera, bob, shake and the tools in your hands
    public override void _Process(double delta)
    {
        if (cam == null) return;
        if (Mode == GameMode.Title)
        {
            // menu backdrop: slow camera pan over the level
            menuT += (float)delta;
            Yaw = 0.55f * Mathf.Sin(menuT * 0.22f);
            Pitch = -0.12f;
            Player.Position = new Vector3(2.5f * Mathf.Sin(menuT * 0.11f), 0f, 4.6f);
        }
        float bobY = Mode == GameMode.Playing ? Mathf.Sin(bob) * 0.035f : 0f;
        float sh = shake > 0f ? shake : 0f;
        cam.Position = new Vector3(Rand(-1f, 1f) * sh * 0.08f, Cfg.Eye + bobY + Rand(-1f, 1f) * sh * 0.08f, 0f);
        float roll = slipT > 0f ? Mathf.Sin(Clock * 14f) * 0.14f * Mathf.Clamp(slipT / 0.4f, 0f, 1f) : 0f;
        cam.Rotation = new Vector3(Pitch, 0f, roll);
        Player.Rotation = new Vector3(0f, Yaw, 0f);

        bool mopOn = ToolSel == Tool.Mop, scrOn = ToolSel == Tool.Scraper, spOn = ToolSel == Tool.Spray;
        shovel.Visible = ToolSel == Tool.Shovel; mop.Visible = mopOn; scraper.Visible = scrOn; spray.Visible = spOn;
        float swing = swingT > 0f ? Mathf.Sin((1f - swingT / 0.3f) * Mathf.Pi) : 0f;
        if (mopOn)
        {
            float w = Mathf.Sin(mopT * 11f);
            bool busy = Scrubbing || Filling;
            mop.Rotation = new Vector3(0.95f + (busy ? 0.15f : 0f), 0f, 0.12f);
            mop.Position = new Vector3(0.3f + (busy ? w * 0.16f : 0f), -0.3f + bobY * 0.5f, -0.66f - (busy ? Mathf.Abs(w) * 0.08f : 0f));
        }
        else if (spOn)
        {
            float kick = coolT > 0f ? Mathf.Clamp(coolT / 0.5f, 0f, 1f) : 0f;
            spray.Rotation = new Vector3(0.35f + kick * 0.25f, 0f, 0.1f);
            spray.Position = new Vector3(0.32f, -0.3f + bobY * 0.5f, -0.6f + kick * 0.06f);
        }
        else if (scrOn)
        {
            float w = Mathf.Sin(mopT * 14f);
            scraper.Rotation = new Vector3(1.0f + (Scraping ? 0.2f : 0f), 0f, 0.12f);
            scraper.Position = new Vector3(0.3f, -0.32f + bobY * 0.5f + (Scraping ? w * 0.05f : 0f), -0.66f - (Scraping ? Mathf.Abs(w) * 0.12f : 0f));
        }
        else
        {
            shovel.Rotation = new Vector3((HasCarry ? 1.3f : 0.95f) + swing * 0.9f, 0f, 0.12f);
            shovel.Position = new Vector3(0.36f, -0.3f + swing * 0.08f + bobY * 0.5f, -0.62f - swing * 0.25f);
            carried.Visible = HasCarry;
            carried.Scale = Vector3.One * (0.7f * (1f + 0.3f * (CarryCount - 1)));
            carried.Rotation = new Vector3(-Mathf.Pi / 2f, 0f, Mathf.Sin(Clock * 6f) * 0.07f);
        }
    }

    // ------------------------------------------------------------ finding things
    public Poop FindTarget()
    {
        Vector2 pp = PP, fw = Fwd;
        Poop best = null; float bd = float.MaxValue;
        foreach (var p in Poops)
        {
            if (p.Hidden != "") continue;
            Vector2 d = p.P - pp; float dl = d.Length();
            if (dl > Cfg.Reach) continue;
            float dot = d.Dot(fw) / (dl > 0.001f ? dl : 1f);
            if ((dot > 0.5f || dl < 0.9f) && dl < bd) { bd = dl; best = p; }
        }
        return best;
    }

    public Poop FindLump()
    {
        Vector2 pp = PP, fw = Fwd;
        Poop best = null; float bd = float.MaxValue;
        foreach (var p in Poops)
        {
            if (p.Hidden != "rug") continue;
            Vector2 d = p.P - pp; float dl = d.Length();
            if (dl > Cfg.Reach) continue;
            if ((d.Dot(fw) / (dl > 0.001f ? dl : 1f) > 0.5f || dl < 0.9f) && dl < bd) { bd = dl; best = p; }
        }
        return best;
    }

    public Puddle PuddleInReach()
    {
        Vector2 pp = PP, fw = Fwd;
        Puddle best = null; float bd = float.MaxValue;
        foreach (var q in Puddles)
        {
            Vector2 d = q.P - pp; float dl = d.Length(); float edge = dl - q.R;
            if (edge > 1.4f) continue;
            if ((d.Dot(fw) / (dl > 0.001f ? dl : 1f) > 0.2f || edge < 0.3f) && edge < bd) { bd = edge; best = q; }
        }
        return best;
    }

    public Crust CrustInReach()
    {
        Vector2 pp = PP, fw = Fwd;
        Crust best = null; float bd = float.MaxValue;
        foreach (var c in Crusts)
        {
            Vector2 d = c.P - pp; float dl = d.Length();
            if (dl > 1.6f) continue;
            if ((dl < 0.8f || d.Dot(fw) / (dl > 0.001f ? dl : 1f) > 0.3f) && dl < bd) { bd = dl; best = c; }
        }
        return best;
    }

    // ------------------------------------------------------------ level 4: Dona Ermelinda's inspection
    void BossTick(float dt)
    {
        if (GraceLeft > 0f) GraceLeft = Mathf.Max(0f, GraceLeft - dt);
        Boss.Tick(dt, this);
        if (Mode != GameMode.Playing) return;
        // Buba keeps leaving surprises
        bossDropT -= dt;
        if (bossDropT <= 0f && StopsDone < Ermelinda.Route.Length)
        {
            bossDropT = Rand(Cfg.Lvl.BossDropMin, Cfg.Lvl.BossDropMax);
            Vector2 bp = Dog.P;
            if (!Room.InFurniture(bp.X, bp.Y, 0.3f) && bp.DistanceTo(PP) > 0.8f)
            {
                if (GD.Randf() < 0.25f) { var q = Puddle.Create(bp.X, bp.Y, Cfg.Lvl.PuddleR0, Cfg.Lvl.PuddleMax); AddChild(q); Puddles.Add(q); }
                else AddPoop(bp.X, bp.Y, false, GD.RandRange(0, Cfg.NormalTypes - 1), "", Colors.White);
                Audio.Pop(); Say(Loc.T("buba_surprise"), 2.5f);
            }
        }
    }

    public bool MessNear(Vector2 p, float r)
    {
        foreach (var o in Poops) if (o.Hidden == "" && o.P.DistanceTo(p) < r) return true;
        foreach (var q in Puddles) if (q.P.DistanceTo(p) < r + q.R * 0.6f) return true;
        foreach (var c in Crusts) if (c.P.DistanceTo(p) < r) return true;
        return false;
    }

    public void BossEntered() { Audio.Bark(); shake = 0.3f; Say(Loc.T("boss_enter"), 4f); }

    public void BossStopDone(int done)
    {
        StopsDone = done;
        if (done >= Ermelinda.Route.Length) { Win(); return; }
        Audio.Ding();
        Say(Loc.T("boss_ok", ("n", (Ermelinda.Route.Length - done).ToString())), 2.5f);
    }

    public void BossRelief() { Audio.Ding(); Say(Loc.T("boss_relief"), 2f); }

    public void BossStrike()
    {
        if (Mode != GameMode.Playing) return;
        Strikes++;
        Audio.Buzz(); shake = Mathf.Max(shake, 0.3f); FlashT = 0.4f; Haptic(60);
        if (Strikes >= 3) { BossFail(); return; }
        Say(Loc.T("boss_strike", ("n", Strikes.ToString()), ("s", Ermelinda.WaitT.ToString("0"))), 3.5f);
    }

    void BossFail()
    {
        Mode = GameMode.Paused;
        bossFailT = 3f;
        Audio.SetMode("off");
        Say(Loc.T("boss_fail"), 3f);
        int token = ++runId;
        GetTree().CreateTimer(3.0).Timeout += () => { if (token == runId && Mode == GameMode.Paused && bossFailT > 0f) { Reset(GameMode.Playing); } };
    }

    // detergent spray: works at a distance, shrinks a puddle fast or softens a dried crust for the scraper
    void DoSpray()
    {
        Vector2 pp = PP, fw = Fwd;
        Puddle bq = null; Crust bc = null; float bd = 3.8f;
        foreach (var q in Puddles)
        {
            Vector2 d = q.P - pp; float dl = d.Length();
            if (dl > 0.01f && dl - q.R < bd && d.Normalized().Dot(fw) > 0.8f) { bd = dl - q.R; bq = q; bc = null; }
        }
        foreach (var c in Crusts)
        {
            Vector2 d = c.P - pp; float dl = d.Length();
            if (dl > 0.01f && dl < bd && d.Normalized().Dot(fw) > 0.8f) { bd = dl; bc = c; bq = null; }
        }
        coolT = 0.5f;
        if (Charges <= 0) { Audio.Buzz(); Say(Loc.T("spray_empty"), 2f); return; }
        if (bq == null && bc == null) { Audio.Buzz(); Say(Loc.T("spray_none"), 1.8f); return; }
        Charges--;
        Audio.Swish(); Audio.Scrub(); Haptic(15);
        Vector2 tp = bq != null ? bq.P : bc.P;
        for (int i = 1; i <= 4; i++)
        {
            Vector2 s = pp + (tp - pp) * (i / 4f);
            Burst(s.X, 1.1f - 0.25f * i, s.Y, new[] { Cfg.Hex("#bff0ff"), Colors.White }, 3, 0.4f, 0.45f, 0.05f);
        }
        if (bq != null)
        {
            bq.R -= 0.7f;
            if (bq.R <= 0.22f) CleanPuddle(bq);
        }
        else
        {
            bc.Soft = true;
            Say(Loc.T("crust_soft"), 2f);
        }
        if (Charges == 0) Say(Loc.T("spray_empty"), 2.4f);
    }

    void FinishCrust(Crust c)
    {
        Crusts.Remove(c);
        Vector3 pos = c.Position; int type = c.Type;
        c.QueueFree();
        AddPoop(pos.X, pos.Z, false, type, "", Colors.White);
        Burst(pos.X, 0.25f, pos.Z, new[] { Cfg.Hex("#7a5532"), Cfg.Hex("#5e4126"), Cfg.Types[type].Body[0] }, 16, 1.6f, 0.7f, 0.05f);
        Audio.Pop(); Haptic(25); shake = Mathf.Max(shake, 0.08f);
        Say(Loc.T("crust_done"), 3f);
    }

    public bool TapInReach()
    {
        if (TapObj == null) return false;
        Vector2 d = TapObj.Pos - PP; float dl = d.Length();
        return dl < Cfg.Reach + 0.5f && (dl < 1.3f || d.Dot(Fwd) / (dl > 0.001f ? dl : 1f) > 0.35f);
    }

    public Bin BinInReach()
    {
        Vector2 pp = PP, fw = Fwd;
        Bin best = null; float bd = float.MaxValue;
        foreach (var b in Bins)
        {
            Vector2 d = b.Pos - pp; float dl = d.Length();
            if (dl > Cfg.Reach + 0.5f) continue;
            if ((dl < 1.3f || d.Dot(fw) / (dl > 0.001f ? dl : 1f) > 0.45f) && dl < bd) { bd = dl; best = b; }
        }
        return best;
    }

    // ------------------------------------------------------------ actions
    void Act()
    {
        swingT = 0.3f; coolT = 0.38f;
        Audio.Swish();
        if (HasCarry)
        {
            var st = StackTarget();
            if (st != null) { PickUp(st); return; }
            if (BinInReach() == null) { DropByChoice(); return; }      // nothing to dump into here: the same button puts it down
            TryDump(); return;
        }
        var t = FindTarget();
        if (t != null) { PickUp(t); return; }
        var l = FindLump();
        if (l != null) { RevealPoop(l, "lift"); return; }
        if (PuddleInReach() != null) Say(Loc.T("puddle_hint", ("k", Loc.T(IsTouch ? "puddle_k_touch" : "puddle_k_desk"))), 2.4f);
        else if (CrustInReach() != null) Say(Loc.T(IsTouch ? "pr_crust_touch" : "pr_crust_desk"), 2.4f);
    }

    // drop what you carry right in front of you (e.g. to grab a different poop first)
    public void DropByChoice()
    {
        if (Mode != GameMode.Playing || !HasCarry) return;
        Vector2 f = Fwd;
        string cn = Loc.TypeName(CarryType);
        DropCarried(f.X, f.Y);
        Say(Loc.T("dropped_choice", ("c", cn)), 2f);
    }

    // with a bigger shovel: grab another poop of the same colour while carrying one (not near a bin)
    Poop StackTarget()
    {
        if (!HasCarry || CarryCount >= Cfg.CarryMax || CarrySpecial || CarryType == Cfg.Rad || BinInReach() != null) return null;
        var t = FindTarget();
        if (t == null || t.Type != CarryType || t.Special || t.Type == Cfg.Rad || t.Glow) return null;
        return t;
    }

    void PickUp(Poop p)
    {
        bool stack = HasCarry;
        Poops.Remove(p);
        p.QueueFree();
        CleanedSpots.Add(p.P);
        if (stack) CarryCount++;
        else CarryCount = 1;
        HasCarry = true; CarryType = p.Type; CarryGlow = p.Glow; CarrySpecial = p.Special; CarrySmall = p.Size < 1f;
        // (grabbing the golden one puts the fuse out: the poop node is gone)
        var body = Cfg.Types[p.Type].Body;
        for (int i = 0; i < 3; i++) ((StandardMaterial3D)carriedBody[i].MaterialOverride).AlbedoColor = body[i];
        Audio.Scoop(); Haptic(15);
        Burst(p.Position.X, 0.2f, p.Position.Z, new[] { body[0], body[1] }, 10, 1.4f, 0.6f, 0.05f);
        shake = Mathf.Max(shake, 0.06f);
        if (CarryCount > 1) Say(Loc.T("stacked", ("c", Loc.TypeName(p.Type)), ("n", CarryCount.ToString()), ("m", Cfg.CarryMax.ToString())), 3.5f);
        else Say(Loc.T("got", ("c", Loc.TypeName(p.Type))), 3.5f);
    }

    void TryDump()
    {
        string cn = Loc.TypeName(CarryType);
        var bin = BinInReach();
        if (bin == null) { Say(Loc.T("takeit", ("c", cn)), 2f); return; }
        if (bin.Type != CarryType) { Audio.Buzz(); Say(Loc.T("wrongbin", ("c", cn)), 2.8f); shake = Mathf.Max(shake, 0.1f); return; }
        DumpCarried(bin);
    }

    void DumpCarried(Bin bin)
    {
        var tp = Cfg.Types[CarryType];
        bool wasSpecial = CarrySpecial;
        int n = CarryCount;
        HasCarry = false; CarrySpecial = false; CarryCount = 1;
        Cleaned += n;
        if (wasSpecial) SpecialDone++; else RegDumped += n;
        AddCoins(wasSpecial ? 2 : n);
        Audio.Splat(); Haptic(30);
        Burst(bin.Position.X, 1.1f, bin.Position.Z, new[] { tp.Body[0], tp.Body[1], tp.Body[2], Cfg.Hex("#ffe27a") }, 18, 2.2f, 0.9f, 0.07f);
        shake = Mathf.Max(shake, 0.15f);
        Scored(Loc.T("dumped"));
    }

    void Scored(string prefix)
    {
        if (finalDropped && RegDumped >= Cfg.RegTotal && SpecialDone >= SpecialTotal) { Win(); return; }
        if (Phase == 1 && RegDumped >= PanicAt) StartPanic();
        else if (Cfg.Lvl.Boss) Say(prefix, 1.6f);
        else Say(Loc.T("togo", ("p", prefix), ("n", Mathf.Max(0, TotalNow - Cleaned).ToString())), 2f);
    }

    void AddCoins(int n)
    {
        n *= 2;                              // coin multiplier
        Save.Coins += n; RunCoins += n;
        Save.Store();
    }

    void CleanPuddle(Puddle q)
    {
        Puddles.Remove(q);
        Burst(q.Position.X, 0.2f, q.Position.Z, new[] { Cfg.Hex("#bfe6f7"), Colors.White, Cfg.Hex("#c9a23a") }, 18, 1.6f, 0.8f, 0.06f);
        q.QueueFree();
        Cleaned++;
        if (q.Special) SpecialDone++; else RegDumped++;
        AddCoins(1);
        Audio.Ding(); Haptic(25); shake = Mathf.Max(shake, 0.1f);
        Scrubbing = false;
        Scored(Loc.T("squeaky"));
    }

    void RevealPoop(Poop p, string how)
    {
        p.Reveal();
        Burst(p.Position.X, 0.3f, p.Position.Z, new[] { Cfg.Types[p.Type].Body[0], Cfg.Types[p.Type].Body[1], Colors.White }, 14, 1.6f, 0.7f, 0.06f);
        Audio.Pop(); Haptic(20);
        Say(Loc.T(how == "table" ? "rev_table" : how == "step" ? "rev_step" : "rev_lift"), 2.6f);
    }

    // the carried poop falls to the floor ahead of the player (direction ax, az), keeping its colour and glow
    void DropCarried(float ax, float az)
    {
        if (!HasCarry) return;
        HasCarry = false;
        Vector2 pp = PP;
        float x = pp.X + ax, z = pp.Y + az;
        float baseA = Mathf.Atan2(ax, az);
        for (int i = 0; i < 12; i++)
        {
            float a = baseA + (i > 0 ? ((i % 2 == 1) ? 1f : -1f) * Mathf.Ceil(i / 2f) * 0.5f : 0f);
            float tx = Mathf.Clamp(pp.X + Mathf.Sin(a) * 0.95f, -Cfg.RoomX + 0.5f, Cfg.RoomX - 0.5f);
            float tz = Mathf.Clamp(pp.Y + Mathf.Cos(a) * 0.95f, -Cfg.RoomZ + 0.5f, Cfg.RoomZ - 0.5f);
            if (!Room.InFurniture(tx, tz, 0.35f)) { x = tx; z = tz; break; }
        }
        var np = AddPoop(x, z, CarryGlow, CarryType, "", Colors.White, CarrySpecial, CarrySmall);
        if (CarrySpecial && CarryGlow) np.Fuse = Rand(Cfg.Lvl.FuseMin, Cfg.Lvl.FuseMax);   // dropped the special one: the fuse starts again
        CarrySpecial = false;
        for (int k = 1; k < CarryCount; k++)        // the rest of the stack spills around it
        {
            float ex = x, ez = z;
            for (int tries = 0; tries < 8; tries++)
            {
                float ea = Rand(0f, Mathf.Tau);
                float cx = Mathf.Clamp(x + Mathf.Sin(ea) * 0.55f, -Cfg.RoomX + 0.5f, Cfg.RoomX - 0.5f);
                float cz = Mathf.Clamp(z + Mathf.Cos(ea) * 0.55f, -Cfg.RoomZ + 0.5f, Cfg.RoomZ - 0.5f);
                if (!Room.InFurniture(cx, cz, 0.3f)) { ex = cx; ez = cz; break; }
            }
            AddPoop(ex, ez, false, CarryType, "", Colors.White);
        }
        CarryCount = 1;
        Burst(x, 0.3f, z, Cfg.Types[CarryType].Body, 10, 1.6f, 0.6f, 0.05f);
        Audio.Thud();
    }

    void Slip(string kind)
    {
        slipT = 1f; slipImm = 2.4f;
        Vector2 d = mv; float l = d.Length();
        if (l < 0.1f) { d = Fwd; l = 1f; }
        d /= l;
        fvx = d.X * 4.6f; fvz = d.Y * 4.6f; mv = Vector2.Zero;
        Audio.Slip(); Haptic(40); shake = Mathf.Max(shake, 0.25f);
        if (HasCarry)
        {
            string cn = Loc.TypeName(CarryType);
            DropCarried(d.X, d.Y);
            Say(Loc.T("slip_carry", ("c", cn)), 3f);
        }
        else Say(Loc.T(kind == "squish" ? "slip_squish" : "slip_puddle"), 2.2f);
    }

    // Buba crashes into the player (panic phase only)
    public void Bump()
    {
        Vector2 n = PP - Dog.P; float l = n.Length();
        if (l < 0.001f) { n = new Vector2(1f, 0f); l = 1f; }
        n /= l;
        bumpCd = Cfg.Lvl.BumpCd;
        fvx = n.X * 7f; fvz = n.Y * 7f;
        Audio.Bark(); Audio.Thud(); Haptic(60); shake = Mathf.Max(shake, 0.35f);
        if (HasCarry)
        {
            string cn = Loc.TypeName(CarryType);
            DropCarried(n.X, n.Y);
            Say(Loc.T("bump_carry", ("c", cn)), 3f);
        }
        else Say(Loc.T("bump"), 1.8f);
    }

    public bool CanBump() { return bumpCd <= 0f; }

    void Zap(Poop p)
    {
        Vector2 n = PP - p.P; float l = n.Length();
        if (l < 0.001f) { n = new Vector2(1f, 0f); l = 1f; }
        n /= l;
        fvx += n.X * 4.5f; fvz += n.Y * 4.5f; slowT = 1.6f; FlashT = 0.5f; shake = Mathf.Max(shake, 0.2f);
        Audio.Zap(); Haptic(30);
        Say(Loc.T("zap"), 2.6f);
    }

    // ------------------------------------------------------------ Buba's panic and the win
    void StartPanic()
    {
        Phase = 2;
        DropsLeft = Cfg.Lvl.Drops;
        ChooseDropSpot();
        Dog.StartPanic();
        Ui.Panic(true);
        Audio.SetMode("panic"); Audio.Bark(); Haptic(80);
        Say(Loc.T("panic"), 3f);
    }

    // a spot that was already cleaned (and not too near the player or an earlier special drop) for Buba's next special poop
    public void ChooseDropSpot()
    {
        Vector2 pp = PP;
        var spots = CleanedSpots.FindAll(c => c.DistanceTo(pp) >= 4f && !Room.InFurniture(c.X, c.Y, 0.1f)
                                              && !Poops.Exists(o => o.Special && o.Glow && o.P.DistanceTo(c) < 3f));
        if (spots.Count == 0) spots = CleanedSpots.FindAll(c => c.DistanceTo(pp) >= 4f && !Room.InFurniture(c.X, c.Y, 0.1f));
        if (spots.Count == 0 && CleanedSpots.Count == 0) spots.Add(new Vector2(0f, -4f));
        else if (spots.Count == 0)
        {
            Vector2 far = CleanedSpots[0];
            foreach (var c in CleanedSpots) if (c.DistanceTo(pp) > far.DistanceTo(pp)) far = c;
            spots.Add(far);
        }
        DropSpot = spots[(int)(GD.Randi() % (uint)spots.Count)];
        FinalType = (int)(GD.Randi() % (uint)Cfg.NormalTypes);
    }

    // called by Buba when he reaches the spot
    public void DropFinal()
    {
        var p = AddPoop(DropSpot.X, DropSpot.Y, true, FinalType, "", Colors.White, true);
        p.Fuse = Rand(Cfg.Lvl.FuseMin, Cfg.Lvl.FuseMax);
        DropsLeft--;
        if (DropsLeft <= 0) { finalDropped = true; panicEndT = 7f; }
        else Dog.RedropT = Rand(4f, 6f);                       // he is not done yet
        Burst(p.Position.X, 0.4f, p.Position.Z, new[] { Cfg.Hex("#ffd66b"), Colors.White, Cfg.Types[FinalType].Body[0] }, 20, 2.2f, 1f, 0.07f);
        shake = Mathf.Max(shake, 0.35f);
        Audio.Pop(); Audio.Bark();
        Say(Loc.T("final", ("c", Loc.TypeName(FinalType))), 5f);
    }

    // ticks the special drops: Buba calms down again a few seconds after the last one, and each golden poop explodes if nobody grabs it
    void TickSpecial(float dt)
    {
        if (panicEndT > 0f)
        {
            panicEndT -= dt;
            if (panicEndT <= 0f && Phase == 2) { Phase = 3; Ui.Panic(false); Audio.SetMode("calm"); }
        }
        foreach (var p in Poops.ToArray())
        {
            if (p.Fuse <= 0f) continue;       // -1 = no fuse
            p.Fuse -= dt;
            if (p.Fuse <= 0f) { p.Fuse = -1f; ExplodeSpecial(p); }
        }
    }

    void ExplodeSpecial(Poop sp)
    {
        if (sp == null || !Poops.Contains(sp)) return;
        Vector2 c = sp.P;
        Poops.Remove(sp); sp.QueueFree();

        var q = Puddle.Create(c.X, c.Y, 0.5f, 1.3f); q.Special = true;
        AddChild(q); Puddles.Add(q);

        int n = 2 + (int)(GD.Randi() % 4);   // 2..5 little ones
        for (int i = 0; i < n; i++)
        {
            float x = c.X, z = c.Y;
            for (int t = 0; t < 30; t++)
            {
                float a = Rand(0f, Mathf.Tau), r = Rand(0.8f, 1.9f);
                float tx = c.X + Mathf.Sin(a) * r, tz = c.Y + Mathf.Cos(a) * r;
                if (Mathf.Abs(tx) > Cfg.RoomX - 0.6f || Mathf.Abs(tz) > Cfg.RoomZ - 0.6f) continue;
                if (Room.InFurniture(tx, tz, 0.4f)) continue;
                x = tx; z = tz; break;
            }
            AddPoop(x, z, false, (int)(GD.Randi() % (uint)Cfg.NormalTypes), "", Colors.White, true, true);
        }
        SpecialTotal += n;   // this drop is now a puddle + n little ones: n more things to clean

        var body = Cfg.Types[sp.Type].Body;
        Burst(c.X, 0.5f, c.Y, new[] { Cfg.Hex("#ffd66b"), Colors.White, body[0], body[1] }, 42, 3.4f, 1.2f, 0.09f);
        shake = Mathf.Max(shake, 0.8f); FlashT = Mathf.Max(FlashT, 0.35f);
        Audio.Pop(); Audio.Splat(); Audio.Bark(); Haptic(90);
        Vector2 nn = PP - c; float dl = nn.Length();
        if (dl < 2.4f) { if (dl < 0.001f) nn = new Vector2(1f, 0f); else nn /= dl; fvx += nn.X * 6f; fvz += nn.Y * 6f; }   // blast knocks you back
        Say(Loc.T("boom", ("n", n.ToString())), 4f);
    }

    void Win()
    {
        Mode = GameMode.Won;
        Save.Unlock(Cfg.Lvl.No + 1);
        Audio.SetMode("off"); Audio.Win(); Haptic(120);
        Ui.Panic(false);
        Ui.HideMsg();
        Input.MouseMode = Input.MouseModeEnum.Visible;
        if (Touch != null) Touch.Reset();
        int token = runId;
        GetTree().CreateTimer(0.7).Timeout += () => { if (token == runId && Mode == GameMode.Won) Ui.ShowVictory(); };
    }

    // ------------------------------------------------------------ text for the HUD
    public float Percent()
    {
        if (Cfg.Lvl.Boss) return Mathf.Round(100f * StopsDone / Ermelinda.Route.Length);
        float v = 90f * RegDumped / Cfg.RegTotal + 10f * SpecialDone / Mathf.Max(1, SpecialTotal);
        return Mathf.Min(100f, Mathf.Round(v));
    }

    public string PromptText(out bool ready)
    {
        ready = false;
        if (Mode != GameMode.Playing) return "";
        string K = IsTouch ? Loc.T("k_prefix_touch") : Loc.T("k_prefix_desk");
        if (HasCarry)
        {
            string cn = Loc.TypeName(CarryType);
            var br = BinInReach();
            if (br != null && br.Type == CarryType) { ready = true; return K + Loc.T("pr_dump", ("c", cn)); }
            if (br != null) return Loc.T("pr_wrong", ("c", cn));
            if (StackTarget() != null) { ready = true; return K + Loc.T("pr_stack", ("c", cn)); }
            return Loc.T("pr_carry", ("c", cn)) + Loc.T(IsTouch ? "drop_hint_touch" : "drop_hint");
        }
        if (ToolSel == Tool.Mop)
        {
            bool atTap = TapInReach();
            if (atTap && (PuddleT == null || Water <= 0f))
            {
                if (Water >= 1f) return Loc.T("bucket_full");
                ready = true; return Loc.T(IsTouch ? "pr_refill_touch" : "pr_refill_desk");
            }
            if (PuddleT != null)
            {
                if (Water <= 0f) return Loc.T("bucket_empty");
                ready = true; return Loc.T(IsTouch ? "pr_scrub_touch" : "pr_scrub_desk");
            }
            if (Target != null) return Loc.T("pr_mopnote", ("k", Loc.T(IsTouch ? "pr_mopnote_touch" : "pr_mopnote_desk")));
            return "";
        }
        if (ToolSel == Tool.Spray)
        {
            if (Charges <= 0) return Loc.T("spray_empty");
            ready = true; return Loc.T(IsTouch ? "pr_spray_touch" : "pr_spray_desk", ("n", Charges.ToString()));
        }
        if (ToolSel == Tool.Scraper)
        {
            if (CrustT != null) { ready = true; return Loc.T(IsTouch ? "pr_scrape_touch" : "pr_scrape_desk"); }
            return "";
        }
        if (Target != null) { ready = true; return K + Loc.T("pr_scoop"); }
        if (LumpT != null) { ready = true; return K + Loc.T("pr_lift"); }
        if (CrustT != null) return Loc.T(IsTouch ? "pr_crust_touch" : "pr_crust_desk");
        if (PuddleT != null) return Loc.T("pr_puddle", ("k", Loc.T(IsTouch ? "pr_puddle_touch" : "pr_puddle_desk")));
        return "";
    }

    public string ActionLabel()
    {
        if (HasCarry) return Loc.T("b_dump");
        if (ToolSel == Tool.Mop) return Loc.T(TapInReach() && (PuddleT == null || Water <= 0f) ? "b_refill" : "b_scrub");
        if (ToolSel == Tool.Scraper) return Loc.T("b_scrape");
        if (ToolSel == Tool.Spray) return Loc.T("b_spray");
        return Loc.T("b_scoop");
    }
}
