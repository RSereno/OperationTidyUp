using Godot;

// Dona Ermelinda, the boss of Level 4: she walks to a fixed route of spots and inspects each with her white glove.
// Any mess near her while she walks or inspects is a strike.
public partial class Ermelinda : Node3D
{
    public static readonly Vector2[] Route =
    {
        new Vector2(-4.5f, 3.8f), new Vector2(-4.5f, -1.0f), new Vector2(-6.5f, -4.2f),
        new Vector2(2.0f, -4.4f), new Vector2(4.5f, -1.1f), new Vector2(5.0f, 2.0f),
    };
    public static readonly Vector2 Door = new Vector2(-8.0f, 4.2f);
    public const float Speed = 1.4f, InspectT = 3.5f;

    public bool Active;                      // has entered the room
    public int StopIdx;                      // index of the spot she is heading to / inspecting
    public Vector2 NextStop { get { return Route[Mathf.Min(StopIdx, Route.Length - 1)]; } }
    public Vector2 P { get { return new Vector2(Position.X, Position.Z); } }

    enum St { Away, Walk, Inspect, Wait, Done }
    St st = St.Away, waitPrev = St.Walk;
    public const float WaitT = 8f;          // how long she taps her foot before the next strike
    float t, strikeCd, waitT, clock;
    Node3D armPivot, marker;
    MeshInstance3D markerRing;
    Label3D nameL, bang;

    public static Ermelinda Create()
    {
        var e = new Ermelinda();
        e.Build();
        return e;
    }

    public void AttachTo(Node parent) { parent.AddChild(this); parent.AddChild(marker); }

    void Build()
    {
        var purple = Cfg.Hex("#8e5fb5"); var skin = Cfg.Hex("#f1c9a5"); var hair = Cfg.Hex("#d3d6dd");
        Gfx.Disc(this, 0.5f, Colors.Black, new Vector3(0f, 0.027f, 0f), 0.16f);
        Gfx.Cyl(this, 0.22f, 0.5f, 1.05f, purple, new Vector3(0f, 0.525f, 0f), 1f, false, 16);                 // dress
        Gfx.Cyl(this, 0.2f, 0.22f, 0.35f, Cfg.Hex("#f6e9fa"), new Vector3(0f, 1.22f, 0f), 1f, false, 14);       // blouse
        Gfx.Sphere(this, 0.17f, skin, new Vector3(0f, 1.5f, 0f), 1f, false, 14);                                 // head
        Gfx.Sphere(this, 0.18f, hair, new Vector3(0f, 1.56f, -0.03f), 1f, false, 12);                            // grey hair
        Gfx.Sphere(this, 0.09f, hair, new Vector3(0f, 1.74f, -0.08f), 1f, false, 10);                            // bun
        for (int s = -1; s <= 1; s += 2)
        {
            Gfx.Sphere(this, 0.03f, new Color(0.1f, 0.1f, 0.1f), new Vector3(s * 0.065f, 1.52f, 0.15f), 1f, false, 6);
            Gfx.Ring(this, 0.05f, 0.008f, Cfg.Hex("#3a3550"), new Vector3(s * 0.065f, 1.52f, 0.165f), 1f, false).RotationDegrees = new Vector3(90f, 0f, 0f);
        }
        Gfx.Box(this, new Vector3(0.1f, 0.012f, 0.01f), Cfg.Hex("#b3405a"), new Vector3(0f, 1.43f, 0.165f));     // lips
        // handbag on the left arm
        Gfx.Cyl(this, 0.03f, 0.03f, 0.55f, purple, new Vector3(-0.3f, 1.05f, 0f), 1f, false, 8);
        Gfx.Box(this, new Vector3(0.28f, 0.22f, 0.12f), Cfg.Hex("#c0392b"), new Vector3(-0.3f, 0.7f, 0.05f));
        // right arm with the white glove (pivot at the shoulder)
        armPivot = new Node3D(); armPivot.Position = new Vector3(0.28f, 1.34f, 0f); AddChild(armPivot);
        Gfx.Cyl(armPivot, 0.04f, 0.04f, 0.5f, Cfg.Hex("#f6e9fa"), new Vector3(0f, -0.25f, 0f), 1f, false, 8);
        Gfx.Sphere(armPivot, 0.085f, Colors.White, new Vector3(0f, -0.54f, 0f), 1f, false, 10);
        nameL = Gfx.Text(this, "", 0.24f, new Color(1f, 0.9f, 1f), new Vector3(0f, 2.15f, 0f));
        bang = Gfx.Text(this, "!", 0.5f, new Color(1f, 0.25f, 0.3f), new Vector3(0.45f, 2.0f, 0f));
        bang.Visible = false;
        Visible = false;

        // floor marker for the next spot
        marker = new Node3D(); marker.Visible = false;
        markerRing = Gfx.Ring(marker, 1.2f, 0.07f, Cfg.Hex("#ffd66b"), new Vector3(0f, 0.04f, 0f), 0.9f, true);
        Gfx.Cyl(marker, 0.1f, 0.1f, 0.01f, Cfg.Hex("#ffd66b"), new Vector3(0f, 0.03f, 0f), 0.6f, true, 16);
        Relabel();
    }

