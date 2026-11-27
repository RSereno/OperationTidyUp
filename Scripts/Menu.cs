using Godot;
using System.Collections.Generic;

// Main menu shown over a slow camera pan of the level: Play, Levels, Settings, Quit.
public partial class MainMenu : CanvasLayer
{
    Main g;
    Control root;
    VBoxContainer pageMain, pageLevels, pageSettings, pageShop;
    Label coinsL;
    Button shopBtn, back3;
    readonly List<Button> shopBtns = new List<Button>();
    Label titleL, subL;
    Button playBtn, levelsBtn, settingsBtn, quitBtn;
    Button langBtn, soundBtn, miniBtn, back1, back2;
    readonly List<Button> levelBtns = new List<Button>();
    static readonly Color Ink = new Color(0.29f, 0.2f, 0.4f);

    ScrollContainer shopScroll;
    Control gapC;
    bool shopOnly;

    public void Init(Main game, bool shopOnlyMode = false)
    {
        g = game; shopOnly = shopOnlyMode;
        Layer = 20;
        root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(root);

        var shade = new ColorRect(); shade.Color = new Color(0.16f, 0.1f, 0.26f, 0.38f);
        shade.SetAnchorsPreset(Control.LayoutPreset.FullRect); shade.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(shade);

        var cc = new CenterContainer(); cc.SetAnchorsPreset(Control.LayoutPreset.FullRect); cc.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(cc);
        var col = new VBoxContainer(); col.AddThemeConstantOverride("separation", 10); cc.AddChild(col);

        titleL = Lbl(col, 54, new Color(1f, 0.84f, 0.42f)); titleL.AddThemeConstantOverride("outline_size", 14);
        subL = Lbl(col, 26, Colors.White); subL.AddThemeConstantOverride("outline_size", 10);
        gapC = new Control(); gapC.CustomMinimumSize = new Vector2(0f, 14f); col.AddChild(gapC);

        pageMain = Page(col); pageLevels = Page(col); pageSettings = Page(col); pageShop = Page(col);

        playBtn = Btn(pageMain, new Color(1f, 0.84f, 0.42f), 26, 300f, 60f, () => g.PlayLevel(Save.Unlocked));
        levelsBtn = Btn(pageMain, new Color(0.8f, 0.9f, 1f), 20, 300f, 48f, () => Show(1));
        shopBtn = Btn(pageMain, new Color(1f, 0.93f, 0.6f), 20, 300f, 48f, () => Show(3));
        settingsBtn = Btn(pageMain, Colors.White, 20, 300f, 48f, () => Show(2));
        quitBtn = Btn(pageMain, new Color(1f, 0.78f, 0.82f), 18, 300f, 42f, () => g.GetTree().Quit());
        quitBtn.Visible = OS.GetName() != "Android" && OS.GetName() != "iOS";

        for (int n = 1; n <= Cfg.MaxLevel; n++)
        {
            int lv = n;
            levelBtns.Add(Btn(pageLevels, Colors.White, 20, 340f, 52f, () => { if (lv <= Save.Unlocked) g.PlayLevel(lv); }));
        }
        back1 = Btn(pageLevels, new Color(0.9f, 0.9f, 0.95f), 18, 340f, 42f, () => Show(0));

        coinsL = Lbl(pageShop, 26, new Color(1f, 0.84f, 0.42f)); coinsL.AddThemeConstantOverride("outline_size", 10);
        shopScroll = new ScrollContainer();
        shopScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        shopScroll.CustomMinimumSize = new Vector2(450f, 300f);
        pageShop.AddChild(shopScroll);
        var shopList = new VBoxContainer(); shopList.AddThemeConstantOverride("separation", 8);
        shopList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        shopScroll.AddChild(shopList);
        for (int i = 0; i < Shop.Items.Length; i++)
        {
            var it = Shop.Items[i];
            shopBtns.Add(Btn(shopList, Colors.White, 17, 420f, 66f, () => { if (Shop.Buy(it)) Relabel(); }));
        }
        back3 = Btn(pageShop, new Color(0.9f, 0.9f, 0.95f), 18, 420f, 42f, () => { if (shopOnly) Close(); else Show(0); });

        langBtn = Btn(pageSettings, Colors.White, 18, 340f, 46f, () => Loc.Toggle());
        soundBtn = Btn(pageSettings, Colors.White, 18, 340f, 46f, () => { if (g.Audio != null) g.Audio.ToggleMute(); Relabel(); });
        miniBtn = Btn(pageSettings, Colors.White, 18, 340f, 46f, () => { g.ShowMinimap = !g.ShowMinimap; Save.Minimap = g.ShowMinimap; Save.Store(); Relabel(); });
        back2 = Btn(pageSettings, new Color(0.9f, 0.9f, 0.95f), 18, 340f, 42f, () => Show(0));

        Loc.Changed += Relabel;
        Show(shopOnly ? 3 : 0);
        Relabel();
        if (g.Ui != null) g.Ui.Visible = false;
    }

