using Godot;

// Buba the golden retriever: wanders calmly, then panics at 90% and drops the last poop on a spot that was already cleaned.
public partial class Buba : CharacterBody3D
{
    public float Ang = Mathf.Pi, Dir = Mathf.Pi;
    float wanderT, pause, wag, zig, stepT, stuckT, stuckResetT;
    int stuckCount;
    public float PanicT, RedropT;
    public bool Dropped, Moving;
    bool hasWp;
    Vector2 wp;

    Node3D visual, head;
    MeshInstance3D tongue;
    Label3D bang;
    readonly Node3D[] legs = new Node3D[4];
    Node3D tail;
    bool assetDog;
    AnimationPlayer ap;
    string aIdle, aWalk, aRun, aCur;

    public static Buba Create()
    {
        var b = new Buba();
        b.CollisionLayer = 4; b.CollisionMask = 1;     // collides with walls and furniture only
        b.MotionMode = MotionModeEnum.Floating;
        b.Position = new Vector3(5.5f, 0f, -3f);
        var cs = new CollisionShape3D();
        var sh = new SphereShape3D(); sh.Radius = Cfg.BubaR;
        cs.Shape = sh; cs.Position = new Vector3(0f, 0.5f, 0f);
        b.AddChild(cs);
        b.BuildModel();
        return b;
    }

    void BuildModel()
    {
        visual = new Node3D(); AddChild(visual);
        if (!TryAssetDog()) BuildPrimitiveDog();
        Gfx.Disc(this, 0.5f, Colors.Black, new Vector3(0f, 0.03f, 0f), 0.18f).Scale = new Vector3(0.8f, 1f, 1.4f);
        bang = Gfx.Text(this, "!", 0.6f, Cfg.Hex("#e8456a"), new Vector3(0f, 1.6f, 0f));
        bang.NoDepthTest = true; bang.Visible = false;
    }

    // Kenney animal-dog.glb (art/dog/animal-dog.glb): picks idle / walk / run animations by name
    bool TryAssetDog()
    {
        var m = Assets.Place(visual, Assets.Dog, 0f, 0f, Assets.DogYaw, 1.1f, 1.1f, 0f, 1.1f);
        if (m == null) return false;
        foreach (Node n in m.FindChildren("*", "AnimationPlayer", true, false))
        {
            ap = n as AnimationPlayer;
            if (ap != null) break;
        }
        if (ap != null)
        {
            string all = "";
            foreach (string nm in ap.GetAnimationList())
            {
                all += nm + " ";
                string l = nm.ToLowerInvariant();
                if (aIdle == null && (l.Contains("idle") || l.Contains("static"))) aIdle = nm;
                else if (aWalk == null && l.Contains("walk")) aWalk = nm;
                else if (aRun == null && (l.Contains("run") || l.Contains("sprint") || l.Contains("gallop") || l.Contains("trot"))) aRun = nm;
            }
            GD.Print("Buba animations: " + all);
            if (aRun == null) aRun = aWalk;
            if (aWalk == null) aWalk = aRun;
            foreach (string nm in new[] { aIdle, aWalk, aRun })
                if (nm != null) ap.GetAnimation(nm).LoopMode = Animation.LoopModeEnum.Linear;
        }
        assetDog = true;
        return true;
    }

