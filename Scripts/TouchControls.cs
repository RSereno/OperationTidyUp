using Godot;

// Mobile controls: floating joystick (left: up/down walks, left/right turns), drag on the right to look up/down,
// big action button, tool chips and a pause button. Only visible/active when Main.IsTouch is true.
public partial class TouchControls : Control
{
    Main g;
    public bool ActHeld;
    public float MoveX, MoveZ;

    int stickId = -1, lookId = -1, actId = -1;
    Vector2 stickBase, stickPos;
    const float StickR = 62f;

    public void Init(Main game)
    {
        g = game;
        MouseFilter = MouseFilterEnum.Ignore;
        Place();
    }

    void Place()
    {
        AnchorLeft = 0f; AnchorTop = 0f; AnchorRight = 1f; AnchorBottom = 1f;
        OffsetLeft = 0f; OffsetTop = 0f; OffsetRight = 0f; OffsetBottom = 0f;
    }

    public void Reset()
    {
        stickId = -1; lookId = -1; actId = -1;
        ActHeld = false; MoveX = 0f; MoveZ = 0f;
    }

    Vector2 View { get { return GetViewport().GetVisibleRect().Size; } }
    Vector2 ActCenter { get { var v = View; return new Vector2(v.X - 82f, v.Y - 96f); } }
    const float ActR = 54f;
    Rect2 MuteRect { get { var v = View; return new Rect2(v.X - 98f, 74f, 40f, 40f); } }
    Rect2 PauseRect { get { var v = View; return new Rect2(v.X - 52f, 74f, 40f, 40f); } }
    Rect2 Chip1 { get { var v = View; return new Rect2(12f, v.Y - 108f, 124f, 40f); } }
    Rect2 Chip2 { get { var v = View; return new Rect2(12f, v.Y - 60f, 124f, 40f); } }

    public override void _Input(InputEvent e)
    {
        if (g == null || !g.IsTouch) return;
        if (g.Mode != GameMode.Playing) { if (stickId != -1 || lookId != -1 || actId != -1) Reset(); return; }

        if (e is InputEventScreenTouch t)
        {
            if (t.Pressed) OnDown(t.Index, t.Position);
            else OnUp(t.Index);
        }
        else if (e is InputEventScreenDrag d)
        {
            if (d.Index == stickId)
            {
                Vector2 off = d.Position - stickBase;
                if (off.Length() > StickR) off = off.Normalized() * StickR;
                stickPos = stickBase + off;
                MoveX = off.X / StickR;
                MoveZ = -off.Y / StickR;            // finger up = walk forward
                if (Mathf.Abs(MoveX) < 0.12f) MoveX = 0f;
                if (Mathf.Abs(MoveZ) < 0.12f) MoveZ = 0f;
            }
            else if (d.Index == lookId)
            {
                g.AddPitch(-d.Relative.Y * 0.005f);   // drag up = look up (yaw stays on the joystick)
            }
        }
    }

    void OnDown(int id, Vector2 p)
    {
        if (PauseRect.HasPoint(p)) { g.Pause(); return; }
        if (MuteRect.HasPoint(p)) { if (g.Audio != null) g.Audio.ToggleMute(); return; }
        if (Chip1.HasPoint(p)) { g.SetTool(Tool.Shovel); return; }
        if (Chip2.HasPoint(p)) { g.SetTool(Tool.Mop); return; }
        if (p.DistanceTo(ActCenter) <= ActR + 14f)
        {
            if (actId == -1) { actId = id; ActHeld = true; g.PressAction(); }
            return;
        }
        float vw = View.X;
        if (p.X < vw * 0.45f)
        {
            if (stickId == -1) { stickId = id; stickBase = p; stickPos = p; MoveX = 0f; MoveZ = 0f; }
        }
        else if (lookId == -1) lookId = id;
    }

    void OnUp(int id)
    {
        if (id == stickId) { stickId = -1; MoveX = 0f; MoveZ = 0f; }
        else if (id == lookId) lookId = -1;
        else if (id == actId) { actId = -1; ActHeld = false; }
    }

    public override void _Process(double delta)
    {
        bool show = g != null && g.IsTouch && g.Mode == GameMode.Playing;
        if (Visible != show) Visible = show;
        if (show) QueueRedraw();
    }

    public override void _Draw()
    {
        if (g == null || !g.IsTouch || g.Mode != GameMode.Playing) return;
        var font = ThemeDB.FallbackFont;

        // joystick (appears where the thumb lands; a faint hint ring is shown when idle)
        if (stickId != -1)
        {
            DrawCircle(stickBase, StickR, new Color(1f, 1f, 1f, 0.22f));
            DrawArc(stickBase, StickR, 0f, Mathf.Tau, 40, new Color(1f, 1f, 1f, 0.6f), 3f);
            DrawCircle(stickPos, 26f, new Color(1f, 1f, 1f, 0.75f));
        }
        else
        {
            var v = View;
            Vector2 hint = new Vector2(92f, v.Y - 96f);
            DrawArc(hint, StickR, 0f, Mathf.Tau, 40, new Color(1f, 1f, 1f, 0.25f), 3f);
            DrawCircle(hint, 22f, new Color(1f, 1f, 1f, 0.2f));
        }

        // action button
        Vector2 ac = ActCenter;
        Color col = g.HasCarry ? new Color(1f, 0.84f, 0.42f, 0.9f) : new Color(0.48f, 0.85f, 0.56f, 0.9f);
        if (ActHeld) col = col.Darkened(0.2f);
        DrawCircle(ac, ActR, col);
        DrawArc(ac, ActR, 0f, Mathf.Tau, 40, new Color(1f, 1f, 1f, 0.9f), 4f);
        string label = g.ActionLabel();
        int fs = 16;
        Vector2 sz = font.GetStringSize(label, HorizontalAlignment.Left, -1f, fs);
        DrawString(font, ac + new Vector2(-sz.X / 2f, sz.Y / 4f), label, HorizontalAlignment.Left, -1f, fs, new Color(0.25f, 0.18f, 0.35f));

        // sound button: speaker, with a slash when muted
        Rect2 mr = MuteRect;
        Color ink = new Color(0.36f, 0.29f, 0.49f);
        DrawRect(mr, new Color(1f, 1f, 1f, 0.8f));
        Vector2 mc = mr.Position + mr.Size / 2f;
        DrawRect(new Rect2(mc + new Vector2(-10f, -4f), new Vector2(6f, 8f)), ink);
        DrawColoredPolygon(new[] { mc + new Vector2(-4f, -4f), mc + new Vector2(3f, -10f), mc + new Vector2(3f, 10f), mc + new Vector2(-4f, 4f) }, ink);
        if (g.Audio != null && g.Audio.Muted) DrawLine(mc + new Vector2(-12f, 12f), mc + new Vector2(12f, -12f), new Color(0.9f, 0.27f, 0.42f), 3f);
        else { DrawArc(mc + new Vector2(3f, 0f), 6f, -0.9f, 0.9f, 8, ink, 2f); DrawArc(mc + new Vector2(3f, 0f), 11f, -0.9f, 0.9f, 10, ink, 2f); }

        // pause button
        Rect2 pr = PauseRect;
        DrawRect(pr, new Color(1f, 1f, 1f, 0.8f));
        DrawRect(new Rect2(pr.Position + new Vector2(12f, 10f), new Vector2(6f, 20f)), new Color(0.36f, 0.29f, 0.49f));
        DrawRect(new Rect2(pr.Position + new Vector2(22f, 10f), new Vector2(6f, 20f)), new Color(0.36f, 0.29f, 0.49f));
    }
}
