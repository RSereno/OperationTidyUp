using Godot;

// Sink with a tap and the bucket: hold the mop here to refill the clean water.
public partial class Tap : Node3D
{
    public static readonly Vector2 Spot = new Vector2(-3.2f, 5.85f);
    public Vector2 Pos = Spot;
    public Label3D Label, MarkerLabel;
    public Node3D Marker;
    MeshInstance3D stream, ring, cone, water;

    public static Tap Create()
    {
        var t = new Tap();
        t.Position = new Vector3(Spot.X, 0f, Spot.Y);
        t.Build();
        return t;
    }

    void Build()
    {
        var steel = Cfg.Hex("#c9d3dc");
        // cabinet + counter + basin (the sink faces the room, so its back is against the south wall)
        Gfx.Box(this, new Vector3(1.2f, 0.85f, 0.7f), Cfg.Hex("#e8dcc8"), new Vector3(0f, 0.425f, 0.1f));
        Gfx.Box(this, new Vector3(1.26f, 0.06f, 0.76f), Cfg.Hex("#8892a0"), new Vector3(0f, 0.88f, 0.1f));
        Gfx.Box(this, new Vector3(0.7f, 0.04f, 0.4f), Cfg.Hex("#5d6b78"), new Vector3(0f, 0.915f, 0.12f));
        // faucet
        Gfx.Cyl(this, 0.035f, 0.035f, 0.35f, steel, new Vector3(0f, 1.08f, 0.38f), 1f, false, 10);
        var spout = Gfx.Cyl(this, 0.03f, 0.03f, 0.22f, steel, new Vector3(0f, 1.26f, 0.28f), 1f, false, 10);
        spout.RotationDegrees = new Vector3(90f, 0f, 0f);
        Gfx.Sphere(this, 0.05f, Cfg.Hex("#e8456a"), new Vector3(0.14f, 0.98f, 0.38f), 1f, false, 8);
        Gfx.Sphere(this, 0.05f, Cfg.Hex("#4a90e2"), new Vector3(-0.14f, 0.98f, 0.38f), 1f, false, 8);
        // water stream (only while refilling)
        stream = Gfx.Cyl(this, 0.025f, 0.025f, 0.34f, new Color(0.5f, 0.78f, 1f), new Vector3(0f, 1.08f, 0.18f), 0.75f, true, 8);
        stream.Visible = false;
        // bucket standing on the floor in front of the sink
        Gfx.Cyl(this, 0.26f, 0.2f, 0.4f, Cfg.Hex("#4a90e2"), new Vector3(0.95f, 0.2f, 0.1f), 1f, false, 18);
        Gfx.Ring(this, 0.26f, 0.025f, Cfg.Hex("#2f6fb8"), new Vector3(0.95f, 0.4f, 0.1f), 1f, false);
        water = Gfx.Cyl(this, 0.235f, 0.235f, 0.02f, Cfg.Hex("#9bd4ff"), new Vector3(0.95f, 0.36f, 0.1f), 1f, false, 18);

        Label = Gfx.Text(this, "", 0.3f, Colors.White, new Vector3(0f, 1.7f, 0.1f));

        Marker = new Node3D(); Marker.Visible = false; AddChild(Marker);
        ring = Gfx.Ring(Marker, 0.95f, 0.08f, Colors.White, new Vector3(0f, 0.03f, -0.9f), 0.9f, true);
        cone = Gfx.Cyl(Marker, 0.14f, 0f, 0.3f, Cfg.Hex("#4a90e2"), new Vector3(0f, 2.1f, 0.1f), 1f, true, 12);
        MarkerLabel = Gfx.Text(Marker, "", 0.4f, new Color(1f, 0.95f, 0.6f), new Vector3(0f, 2.55f, 0.1f));
        Relabel();
    }

    public void Relabel()
    {
        Label.Text = Loc.T("tap_label");
        MarkerLabel.Text = Loc.T("tap_here");
    }

    // water: 0..1 level in the bucket; hint: pulse a marker so the player finds the tap; filling: show the stream
    public void Update(float clock, float waterLevel, bool hint, bool filling)
    {
        Marker.Visible = hint;
        stream.Visible = filling;
        water.Position = new Vector3(0.95f, 0.14f + 0.22f * waterLevel, 0.1f);
        water.Visible = waterLevel > 0.02f;
        if (!hint) return;
        float w = Mathf.Sin(clock * 5f) * 0.07f;
        MarkerLabel.Position = new Vector3(0f, 2.55f + w, 0.1f);
        cone.Position = new Vector3(0f, 2.1f + w, 0.1f);
        ring.Scale = Vector3.One * (1f + 0.08f * Mathf.Sin(clock * 6f));
    }
}