    void BuildPrimitiveDog()
    {
        var fur = Cfg.Hex("#f2b441"); var dark = Cfg.Hex("#d98f26");
        var body = Gfx.Sphere(visual, 0.32f, fur, new Vector3(0f, 0.6f, 0f), 1f, false, 18); body.Scale = new Vector3(1f, 0.95f, 1.7f);
        var belly = Gfx.Sphere(visual, 0.28f, Cfg.Hex("#ffe2a1"), new Vector3(0f, 0.5f, 0.05f), 1f, false, 14); belly.Scale = new Vector3(1f, 0.7f, 1.5f);
        head = new Node3D(); head.Position = new Vector3(0f, 0.86f, 0.62f); visual.AddChild(head);
        Gfx.Sphere(head, 0.23f, Cfg.Hex("#f7c45c"), Vector3.Zero, 1f, false, 18);
        var snout = Gfx.Sphere(head, 0.12f, Cfg.Hex("#ffe2a1"), new Vector3(0f, -0.05f, 0.2f), 1f, false, 12); snout.Scale = new Vector3(1f, 0.8f, 1.3f);
        Gfx.Sphere(head, 0.05f, Cfg.Hex("#222222"), new Vector3(0f, -0.01f, 0.34f), 1f, false, 8);
        for (int s = -1; s <= 1; s += 2)
        {
            Gfx.Sphere(head, 0.055f, Colors.White, new Vector3(s * 0.09f, 0.07f, 0.17f), 1f, false, 10);
            Gfx.Sphere(head, 0.028f, Cfg.Hex("#111111"), new Vector3(s * 0.09f, 0.07f, 0.215f), 1f, false, 8);
            var ear = Gfx.Sphere(head, 0.14f, Cfg.Hex("#c98a22"), new Vector3(s * 0.19f, 0.08f, -0.03f), 1f, false, 10);
            ear.Scale = new Vector3(0.45f, 1f, 0.7f); ear.RotationDegrees = new Vector3(0f, 0f, -s * 26f);
        }
        tongue = Gfx.Sphere(head, 0.05f, Cfg.Hex("#ff7aa0"), new Vector3(0f, -0.14f, 0.27f), 1f, false, 8);
        tongue.Scale = new Vector3(0.8f, 0.5f, 1.3f); tongue.Visible = false;
        var collar = Gfx.Ring(visual, 0.2f, 0.035f, Cfg.Hex("#e8456a"), new Vector3(0f, 0.78f, 0.46f), 1f, false);
        collar.RotationDegrees = new Vector3(90f, 0f, 0f);
        Gfx.Sphere(visual, 0.04f, Cfg.Hex("#ffd23f"), new Vector3(0f, 0.58f, 0.5f), 1f, false, 8);

        float[,] lp = { { -0.17f, 0.38f }, { 0.17f, 0.38f }, { -0.17f, -0.38f }, { 0.17f, -0.38f } };
        for (int i = 0; i < 4; i++)
        {
            var pivot = new Node3D(); pivot.Position = new Vector3(lp[i, 0], 0.44f, lp[i, 1]); visual.AddChild(pivot);
            Gfx.Cyl(pivot, 0.065f, 0.06f, 0.44f, dark, new Vector3(0f, -0.22f, 0f), 1f, false, 8);
            Gfx.Sphere(pivot, 0.075f, Cfg.Hex("#e8a52f"), new Vector3(0f, -0.42f, 0.02f), 1f, false, 8);
            legs[i] = pivot;
        }
        tail = new Node3D(); tail.Position = new Vector3(0f, 0.72f, -0.5f); visual.AddChild(tail);
        var tilt = new Node3D(); tilt.RotationDegrees = new Vector3(-54f, 0f, 0f); tail.AddChild(tilt);
        Gfx.Cyl(tilt, 0.04f, 0.06f, 0.5f, Cfg.Hex("#e19a2a"), new Vector3(0f, 0.25f, 0f), 1f, false, 8);
        Gfx.Sphere(tilt, 0.06f, Cfg.Hex("#e19a2a"), new Vector3(0f, 0.5f, 0f), 1f, false, 8);
    }

    public void ResetState()
    {
        Position = new Vector3(5.5f, 0f, -3f);
        Ang = Mathf.Pi; Dir = Mathf.Pi; wanderT = 0f; pause = 0f; wag = 0f; zig = 0f; stepT = 0f;
        PanicT = 0f; Dropped = false; Moving = false; hasWp = false;
    }

    public void StartPanic() { PanicT = 0f; Dropped = false; hasWp = false; }

    static float AngDiff(float a, float b) { return Mathf.PosMod(b - a + Mathf.Pi * 3f, Mathf.Tau) - Mathf.Pi; }

    public Vector2 P { get { return new Vector2(Position.X, Position.Z); } }

