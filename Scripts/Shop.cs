using Godot;

// Upgrades bought with coins (1 coin per mess cleaned, 2 for Buba's special drops).
public static class Shop
{
    public class Item { public string Id; public int Price; public string Requires; }

    public static readonly Item[] Items =
    {
        new Item { Id = "shovel2",   Price = 25 },   // carry 2 poops of the same colour
        new Item { Id = "bucket",    Price = 20 },   // the mop water lasts much longer
        new Item { Id = "slippers",  Price = 30 },   // no more slipping
        new Item { Id = "spray",     Price = 40 },   // +4 spray charges
        new Item { Id = "shovel3",   Price = 60, Requires = "shovel2" },   // carry 3
    };

    public static bool Has(string id) { return Save.Owned.Contains(id); }

    public static bool CanBuy(Item it) { return !Has(it.Id) && Save.Coins >= it.Price && (it.Requires == null || Has(it.Requires)); }

    public static bool Buy(Item it)
    {
        if (!CanBuy(it)) return false;
        Save.Coins -= it.Price;
        Save.Owned.Add(it.Id);
        Save.Store();
        return true;
    }
}
