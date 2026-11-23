using Godot;

// Top-down map: bins, furniture, puddles, poops, "?" for hidden poops nearby, Buba and the player.
public partial class Minimap : Control
{
    Main g;

    public void Init(Main game)
    {
        g = game;
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(126f, 91f);
    }

    public override void _Process(double delta) { if (Visible) QueueRedraw(); }

    public override void _Draw()
    {
        if (g == null || g.Player == null) return;
        float ms = Size.X / (Cfg.RoomX * 2f);
        Vector2 O = Size / 2f;
        Vector2 Pt(float x, float z) { return new Vector2(O.X + x * ms, O.Y + z * ms); }

        DrawRect(new Rect2(Vector2.Zero, Size), new Color(1f, 0.97f, 0.9f, 0.92f));
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(1f, 1f, 1f, 0.9f), false, 2f);
        foreach (var b in Room.Boxes)
        {
            Vector2 a = Pt(b.X, b.Y), c = Pt(b.Z, b.W);
            DrawRect(new Rect2(a, c - a), new Color(0.78f, 0.64f, 0.48f, 0.85f));
        }
        foreach (var bin in g.Bins)
            DrawCircle(Pt(bin.Pos.X, bin.Pos.Y), Mathf.Max(3.5f, 5f * ms / 10f), Cfg.Types[bin.Type].Bin);
        foreach (var cr in g.Crusts)
            DrawRect(new Rect2(Pt(cr.Position.X, cr.Position.Z) - new Vector2(3f, 3f), new Vector2(6f, 6f)), new Color(0.45f, 0.3f, 0.15f, 0.95f));
        foreach (var q in g.Puddles)
            DrawCircle(Pt(q.Position.X, q.Position.Z), Mathf.Max(3f, q.R * ms), new Color(0.79f, 0.64f, 0.23f, 0.7f));
        var font = ThemeDB.FallbackFont;
        foreach (var p in g.Poops)
        {
            Vector2 pos = Pt(p.Position.X, p.Position.Z);
            if (p.Hidden != "")
            {
                if (p.P.DistanceTo(g.PP) < 5f) DrawString(font, pos + new Vector2(-4f, 5f), "?", HorizontalAlignment.Left, -1f, 14, new Color(0.36f, 0.29f, 0.49f));
                continue;
            }
            float r = p.Glow ? 5f + Mathf.Sin(g.Clock * 10f) * 1.5f : 3.5f * Mathf.Max(0.7f, ms / 10f);
            DrawCircle(pos, r, Cfg.Types[p.Type].Dot);
            if (p.Glow) DrawArc(pos, r, 0f, Mathf.Tau, 16, new Color(1f, 0.69f, 0f), 2f);
            if (p.Type == Cfg.Rad) DrawArc(pos, 5f + 3f * Mathf.Abs(Mathf.Sin(g.Clock * 3f)), 0f, Mathf.Tau, 20, new Color(0.35f, 0.48f, 0f), 1.5f);
        }
        DrawCircle(Pt(g.Dog.Position.X, g.Dog.Position.Z), 4.5f, new Color(0.95f, 0.71f, 0.25f));

        // player: arrow pointing the way he faces
        Vector2 pp = Pt(g.Player.Position.X, g.Player.Position.Z);
        Vector2 f = new Vector2(-Mathf.Sin(g.Yaw), -Mathf.Cos(g.Yaw));
        Vector2 s = new Vector2(-f.Y, f.X);
        DrawColoredPolygon(new[] { pp + f * 7f, pp - f * 4f + s * 4.5f, pp - f * 4f - s * 4.5f }, new Color(0.29f, 0.44f, 0.9f));
    }
}