    // called every physics frame by Main
    public void Tick(float dt, Main g)
    {
        if (g.Mode == GameMode.Won) { wag += dt * 14f; Moving = false; Velocity = Vector3.Zero; Animate(g, dt); return; }
        if (g.Mode != GameMode.Playing) { Velocity = Vector3.Zero; Animate(g, 0f); return; }

        float vx = 0f, vz = 0f; bool moving = false;
        bool panic = g.Phase == 2;
        if (!panic)
        {
            wanderT -= dt;
            if (wanderT <= 0f)
            {
                wanderT = g.Rand(1.6f, 3.6f);
                if (GD.Randf() < 0.25f) pause = g.Rand(0.8f, 1.8f);
                Dir += g.Rand(-1.5f, 1.5f);
            }
            if (pause > 0f) pause -= dt;
            else { vx = Mathf.Sin(Dir) * 0.9f; vz = Mathf.Cos(Dir) * 0.9f; moving = true; }
            wag += dt * 6f;
        }
        else
        {
            PanicT += dt; zig += dt;
            if (Dropped && g.DropsLeft > 0)
            {
                // level 2: he has another special drop in him
                RedropT -= dt;
                if (RedropT <= 0f) { Dropped = false; PanicT = 1f; hasWp = false; g.ChooseDropSpot(); }
            }
            Vector2 tgt;
            bool goDrop = !Dropped && PanicT >= 1.7f;
            if (goDrop) tgt = g.DropSpot;
            else
            {
                if (!hasWp || P.DistanceTo(wp) < 0.7f) { wp = g.RandomWaypoint(P); hasWp = true; }
                tgt = wp;
            }
            float d = P.DistanceTo(tgt);
            float a = Mathf.Atan2(tgt.X - Position.X, tgt.Y - Position.Z);
            float lat = Mathf.Sin(zig * 11f) * 3.6f;
            if (goDrop) lat *= Mathf.Clamp(d / 1.8f, 0.1f, 1f);
            float fx = Mathf.Sin(a), fz = Mathf.Cos(a), rx = Mathf.Cos(a), rz = -Mathf.Sin(a);
            float spd = 5.6f * Cfg.Lvl.BubaSpeed;
            vx = fx * spd + rx * lat; vz = fz * spd + rz * lat;
            moving = true; wag += dt * 28f;
            if (goDrop && (d < 0.8f || PanicT > 7f))
            {
                if (d >= 0.8f) Position = new Vector3(g.DropSpot.X, 0f, g.DropSpot.Y);   // failsafe if he got snagged on furniture
                Dropped = true; hasWp = false;
                g.DropFinal();
            }
        }

        Vector3 before = Position;
        Velocity = new Vector3(vx, 0f, vz);
        MoveAndSlide();
        // in the panic phase he crashes into the player and knocks the carried poop loose
        if (panic && g.CanBump() && P.DistanceTo(g.PP) < Cfg.BubaR + Cfg.PlayerR + 0.12f) g.Bump();
        bool bumped = GetSlideCollisionCount() > 0;
        // anti-stuck: if he is trying to move but barely gets anywhere, turn away; if that keeps failing, hop to a free spot
        if (moving)
        {
            float moved = new Vector2(Position.X - before.X, Position.Z - before.Z).Length();
            float expect = new Vector2(vx, vz).Length() * dt;
            if (moved < expect * 0.3f) stuckT += dt; else stuckT = Mathf.Max(0f, stuckT - dt * 2f);
        }
        else stuckT = 0f;
        if (stuckResetT > 0f) { stuckResetT -= dt; if (stuckResetT <= 0f) stuckCount = 0; }
        bool inside = Room.InFurniture(Position.X, Position.Z, 0.0f);
        if (stuckT > 0.45f || inside)
        {
            stuckT = 0f; stuckCount++; stuckResetT = 5f;
            if (inside || stuckCount >= 3)
            {
                stuckCount = 0;
                Vector2 free = g.RandomWaypoint(P);
                Position = new Vector3(free.X, 0f, free.Y);
                hasWp = false; wanderT = 0f;
            }
            else
            {
                Dir = g.Rand(0f, Mathf.Tau); pause = 0f; wanderT = g.Rand(1f, 2f);
                hasWp = false;
            }
        }
        if (bumped)
        {
            if (!panic)
            {
                Vector3 n = GetSlideCollision(0).GetNormal();
                Dir = Mathf.Atan2(n.X, n.Z) + g.Rand(-0.8f, 0.8f);   // bounce away from whatever he hit
            }
            else if (Dropped || PanicT < 1.7f) { wp = g.RandomWaypoint(P); hasWp = true; }
        }
        if (moving)
        {
            Vector3 mv = Position - before;
            if (new Vector2(mv.X, mv.Z).Length() > 1e-5f)
            {
                float target = Mathf.Atan2(mv.X, mv.Z);
                Ang += AngDiff(Ang, target) * Mathf.Min(1f, dt * (panic ? 14f : 5f));
            }
            stepT += dt * (panic ? 26f : 8f);
        }
        Moving = moving;
        Animate(g, dt);
    }

    void Animate(Main g, float dt)
    {
        bool panic = g.Phase == 2 && g.Mode != GameMode.Won;
        if (assetDog)
        {
            visual.Rotation = new Vector3(0f, Ang, 0f);
            if (ap != null)
            {
                string want = Moving ? (panic ? aRun : aWalk) : aIdle;
                if (want != null && want != aCur) { ap.Play(want); aCur = want; }
                if (want == null && aCur != null) { ap.Stop(); aCur = null; }
                ap.SpeedScale = panic ? 1.4f : 1f;
            }
            else visual.Position = new Vector3(0f, Mathf.Abs(Mathf.Sin(stepT * 0.5f)) * (Moving ? (panic ? 0.08f : 0.025f) : 0f), 0f);
            bang.Visible = panic;
            if (panic) bang.Position = new Vector3(0f, 1.6f + Mathf.Sin(g.Clock * 20f) * 0.06f, 0f);
            return;
        }
        visual.Position = new Vector3(0f, Mathf.Abs(Mathf.Sin(stepT * 0.5f)) * (Moving ? (panic ? 0.08f : 0.025f) : 0f), 0f);
        visual.Rotation = new Vector3(0f, Ang, 0f);
        float amp = Moving ? (panic ? 0.9f : 0.5f) : 0f;
        for (int i = 0; i < 4; i++)
            legs[i].Rotation = new Vector3(Mathf.Sin(stepT + ((i == 0 || i == 3) ? 0f : Mathf.Pi)) * amp, 0f, 0f);
        tail.Rotation = new Vector3(0f, Mathf.Sin(wag) * (panic ? 0.9f : 0.55f), 0f);
        tongue.Visible = panic || Moving;
        bang.Visible = panic;
        if (panic) bang.Position = new Vector3(0f, 1.6f + Mathf.Sin(g.Clock * 20f) * 0.06f, 0f);
        head.Rotation = new Vector3(panic ? Mathf.Sin(g.Clock * 22f) * 0.12f : 0f, 0f, 0f);
    }
}
