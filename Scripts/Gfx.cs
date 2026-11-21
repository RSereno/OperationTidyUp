using Godot;
using System.Collections.Generic;

// Small helpers to build primitive meshes with flat colours.
public static class Gfx
{
    static readonly Dictionary<string, StandardMaterial3D> cache = new Dictionary<string, StandardMaterial3D>();

    public static Color Hex(string h) { return Color.FromHtml(h); }

    public static StandardMaterial3D Make(Color c, float alpha = 1f, bool unshaded = false, bool doubleSided = false)
    {
        var m = new StandardMaterial3D();
        m.AlbedoColor = new Color(c.R, c.G, c.B, alpha);
        m.Roughness = 1f;
        if (alpha < 1f) m.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        if (unshaded) m.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        if (doubleSided) m.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        return m;
    }

    // shared (cached) material
    public static StandardMaterial3D Mat(Color c, float alpha = 1f, bool unshaded = false, bool doubleSided = false)
    {
        string key = c.ToHtml() + "|" + alpha + "|" + unshaded + "|" + doubleSided;
        StandardMaterial3D m;
        if (!cache.TryGetValue(key, out m)) { m = Make(c, alpha, unshaded, doubleSided); cache[key] = m; }
        return m;
    }

    static MeshInstance3D Inst(Node parent, Mesh mesh, Material mat, Vector3 pos)
    {
        var mi = new MeshInstance3D();
        mi.Mesh = mesh;
        mi.MaterialOverride = mat;
        mi.Position = pos;
        if (parent != null) parent.AddChild(mi);
        return mi;
    }

    public static MeshInstance3D Box(Node parent, Vector3 size, Color c, Vector3 pos, float alpha = 1f, bool unshaded = false)
    {
        var mesh = new BoxMesh(); mesh.Size = size;
        return Inst(parent, mesh, Mat(c, alpha, unshaded), pos);
    }

    public static MeshInstance3D Sphere(Node parent, float r, Color c, Vector3 pos, float alpha = 1f, bool unshaded = false, int seg = 16)
    {
        var mesh = new SphereMesh();
        mesh.Radius = r; mesh.Height = r * 2f; mesh.RadialSegments = seg; mesh.Rings = System.Math.Max(6, seg / 2);
        return Inst(parent, mesh, Mat(c, alpha, unshaded), pos);
    }

    public static MeshInstance3D Cyl(Node parent, float rTop, float rBottom, float h, Color c, Vector3 pos, float alpha = 1f, bool unshaded = false, int seg = 16)
    {
        var mesh = new CylinderMesh();
        mesh.TopRadius = rTop; mesh.BottomRadius = rBottom; mesh.Height = h; mesh.RadialSegments = seg; mesh.Rings = 1;
        return Inst(parent, mesh, Mat(c, alpha, unshaded, alpha < 1f), pos);
    }

    // flat ring lying on the floor (axis = Y). radius = centre of the tube, tube = thickness radius
    public static MeshInstance3D Ring(Node parent, float radius, float tube, Color c, Vector3 pos, float alpha = 1f, bool unshaded = true)
    {
        var mesh = new TorusMesh();
        mesh.InnerRadius = radius - tube; mesh.OuterRadius = radius + tube;
        return Inst(parent, mesh, Mat(c, alpha, unshaded), pos);
    }

    // very thin round disc on the floor
    public static MeshInstance3D Disc(Node parent, float radius, Color c, Vector3 pos, float alpha = 1f, bool unshaded = false)
    {
        return Cyl(parent, radius, radius, 0.004f, c, pos, alpha, unshaded, 24);
    }

    // a copy of the mesh material that ignores depth, for the first-person tools drawn over the world
    public static void OnTop(MeshInstance3D mi, int priority = 10)
    {
        var src = mi.MaterialOverride as StandardMaterial3D;
        var m = (StandardMaterial3D)src.Duplicate();
        m.NoDepthTest = true;
        m.RenderPriority = priority;
        mi.MaterialOverride = m;
    }

    public static Label3D Text(Node parent, string text, float worldHeight, Color color, Vector3 pos, bool billboard = true)
    {
        var l = new Label3D();
        l.Text = text;
        l.FontSize = 64;
        l.PixelSize = worldHeight / 64f;
        l.Modulate = color;
        l.OutlineSize = 14;
        l.OutlineModulate = new Color(0.16f, 0.1f, 0.24f, 0.85f);
        l.Position = pos;
        if (billboard) l.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        if (parent != null) parent.AddChild(l);
        return l;
    }
}