    public override void _ExitTree() { Loc.Changed -= Relabel; }

    public void Close()
    {
        if (g.Ui != null) g.Ui.Visible = true;
        QueueFree();
    }

    void Show(int page)
    {
        pageMain.Visible = page == 0; pageLevels.Visible = page == 1; pageSettings.Visible = page == 2; pageShop.Visible = page == 3;
        bool shop = page == 3;
        titleL.Visible = !shop; subL.Visible = !shop; gapC.Visible = !shop;
        if (shop) shopScroll.CustomMinimumSize = new Vector2(450f, Mathf.Clamp(g.GetViewport().GetVisibleRect().Size.Y - 190f, 130f, 420f));
        Relabel();
    }

    void Relabel()
    {
        if (g == null) return;
        titleL.Text = Loc.T("ov_title");
        subL.Text = Loc.T("ov_sub");
        playBtn.Text = Loc.T("menu_play") + "  ·  " + Loc.T("lvl_name" + Save.Unlocked);
        levelsBtn.Text = Loc.T("menu_levels");
        settingsBtn.Text = Loc.T("menu_settings");
        shopBtn.Text = Loc.T("menu_shop");
        coinsL.Text = Loc.T("shop_title", ("n", Save.Coins.ToString()));
        back3.Text = Loc.T("menu_back");
        for (int i = 0; i < Shop.Items.Length; i++)
        {
            var it = Shop.Items[i];
            bool owned = Shop.Has(it.Id);
            shopBtns[i].Text = Loc.T("item_" + it.Id) + "  ·  " + (owned ? Loc.T("shop_owned") : it.Price + " " + Loc.T("coins_word")) + "\n" + Loc.T("item_" + it.Id + "_d");
            shopBtns[i].Disabled = !Shop.CanBuy(it);
        }
        quitBtn.Text = Loc.T("menu_quit");
        back1.Text = Loc.T("menu_back"); back2.Text = Loc.T("menu_back");
        for (int i = 0; i < levelBtns.Count; i++)
        {
            bool open = i + 1 <= Save.Unlocked;
            levelBtns[i].Text = (i + 1) + " · " + Loc.T("lvl_name" + (i + 1)) + (open ? "" : "  (" + Loc.T("lvl_locked") + ")");
            levelBtns[i].Disabled = !open;
        }
        langBtn.Text = Loc.T("langBtn");
        bool muted = g.Audio != null && g.Audio.Muted;
        soundBtn.Text = Loc.T(muted ? "snd_off" : "snd_on").Replace(" (M)", "");
        miniBtn.Text = Loc.T(g.ShowMinimap ? "mm_on" : "mm_off");
    }

    static Label Lbl(Node parent, int size, Color c)
    {
        var l = new Label();
        l.HorizontalAlignment = HorizontalAlignment.Center;
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", c);
        l.AddThemeColorOverride("font_outline_color", new Color(0.36f, 0.29f, 0.49f));
        parent.AddChild(l);
        return l;
    }

    static VBoxContainer Page(Node parent)
    {
        var v = new VBoxContainer(); v.AddThemeConstantOverride("separation", 10); v.Alignment = BoxContainer.AlignmentMode.Center;
        parent.AddChild(v);
        return v;
    }

    static Button Btn(Node parent, Color bg, int size, float w, float h, System.Action onPress)
    {
        var b = new Button();
        b.CustomMinimumSize = new Vector2(w, h);
        b.AddThemeFontSizeOverride("font_size", size);
        b.AddThemeColorOverride("font_color", Ink);
        b.AddThemeColorOverride("font_hover_color", Ink);
        b.AddThemeColorOverride("font_pressed_color", Ink);
        b.AddThemeColorOverride("font_disabled_color", new Color(0.29f, 0.2f, 0.4f, 0.5f));
        foreach (var st in new[] { "normal", "hover", "pressed", "disabled", "focus" })
        {
            var sb = new StyleBoxFlat();
            sb.BgColor = st == "hover" ? bg.Lightened(0.15f) : (st == "pressed" ? bg.Darkened(0.15f) : (st == "disabled" ? bg.Darkened(0.2f) : bg));
            sb.SetCornerRadiusAll(14);
            if (st == "focus") { sb.BgColor = new Color(0, 0, 0, 0); sb.SetBorderWidthAll(3); sb.BorderColor = Colors.White; }
            b.AddThemeStyleboxOverride(st, sb);
        }
        b.Pressed += onPress;
        parent.AddChild(b);
        return b;
    }
}
