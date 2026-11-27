using Godot;
using System.Collections.Generic;

// Optional free 3D models (Kenney furniture kit etc.). Every spot falls back to the built-in primitive shapes
// when the file is missing, so the game still runs without any art.
// Models are auto-fitted to the footprint the room already uses, so collisions and the minimap don't change.
public class FadeMat { public BaseMaterial3D M; public Color C; }

public static class Assets
{
    public const string Dir = "res://art/furniture/";
    public const string Dog = "res://art/dog/animal-dog.glb";
    public const string RoomDir = "res://art/room/";
    public const string ToolDir = "res://art/tools/";
    public const float ShovelRoll = 0f;     // spin the shovel model around its handle (degrees) if the blade dish faces the wrong way

    // Yaw in degrees so a model's front faces the right way. glTF models normally face +Z (towards you, south).
    // If something looks backwards in game, change its number here (add 180) and nothing else.
    public const float SofaYaw = 180f;     // faces the TV on the north wall
    public const float TvYaw = 0f;
    public const float ShelfYaw = -90f;    // on the east wall, front faces west
    public const float ChairYaw = 90f;     // on the west side, faces east
    public const float DogYaw = 0f;

    // bins face away from the nearest wall, into the room. Add 180 to BinFlip if the bins still face the wall.
    public const float BinFlip = 0f;
    public static float BinYaw(Vector2 pos)
    {
        float dx = Cfg.RoomX - Mathf.Abs(pos.X), dz = Cfg.RoomZ - Mathf.Abs(pos.Y);
        float yaw;
        if (dz < dx) yaw = pos.Y > 0f ? 180f : 0f;         // on the south wall -> face north, on the north wall -> face south
        else yaw = pos.X > 0f ? -90f : 90f;                // on the east wall -> face west, on the west wall -> face east
        return yaw + BinFlip;
    }

    public enum Fit { Uniform, Stretch }

    public static float LastTop;           // world-space top (y) of the model placed by the last successful Place()

    public static bool Has(string path) { return ResourceLoader.Exists(path); }

    static Node3D Spawn(string path)
    {
        if (!ResourceLoader.Exists(path)) return null;
        var ps = GD.Load<PackedScene>(path);
        if (ps == null) return null;
        return ps.Instantiate() as Node3D;
    }

