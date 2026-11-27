using Godot;

// Upgrades bought with coins (1 coin per mess cleaned, 2 for Buba's special drops).
public static class Shop
{
    public class Item { public string Id; public int Price; public string Requires; public int Unlock = 1; }       // Unlock: the level you must have reached

    public static readonly Item[] Items =
    {
        new Item { Id = "shovel2",   Price = 25 },   // carry 2 poops of the same colour
        new Item { Id = "bucket",    Price = 20 },   // the mop water lasts much longer
        new Item { Id = "slippers",  Price = 30 },   // no more slipping
        new Item { Id = "spray",     Price = 40, Unlock = 3 },   // +4 spray charges
        new Item { Id = "shovel3",   Price = 60, Requires = "shovel2", Unlock = 3 },   // carry 3
    };

    public static bool Has(string id) { return Save.Owned.Contains(id); }

    public static bool Available(Item it) { return Save.Unlocked >= it.Unlock; }

    // items that become available when the player reaches this level
    public static bool HasNewAt(int level)
    {
        foreach (var it in Items) if (it.Unlock == level) return true;
        return false;
    }

    public static bool CanBuy(Item it) { return Available(it) && !Has(it.Id) && Save.Coins >= it.Price && (it.Requires == null || Has(it.Requires)); }

    public static bool Buy(Item it)
    {
        if (!CanBuy(it)) return false;
        Save.Coins -= it.Price;
        Save.Owned.Add(it.Id);
        Save.Store();
        return true;
    }
}
