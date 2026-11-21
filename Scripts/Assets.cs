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
                               float y0 = 0f, float maxH = 0f, Fit fit = Fit.Uniform, Color? tint = null)
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