    static void Grow(Node3D n, Transform3D acc, ref bool any, ref Vector3 mn, ref Vector3 mx)
    {
        Transform3D t = acc * n.Transform;
        var mi = n as MeshInstance3D;
        if (mi != null && mi.Mesh != null && mi.Visible)
        {
            Aabb a = mi.GetAabb();
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = t * a.GetEndpoint(i);
                if (!any) { mn = c; mx = c; any = true; }
                else
                {
                    mn = new Vector3(Mathf.Min(mn.X, c.X), Mathf.Min(mn.Y, c.Y), Mathf.Min(mn.Z, c.Z));
                    mx = new Vector3(Mathf.Max(mx.X, c.X), Mathf.Max(mx.Y, c.Y), Mathf.Max(mx.Z, c.Z));
                }
            }
        }
        foreach (Node ch in n.GetChildren())
        {
            var c3 = ch as Node3D;
            if (c3 != null) Grow(c3, t, ref any, ref mn, ref mx);
        }
    }

    // Places a model so its bounding box is centred on (x, z), sits on y0, and fits extX x extZ (world axes, after the yaw).
    // Uniform: keeps proportions (fits inside the box). Stretch: fills the box, height from the average, clamped by maxH.
    // Returns the holder node, or null if the file is missing/empty.
    public static Node3D Place(Node parent, string path, float x, float z, float yawDeg, float extX, float extZ,
                               float y0 = 0f, float maxH = 0f, Fit fit = Fit.Uniform, Color? tint = null, float targetH = 0f)
    {
        Node3D inst = Spawn(path);
        if (inst == null) return null;
        inst.RotationDegrees = new Vector3(0f, yawDeg, 0f);
        bool any = false; Vector3 mn = Vector3.Zero, mx = Vector3.Zero;
        Grow(inst, Transform3D.Identity, ref any, ref mn, ref mx);
        Vector3 size = mx - mn;
        if (!any || size.X < 1e-4f || size.Z < 1e-4f || size.Y < 1e-4f)
        {
            GD.PrintErr("Assets: empty model " + path);
            inst.QueueFree();
            return null;
        }
        float sx = extX / size.X, sz = extZ / size.Z, sy;
        if (fit == Fit.Uniform)
        {
            float s = Mathf.Min(sx, sz);
            if (maxH > 0f && size.Y * s > maxH) s = maxH / size.Y;
            sx = s; sy = s; sz = s;
        }
        else
        {
            sy = (sx + sz) / 2f;
            if (maxH > 0f && size.Y * sy > maxH) sy = maxH / size.Y;
            if (targetH > 0f) sy = targetH / size.Y;               // walls / floor tiles: exact height
        }
        var holder = new Node3D();
        holder.Scale = new Vector3(sx, sy, sz);                     // scale in world axes (the yaw lives on the child)
        holder.Position = new Vector3(x - (mn.X + mx.X) / 2f * sx, y0 - mn.Y * sy, z - (mn.Z + mx.Z) / 2f * sz);
        holder.AddChild(inst);
        parent.AddChild(holder);
        LastTop = y0 + size.Y * sy;
        MakeLit(holder);
        if (tint.HasValue) Tint(holder, tint.Value);
        return holder;
    }

    static void Verts(Node3D n, Transform3D acc, List<Vector3> list)
    {
        Transform3D t = acc * n.Transform;
        var mi = n as MeshInstance3D;
        if (mi != null && mi.Mesh != null && mi.Visible)
            foreach (Vector3 v in mi.Mesh.GetFaces()) list.Add(t * v);
        foreach (Node ch in n.GetChildren())
        {
            var c3 = ch as Node3D;
            if (c3 != null) Verts(c3, t, list);
        }
    }

    // A hand tool (shovel): the model is turned so its longest axis runs along Y with the wider end (the blade) at the bottom,
    // scaled to `length` and centred on `centre` inside `parent`. Returns null if the file is missing.
    public static Node3D PlaceTool(Node parent, string path, float length, Vector3 centre)
    {
        Node3D inst = Spawn(path);
        if (inst == null) return null;
        bool any = false; Vector3 mn = Vector3.Zero, mx = Vector3.Zero;
        Grow(inst, Transform3D.Identity, ref any, ref mn, ref mx);
        Vector3 sz = mx - mn;
        if (!any || sz.X < 1e-4f || sz.Y < 1e-4f || sz.Z < 1e-4f) { GD.PrintErr("Assets: empty tool model " + path); inst.QueueFree(); return null; }
        GD.Print("Assets: " + path + " size " + sz);
        Basis rot = Basis.Identity;
        if (sz.X > sz.Y && sz.X >= sz.Z) rot = new Basis(Vector3.Back, Mathf.Pi / 2f);          // X -> Y
        else if (sz.Z > sz.Y && sz.Z > sz.X) rot = new Basis(Vector3.Right, -Mathf.Pi / 2f);    // Z -> Y
        inst.Basis = rot;
        // the wider end is the blade: compare the cross-section of the top and bottom quarters
        var vs = new List<Vector3>();
        Verts(inst, Transform3D.Identity, vs);
        float lo = float.MaxValue, hi = float.MinValue;
        foreach (var v in vs) { lo = Mathf.Min(lo, v.Y); hi = Mathf.Max(hi, v.Y); }
        float q = (hi - lo) * 0.25f;
        float bxn = float.MaxValue, bxx = float.MinValue, bzn = float.MaxValue, bzx = float.MinValue;
        float txn = float.MaxValue, txx = float.MinValue, tzn = float.MaxValue, tzx = float.MinValue;
        foreach (var v in vs)
        {
            if (v.Y < lo + q) { bxn = Mathf.Min(bxn, v.X); bxx = Mathf.Max(bxx, v.X); bzn = Mathf.Min(bzn, v.Z); bzx = Mathf.Max(bzx, v.Z); }
            if (v.Y > hi - q) { txn = Mathf.Min(txn, v.X); txx = Mathf.Max(txx, v.X); tzn = Mathf.Min(tzn, v.Z); tzx = Mathf.Max(tzx, v.Z); }
        }
        float botArea = (bxx - bxn) * (bzx - bzn), topArea = (txx - txn) * (tzx - tzn);
        if (topArea > botArea) rot = new Basis(Vector3.Right, Mathf.Pi) * rot;                // flip: blade down
        rot = new Basis(Vector3.Up, Mathf.DegToRad(ShovelRoll)) * rot;
        inst.Basis = rot;
        any = false;
        Grow(inst, Transform3D.Identity, ref any, ref mn, ref mx);
        float s = length / Mathf.Max(1e-4f, mx.Y - mn.Y);
        var holder = new Node3D();
        holder.Scale = Vector3.One * s;
        holder.Position = centre - s * (mn + mx) / 2f;
        holder.AddChild(inst);
        parent.AddChild(holder);
        MakeLit(holder);
        return holder;
    }

    // Room shell from the wall / floor tiles. Returns false (and places nothing) when wall.glb is missing, so the caller keeps its boxes.
    // windowZ/windowW: a window piece on the west wall (windowPlaced tells the caller whether the model had one).
    public static bool BuildWalls(Node parent, float RX, float RZ, float RH, Color tint, float windowZ, float windowW, out bool windowPlaced)
    {
        windowPlaced = false;
        string wall = RoomDir + "wall.glb";
        if (!ResourceLoader.Exists(wall)) return false;
        string win = RoomDir + "wallWindow.glb";
        bool hasWin = ResourceLoader.Exists(win);
        const float T = 0.1f;
        // north (z = -RZ) and south: faces +Z / -Z
        WallRun(parent, wall, true, -RZ - T / 2f, -RX, RX, RH, 0f, tint);
        WallRun(parent, wall, true, RZ + T / 2f, -RX, RX, RH, 180f, tint);
        // east: faces -X
        WallRun(parent, wall, false, RX + T / 2f, -RZ, RZ, RH, -90f, tint);
        // west: faces +X, with the window piece
        float a = windowZ - windowW / 2f, b = windowZ + windowW / 2f;
        WallRun(parent, wall, false, -RX - T / 2f, -RZ, a, RH, 90f, tint);
        WallRun(parent, wall, false, -RX - T / 2f, b, RZ, RH, 90f, tint);
        string wp = hasWin ? win : wall;
        PlaceSegment(parent, wp, false, -RX - T / 2f, (a + b) / 2f, windowW, RH, 90f, tint);
        windowPlaced = hasWin;
        return true;
    }

    static void WallRun(Node parent, string path, bool alongX, float fixedC, float from, float to, float RH, float yaw, Color tint)
    {
        float len = to - from;
        if (len < 0.05f) return;
        int n = Mathf.Max(1, Mathf.RoundToInt(len / 2f));
        float seg = len / n;
        for (int i = 0; i < n; i++) PlaceSegment(parent, path, alongX, fixedC, from + seg * (i + 0.5f), seg, RH, yaw, tint);
    }

    static void PlaceSegment(Node parent, string path, bool alongX, float fixedC, float along, float segLen, float RH, float yaw, Color tint)
    {
        const float T = 0.1f;
        float x = alongX ? along : fixedC, z = alongX ? fixedC : along;
        float ex = alongX ? segLen : T, ez = alongX ? T : segLen;
        Place(parent, path, x, z, yaw, ex, ez, 0f, 0f, Fit.Stretch, tint, RH);
    }

    // floor from tiles (about 2 m each); the top surface sits at y = 0
    public static bool BuildFloor(Node parent, float RX, float RZ)
    {
        string f = RoomDir + "floorFull.glb";
        if (!ResourceLoader.Exists(f)) return false;
        int nx = Mathf.Max(1, Mathf.RoundToInt(RX)), nz = Mathf.Max(1, Mathf.RoundToInt(RZ));   // 2 m tiles
        float tx = RX * 2f / nx, tz = RZ * 2f / nz;
        const float H = 0.05f;
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
                Place(parent, f, -RX + tx * (i + 0.5f), -RZ + tz * (j + 0.5f), 0f, tx, tz, -H + 0.002f, 0f, Fit.Stretch, null, H);
        return true;
    }

    // converts a point in a placed model's own coordinates into the parent's space (used to put props inside shelves)
    public static Vector3 ToParent(Node3D holder, Vector3 modelPoint)
    {
        var inst = holder.GetChild(0) as Node3D;
        return holder.Transform * (inst.Transform * modelPoint);
    }

    // Kenney's .glb files use unlit materials (flat colour, no shading), which hides the edges of boards and undersides.
    // Switch them to normal lighting so they match the rest of the room.
    public static void MakeLit(Node n)
    {
        var mi = n as MeshInstance3D;
        if (mi != null && mi.Mesh != null)
        {
            for (int i = 0; i < mi.Mesh.GetSurfaceCount(); i++)
            {
                var src = mi.GetActiveMaterial(i) as BaseMaterial3D;
                if (src == null || src.ShadingMode != BaseMaterial3D.ShadingModeEnum.Unshaded) continue;
                var dup = (BaseMaterial3D)src.Duplicate();
                dup.ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel;
                mi.SetSurfaceOverrideMaterial(i, dup);
            }
        }
        foreach (Node ch in n.GetChildren()) MakeLit(ch);
    }

    // per-surface copy of every material, multiplied by a colour (used to colour the bins)
    public static void Tint(Node n, Color c)
    {
        var mi = n as MeshInstance3D;
        if (mi != null && mi.Mesh != null)
        {
            for (int i = 0; i < mi.Mesh.GetSurfaceCount(); i++)
            {
                var src = mi.GetActiveMaterial(i) as BaseMaterial3D;
                if (src == null) continue;
                var dup = (BaseMaterial3D)src.Duplicate();
                dup.AlbedoColor = c;
                mi.SetSurfaceOverrideMaterial(i, dup);
            }
        }
        foreach (Node ch in n.GetChildren()) Tint(ch, c);
    }

    // makes every material of a model alpha-blended and returns them so the caller can fade them (coffee table peek)
    public static List<FadeMat> MakeFadable(Node n)
    {
        var list = new List<FadeMat>();
        CollectFade(n, list);
        return list;
    }

    static void CollectFade(Node n, List<FadeMat> list)
    {
        var mi = n as MeshInstance3D;
        if (mi != null && mi.Mesh != null)
        {
            for (int i = 0; i < mi.Mesh.GetSurfaceCount(); i++)
            {
                var src = mi.GetActiveMaterial(i) as BaseMaterial3D;
                if (src == null) continue;
                var dup = (BaseMaterial3D)src.Duplicate();
                dup.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                mi.SetSurfaceOverrideMaterial(i, dup);
                list.Add(new FadeMat { M = dup, C = dup.AlbedoColor });
            }
        }
        foreach (Node ch in n.GetChildren()) CollectFade(ch, list);
    }
}
