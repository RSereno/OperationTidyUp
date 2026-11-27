using Godot;
using System.Collections.Generic;

// Builds the living room (meshes + collision), mirroring the HTML prototype.
public static class Room
{
    // axis-aligned footprints of solid furniture (x1, z1, x2, z2), used for spawn checks
    public static readonly List<Vector4> Boxes = new List<Vector4>();
    public static readonly List<FadeMat> TableFade = new List<FadeMat>();   // materials that turn see-through when you peek under the table
    public static Label3D PortraitBanner, PortraitName;

    public static bool InFurniture(float x, float z, float margin)
    {
        foreach (var b in Boxes)
            if (x > b.X - margin && x < b.Z + margin && z > b.Y - margin && z < b.W + margin) return true;
        return false;
    }

    public static StaticBody3D Body(Node parent, Vector3 size, Vector3 pos)
    {
        var sb = new StaticBody3D();
        sb.CollisionLayer = 1; sb.CollisionMask = 0;
        var cs = new CollisionShape3D();
        var box = new BoxShape3D(); box.Size = size;
        cs.Shape = box;
        sb.AddChild(cs);
        sb.Position = pos;
        parent.AddChild(sb);
        return sb;
    }

    // solid furniture: invisible collider + footprint
    public static void Solid(Node parent, float x1, float z1, float x2, float z2)
    {
        Boxes.Add(new Vector4(x1, z1, x2, z2));
        Body(parent, new Vector3(x2 - x1, 3f, z2 - z1), new Vector3((x1 + x2) / 2f, 1.5f, (z1 + z2) / 2f));
    }

    static ImageTexture FloorTexture()
    {
        var img = Image.CreateEmpty(256, 256, false, Image.Format.Rgba8);
        var baseCol = Cfg.Hex("#efd5ab");
        var line = Cfg.Hex("#c9a578");
        img.Fill(baseCol);
        for (int i = 0; i < 4; i++)
        {
            int y = i * 64;
            for (int x = 0; x < 256; x++) for (int t = 0; t < 2; t++) img.SetPixel(x, y + t, line);
            int off = (i % 2 == 1) ? 40 : 150;
            for (int yy = y; yy < y + 64; yy++) for (int t = 0; t < 2; t++) img.SetPixel(off + t, yy, line);
        }
        return ImageTexture.CreateFromImage(img);
    }

