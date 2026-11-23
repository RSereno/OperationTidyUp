using Godot;

// A poop on the floor (also the radioactive one, hidden ones and Buba's final golden drop).
public partial class Poop : Node3D
{
    public int Type;
    public bool Glow;
    public bool Special;                // part of Buba's special drop (the golden one or the little ones from its explosion)
    public float Size = 1f;             // 1 = normal, smaller for the explosion leftovers
    public float Fuse = -1f;            // seconds until it explodes (Buba's golden drop only), -1 = no fuse
    public string Hidden = "";          // "", "rug" (lump on the rug) or "table" (under the coffee table)
    public float R = 0.26f;
    public float Seed;
    public float Pt;                    // pulse timer (radioactive only)
    public int ZapId = -1;
    public Node3D Visual;               // the scaled / rotated part
    public MeshInstance3D Lump;
    public MeshInstance3D Ring;
    public CollisionShape3D Shape;
    public MeshInstance3D[] Puffs = new MeshInstance3D[3];
    public MeshInstance3D Beam, Halo, Aura;
    public StandardMaterial3D RingMat;
    public Vector2 P { get { return new Vector2(Position.X, Position.Z); } }

    static readonly Color Stink = new Color(0.49f, 0.8f, 0.39f);

    public static Poop Create(int type, bool glow, float x, float z, string hidden, Color rugColor)
    {
        var p = new Poop();
        p.Type = type; p.Glow = glow; p.Hidden = hidden;
        p.Seed = GD.Randf() * 10f;
        p.Pt = 0.5f + GD.Randf() * 2f;
        p.Position = new Vector3(x, 0f, z);
        p.Build(rugColor);
        return p;
    }

    void Build(Color rugColor)
    {
        var body = Cfg.Types[Type].Body;
        Visual = new Node3D();
        AddChild(Visual);

        var a = Gfx.Sphere(Visual, 0.26f, body[0], new Vector3(0f, 0.13f, 0f), 1f, false, 14); a.Scale = new Vector3(1f, 0.55f, 1f);
        var b = Gfx.Sphere(Visual, 0.2f, body[1], new Vector3(0f, 0.3f, 0f), 1f, false, 14); b.Scale = new Vector3(1f, 0.6f, 1f);
        var c = Gfx.Sphere(Visual, 0.13f, body[2], new Vector3(0f, 0.45f, 0f), 1f, false, 12); c.Scale = new Vector3(1f, 0.65f, 1f);
        var tip = Gfx.Cyl(Visual, 0f, 0.05f, 0.14f, body[2], new Vector3(0.01f, 0.58f, 0f), 1f, false, 8);
        tip.RotationDegrees = new Vector3(0f, 0f, 17f);
        for (int s = -1; s <= 1; s += 2)
        {
            Gfx.Sphere(Visual, 0.055f, Colors.White, new Vector3(s * 0.075f, 0.33f, 0.16f), 1f, false, 10);
            Gfx.Sphere(Visual, 0.028f, new Color(0.07f, 0.07f, 0.07f), new Vector3(s * 0.075f, 0.33f, 0.205f), 1f, false, 8);
        }
        Gfx.Box(Visual, new Vector3(0.075f, 0.014f, 0.01f), new Color(0.23f, 0.14f, 0.06f), new Vector3(0f, 0.25f, 0.188f));
        Gfx.Disc(this, 0.34f, Colors.Black, new Vector3(0f, 0.027f, 0f), 0.16f);

        for (int i = 0; i < 3; i++)
            Puffs[i] = Gfx.Sphere(Visual, 0.045f, Stink, Vector3.Zero, 0.6f, true, 6);

        if (Glow)
        {
            Beam = Gfx.Cyl(this, 0.16f, 0.16f, 7f, new Color(1f, 0.84f, 0.42f), new Vector3(0f, 3.5f, 0f), 0.28f, true, 14);
            Halo = Gfx.Ring(this, 0.47f, 0.05f, new Color(1f, 0.84f, 0.42f), new Vector3(0f, 0.03f, 0f), 0.7f, true);
        }
        if (Type == Cfg.Rad)
        {
            Aura = Gfx.Sphere(Visual, 0.55f, new Color(0.84f, 1f, 0.18f), new Vector3(0f, 0.32f, 0f), 0.22f, true, 14);
            RingMat = Gfx.Make(new Color(0.84f, 1f, 0.18f), 0.8f, true);
            Ring = new MeshInstance3D();
            var tm = new TorusMesh(); tm.InnerRadius = 0.86f; tm.OuterRadius = 1f;
            Ring.Mesh = tm; Ring.MaterialOverride = RingMat; Ring.Position = new Vector3(0f, 0.06f, 0f); Ring.Visible = false;
            AddChild(Ring);
        }

        // solid collider so the player cannot walk through it
        var body3d = new StaticBody3D();
        body3d.CollisionLayer = 2; body3d.CollisionMask = 0;
        Shape = new CollisionShape3D();
        var cs = new CylinderShape3D(); cs.Radius = R; cs.Height = 0.6f;
        Shape.Shape = cs; Shape.Position = new Vector3(0f, 0.3f, 0f);
        body3d.AddChild(Shape);
        AddChild(body3d);

        if (Hidden == "rug")
        {
            Visual.Visible = false;
            Shape.Disabled = true;
            Lump = Gfx.Sphere(this, 0.42f, rugColor, new Vector3(0f, 0.05f, 0f), 1f, false, 14);
            Lump.Scale = new Vector3(1f, 0.32f, 0.8f);
            Lump.RotationDegrees = new Vector3(0f, GD.Randf() * 170f, 0f);
        }
        else if (Hidden == "table")
        {
            Shape.Disabled = true;   // it sits under the table, out of the player's way
        }
    }

