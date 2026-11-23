using Godot;

// Game constants, ported 1:1 from the HTML prototype.
public static class Cfg
{
    public const float RoomX = 9f, RoomZ = 6.5f, RoomH = 3.2f;
    public const float Eye = 1.6f, PlayerR = 0.35f, BubaR = 0.45f, Reach = 1.9f;
    public const float WalkSpeed = 3.6f, CarrySlow = 0.72f;

    public const int NormalTypes = 3;         // brown, green, pink
    public const int Rad = 3;                 // index of the radioactive type
    public const int MaxLevel = 4;

    // Per-level settings (level 1 = the living room, level 2 = the neighbours' garden party).
    public class LevelDef
    {
        public int No = 1;
        public int PerColor = 5;               // poops of each normal colour (some are hidden, some start as dried crusts)
        public int Puddles = 1;
        public int Crusts = 0;                 // dried mess that needs the scraper (each turns into one of the poops)
        public int Drops = 1;                  // how many special golden drops Buba makes
        public float PanicMin = 0.7f, PanicMax = 1f;   // Buba panics when this fraction of the regular mess is cleaned (random in range)
        public float BubaSpeed = 1f, BumpCd = 1.5f, FuseMin = 3f, FuseMax = 5f;
        public float PuddleR0 = 0.6f, PuddleMax = 1.7f, PuddleGrow = 0.02f;
        public bool Scraper, DirtyWater;
        public bool Boss;                      // level 4: Dona Ermelinda inspects the room
        public float BossGrace = 20f, BossDropMin = 7f, BossDropMax = 11f;
        public int Spray;                      // detergent spray charges (0 = no spray tool)
        public Vector2[] BinPos;
        public Vector2 TablePos = new Vector2(0f, -2.6f);
        public float TapX = -3.2f;
        public Vector4 RugCircle;              // x, z, radius, 0
        public Vector4 RugRect;                // x1, z1, x2, z2
        public Color RugCircleColor, RugRectColor;
    }

    public static LevelDef Lvl;
    public static bool AutoStart;              // set before reloading the scene so the next level starts straight away

    public static int RegTotal { get { return NormalTypes * Lvl.PerColor + 1 + Lvl.Puddles; } }   // poops + radioactive + puddles = the first 90%

    public static void SetLevel(int n)
    {
        if (n == 4)
        {
            Lvl = new LevelDef
            {
                No = 4, PerColor = 2, Puddles = 1, Crusts = 2, Drops = 0, Boss = true,
                BubaSpeed = 1.1f,
                PuddleR0 = 0.7f, PuddleMax = 1.9f, PuddleGrow = 0.025f,
                Scraper = true, DirtyWater = true, Spray = 6,
                BinPos = new[] { new Vector2(3.4f, 5.6f), new Vector2(-6.8f, 5.5f), new Vector2(7.5f, -5.3f), new Vector2(-8.3f, -2.6f) },
                RugCircle = new Vector4(5.2f, 3.6f, 1.2f, 0f), RugRect = new Vector4(-3.2f, -3.3f, 3.2f, 0.9f),
                RugCircleColor = Hex("#a8d9ba"), RugRectColor = Hex("#b9a8e0"),
            };
        }
        else if (n == 3)
        {
            Lvl = new LevelDef
            {
                No = 3, PerColor = 7, Puddles = 4, Crusts = 6, Drops = 3,
                PanicMin = 0.3f, PanicMax = 0.6f,
                BubaSpeed = 1.35f, BumpCd = 0.8f, FuseMin = 2f, FuseMax = 3.5f,
                PuddleR0 = 0.9f, PuddleMax = 2.4f, PuddleGrow = 0.035f,
                Scraper = true, DirtyWater = true, Spray = 8,
                BinPos = new[] { new Vector2(-8.2f, 5.4f), new Vector2(8.0f, 5.4f), new Vector2(8.4f, -0.4f), new Vector2(-8.3f, -1.2f) },
                TablePos = new Vector2(0f, -2.6f), TapX = 3.0f,
                RugCircle = new Vector4(-5.0f, 3.4f, 1.1f, 0f), RugRect = new Vector4(-1.6f, 0.4f, 1.6f, 1.4f),
                RugCircleColor = Hex("#f2b6c9"), RugRectColor = Hex("#b6a8e8"),
            };
        }
        else if (n == 2)
        {
            Lvl = new LevelDef
            {
                No = 2, PerColor = 6, Puddles = 3, Crusts = 4, Drops = 2,
                PanicMin = 0.4f, PanicMax = 0.75f,
                BubaSpeed = 1.2f, BumpCd = 1.0f, FuseMin = 2.5f, FuseMax = 4f,
                PuddleR0 = 0.8f, PuddleMax = 2.2f, PuddleGrow = 0.03f,
                Scraper = true, DirtyWater = true,
                BinPos = new[] { new Vector2(-8.2f, 5.4f), new Vector2(8f, 5.4f), new Vector2(-8.3f, -3.8f), new Vector2(8.4f, -1.6f) },
                TablePos = new Vector2(0f, -2.6f), TapX = -0.2f,
                RugCircle = new Vector4(4.6f, 3f, 1.3f, 0f), RugRect = new Vector4(-4.3f, 0.2f, -1.7f, 2.2f),
                RugCircleColor = Hex("#f4b8a8"), RugRectColor = Hex("#a8c8f0"),
            };
        }
        else
        {
            Lvl = new LevelDef
            {
                No = 1,
                BinPos = new[] { new Vector2(3.4f, 5.6f), new Vector2(-6.8f, 5.5f), new Vector2(7.5f, -5.3f), new Vector2(-8.3f, -2.6f) },
                RugCircle = new Vector4(5.2f, 3.6f, 1.2f, 0f), RugRect = new Vector4(-3.2f, -3.3f, 3.2f, 0.9f),
                RugCircleColor = Hex("#a8d9ba"), RugRectColor = Hex("#b9a8e0"),
            };
        }
        for (int i = 0; i < Types.Length; i++) Types[i].BinPos = Lvl.BinPos[i];
    }

    static Cfg() { SetLevel(1); }

    public static readonly bool AntiSlip = false;       // future upgrade: non-slip slippers
    public const float PulseT = 3.6f, PulseLife = 1.2f, PulseR = 2.9f;
    public static Vector2 Table { get { return Lvl.TablePos; } }

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