    public static void Build(Node3D root, out List<Bin> bins)
    {
        Boxes.Clear(); TableFade.Clear();
        string D = Assets.Dir;
        float RX = Cfg.RoomX, RZ = Cfg.RoomZ, RH = Cfg.RoomH;

        // floor (planks) + ceiling
        if (!Assets.BuildFloor(root, RX, RZ))
        {
            var floor = new MeshInstance3D();
            var pm = new PlaneMesh(); pm.Size = new Vector2(RX * 2f, RZ * 2f);
            floor.Mesh = pm;
            var fm = new StandardMaterial3D();
            fm.AlbedoTexture = FloorTexture(); fm.Uv1Scale = new Vector3(RX, RZ, 1f); fm.Roughness = 1f;
            fm.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps;
            floor.MaterialOverride = fm;
            root.AddChild(floor);
        }
        Gfx.Box(root, new Vector3(RX * 2f, 0.1f, RZ * 2f), Cfg.Hex("#fff8ee"), new Vector3(0f, RH + 0.05f, 0f));

        // walls (visual + collision outside the room)
        var wallCol = Cfg.Hex("#f4d9e6");
        bool windowModel;
        bool wallModels = Assets.BuildWalls(root, RX, RZ, RH, wallCol, 1.5f, 2.6f, out windowModel);
        if (!wallModels)
        {
            Gfx.Box(root, new Vector3(RX * 2f, RH, 0.1f), wallCol, new Vector3(0f, RH / 2f, -RZ - 0.05f));
            Gfx.Box(root, new Vector3(RX * 2f, RH, 0.1f), wallCol, new Vector3(0f, RH / 2f, RZ + 0.05f));
            Gfx.Box(root, new Vector3(0.1f, RH, RZ * 2f), wallCol, new Vector3(-RX - 0.05f, RH / 2f, 0f));
            Gfx.Box(root, new Vector3(0.1f, RH, RZ * 2f), wallCol, new Vector3(RX + 0.05f, RH / 2f, 0f));
        }
        else if (windowModel) Gfx.Box(root, new Vector3(0.04f, RH, 2.6f), Cfg.Hex("#bfe6f7"), new Vector3(-RX - 0.2f, RH / 2f, 1.5f));   // sky behind the window piece
        Body(root, new Vector3(RX * 2f + 4f, 3f, 2f), new Vector3(0f, 1.5f, -RZ - 1f));
        Body(root, new Vector3(RX * 2f + 4f, 3f, 2f), new Vector3(0f, 1.5f, RZ + 1f));
        Body(root, new Vector3(2f, 3f, RZ * 2f + 4f), new Vector3(-RX - 1f, 1.5f, 0f));
        Body(root, new Vector3(2f, 3f, RZ * 2f + 4f), new Vector3(RX + 1f, 1.5f, 0f));
        var skirt = Cfg.Hex("#ecc4d8");
        Gfx.Box(root, new Vector3(RX * 2f, 0.18f, 0.06f), skirt, new Vector3(0f, 0.09f, -RZ + 0.03f));
        Gfx.Box(root, new Vector3(RX * 2f, 0.18f, 0.06f), skirt, new Vector3(0f, 0.09f, RZ - 0.03f));
        Gfx.Box(root, new Vector3(0.06f, 0.18f, RZ * 2f), skirt, new Vector3(-RX + 0.03f, 0.09f, 0f));
        Gfx.Box(root, new Vector3(0.06f, 0.18f, RZ * 2f), skirt, new Vector3(RX - 0.03f, 0.09f, 0f));

        // rugs (decoration)
        Gfx.Box(root, new Vector3(7.2f, 0.02f, 4.6f), Cfg.Hex("#d9cdf2"), new Vector3(0f, 0.011f, -1.2f));
        Gfx.Box(root, new Vector3(6.2f, 0.022f, 3.6f), Cfg.Hex("#e9e1fa"), new Vector3(0f, 0.012f, -1.2f));
        if (Assets.Place(root, D + "rugRound.glb", 5.2f, 3.6f, 0f, 3.8f, 3.8f, 0f, 0.024f, Assets.Fit.Stretch) == null)
        {
            Gfx.Cyl(root, 1.9f, 1.9f, 0.022f, Cfg.Hex("#cdebd8"), new Vector3(5.2f, 0.012f, 3.6f), 1f, false, 40);
            Gfx.Cyl(root, 1.3f, 1.3f, 0.024f, Cfg.Hex("#e6f6ec"), new Vector3(5.2f, 0.013f, 3.6f), 1f, false, 40);
        }

        // window (left wall)
        if (!windowModel)
        {
            Gfx.Box(root, new Vector3(0.12f, 1.5f, 2.6f), Colors.White, new Vector3(-RX + 0.07f, 1.7f, 1.5f));
            Gfx.Box(root, new Vector3(0.03f, 1.3f, 2.4f), Cfg.Hex("#bfe6f7"), new Vector3(-RX + 0.14f, 1.7f, 1.5f), 1f, true);
            Gfx.Box(root, new Vector3(0.05f, 1.3f, 0.06f), Colors.White, new Vector3(-RX + 0.16f, 1.7f, 1.5f));
            Gfx.Box(root, new Vector3(0.05f, 0.06f, 2.4f), Colors.White, new Vector3(-RX + 0.16f, 1.7f, 1.5f));
        }

        BuildPortrait(root);
        Gfx.Box(root, new Vector3(0.7f, 0.9f, 0.05f), Colors.White, new Vector3(5f, 1.9f, -RZ + 0.04f));
        Gfx.Box(root, new Vector3(0.58f, 0.78f, 0.05f), Cfg.Hex("#b8e0d0"), new Vector3(5f, 1.9f, -RZ + 0.07f));

        // ceiling lamp + a light
        Gfx.Cyl(root, 0.03f, 0.03f, 0.5f, Cfg.Hex("#999999"), new Vector3(0f, RH - 0.25f, 0f), 1f, false, 6);
        Gfx.Sphere(root, 0.35f, Cfg.Hex("#fffbe8"), new Vector3(0f, RH - 0.55f, 0f), 1f, true, 16);

        // TV unit + TV
        Gfx.Box(root, new Vector3(3.2f, 0.6f, 0.8f), Cfg.Hex("#c9a27a"), new Vector3(0f, 0.3f, -RZ + 0.45f));
        if (Assets.Place(root, D + "televisionModern.glb", 0f, -RZ + 0.45f, Assets.TvYaw, 2.1f, 0.6f, 0.6f, 1.5f) == null)
        {
            Gfx.Box(root, new Vector3(2.3f, 1.3f, 0.12f), Cfg.Hex("#3a3550"), new Vector3(0f, 1.35f, -RZ + 0.45f));
            Gfx.Box(root, new Vector3(2.1f, 1.1f, 0.02f), Cfg.Hex("#9fd6e8"), new Vector3(0f, 1.35f, -RZ + 0.52f), 1f, true);
        }
        Assets.Place(root, D + "plantSmall1.glb", -1.35f, -RZ + 0.45f, 0f, 0.35f, 0.35f, 0.6f, 0.5f);
        Assets.Place(root, D + "plantSmall2.glb", 1.35f, -RZ + 0.45f, 0f, 0.35f, 0.35f, 0.6f, 0.5f);
        Solid(root, -1.6f, -RZ, 1.6f, -RZ + 0.85f);

        // sofa (faces the TV)
        if (Assets.Place(root, D + "loungeSofa.glb", 0f, 0.525f, Assets.SofaYaw, 4.3f, 1.4f, 0f, 1.3f, Assets.Fit.Stretch) == null)
        {
            Gfx.Box(root, new Vector3(4f, 0.5f, 1.3f), Cfg.Hex("#8fc9de"), new Vector3(0f, 0.25f, 0.5f));
            Gfx.Box(root, new Vector3(4f, 0.85f, 0.35f), Cfg.Hex("#7ab8d0"), new Vector3(0f, 0.7f, 1.05f));
            Gfx.Box(root, new Vector3(0.4f, 0.75f, 1.3f), Cfg.Hex("#7ab8d0"), new Vector3(-2f, 0.45f, 0.5f));
            Gfx.Box(root, new Vector3(0.4f, 0.75f, 1.3f), Cfg.Hex("#7ab8d0"), new Vector3(2f, 0.45f, 0.5f));
            Gfx.Box(root, new Vector3(1.6f, 0.18f, 0.95f), Cfg.Hex("#a6d6e8"), new Vector3(-0.85f, 0.58f, 0.4f));
            Gfx.Box(root, new Vector3(1.6f, 0.18f, 0.95f), Cfg.Hex("#a6d6e8"), new Vector3(0.85f, 0.58f, 0.4f));
            Gfx.Box(root, new Vector3(0.5f, 0.5f, 0.15f), Cfg.Hex("#ffb8cf"), new Vector3(-1.3f, 0.85f, 0.72f));
        }
        Solid(root, -2.2f, -0.2f, 2.2f, 1.25f);

        // coffee table (its top turns see-through when you look down at it)
        var tbl = Assets.Place(root, D + "tableGlass.glb", 0f, -2.6f, 0f, 2.4f, 1.1f, 0f, 0.55f, Assets.Fit.Stretch);
        if (tbl != null)
        {
            float top = Assets.LastTop;
            TableFade.AddRange(Assets.MakeFadable(tbl));
            Assets.Place(root, D + "books.glb", -0.6f, -2.6f, 0f, 0.5f, 0.4f, top, 0.3f);
            Assets.Place(root, D + "plantSmall3.glb", 0.7f, -2.55f, 0f, 0.3f, 0.3f, top, 0.45f);
        }
        else
        {
            var tt = Gfx.Box(root, new Vector3(2.4f, 0.1f, 1.1f), Cfg.Hex("#e6c9a0"), new Vector3(0f, 0.48f, -2.6f));
            var tmat = Gfx.Make(Cfg.Hex("#e6c9a0"), 1f);
            tmat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            tt.MaterialOverride = tmat;
            TableFade.Add(new FadeMat { M = tmat, C = tmat.AlbedoColor });
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                Gfx.Box(root, new Vector3(0.1f, 0.43f, 0.1f), Cfg.Hex("#d2ac78"), new Vector3(sx * 1.05f, 0.215f, -2.6f + sz * 0.45f));
            Gfx.Box(root, new Vector3(0.4f, 0.18f, 0.3f), Cfg.Hex("#ffd66b"), new Vector3(0.6f, 0.62f, -2.5f));   // snack bowl
        }
        Solid(root, -1.2f, -3.15f, 1.2f, -2.05f);

        // armchair
        if (Assets.Place(root, D + "loungeChair.glb", -6f, 2.5f, Assets.ChairYaw, 1.5f, 1.5f, 0f, 1.2f) == null)
        {
            Gfx.Box(root, new Vector3(1.4f, 0.5f, 1.4f), Cfg.Hex("#ffc98a"), new Vector3(-6f, 0.25f, 2.5f));
            Gfx.Box(root, new Vector3(0.35f, 0.95f, 1.4f), Cfg.Hex("#f0b574"), new Vector3(-6.52f, 0.6f, 2.5f));
            Gfx.Box(root, new Vector3(0.95f, 0.15f, 1.1f), Cfg.Hex("#ffdcae"), new Vector3(-5.85f, 0.57f, 2.5f));
        }
        Solid(root, -6.75f, 1.75f, -5.25f, 3.25f);

        // bookshelf (right wall)
        var shelf = Assets.Place(root, D + "bookcaseClosedWide.glb", RX - 0.3f, -2f, Assets.ShelfYaw, 0.6f, 3f, 0f, 2.4f, Assets.Fit.Stretch);
        if (shelf != null)
        {
            // the shelf (despite its name) is open: 3 compartments, boards at y = 0.122 / 0.362 / 0.602 in model units,
            // interior x 0.04..0.76, depth z -0.21..0 (read from the .glb). Fill each compartment with a row of thin books.
            GD.Print("Room: shelf books go inside the compartments (build marker v5: room tiles + shovel model)");
            float[] floorsY = { 0.122f, 0.362f, 0.602f };
            string[] cols = { "#ff8fb1", "#8fd3ff", "#ffd66b", "#b79cff", "#7bd88f", "#ff9f6b", "#e8456a", "#5b4a7e" };
            var rng = new RandomNumberGenerator(); rng.Seed = 7;
            for (int r = 0; r < floorsY.Length; r++)
            {
                Vector3 a0 = Assets.ToParent(shelf, new Vector3(0.07f, floorsY[r], -0.11f));
                Vector3 a1 = Assets.ToParent(shelf, new Vector3(0.73f, floorsY[r], -0.11f));
                float zLo = Mathf.Min(a0.Z, a1.Z), zHi = Mathf.Max(a0.Z, a1.Z);
                float zc = zLo + rng.RandfRange(0f, 0.15f);
                int run = 0;
                while (zc < zHi - 0.1f)
                {
                    if (run == 0 && rng.Randf() < 0.25f) { zc += rng.RandfRange(0.15f, 0.4f); run = rng.RandiRange(3, 7); continue; }   // a gap between groups
                    float w = rng.RandfRange(0.06f, 0.13f), h = rng.RandfRange(0.28f, 0.42f), d = rng.RandfRange(0.26f, 0.34f);
                    if (zc + w > zHi) break;
                    Gfx.Box(root, new Vector3(d, h, w), Cfg.Hex(cols[rng.RandiRange(0, cols.Length - 1)]), new Vector3(a0.X, a0.Y + h / 2f, zc + w / 2f));
                    zc += w + 0.005f;
                    if (run > 0) run--;
                }
            }
        }
        else
        {
            Gfx.Box(root, new Vector3(0.5f, 2.4f, 3f), Cfg.Hex("#c9a27a"), new Vector3(RX - 0.27f, 1.2f, -2f));
            string[] bookCols = { "#ff8fb1", "#8fd3ff", "#ffd66b", "#b79cff", "#7bd88f" };
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 9; i++)
                    Gfx.Box(root, new Vector3(0.3f, 0.5f + (i % 3) * 0.06f, 0.2f), Cfg.Hex(bookCols[(i + row) % 5]), new Vector3(RX - 0.55f, 0.5f + row * 0.75f + 0.15f, -3.3f + i * 0.3f));
        }
        Solid(root, RX - 0.6f, -3.5f, RX, -0.5f);

