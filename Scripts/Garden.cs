using Godot;
using System.Collections.Generic;

// Level 2: the neighbours' birthday party in the garden / terrace.
public static class Garden
{
    static Label3D banner;

    static ImageTexture GrassTexture()
    {
        var img = Image.CreateEmpty(128, 128, false, Image.Format.Rgba8);
        var rng = new RandomNumberGenerator(); rng.Seed = 11;
        var a = Cfg.Hex("#8fd16f"); var b = Cfg.Hex("#7cc45e"); var c = Cfg.Hex("#a3dc83");
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                float r = rng.Randf();
                img.SetPixel(x, y, r < 0.55f ? a : (r < 0.8f ? b : c));
            }
        return ImageTexture.CreateFromImage(img);
    }

    static MeshInstance3D Quad(Node parent, Vector2 size, Material mat, Vector3 pos, Vector3 rotDeg)
    {
        var mi = new MeshInstance3D();
        var q = new QuadMesh(); q.Size = size;
        mi.Mesh = q; mi.MaterialOverride = mat;
        mi.Position = pos; mi.RotationDegrees = rotDeg;
        parent.AddChild(mi);
        return mi;
    }

    public static void Build(Node3D root, out List<Bin> bins)
    {
        Room.Boxes.Clear(); Room.TableFade.Clear();
        string D = Assets.Dir;
        float RX = Cfg.RoomX, RZ = Cfg.RoomZ;

        // grass
        var floor = new MeshInstance3D();
        var pm = new PlaneMesh(); pm.Size = new Vector2(RX * 2f + 40f, RZ * 2f + 40f);
        floor.Mesh = pm;
        var gm = new StandardMaterial3D();
        gm.AlbedoTexture = GrassTexture(); gm.Uv1Scale = new Vector3((RX * 2f + 40f) / 1.5f, (RZ * 2f + 40f) / 1.5f, 1f);
        gm.Roughness = 1f; gm.TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps;
        floor.MaterialOverride = gm;
        root.AddChild(floor);
        // paved patio strip along the house
        Gfx.Box(root, new Vector3(RX * 2f, 0.02f, 1.6f), Cfg.Hex("#d8d2c6"), new Vector3(0f, 0.01f, -RZ + 0.8f));

        // boundary colliders
        Room.Body(root, new Vector3(RX * 2f + 4f, 3f, 2f), new Vector3(0f, 1.5f, -RZ - 1f));
        Room.Body(root, new Vector3(RX * 2f + 4f, 3f, 2f), new Vector3(0f, 1.5f, RZ + 1f));
        Room.Body(root, new Vector3(2f, 3f, RZ * 2f + 4f), new Vector3(-RX - 1f, 1.5f, 0f));
        Room.Body(root, new Vector3(2f, 3f, RZ * 2f + 4f), new Vector3(RX + 1f, 1.5f, 0f));

        // wooden fence (west, east, south), faces inward
        var fenceMat = Gfx.Make(Cfg.Hex("#d9a066"), 1f, false, true);
        float fh = 1.7f;
        Quad(root, new Vector2(RX * 2f, fh), fenceMat, new Vector3(0f, fh / 2f, RZ - 0.02f), new Vector3(0f, 180f, 0f));
        Quad(root, new Vector2(RZ * 2f, fh), fenceMat, new Vector3(-RX + 0.02f, fh / 2f, 0f), new Vector3(0f, 90f, 0f));
        Quad(root, new Vector2(RZ * 2f, fh), fenceMat, new Vector3(RX - 0.02f, fh / 2f, 0f), new Vector3(0f, -90f, 0f));
        for (float x = -RX; x <= RX + 0.01f; x += 1.5f)
        {
            Gfx.Box(root, new Vector3(0.12f, fh + 0.15f, 0.12f), Cfg.Hex("#b57d45"), new Vector3(x, (fh + 0.15f) / 2f, RZ - 0.05f));
            Gfx.Box(root, new Vector3(0.9f, 0.06f, 0.03f), Cfg.Hex("#c48e55"), new Vector3(x + 0.75f, 0.45f, RZ - 0.04f));
            Gfx.Box(root, new Vector3(0.9f, 0.06f, 0.03f), Cfg.Hex("#c48e55"), new Vector3(x + 0.75f, 1.2f, RZ - 0.04f));
        }
        for (float z = -RZ; z <= RZ + 0.01f; z += 1.5f)
        {
            Gfx.Box(root, new Vector3(0.12f, fh + 0.15f, 0.12f), Cfg.Hex("#b57d45"), new Vector3(-RX + 0.05f, (fh + 0.15f) / 2f, z));
            Gfx.Box(root, new Vector3(0.12f, fh + 0.15f, 0.12f), Cfg.Hex("#b57d45"), new Vector3(RX - 0.05f, (fh + 0.15f) / 2f, z));
        }

        // neighbours' house wall (north) with windows, a door and the birthday banner
        float hh = 4.2f;
        Gfx.Box(root, new Vector3(RX * 2f + 4f, hh, 0.2f), Cfg.Hex("#f2c9a0"), new Vector3(0f, hh / 2f, -RZ - 0.1f));
        Gfx.Box(root, new Vector3(RX * 2f + 4f, 0.25f, 0.3f), Cfg.Hex("#c8756a"), new Vector3(0f, hh + 0.1f, -RZ - 0.1f));
        foreach (float wx in new[] { -6.2f, 5.6f })
        {
            Gfx.Box(root, new Vector3(2.0f, 1.5f, 0.06f), Colors.White, new Vector3(wx, 2.0f, -RZ + 0.03f));
            Gfx.Box(root, new Vector3(1.8f, 1.3f, 0.07f), Cfg.Hex("#bfe6f7"), new Vector3(wx, 2.0f, -RZ + 0.05f), 1f, true);
            Gfx.Box(root, new Vector3(0.05f, 1.3f, 0.08f), Colors.White, new Vector3(wx, 2.0f, -RZ + 0.07f));
            Gfx.Box(root, new Vector3(1.8f, 0.05f, 0.08f), Colors.White, new Vector3(wx, 2.0f, -RZ + 0.07f));
        }
        Gfx.Box(root, new Vector3(1.4f, 2.3f, 0.06f), Cfg.Hex("#7a4f9a"), new Vector3(-1.2f, 1.15f, -RZ + 0.03f));
        Gfx.Sphere(root, 0.07f, Cfg.Hex("#ffd66b"), new Vector3(-0.7f, 1.1f, -RZ + 0.1f), 1f, false, 8);
        Gfx.Box(root, new Vector3(5.2f, 0.9f, 0.05f), Cfg.Hex("#ff8fb1"), new Vector3(2.0f, 3.4f, -RZ + 0.04f));
        banner = Gfx.Text(root, "", 0.5f, Colors.White, new Vector3(2.0f, 3.4f, -RZ + 0.12f), false);
        Relabel();

        // bunting along the house
        string[] flagCols = { "#ff8fb1", "#8fd3ff", "#ffd66b", "#b79cff", "#7bd88f" };
        for (int i = 0; i < 18; i++)
        {
            float fx = -RX + 0.7f + i * 1.0f;
            float sag = 0.25f * Mathf.Sin((i / 17f) * Mathf.Pi);
            var f = Gfx.Cyl(root, 0.0f, 0.16f, 0.28f, Cfg.Hex(flagCols[i % 5]), new Vector3(fx, 2.7f - sag, -RZ + 1.4f), 1f, false, 3);
            f.RotationDegrees = new Vector3(180f, 0f, 0f);
        }

        // party table with cloth (turns see-through when you peek under it)
        Gfx.Box(root, new Vector3(0.1f, 0.74f, 0.1f), Cfg.Hex("#b57d45"), new Vector3(-1.8f, 0.37f, -3.05f));
        Gfx.Box(root, new Vector3(0.1f, 0.74f, 0.1f), Cfg.Hex("#b57d45"), new Vector3(1.8f, 0.37f, -3.05f));
        Gfx.Box(root, new Vector3(0.1f, 0.74f, 0.1f), Cfg.Hex("#b57d45"), new Vector3(-1.8f, 0.37f, -2.15f));
        Gfx.Box(root, new Vector3(0.1f, 0.74f, 0.1f), Cfg.Hex("#b57d45"), new Vector3(1.8f, 0.37f, -2.15f));
        var cloth = new StandardMaterial3D();
        var clothCol = Cfg.Hex("#fff3a8");
        cloth.AlbedoColor = clothCol; cloth.Transparency = BaseMaterial3D.TransparencyEnum.Alpha; cloth.Roughness = 1f;
        var top = new MeshInstance3D();
        var tb = new BoxMesh(); tb.Size = new Vector3(4.0f, 0.08f, 1.2f);
        top.Mesh = tb; top.MaterialOverride = cloth; top.Position = new Vector3(0f, 0.78f, -2.6f);
        root.AddChild(top);
        Room.TableFade.Add(new FadeMat { M = cloth, C = clothCol });
        // cake + cups + plates
        Gfx.Cyl(root, 0.32f, 0.32f, 0.2f, Cfg.Hex("#fff0f5"), new Vector3(0f, 0.92f, -2.6f), 1f, false, 20);
        Gfx.Cyl(root, 0.22f, 0.22f, 0.16f, Cfg.Hex("#ff8fb1"), new Vector3(0f, 1.1f, -2.6f), 1f, false, 20);
        Gfx.Cyl(root, 0.015f, 0.015f, 0.12f, Cfg.Hex("#ffd66b"), new Vector3(0f, 1.24f, -2.6f), 1f, false, 6);
        Gfx.Sphere(root, 0.03f, Cfg.Hex("#ff7a2e"), new Vector3(0f, 1.32f, -2.6f), 1f, true, 6);
        for (int i = 0; i < 6; i++)
            Gfx.Cyl(root, 0.06f, 0.04f, 0.1f, Cfg.Hex(flagCols[i % 5]), new Vector3(-1.5f + i * 0.6f + (i > 2 ? 0.3f : 0f), 0.87f, i % 2 == 0 ? -2.95f : -2.3f), 1f, false, 10);
        Room.Solid(root, -2f, -3.2f, 2f, -2f);
        // chairs around the table
        float[] cxs = { -1.0f, 1.0f };
        foreach (float cx in cxs)
        {
            if (Assets.Place(root, D + "chairCushion.glb", cx, -1.55f, 180f, 0.7f, 0.7f, 0f, 1.0f) == null)
            {
                Gfx.Box(root, new Vector3(0.55f, 0.06f, 0.55f), Cfg.Hex("#ffb3c7"), new Vector3(cx, 0.45f, -1.5f));
                Gfx.Box(root, new Vector3(0.55f, 0.55f, 0.06f), Cfg.Hex("#ffb3c7"), new Vector3(cx, 0.75f, -1.25f));
            }
            Room.Solid(root, cx - 0.3f, -1.85f, cx + 0.3f, -1.25f);
        }

        // BBQ (west)
        Gfx.Box(root, new Vector3(1.2f, 0.12f, 0.7f), Cfg.Hex("#3a3550"), new Vector3(-6.5f, 0.95f, -4.8f));
        Gfx.Box(root, new Vector3(1.2f, 0.4f, 0.7f), Cfg.Hex("#2b2840"), new Vector3(-6.5f, 0.74f, -4.8f));
        foreach (float lx in new[] { -0.5f, 0.5f })
            foreach (float lz in new[] { -0.25f, 0.25f })
                Gfx.Box(root, new Vector3(0.06f, 0.55f, 0.06f), Cfg.Hex("#555566"), new Vector3(-6.5f + lx, 0.27f, -4.8f + lz));
        Gfx.Box(root, new Vector3(0.9f, 0.04f, 0.5f), Cfg.Hex("#cc4a2e"), new Vector3(-6.5f, 1.02f, -4.8f));
        Gfx.Cyl(root, 0.18f, 0.18f, 0.5f, Cfg.Hex("#e8456a"), new Vector3(-5.95f, 0.25f, -4.1f), 1f, false, 12);   // gas bottle
        Room.Solid(root, -7.2f, -5.4f, -5.8f, -4.2f);

        // tree (east)
        Gfx.Cyl(root, 0.22f, 0.28f, 2.2f, Cfg.Hex("#8b5a2b"), new Vector3(6.6f, 1.1f, -4.6f), 1f, false, 10);
        Gfx.Sphere(root, 1.3f, Cfg.Hex("#5cc477"), new Vector3(6.6f, 3.0f, -4.6f), 1f, false, 14);
        Gfx.Sphere(root, 0.9f, Cfg.Hex("#6fd486"), new Vector3(7.3f, 3.5f, -4.3f), 1f, false, 12);
        Room.Solid(root, 6.2f, -5.0f, 7.0f, -4.2f);

        // bench (west)
        Gfx.Box(root, new Vector3(1.9f, 0.08f, 0.5f), Cfg.Hex("#c48e55"), new Vector3(-6.2f, 0.5f, 1.6f));
        Gfx.Box(root, new Vector3(1.9f, 0.5f, 0.06f), Cfg.Hex("#c48e55"), new Vector3(-6.2f, 0.8f, 1.38f));
        Gfx.Box(root, new Vector3(0.08f, 0.5f, 0.45f), Cfg.Hex("#8b5a2b"), new Vector3(-7.05f, 0.25f, 1.6f));
        Gfx.Box(root, new Vector3(0.08f, 0.5f, 0.45f), Cfg.Hex("#8b5a2b"), new Vector3(-5.35f, 0.25f, 1.6f));
        Room.Solid(root, -7.3f, 1.3f, -5.1f, 1.9f);

        // flower bed along the east fence
        Gfx.Box(root, new Vector3(1.0f, 0.3f, 3.0f), Cfg.Hex("#8b5a2b"), new Vector3(8.4f, 0.15f, 2.0f));
        Gfx.Box(root, new Vector3(0.9f, 0.04f, 2.9f), Cfg.Hex("#4a3320"), new Vector3(8.4f, 0.31f, 2.0f));
        for (int i = 0; i < 7; i++)
        {
            float fz = 0.7f + i * 0.43f;
            Gfx.Cyl(root, 0.015f, 0.015f, 0.35f, Cfg.Hex("#3f9e5a"), new Vector3(8.4f, 0.5f, fz), 1f, false, 5);
            Gfx.Sphere(root, 0.09f, Cfg.Hex(flagCols[i % 5]), new Vector3(8.4f, 0.7f, fz), 1f, false, 8);
        }
        Room.Solid(root, 7.9f, 0.5f, 8.9f, 3.5f);

        // picnic blankets (match the level's rug spots)
        var L = Cfg.Lvl;
        Gfx.Cyl(root, L.RugCircle.Z + 0.1f, L.RugCircle.Z + 0.1f, 0.02f, Colors.White, new Vector3(L.RugCircle.X, 0.012f, L.RugCircle.Y), 1f, false, 40);
        Gfx.Cyl(root, L.RugCircle.Z, L.RugCircle.Z, 0.022f, L.RugCircleColor, new Vector3(L.RugCircle.X, 0.014f, L.RugCircle.Y), 1f, false, 40);
        float rw = L.RugRect.Z - L.RugRect.X, rd = L.RugRect.W - L.RugRect.Y;
        float rcx = (L.RugRect.X + L.RugRect.Z) / 2f, rcz = (L.RugRect.Y + L.RugRect.W) / 2f;
        Gfx.Box(root, new Vector3(rw + 0.2f, 0.02f, rd + 0.2f), Colors.White, new Vector3(rcx, 0.012f, rcz));
        Gfx.Box(root, new Vector3(rw, 0.022f, rd), L.RugRectColor, new Vector3(rcx, 0.014f, rcz));

        // balloons tied to the fence corners
        string[] bal = { "#ff5c8a", "#5cc8ff", "#ffd13d", "#a07cff" };
        float[][] bp = { new[] { -8.5f, -2.5f }, new[] { 8.4f, -3.4f }, new[] { -8.5f, 3.6f }, new[] { 8.4f, 4.2f } };
        for (int i = 0; i < bp.Length; i++)
        {
            Gfx.Cyl(root, 0.005f, 0.005f, 1.6f, Colors.White, new Vector3(bp[i][0], 0.8f, bp[i][1]), 1f, false, 4);
            var s = Gfx.Sphere(root, 0.3f, Cfg.Hex(bal[i]), new Vector3(bp[i][0], 1.95f + (i % 2) * 0.2f, bp[i][1]), 1f, false, 14);
            s.Scale = new Vector3(1f, 1.2f, 1f);
        }

        // plants near the patio
        Assets.Place(root, D + "pottedPlant.glb", -8.2f, -5.6f, 0f, 0.8f, 0.8f, 0f, 1.7f);
        Assets.Place(root, D + "pottedPlant.glb", 8.2f, -5.6f, 0f, 0.8f, 0.8f, 0f, 1.7f);
        Room.Solid(root, -8.6f, -6f, -7.8f, -5.2f);
        Room.Solid(root, 7.8f, -6f, 8.6f, -5.2f);

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
        if (banner != null && GodotObject.IsInstanceValid(banner)) banner.Text = Loc.T("party_banner");
    }
}
