using Godot;
using System.Collections.Generic;

// All 2D UI, built in code: top bar, messages, prompt, tool chips, minimap, title/pause overlay, victory screen.
public partial class Hud : CanvasLayer
{
    Main g;
    Control root;
    Label title, count, snd, pct, msg, prompt, banner, tool1, tool2, tool3, tool4;
    ColorRect top, barBg, barFill, cross;
    TextureRect vig, flash;
    ColorRect waterBg, waterFill;
    Label waterLbl;
    Minimap mini;
    ColorRect overlay;
    Label ovTitle, ovSub, ovText, ovP1, ovP2, ovCtl;
    Button startBtn, langBtn, soundBtn;
    ColorRect victory;
    Label winH, winP;
    Button restartBtn, nextBtn, levelBtn, menuBtn, shopBtnP, shopBtnW;
    ColorRect scrapeBg, scrapeFill;
    bool overlayPaused;
    float bannerT;
    bool panicOn;
    StyleBoxFlat promptIdle, promptReady, chipOn, chipOff;
    Vector2 lastView;

    class Conf { public ColorRect R; public float Vy, Vx, Spin; }
    readonly List<Conf> confetti = new List<Conf>();

    static readonly Color Ink = new Color(0.357f, 0.29f, 0.494f);

    // ---------------------------------------------------------------- small helpers
    static void Place(Control c, float al, float at, float ar, float ab, float ol, float ot, float oR, float ob)
    {
        c.AnchorLeft = al; c.AnchorTop = at; c.AnchorRight = ar; c.AnchorBottom = ab;
        c.OffsetLeft = ol; c.OffsetTop = ot; c.OffsetRight = oR; c.OffsetBottom = ob;
    }

    static void Full(Control c) { Place(c, 0f, 0f, 1f, 1f, 0f, 0f, 0f, 0f); }

    static Label Lbl(Node parent, int size, Color color, HorizontalAlignment align)
    {
        var l = new Label();
        l.HorizontalAlignment = align;
        l.VerticalAlignment = VerticalAlignment.Center;
        l.MouseFilter = Control.MouseFilterEnum.Ignore;
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        parent.AddChild(l);
        return l;
    }

    static StyleBoxFlat Box(Color bg, Color border, int bw, int radius, int margin)
    {
        var s = new StyleBoxFlat();
        s.BgColor = bg; s.BorderColor = border;
        s.BorderWidthLeft = bw; s.BorderWidthRight = bw; s.BorderWidthTop = bw; s.BorderWidthBottom = bw;
        s.CornerRadiusTopLeft = radius; s.CornerRadiusTopRight = radius; s.CornerRadiusBottomLeft = radius; s.CornerRadiusBottomRight = radius;
        s.ContentMarginLeft = margin; s.ContentMarginRight = margin; s.ContentMarginTop = margin / 2f; s.ContentMarginBottom = margin / 2f;
        return s;
    }

    static TextureRect Vignette(Color edge)
    {
        var grad = new Gradient();
        grad.SetColor(0, new Color(edge.R, edge.G, edge.B, 0f));
        grad.SetColor(1, edge);
        grad.SetOffset(0, 0.5f);
        grad.SetOffset(1, 1f);
        var tex = new GradientTexture2D();
        tex.Gradient = grad; tex.Fill = GradientTexture2D.FillEnum.Radial;
        tex.FillFrom = new Vector2(0.5f, 0.5f); tex.FillTo = new Vector2(1f, 0.5f);
        tex.Width = 256; tex.Height = 256;
        var tr = new TextureRect();
        tr.Texture = tex; tr.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize; tr.StretchMode = TextureRect.StretchModeEnum.Scale;
        tr.MouseFilter = Control.MouseFilterEnum.Ignore;
        Full(tr);
        return tr;
    }