    public void MakeSmall()
    {
        Size = 0.62f; R = 0.18f;
        var cs = Shape.Shape as CylinderShape3D;
        if (cs != null) cs.Radius = 0.18f;
    }

    // called when a hidden poop is discovered
    public void Reveal()
    {
        Hidden = "";
        Visual.Visible = true;
        if (Lump != null) { Lump.QueueFree(); Lump = null; }
        Shape.SetDeferred(CollisionShape3D.PropertyName.Disabled, false);
    }
}

// A spreading diarrhea puddle.
public partial class Puddle : Node3D
{
    public float R = 0.6f, Max = 1.7f, Seed;
    public bool Special;                // left by the exploding special poop
    Node3D visual;
    public Vector2 P { get { return new Vector2(Position.X, Position.Z); } }

    public static Puddle Create(float x, float z, float r0 = 0.6f, float max = 1.7f)
    {
        var q = new Puddle();
        q.R = r0; q.Max = max;
        q.Seed = GD.Randf() * 10f;
        q.Position = new Vector3(x, 0.03f, z);   // above the rugs (their tops are at ~0.024)
        q.visual = new Node3D();
        q.AddChild(q.visual);
        var col = new Color(0.79f, 0.64f, 0.23f);
        float[,] blobs = { { 0f, 0f, 1f }, { 0.55f, 0.2f, 0.62f }, { -0.5f, 0.35f, 0.58f }, { 0.1f, -0.6f, 0.55f } };
        for (int i = 0; i < 4; i++)
            Gfx.Disc(q.visual, blobs[i, 2], col, new Vector3(blobs[i, 0], 0f, blobs[i, 1]), 0.88f);
        var sheen = Gfx.Disc(q.visual, 0.26f, new Color(0.94f, 0.86f, 0.54f), new Vector3(-0.25f, 0.003f, -0.15f), 0.7f);
        sheen.Scale = new Vector3(1.7f, 1f, 1f);
        q.ApplyScale(1f);
        return q;
    }

    public void ApplyScale(float wobble) { visual.Scale = new Vector3(R * wobble, 1f, R * wobble); }
}

// A coloured bin. The matching one shows a marker while you carry that colour.
public partial class Bin : Node3D
{
    public int Type;
    public Vector2 Pos;
    public Label3D Label, MarkerLabel;
    public Node3D Marker;
    public MeshInstance3D MarkerRing, MarkerCone;

