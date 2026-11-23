using Godot;
using System.Collections.Generic;

// Level 3: the kitchen, final stretch before Dona Ermelinda arrives.
public static class Kitchen
{
    static Label3D clock;

    static ImageTexture TileTexture()
    {
        var img = Image.CreateEmpty(128, 128, false, Image.Format.Rgba8);
        var a = Cfg.Hex("#f4efe6"); var b = Cfg.Hex("#9ec7d6"); var grout = Cfg.Hex("#cfc6b6");
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                bool edge = x % 64 < 2 || y % 64 < 2;
                bool alt = ((x / 64) + (y / 64)) % 2 == 0;
                img.SetPixel(x, y, edge ? grout : (alt ? a : b));
            }
        return ImageTexture.CreateFromImage(img);
    }

    public static void Build(Node3D root, out List<Bin> bins)
    {
        Room.Boxes.Clear(); Room.TableFade.Clear();
        string D = Assets.Dir;
        float RX = Cfg.RoomX, RZ = Cfg.RoomZ, RH = Cfg.RoomH;

        // tiled floor + ceiling
        var floor = new MeshInstance3D();
        var pm = new PlaneMesh(); pm.Size = new Vector2(RX * 2f, RZ * 2f);
        floor.Mesh = pm;
        var fm = new StandardMaterial3D();
        fm.AlbedoTexture = TileTexture(); fm.Uv1Scale = new Vector3(RX, RZ, 1f); fm.Roughness = 0.6f;
        fm.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps;
        floor.MaterialOverride = fm;
        root.AddChild(floor);
        Gfx.Box(root, new Vector3(RX * 2f, 0.1f, RZ * 2f), Cfg.Hex("#fffdf4"), new Vector3(0f, RH + 0.05f, 0f));

        // walls + colliders
        var wallCol = Cfg.Hex("#fbe3a8");
        Gfx.Box(root, new Vector3(RX * 2f, RH, 0.1f), wallCol, new Vector3(0f, RH / 2f, -RZ - 0.05f));
        Gfx.Box(root, new Vector3(RX * 2f, RH, 0.1f), wallCol, new Vector3(0f, RH / 2f, RZ + 0.05f));
        Gfx.Box(root, new Vector3(0.1f, RH, RZ * 2f), wallCol, new Vector3(-RX - 0.05f, RH / 2f, 0f));
        Gfx.Box(root, new Vector3(0.1f, RH, RZ * 2f), wallCol, new Vector3(RX + 0.05f, RH / 2f, 0f));
        Room.Body(root, new Vector3(RX * 2f + 4f, 3f, 2f), new Vector3(0f, 1.5f, -RZ - 1f));
        Room.Body(root, new Vector3(RX * 2f + 4f, 3f, 2f), new Vector3(0f, 1.5f, RZ + 1f));
        Room.Body(root, new Vector3(2f, 3f, RZ * 2f + 4f), new Vector3(-RX - 1f, 1.5f, 0f));
        Room.Body(root, new Vector3(2f, 3f, RZ * 2f + 4f), new Vector3(RX + 1f, 1.5f, 0f));
        // tiled splashback behind the counters
        Gfx.Box(root, new Vector3(RX * 2f - 4.2f, 0.9f, 0.04f), Cfg.Hex("#cfe9f2"), new Vector3(-2.1f, 1.5f, -RZ + 0.03f));

        // counters + cabinets along the north wall
        var cab = Cfg.Hex("#e9b88a"); var top = Cfg.Hex("#6b7280");
        Gfx.Box(root, new Vector3(9.4f, 0.85f, 0.75f), cab, new Vector3(-3.2f, 0.425f, -RZ + 0.4f));
        Gfx.Box(root, new Vector3(9.5f, 0.06f, 0.8f), top, new Vector3(-3.2f, 0.88f, -RZ + 0.4f));
        for (int i = 0; i < 9; i++)
            Gfx.Box(root, new Vector3(0.04f, 0.6f, 0.02f), Cfg.Hex("#c99a6c"), new Vector3(-7.9f + i * 1.05f, 0.45f, -RZ + 0.79f));
        for (int i = 0; i < 5; i++)    // wall cabinets
            Gfx.Box(root, new Vector3(1.8f, 0.8f, 0.4f), cab, new Vector3(-7.6f + i * 1.9f, 2.35f, -RZ + 0.22f));
        // stove + hood
        Gfx.Box(root, new Vector3(1.0f, 0.04f, 0.7f), Cfg.Hex("#2b2840"), new Vector3(-1.4f, 0.92f, -RZ + 0.4f));
        for (int i = 0; i < 4; i++)
            Gfx.Cyl(root, 0.13f, 0.13f, 0.02f, Cfg.Hex("#555566"), new Vector3(-1.65f + (i % 2) * 0.5f, 0.95f, -RZ + 0.25f + (i / 2) * 0.35f), 1f, false, 14);
        Gfx.Box(root, new Vector3(1.2f, 0.5f, 0.6f), Cfg.Hex("#c9d3dc"), new Vector3(-1.4f, 2.2f, -RZ + 0.35f));
        // pots on the stove
        Gfx.Cyl(root, 0.16f, 0.16f, 0.2f, Cfg.Hex("#b0b8c4"), new Vector3(-1.65f, 1.05f, -RZ + 0.25f), 1f, false, 12);
        Room.Solid(root, -7.9f, -RZ, 1.5f, -RZ + 0.85f);

        // fridge (north-east corner)
        Gfx.Box(root, new Vector3(1.1f, 2.1f, 0.9f), Cfg.Hex("#eef4f8"), new Vector3(7.8f, 1.05f, -RZ + 0.5f));
        Gfx.Box(root, new Vector3(1.1f, 0.03f, 0.02f), Cfg.Hex("#9aa6b2"), new Vector3(7.8f, 1.4f, -RZ + 0.96f));
        Gfx.Box(root, new Vector3(0.05f, 0.5f, 0.05f), Cfg.Hex("#9aa6b2"), new Vector3(7.4f, 1.7f, -RZ + 0.98f));
        Gfx.Box(root, new Vector3(0.05f, 0.5f, 0.05f), Cfg.Hex("#9aa6b2"), new Vector3(7.4f, 0.9f, -RZ + 0.98f));
        Room.Solid(root, 7.2f, -RZ, 8.4f, -RZ + 1.0f);

        // window + wall clock (west wall)
        Gfx.Box(root, new Vector3(0.12f, 1.4f, 2.4f), Colors.White, new Vector3(-RX + 0.07f, 1.9f, 1.0f));
        Gfx.Box(root, new Vector3(0.03f, 1.2f, 2.2f), Cfg.Hex("#bfe6f7"), new Vector3(-RX + 0.14f, 1.9f, 1.0f), 1f, true);
        Gfx.Box(root, new Vector3(0.05f, 1.2f, 0.06f), Colors.White, new Vector3(-RX + 0.16f, 1.9f, 1.0f));
        Gfx.Cyl(root, 0.3f, 0.3f, 0.05f, Colors.White, new Vector3(5.5f, 2.3f, -RZ + 0.04f), 1f, false, 24).RotationDegrees = new Vector3(90f, 0f, 0f);
        clock = Gfx.Text(root, "", 0.16f, Cfg.Hex("#c0392b"), new Vector3(5.5f, 2.3f, -RZ + 0.09f), false);
        Relabel();

        // dining table (peek underneath: the cloth fades) + chairs
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sz = -1; sz <= 1; sz += 2)
                Gfx.Box(root, new Vector3(0.09f, 0.72f, 0.09f), Cfg.Hex("#b57d45"), new Vector3(sx * 1.05f, 0.36f, -2.6f + sz * 0.5f));
        var cloth = new StandardMaterial3D();
        var clothCol = Cfg.Hex("#ffd9d9");
        cloth.AlbedoColor = clothCol; cloth.Transparency = BaseMaterial3D.TransparencyEnum.Alpha; cloth.Roughness = 1f;
        var tp = new MeshInstance3D();
        var tb = new BoxMesh(); tb.Size = new Vector3(2.5f, 0.08f, 1.3f);
        tp.Mesh = tb; tp.MaterialOverride = cloth; tp.Position = new Vector3(0f, 0.76f, -2.6f);
        root.AddChild(tp);
        Room.TableFade.Add(new FadeMat { M = cloth, C = clothCol });
        Gfx.Cyl(root, 0.18f, 0.12f, 0.16f, Cfg.Hex("#ff8fb1"), new Vector3(0f, 0.88f, -2.6f), 1f, false, 14);    // fruit bowl
        Gfx.Sphere(root, 0.07f, Cfg.Hex("#ff5c3a"), new Vector3(-0.05f, 1.0f, -2.6f), 1f, false, 8);
        Gfx.Sphere(root, 0.07f, Cfg.Hex("#ffd13d"), new Vector3(0.07f, 1.0f, -2.55f), 1f, false, 8);
        Room.Solid(root, -1.3f, -3.25f, 1.3f, -1.95f);
        foreach (float cx in new[] { -0.6f, 0.6f })
        {
            if (Assets.Place(root, D + "chairCushion.glb", cx, -1.55f, 180f, 0.7f, 0.7f, 0f, 1.0f) == null)
            {
                Gfx.Box(root, new Vector3(0.55f, 0.06f, 0.55f), Cfg.Hex("#9ec7d6"), new Vector3(cx, 0.45f, -1.5f));
                Gfx.Box(root, new Vector3(0.55f, 0.55f, 0.06f), Cfg.Hex("#9ec7d6"), new Vector3(cx, 0.75f, -1.25f));
            }
            Room.Solid(root, cx - 0.3f, -1.85f, cx + 0.3f, -1.25f);
        }

        // rugs: a runner and a round mat (hidden mess spots, match the level data)
        var L = Cfg.Lvl;
        float rw = L.RugRect.Z - L.RugRect.X, rd = L.RugRect.W - L.RugRect.Y;
        Gfx.Box(root, new Vector3(rw, 0.02f, rd), L.RugRectColor, new Vector3((L.RugRect.X + L.RugRect.Z) / 2f, 0.011f, (L.RugRect.Y + L.RugRect.W) / 2f));
        Gfx.Cyl(root, L.RugCircle.Z, L.RugCircle.Z, 0.022f, L.RugCircleColor, new Vector3(L.RugCircle.X, 0.012f, L.RugCircle.Y), 1f, false, 40);

        // pantry shelf + plant (west wall)
        Gfx.Box(root, new Vector3(0.5f, 2.2f, 1.6f), Cfg.Hex("#d9a066"), new Vector3(-RX + 0.28f, 1.1f, -3.6f));
        for (int r = 0; r < 4; r++)
            for (int i = 0; i < 4; i++)
                Gfx.Box(root, new Vector3(0.2f, 0.28f, 0.22f), Cfg.Hex(new[] { "#ff8fb1", "#8fd3ff", "#ffd66b", "#7bd88f" }[(i + r) % 4]), new Vector3(-RX + 0.42f, 0.28f + r * 0.5f, -4.15f + i * 0.4f));
        Room.Solid(root, -RX, -4.4f, -RX + 0.6f, -2.8f);
        Assets.Place(root, D + "pottedPlant.glb", 8.2f, 4.2f, 0f, 0.8f, 0.8f, 0f, 1.7f);
        Room.Solid(root, 7.8f, 3.8f, 8.6f, 4.6f);

        // bins
        bins = new List<Bin>();
        for (int t = 0; t < Cfg.Types.Length; t++)
        {
            var bin = Bin.Create(t);
            root.AddChild(bin);
            var p = Cfg.Types[t].BinPos;
            Room.Solid(root, p.X - 0.45f, p.Y - 0.45f, p.X + 0.45f, p.Y + 0.45f);
            bins.Add(bin);
        }
    }

    public static void Relabel()
    {
        if (clock != null && GodotObject.IsInstanceValid(clock)) clock.Text = Loc.T("kitchen_clock");
    }
}