    Button MakeButton(Node parent, int size, Color bg, Color fg, Vector2 minSize)
    {
        var b = new Button();
        b.CustomMinimumSize = minSize;
        b.AddThemeFontSizeOverride("font_size", size);
        b.AddThemeColorOverride("font_color", fg);
        b.AddThemeColorOverride("font_hover_color", fg);
        b.AddThemeColorOverride("font_pressed_color", fg);
        b.AddThemeColorOverride("font_focus_color", fg);
        var normal = Box(bg, new Color(0f, 0f, 0f, 0f), 0, 18, 18);
        var hover = Box(bg.Lightened(0.15f), new Color(0f, 0f, 0f, 0f), 0, 18, 18);
        var pressed = Box(bg.Darkened(0.1f), new Color(0f, 0f, 0f, 0f), 0, 18, 18);
        b.AddThemeStyleboxOverride("normal", normal);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", pressed);
        b.AddThemeStyleboxOverride("focus", normal);
        b.FocusMode = Control.FocusModeEnum.None;
        parent.AddChild(b);
        return b;
    }

    // ---------------------------------------------------------------- construction
    public void Init(Main game)
    {
        g = game;
        Layer = 5;
        root = new Control(); root.MouseFilter = Control.MouseFilterEnum.Ignore; Full(root); AddChild(root);

        vig = Vignette(new Color(0.85f, 0.1f, 0.15f, 0.5f)); vig.Visible = false; root.AddChild(vig);
        flash = Vignette(new Color(0.65f, 1f, 0.16f, 0.85f)); flash.Modulate = new Color(1f, 1f, 1f, 0f); root.AddChild(flash);

        cross = new ColorRect(); cross.Color = new Color(1f, 1f, 1f, 0.85f); cross.Size = new Vector2(8f, 8f); cross.MouseFilter = Control.MouseFilterEnum.Ignore; root.AddChild(cross);

        top = new ColorRect(); top.Color = new Color(1f, 1f, 1f, 0.78f); top.MouseFilter = Control.MouseFilterEnum.Ignore;
        Place(top, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 66f); root.AddChild(top);
        title = Lbl(top, 13, Ink, HorizontalAlignment.Left); title.VerticalAlignment = VerticalAlignment.Top;
        Place(title, 0f, 0f, 0.34f, 0f, 14f, 8f, 0f, 60f); title.ClipText = true;
        pct = Lbl(top, 32, new Color(0.29f, 0.54f, 0.35f), HorizontalAlignment.Center);
        Place(pct, 0.34f, 0f, 0.66f, 0f, 0f, 2f, 0f, 42f);
        barBg = new ColorRect(); barBg.Color = new Color(0.91f, 0.88f, 0.98f); barBg.MouseFilter = Control.MouseFilterEnum.Ignore;
        Place(barBg, 0.4f, 0f, 0.6f, 0f, 0f, 47f, 0f, 58f); top.AddChild(barBg);
        barFill = new ColorRect(); barFill.Color = new Color(0.48f, 0.85f, 0.56f); barFill.MouseFilter = Control.MouseFilterEnum.Ignore; barBg.AddChild(barFill);
        count = Lbl(top, 15, Ink, HorizontalAlignment.Right); Place(count, 0.66f, 0f, 1f, 0f, 0f, 8f, -14f, 30f);
        snd = Lbl(top, 12, new Color(0.6f, 0.55f, 0.72f), HorizontalAlignment.Right); Place(snd, 0.66f, 0f, 1f, 0f, 0f, 32f, -14f, 56f);

        msg = Lbl(root, 16, Colors.White, HorizontalAlignment.Center);
        msg.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        msg.AddThemeStyleboxOverride("normal", Box(new Color(0.36f, 0.29f, 0.49f, 0.9f), new Color(0f, 0f, 0f, 0f), 0, 16, 18));
        msg.Visible = false;

        promptIdle = Box(new Color(1f, 1f, 1f, 0.82f), new Color(0f, 0f, 0f, 0f), 0, 14, 16);
        promptReady = Box(new Color(1f, 0.93f, 0.62f, 0.95f), new Color(1f, 0.72f, 0.2f), 3, 14, 16);
        prompt = Lbl(root, 16, Ink, HorizontalAlignment.Center);
        prompt.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        prompt.AddThemeStyleboxOverride("normal", promptIdle);
        prompt.Visible = false;

        banner = Lbl(root, 44, new Color(1f, 0.84f, 0.42f), HorizontalAlignment.Center);
        banner.AddThemeConstantOverride("outline_size", 12);
        banner.AddThemeColorOverride("font_outline_color", new Color(0.55f, 0.1f, 0.2f));
        banner.Visible = false;

        chipOn = Box(new Color(1f, 1f, 1f, 0.95f), new Color(1f, 0.84f, 0.42f), 3, 14, 14);
        chipOff = Box(new Color(1f, 1f, 1f, 0.78f), new Color(0f, 0f, 0f, 0f), 3, 14, 14);
        tool1 = Lbl(root, 15, Ink, HorizontalAlignment.Center); tool2 = Lbl(root, 15, Ink, HorizontalAlignment.Center);
        tool1.AddThemeStyleboxOverride("normal", chipOn); tool2.AddThemeStyleboxOverride("normal", chipOff);
        tool3 = Lbl(root, 15, Ink, HorizontalAlignment.Center); tool3.AddThemeStyleboxOverride("normal", chipOff);
        tool4 = Lbl(root, 15, Ink, HorizontalAlignment.Center); tool4.AddThemeStyleboxOverride("normal", chipOff);
        scrapeBg = new ColorRect(); scrapeBg.Color = new Color(1f, 1f, 1f, 0.8f); scrapeBg.MouseFilter = Control.MouseFilterEnum.Ignore; scrapeBg.Visible = false; root.AddChild(scrapeBg);
        scrapeFill = new ColorRect(); scrapeFill.Color = new Color(0.62f, 0.45f, 0.25f); scrapeFill.MouseFilter = Control.MouseFilterEnum.Ignore; scrapeBg.AddChild(scrapeFill);

        waterBg = new ColorRect(); waterBg.Color = new Color(1f, 1f, 1f, 0.8f); waterBg.MouseFilter = Control.MouseFilterEnum.Ignore; waterBg.Visible = false; root.AddChild(waterBg);
        waterFill = new ColorRect(); waterFill.MouseFilter = Control.MouseFilterEnum.Ignore; waterBg.AddChild(waterFill);
        waterLbl = Lbl(waterBg, 11, Ink, HorizontalAlignment.Center); Full(waterLbl);

        mini = new Minimap(); mini.Init(g); root.AddChild(mini);

        BuildOverlay();
        BuildVictory();

        GetViewport().SizeChanged += Layout;
        Relabel();
    }