    public void Relabel() { if (nameL != null) nameL.Text = Loc.T("ermelinda"); }

    public void ResetState()
    {
        st = St.Away; Active = false; StopIdx = 0; t = 0f; strikeCd = 0f; waitT = 0f;
        Visible = false; marker.Visible = false; bang.Visible = false;
        Position = new Vector3(Door.X, 0f, Door.Y);
    }

    public void Tick(float dt, Main g)
    {
        clock += dt;
        if (st == St.Away)
        {
            if (g.GraceLeft > 0f) { Visible = false; marker.Visible = false; return; }
            st = St.Walk; Active = true; Visible = true;
            Position = new Vector3(Door.X, 0f, Door.Y);
            g.BossEntered();
        }
        if (st == St.Done) { UpdateAnim(dt, false); return; }

        if (strikeCd > 0f) strikeCd -= dt;
        if (st == St.Wait)
        {
            // she stands there tapping her foot: clean the mess and she carries on; if not, another strike
            waitT -= dt;
            bang.Visible = true;
            bang.Text = "! " + Mathf.CeilToInt(Mathf.Max(0f, waitT));
            marker.Visible = true;
            if (!g.MessNear(P, 1.7f))
            {
                st = waitPrev; strikeCd = 1.5f; bang.Visible = false;
                g.BossRelief();
            }
            else if (waitT <= 0f)
            {
                waitT = WaitT;
                g.BossStrike();
            }
            UpdateAnim(dt, false);
            return;
        }

        Vector2 stop = NextStop;
        marker.Visible = true;
        marker.Position = new Vector3(stop.X, 0f, stop.Y);
        markerRing.Scale = Vector3.One * (1f + 0.06f * Mathf.Sin(clock * 5f));

        bool walking = false, inspecting = false;
        float radius;
        if (st == St.Walk)
        {
            Vector2 d = stop - P; float dl = d.Length();
            if (dl < 0.15f) { st = St.Inspect; t = InspectT; }
            else
            {
                Vector2 step = d / dl * Speed * dt;
                if (step.Length() > dl) step = d;
                Position += new Vector3(step.X, 0f, step.Y);
                Rotation = new Vector3(0f, Mathf.Atan2(d.X, d.Y), 0f);
                walking = true;
            }
            radius = 0.95f;
        }
        else
        {
            inspecting = true;
            t -= dt;
            radius = 1.7f;
            if (t <= 0f)
            {
                StopIdx++;
                g.BossStopDone(StopIdx);
                if (StopIdx >= Route.Length) { st = St.Done; marker.Visible = false; UpdateAnim(dt, false); return; }
                st = St.Walk;
            }
        }

        if (strikeCd <= 0f && g.MessNear(P, radius))
        {
            waitPrev = st; st = St.Wait; waitT = WaitT; bang.Visible = true;
            g.BossStrike();
        }
        UpdateAnim(dt, walking, inspecting);
    }

    void UpdateAnim(float dt, bool walking, bool inspecting = false)
    {
        float by = walking ? Mathf.Abs(Mathf.Sin(clock * 7f)) * 0.04f : 0f;
        Position = new Vector3(Position.X, by, Position.Z);
        float ax = walking ? Mathf.Sin(clock * 7f) * 0.35f : 0.05f;
        if (inspecting) ax = -1.5f + Mathf.Sin(clock * 7f) * 0.35f;        // arm out in front, wiping
        armPivot.Rotation = new Vector3(ax, 0f, inspecting ? Mathf.Sin(clock * 5f) * 0.3f : 0f);
    }
}