        // plant
        if (Assets.Place(root, D + "pottedPlant.glb", -8.2f, -5.6f, 0f, 0.8f, 0.8f, 0f, 1.7f) == null)
        {
            Gfx.Cyl(root, 0.32f, 0.25f, 0.5f, Cfg.Hex("#e59a6a"), new Vector3(-8.2f, 0.25f, -5.6f));
            Gfx.Sphere(root, 0.5f, Cfg.Hex("#7bd88f"), new Vector3(-8.2f, 0.95f, -5.6f), 1f, false, 14);
            Gfx.Sphere(root, 0.32f, Cfg.Hex("#5cc477"), new Vector3(-8f, 1.35f, -5.5f), 1f, false, 12);
            Gfx.Sphere(root, 0.28f, Cfg.Hex("#5cc477"), new Vector3(-8.4f, 1.25f, -5.7f), 1f, false, 12);
        }
        Solid(root, -8.6f, -6f, -7.8f, -5.2f);

        // bins
        bins = new List<Bin>();
        for (int t = 0; t < Cfg.Types.Length; t++)
        {
            var bin = Bin.Create(t);
            root.AddChild(bin);
            var p = Cfg.Types[t].BinPos;
            Solid(root, p.X - 0.45f, p.Y - 0.45f, p.X + 0.45f, p.Y + 0.45f);
            bins.Add(bin);
        }
    }

    // framed portrait of Mr. Custódio on the back wall: a little 3D bust, or a photo if art/custodio.png exists
    static void BuildPortrait(Node3D root)
    {
        float fx = -5f, fy = 1.85f, fz = -Cfg.RoomZ;
        var frame = new Node3D();
        frame.Position = new Vector3(fx, fy, fz + 0.075f);
        root.AddChild(frame);
        Gfx.Box(frame, new Vector3(1.5f, 1.85f, 0.06f), Cfg.Hex("#8a5a2e"), new Vector3(0f, 0f, -0.035f));

        if (ResourceLoader.Exists("res://art/custodio.png"))
        {
            var tex = GD.Load<Texture2D>("res://art/custodio.png");
            var quad = new MeshInstance3D();
            var qm = new QuadMesh(); qm.Size = new Vector2(1.3f, 1.65f);
            quad.Mesh = qm;
            var mat = new StandardMaterial3D(); mat.AlbedoTexture = tex; mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            quad.MaterialOverride = mat;
            frame.AddChild(quad);
        }
        else
        {
            Gfx.Box(frame, new Vector3(1.3f, 1.65f, 0.02f), Cfg.Hex("#ffd9bd"), new Vector3(0f, 0f, 0f));
            Gfx.Sphere(frame, 0.55f, Cfg.Hex("#6aa5e8"), new Vector3(0f, -0.95f, 0.02f), 1f, false, 14).Scale = new Vector3(1.1f, 0.8f, 0.2f);   // shoulders
            Gfx.Box(frame, new Vector3(0.45f, 0.5f, 0.03f), Colors.White, new Vector3(0f, -0.58f, 0.04f));                                          // apron
            var head = Gfx.Sphere(frame, 0.3f, Cfg.Hex("#f5c6a0"), new Vector3(0f, 0.0f, 0.06f), 1f, false, 16); head.Scale = new Vector3(0.85f, 1.1f, 0.5f);
            var hair = Gfx.Sphere(frame, 0.3f, Cfg.Hex("#6b4a2f"), new Vector3(0f, 0.17f, 0.04f), 1f, false, 16); hair.Scale = new Vector3(0.9f, 0.55f, 0.5f);
            for (int s = -1; s <= 1; s += 2)
            {
                Gfx.Sphere(frame, 0.05f, Colors.White, new Vector3(s * 0.1f, 0.04f, 0.17f), 1f, false, 10);
                Gfx.Sphere(frame, 0.022f, Cfg.Hex("#222222"), new Vector3(s * 0.1f, 0.04f, 0.215f), 1f, false, 8);
                var glass = Gfx.Ring(frame, 0.085f, 0.008f, Cfg.Hex("#222222"), new Vector3(s * 0.1f, 0.04f, 0.2f), 1f, false);
                glass.RotationDegrees = new Vector3(90f, 0f, 0f);
                Gfx.Sphere(frame, 0.09f, Cfg.Hex("#ffd23f"), new Vector3(s * 0.6f, -0.62f, 0.07f), 1f, false, 10);   // rubber gloves
            }
            Gfx.Box(frame, new Vector3(0.3f, 0.05f, 0.03f), Cfg.Hex("#4b3220"), new Vector3(0f, -0.1f, 0.19f));       // moustache
        }
        Gfx.Box(frame, new Vector3(1.3f, 0.2f, 0.03f), Cfg.Hex("#5b4a7e"), new Vector3(0f, 0.72f, 0.03f));
        PortraitBanner = Gfx.Text(frame, "", 0.1f, Cfg.Hex("#ffd66b"), new Vector3(0f, 0.72f, 0.06f), false);
        PortraitName = Gfx.Text(frame, "", 0.1f, Cfg.Hex("#5b4a7e"), new Vector3(0f, -0.76f, 0.06f), false);
        PortraitBanner.OutlineSize = 0; PortraitName.OutlineSize = 0;
        PortraitBanner.PixelSize = 0.0016f; PortraitName.PixelSize = 0.0016f;
        RelabelPortrait();
    }

    public static void RelabelPortrait()
    {
        if (PortraitBanner != null && GodotObject.IsInstanceValid(PortraitBanner)) PortraitBanner.Text = Loc.T("portrait_banner");
        if (PortraitName != null && GodotObject.IsInstanceValid(PortraitName)) PortraitName.Text = Loc.T("portrait_name");
    }
}