    public static Bin Create(int type)
    {
        var t = Cfg.Types[type];
        var b = new Bin();
        b.Type = type; b.Pos = t.BinPos;
        b.Position = new Vector3(t.BinPos.X, 0f, t.BinPos.Y);
        var col = t.Bin;
        float labelY = 1.35f;
        if (Assets.Place(b, Assets.Dir + "trashcan.glb", 0f, 0f, Assets.BinYaw(t.BinPos), 0.8f, 0.8f, 0f, 0.95f, Assets.Fit.Uniform, col.Lerp(Colors.White, 0.15f)) != null)
        {
            // model bin: a coloured bag on top keeps the bin colour readable from far away
            float top = Assets.LastTop;
            var bag = Gfx.Sphere(b, 0.27f, col * 0.8f, new Vector3(0f, top + 0.03f, 0f), 1f, false, 12); bag.Scale = new Vector3(1f, 0.55f, 1f);
            labelY = Mathf.Max(1.35f, top + 0.5f);
        }
        else
        {
            Gfx.Cyl(b, 0.36f, 0.3f, 0.85f, col, new Vector3(0f, 0.425f, 0f), 1f, false, 20);
            Gfx.Cyl(b, 0.365f, 0.315f, 0.1f, Colors.White, new Vector3(0f, 0.45f, 0f), 1f, false, 20);
            Gfx.Ring(b, 0.37f, 0.04f, col * 0.7f, new Vector3(0f, 0.86f, 0f), 1f, false);
            var bag = Gfx.Sphere(b, 0.3f, col * 0.6f, new Vector3(0f, 0.92f, 0f), 1f, false, 12); bag.Scale = new Vector3(1f, 0.55f, 1f);
        }
        b.Label = Gfx.Text(b, "", 0.3f, Colors.White, new Vector3(0f, labelY, 0f));

        b.Marker = new Node3D(); b.Marker.Visible = false; b.AddChild(b.Marker);
        b.MarkerRing = Gfx.Ring(b.Marker, 0.835f, 0.085f, Colors.White, new Vector3(0f, 0.03f, 0f), 0.9f, true);
        b.MarkerCone = Gfx.Cyl(b.Marker, 0.14f, 0f, 0.3f, col, new Vector3(0f, 1.7f, 0f), 1f, true, 12);
        b.MarkerLabel = Gfx.Text(b.Marker, "", 0.4f, new Color(1f, 0.95f, 0.6f), new Vector3(0f, 2.25f, 0f));
        b.Relabel();
        return b;
    }

    public void Relabel()
    {
        Label.Text = Type == Cfg.Rad ? Loc.T("hazard") : Loc.TypeName(Type).ToUpper();
        Label.Modulate = Colors.White;
        MarkerLabel.Text = Loc.T("dump_here");
    }
}

// Dried, encrusted mess stuck to the floor: needs the scraper (hold the action for 2 s), then it turns into a normal poop.
public partial class Crust : Node3D
{
    public int Type;                    // colour of the poop hiding in it
    public float Progress;              // 0..1 while scraping
    public float Seed;
    public float R = 0.5f;
    Node3D visual;
    public Vector2 P { get { return new Vector2(Position.X, Position.Z); } }

    public static Crust Create(float x, float z, int type)
    {
        var c = new Crust();
        c.Type = type; c.Seed = GD.Randf() * 10f;
        c.Position = new Vector3(x, 0.03f, z);
        c.visual = new Node3D(); c.AddChild(c.visual);
        Color dark = Cfg.Hex("#5e4126"), mid = Cfg.Hex("#7a5532");
        float[,] blobs = { { 0f, 0f, 0.42f }, { 0.3f, 0.15f, 0.26f }, { -0.28f, 0.2f, 0.24f }, { 0.05f, -0.34f, 0.22f }, { -0.22f, -0.26f, 0.18f } };
        for (int i = 0; i < 5; i++)
            Gfx.Disc(c.visual, blobs[i, 2], (i % 2 == 0) ? dark : mid, new Vector3(blobs[i, 0], 0.004f * i, blobs[i, 1]), 1f);
        // cracks
        for (int i = 0; i < 4; i++)
        {
            var cr = Gfx.Box(c.visual, new Vector3(0.4f, 0.012f, 0.022f), Cfg.Hex("#3f2b17"), new Vector3(Mathf.Sin(i * 1.7f) * 0.15f, 0.026f, Mathf.Cos(i * 2.3f) * 0.15f));
            cr.RotationDegrees = new Vector3(0f, i * 47f + 12f, 0f);
        }
        // dried lumps, tinted with the colour of what is inside
        Color tint = Cfg.Types[type].Body[0].Darkened(0.35f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * 1.26f + c.Seed;
            var ch = Gfx.Sphere(c.visual, 0.07f + (i % 3) * 0.02f, tint, new Vector3(Mathf.Cos(a) * 0.22f, 0.04f, Mathf.Sin(a) * 0.22f), 1f, false, 8);
            ch.Scale = new Vector3(1f, 0.55f, 1f);
        }
        return c;
    }

    // shakes and swells while it is being scraped loose
    public void Animate(float clock)
    {
        float k = Progress;
        visual.Position = new Vector3(Mathf.Sin(clock * 55f + Seed) * 0.012f * k, 0f, Mathf.Cos(clock * 47f + Seed) * 0.012f * k);
        visual.Scale = new Vector3(1f + 0.18f * k, 1f + 2.5f * k, 1f + 0.18f * k);
    }
}
