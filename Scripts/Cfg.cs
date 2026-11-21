using Godot;

// Game constants, ported 1:1 from the HTML prototype.
public static class Cfg
{
    public const float RoomX = 9f, RoomZ = 6.5f, RoomH = 3.2f;
    public const float Eye = 1.6f, PlayerR = 0.35f, BubaR = 0.45f, Reach = 1.9f;
    public const float WalkSpeed = 3.6f, CarrySlow = 0.72f;

    public const int PerColor = 5;            // poops of each normal colour
    public const int NormalTypes = 3;         // brown, green, pink
    public const int Rad = 3;                 // index of the radioactive type
    public const int RegTotal = NormalTypes * PerColor + 2;   // 15 poops + 1 radioactive + 1 puddle = first 90%
    public const int Total = RegTotal + 1;                    // + Buba's final drop = 100%

    public static readonly bool AntiSlip = false;       // future upgrade: non-slip slippers
    public const float PulseT = 3.6f, PulseLife = 1.2f, PulseR = 2.9f;
    public static readonly Vector2 Table = new Vector2(0f, -2.6f);

    public class MessType
    {
        public string Name;       // English name (used for debugging only, texts come from Loc)
        public Color[] Body;
        public Color Bin;
        public Color Dot;
        public Vector2 BinPos;    // x, z
    }

    public static readonly MessType[] Types =
    {
        new MessType { Name = "brown", Body = new[] { Hex("#8b5a2e"), Hex("#9a6732"), Hex("#a9753d") }, Bin = Hex("#b0824f"), Dot = Hex("#7a4a1f"), BinPos = new Vector2(3.4f, 5.6f) },
        new MessType { Name = "green", Body = new[] { Hex("#4f9f3a"), Hex("#62b348"), Hex("#7ccb5c") }, Bin = Hex("#3fae6a"), Dot = Hex("#3fa33a"), BinPos = new Vector2(-6.8f, 5.5f) },
        new MessType { Name = "pink", Body = new[] { Hex("#dc5f93"), Hex("#e879a8"), Hex("#f293bb") }, Bin = Hex("#f07fae"), Dot = Hex("#e0609a"), BinPos = new Vector2(7.5f, -5.3f) },
        new MessType { Name = "radioactive", Body = new[] { Hex("#c8ff2e"), Hex("#dcff5a"), Hex("#eeff8e") }, Bin = Hex("#d6f21e"), Dot = Hex("#b4ff00"), BinPos = new Vector2(-8.3f, -2.6f) },
    };

    public static Color Hex(string h) { return Color.FromHtml(h); }
}