    public override void _ExitTree()
    {
        var vp = GetViewport();
        if (vp != null) vp.SizeChanged -= Layout;
    }

    void BuildOverlay()
    {
        overlay = new ColorRect(); overlay.Color = new Color(0.29f, 0.23f, 0.44f, 0.78f); overlay.MouseFilter = Control.MouseFilterEnum.Stop;
        Full(overlay); root.AddChild(overlay);
        var margin = new MarginContainer(); Full(margin);
        margin.AddThemeConstantOverride("margin_left", 16); margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 12); margin.AddThemeConstantOverride("margin_bottom", 12);
        overlay.AddChild(margin);
        var scroll = new ScrollContainer(); scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled; margin.AddChild(scroll);
        var center = new CenterContainer(); center.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; center.SizeFlagsVertical = Control.SizeFlags.ExpandFill; scroll.AddChild(center);
        var box = new VBoxContainer(); box.AddThemeConstantOverride("separation", 10); center.AddChild(box);

        ovTitle = Lbl(box, 40, new Color(1f, 0.84f, 0.42f), HorizontalAlignment.Center);
        ovSub = Lbl(box, 24, Colors.White, HorizontalAlignment.Center);
        ovText = Lbl(box, 16, new Color(1f, 1f, 1f, 0.95f), HorizontalAlignment.Center);
        ovP1 = Lbl(box, 14, new Color(1f, 1f, 1f, 0.88f), HorizontalAlignment.Center);
        ovP2 = Lbl(box, 14, new Color(1f, 1f, 1f, 0.88f), HorizontalAlignment.Center);
        ovCtl = Lbl(box, 13, new Color(1f, 0.93f, 0.7f), HorizontalAlignment.Center);
        foreach (var l in new[] { ovText, ovP1, ovP2, ovCtl }) l.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        startBtn = MakeButton(box, 22, new Color(1f, 0.84f, 0.42f), new Color(0.29f, 0.2f, 0.4f), new Vector2(260f, 52f));
        startBtn.Pressed += () => g.StartPressed();
        levelBtn = MakeButton(box, 16, new Color(0.8f, 0.9f, 1f), Ink, new Vector2(220f, 40f));
        levelBtn.Pressed += () => g.SelectLevel(Cfg.Lvl.No % Cfg.MaxLevel + 1);
        menuBtn = MakeButton(box, 16, new Color(1f, 0.78f, 0.82f), Ink, new Vector2(220f, 40f));
        menuBtn.Pressed += () => g.ToMenu();
        shopBtnP = MakeButton(box, 16, new Color(1f, 0.93f, 0.6f), Ink, new Vector2(220f, 40f));
        shopBtnP.Pressed += () => g.OpenShop();
        langBtn = MakeButton(box, 14, Colors.White, Ink, new Vector2(180f, 36f));
        langBtn.Pressed += () => Loc.Toggle();
        soundBtn = MakeButton(box, 14, Colors.White, Ink, new Vector2(180f, 36f));
        soundBtn.Pressed += () => { if (g.Audio != null) g.Audio.ToggleMute(); };
    }

    void BuildVictory()
    {
        victory = new ColorRect(); victory.Color = new Color(1f, 0.82f, 0.4f, 0.55f); victory.MouseFilter = Control.MouseFilterEnum.Stop; victory.Visible = false;
        Full(victory); root.AddChild(victory);
        var cc = new CenterContainer(); Full(cc); cc.MouseFilter = Control.MouseFilterEnum.Ignore; victory.AddChild(cc);
        var box = new VBoxContainer(); box.AddThemeConstantOverride("separation", 14); cc.AddChild(box);
        winH = Lbl(box, 40, Colors.White, HorizontalAlignment.Center);
        winH.AddThemeConstantOverride("outline_size", 10); winH.AddThemeColorOverride("font_outline_color", new Color(0.36f, 0.29f, 0.49f));
        winP = Lbl(box, 18, new Color(0.29f, 0.2f, 0.4f), HorizontalAlignment.Center);
        foreach (var l in new[] { winH, winP }) l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        restartBtn = MakeButton(box, 22, new Color(0.48f, 0.85f, 0.56f), new Color(0.12f, 0.3f, 0.18f), new Vector2(240f, 52f));
        restartBtn.Pressed += () => g.RestartPressed();
        nextBtn = MakeButton(box, 22, new Color(1f, 0.84f, 0.42f), new Color(0.29f, 0.2f, 0.4f), new Vector2(240f, 52f));
        nextBtn.Pressed += () => g.NextLevel();
        shopBtnW = MakeButton(box, 18, new Color(1f, 0.93f, 0.6f), Ink, new Vector2(240f, 44f));
        shopBtnW.Pressed += () => g.OpenShop();
    }

    // ---------------------------------------------------------------- layout and texts
    public void Layout()
    {
        if (g == null) return;
        Vector2 v = GetViewport().GetVisibleRect().Size;
        lastView = v;
        bool touch = g.IsTouch;
        float vw = v.X, vh = v.Y;

        cross.Position = new Vector2(vw / 2f - 4f, vh / 2f - 4f);
        float mw = touch ? Mathf.Min(520f, vw - 154f) : Mathf.Min(560f, vw - 24f);
        float mx = touch ? 142f + (vw - 154f - mw) / 2f : (vw - mw) / 2f;
        msg.Position = new Vector2(mx, 76f); msg.Size = new Vector2(mw, 10f);
        float pw = Mathf.Min(380f, vw - 24f);
        prompt.Position = new Vector2((vw - pw) / 2f, vh / 2f + 40f); prompt.Size = new Vector2(pw, 10f);
        scrapeBg.Position = new Vector2(vw / 2f - 40f, vh / 2f + 22f); scrapeBg.Size = new Vector2(80f, 9f);
        scrapeFill.Size = new Vector2(0f, 9f);
        banner.Position = new Vector2(0f, 112f); banner.Size = new Vector2(vw, 60f);

        if (touch)
        {
            tool1.Position = new Vector2(12f, vh - 108f); tool1.Size = new Vector2(124f, 40f);
            tool2.Position = new Vector2(12f, vh - 60f); tool2.Size = new Vector2(124f, 40f);
            tool3.Position = new Vector2(12f, vh - 156f); tool3.Size = new Vector2(124f, 40f);
            tool4.Position = new Vector2(12f, vh - 204f); tool4.Size = new Vector2(124f, 40f);
            mini.Position = new Vector2(8f, 72f); mini.Size = new Vector2(126f, 91f);
            waterBg.Position = new Vector2(12f, vh - 18f); waterBg.Size = new Vector2(124f, 16f);
        }
        else
        {
            int nChips = 2 + (Cfg.Lvl.Scraper ? 1 : 0) + (Cfg.Lvl.Spray > 0 ? 1 : 0);
            float startX = vw / 2f - nChips * 67f;
            if (startX + nChips * 134f > vw - 200f) startX = 12f;      // narrow window: keep clear of the minimap
            var chips = new[] { tool1, tool2, tool3, tool4 };
            for (int i = 0; i < nChips; i++) { chips[i].Position = new Vector2(startX + i * 134f, vh - 52f); chips[i].Size = new Vector2(128f, 38f); }
            mini.Position = new Vector2(vw - 192f, vh - 142f); mini.Size = new Vector2(180f, 130f);
            waterBg.Position = new Vector2(tool2.Position.X, vh - 72f); waterBg.Size = new Vector2(128f, 16f);
        }
        float tw = Mathf.Min(520f, vw - 60f);
        foreach (var l in new[] { ovText, ovP1, ovP2, ovCtl, winH, winP }) l.CustomMinimumSize = new Vector2(tw, 0f);
    }

    public void Relabel()
    {
        if (g == null) return;
        title.Text = Loc.T(Cfg.Lvl.No > 1 ? "hud_title" + Cfg.Lvl.No : "hud_title");
        tool1.Text = Loc.T("tool1"); tool2.Text = Loc.T("tool2"); tool3.Text = Loc.T("tool3"); tool4.Text = Loc.T("tool4");
        banner.Text = Loc.T("banner");
        langBtn.Text = Loc.T("langBtn");
        ovP1.Text = Loc.T("p1"); ovP2.Text = Loc.T("p2");
        ovCtl.Text = Loc.T(g.IsTouch ? "touch" : "desk");
        string wk = Cfg.Lvl.No > 1 ? "win" + Cfg.Lvl.No + "_" : "win_";
        winH.Text = Loc.T(wk + "h"); winP.Text = Loc.T(wk + "p"); restartBtn.Text = Loc.T("restart");
        nextBtn.Text = Loc.T("next_level"); shopBtnW.Text = Loc.T("menu_shop");
        menuBtn.Text = Loc.T("menu_main"); nextBtn.Visible = Cfg.Lvl.No < Cfg.MaxLevel;
        RelabelOverlay();
        Layout();
    }

    void RelabelOverlay()
    {
        string sfx = g != null && g.IsTouch ? "_touch" : "";
        ovTitle.Text = Loc.T(overlayPaused ? "pa_title" : "ov_title");
        ovSub.Text = Loc.T(overlayPaused ? "pa_sub" : "ov_sub");
        ovText.Text = Loc.T(overlayPaused ? "pa_text" : (Cfg.Lvl.No > 1 ? "ov_text" + Cfg.Lvl.No : "ov_text"));
        startBtn.Text = Loc.T((overlayPaused ? "resume" : "start") + sfx);
        levelBtn.Text = Loc.T("level_btn", ("n", (Cfg.Lvl.No % Cfg.MaxLevel + 1).ToString()));
        levelBtn.Visible = false; menuBtn.Text = Loc.T("menu_main"); menuBtn.Visible = overlayPaused; shopBtnP.Text = Loc.T("menu_shop"); shopBtnP.Visible = overlayPaused;
    }

    // ---------------------------------------------------------------- show / hide
    public void ShowOverlay(bool paused) { overlayPaused = paused; RelabelOverlay(); overlay.Visible = true; }
    public void HideOverlay() { overlay.Visible = false; }
    public void ShowMsg(string text) { msg.Text = text; msg.Visible = true; msg.Size = new Vector2(msg.Size.X, 10f); }
    public void HideMsg() { msg.Visible = false; }
    public void Panic(bool on) { panicOn = on; if (on) bannerT = 2.4f; if (!on) banner.Visible = false; }

    public void HideVictory()
    {
        victory.Visible = false;
        foreach (var c in confetti) c.R.QueueFree();
        confetti.Clear();
    }

    public void ShowVictory()
    {
        victory.Visible = true;
        winP.Text = Loc.T(Cfg.Lvl.No > 1 ? "win" + Cfg.Lvl.No + "_p" : "win_p") + "\n" + Loc.T("coins_run", ("n", g.RunCoins.ToString()), ("t", Save.Coins.ToString()));
        var rng = new RandomNumberGenerator(); rng.Randomize();
        Vector2 v = GetViewport().GetVisibleRect().Size;
        string[] cols = { "#ffd66b", "#ff8fb1", "#8fd3ff", "#7bd88f", "#b79cff", "#ffffff" };
        for (int i = 0; i < 80; i++)
        {
            var r = new ColorRect(); r.Color = Cfg.Hex(cols[i % cols.Length]); r.Size = new Vector2(rng.RandfRange(6f, 12f), rng.RandfRange(8f, 16f));
            r.MouseFilter = Control.MouseFilterEnum.Ignore;
            r.Position = new Vector2(rng.RandfRange(0f, v.X), rng.RandfRange(-v.Y, 0f));
            victory.AddChild(r); victory.MoveChild(r, 0);
            confetti.Add(new Conf { R = r, Vy = rng.RandfRange(90f, 220f), Vx = rng.RandfRange(-40f, 40f), Spin = rng.RandfRange(-3f, 3f) });
        }
    }

    // ---------------------------------------------------------------- per frame
    public override void _Process(double delta)
    {
        if (g == null) return;
        float dt = (float)delta;
        Vector2 v = GetViewport().GetVisibleRect().Size;
        if (v != lastView) Layout();

        bool playing = g.Mode == GameMode.Playing;
        pct.Text = (int)g.Percent() + "%";
        float frac = Mathf.Clamp(g.Percent() / 100f, 0f, 1f);
        barFill.Position = Vector2.Zero; barFill.Size = new Vector2(barBg.Size.X * frac, barBg.Size.Y);
        if (Cfg.Lvl.Boss) count.Text = g.GraceLeft > 0f ? Loc.T("boss_wait", ("n", ((int)Mathf.Ceil(g.GraceLeft)).ToString()))
            : Loc.T("boss_hud", ("s", g.Strikes.ToString()), ("n", g.StopsDone.ToString()), ("t", Ermelinda.Route.Length.ToString()));
        else count.Text = Loc.T("cleaned", ("n", g.Cleaned.ToString()), ("t", g.TotalNow.ToString()));
        string st = Loc.T(g.Audio != null && g.Audio.Muted ? "snd_off" : "snd_on");
        if (g.IsTouch) st = st.Replace(" (M)", "");      // no keyboard on a phone
        snd.Text = Loc.T("coins_hud", ("n", Save.Coins.ToString())) + "  ·  " + st;
        soundBtn.Text = st;

        bool ready;
        string pt = g.PromptText(out ready);
        prompt.Text = pt; prompt.Visible = pt != "";
        prompt.AddThemeStyleboxOverride("normal", ready ? promptReady : promptIdle);

        bool showTools = playing;
        tool1.Visible = showTools; tool2.Visible = showTools; tool3.Visible = showTools && Cfg.Lvl.Scraper; tool4.Visible = showTools && Cfg.Lvl.Spray > 0;
        tool1.AddThemeStyleboxOverride("normal", g.ToolSel == Tool.Shovel ? chipOn : chipOff);
        tool2.AddThemeStyleboxOverride("normal", g.ToolSel == Tool.Mop ? chipOn : chipOff);
        tool3.AddThemeStyleboxOverride("normal", g.ToolSel == Tool.Scraper ? chipOn : chipOff);
        tool3.Modulate = g.HasCarry ? new Color(1f, 1f, 1f, 0.45f) : Colors.White;
        tool4.AddThemeStyleboxOverride("normal", g.ToolSel == Tool.Spray ? chipOn : chipOff);
        tool4.Modulate = g.HasCarry ? new Color(1f, 1f, 1f, 0.45f) : Colors.White;
        scrapeBg.Visible = playing && g.ScrapeP > 0.01f;
        if (scrapeBg.Visible) scrapeFill.Size = new Vector2(scrapeBg.Size.X * Mathf.Clamp(g.ScrapeP, 0f, 1f), scrapeBg.Size.Y);
        tool2.Modulate = g.HasCarry ? new Color(1f, 1f, 1f, 0.45f) : Colors.White;
        cross.Visible = playing && !g.IsTouch;
        waterBg.Visible = playing && (g.ToolSel == Tool.Mop || g.ToolSel == Tool.Spray);
        if (waterBg.Visible)
        {
            if (g.ToolSel == Tool.Spray)
            {
                float sf = Cfg.SprayMax > 0 ? (float)g.Charges / Cfg.SprayMax : 0f;
                waterFill.Position = Vector2.Zero; waterFill.Size = new Vector2(waterBg.Size.X * sf, waterBg.Size.Y);
                waterFill.Color = new Color(0.37f, 0.78f, 0.91f);
                waterLbl.Text = Loc.T("spray_bar", ("n", g.Charges.ToString()), ("t", Cfg.SprayMax.ToString()));
            }
            else
            {
            float wl = Mathf.Clamp(g.Water, 0f, 1f);
            waterFill.Position = Vector2.Zero; waterFill.Size = new Vector2(waterBg.Size.X * wl, waterBg.Size.Y);
            waterFill.Color = new Color(0.55f, 0.42f, 0.25f).Lerp(new Color(0.35f, 0.7f, 1f), wl);   // murkier as the water runs out
            waterLbl.Text = Loc.T(Cfg.Lvl.DirtyWater ? "bucket2" : "bucket", ("n", ((int)Mathf.Round(wl * 100f)).ToString()));
            }
        }

        mini.Visible = g.ShowMinimap && (playing || g.Mode == GameMode.Paused);
        flash.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(g.FlashT * 2f, 0f, 0.7f));
        vig.Visible = panicOn && playing;
        if (vig.Visible) vig.Modulate = new Color(1f, 1f, 1f, 0.55f + 0.45f * Mathf.Sin(g.Clock * 6f));
        if (bannerT > 0f) { bannerT -= dt; banner.Visible = true; banner.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(bannerT * 2f, 0f, 1f)); if (bannerT <= 0f) banner.Visible = false; }

        if (victory.Visible)
        {
            foreach (var c in confetti)
            {
                var p = c.R.Position; p.Y += c.Vy * dt; p.X += c.Vx * dt;
                if (p.Y > v.Y + 20f) { p.Y = -20f; p.X = GD.Randf() * v.X; }
                c.R.Position = p; c.R.Rotation += c.Spin * dt;
            }
        }
    }
}
