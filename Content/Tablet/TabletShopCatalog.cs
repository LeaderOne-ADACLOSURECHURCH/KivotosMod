using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using KivotosMod.Globals.Classes;

namespace KivotosMod.Content.Tablet;

public enum TabletShopCategory
{
    All,
    Weapons,
    Tools,
    Armor,
    Equipment,
    Consumables,
    Ammunition,
    Tiles,
    Materials,
    Other,
    Favorites,
}

public sealed class TabletShopProduct
{
    public int ItemType { get; }
    public string InternalName { get; }
    public int PriceCredits { get; }
    public int Stack { get; }
    public TabletShopCategory Category { get; }
    public int UnlockStage { get; }
    public bool Available => UnlockStage switch
    {
        1 => Main.hardMode,
        2 => NPC.downedPlantBoss,
        3 => NPC.downedMoonlord,
        _ => true
    };

    public TabletShopProduct(int itemType, string internalName, int priceCredits, TabletShopCategory category, int stack = 1, int unlockStage = 0)
    {
        ItemType = itemType;
        InternalName = internalName;
        PriceCredits = Math.Max(1, priceCredits);
        Category = category;
        Stack = Math.Max(1, stack);
        UnlockStage = unlockStage;
    }
}

public static class TabletShopCatalog
{
    private static readonly List<TabletShopProduct> _products = new();
    private static readonly Dictionary<TabletShopCategory, List<TabletShopProduct>> _productsByCategory = new();

    public static IReadOnlyList<TabletShopProduct> Products => _products;

    internal static void Rebuild(Mod mod)
    {
        _products.Clear();
        _productsByCategory.Clear();

        foreach (TabletShopCategory category in Enum.GetValues(typeof(TabletShopCategory)))
            _productsByCategory[category] = new List<TabletShopProduct>();

        // Kivotos weapons.
        foreach (ModItem item in mod.GetContent<ModItem>())
        {
            string ns = item.GetType().Namespace ?? string.Empty;
            if (!ns.StartsWith("KivotosMod.Content.Items.Weapons", StringComparison.Ordinal))
                continue;

            int price = item.Name switch
            {
                "HinaWeapon" or "ArisuWeapon" => 300000,
                "HarukaWeapon" => 220000,
                "MeguWeapon" => 250000,
                "YuzuWeapon" => 230000,
                "HoshinoWeapon" => 170000,
                "IbukiWeapon" => 200000,
                "MikaWeapon" => 150000,
                "YuukaWeapon" or "NoaWeapon" => 170000,
                "MomoiWeapon" or "MidoriWeapon" => 180000,
                "BaseballBat" or "PracticeCricketBat" or "PracticeHockeyClub" => 20000,
                "MetalBaseballBat" => 30000,
                _ => item is StudentWeaponClass ? 120000 : 100000
            };
            Add(new TabletShopProduct(item.Type, item.Name, price, TabletShopCategory.Weapons));
        }

        AddAmmo(0, (ItemID.SilverBullet, 15), (ItemID.MusketBall, 7), (ItemID.MeteorShot, 13),
            (ItemID.TungstenBullet, 18), (ItemID.EndlessMusketPouch, 60000), (ItemID.RocketI, 50),
            (ItemID.HoneyRocket, 5000), (ItemID.WetRocket, 4000), (ItemID.Grenade, 75),
            (ItemID.Beenade, 2500), (ItemID.StickyGrenade, 85), (ItemID.BouncyGrenade, 100));
        AddAmmo(1, (ItemID.IchorBullet, 37), (ItemID.CursedBullet, 33), (ItemID.CrystalBullet, 85),
            (ItemID.PartyBullet, 10), (ItemID.VenomBullet, 35), (ItemID.ExplodingBullet, 30),
            (ItemID.GoldenBullet, 40), (ItemID.RocketII, 300), (ItemID.RocketIII, 100),
            (ItemID.RocketIV, 600), (ItemID.LavaRocket, 5000), (ItemID.PartyGirlGrenade, 250));
        AddAmmo(2, (ItemID.ChlorophyteBullet, 600), (ItemID.NanoBullet, 35));
        AddAmmo(3, (ItemID.MoonlordBullet, 180), (ItemID.MiniNukeI, 600), (ItemID.MiniNukeII, 1000));

        _products.Sort(CompareProducts);
        foreach (List<TabletShopProduct> list in _productsByCategory.Values)
            list.Sort(CompareProducts);
    }

    private static void AddAmmo(int stage, params (int Type, int Price)[] items)
    {
        foreach (var (type, price) in items)
            Add(new TabletShopProduct(type, ItemID.Search.GetName(type), price, TabletShopCategory.Ammunition, unlockStage: stage));
    }

    private static int CompareProducts(TabletShopProduct a, TabletShopProduct b)
    {
        int category = a.Category.CompareTo(b.Category);
        if (category != 0)
            return category;
        return string.Compare(a.InternalName, b.InternalName, StringComparison.OrdinalIgnoreCase);
    }

    private static void Add(TabletShopProduct product)
    {
        _products.Add(product);
        _productsByCategory[TabletShopCategory.All].Add(product);
        if (product.Category != TabletShopCategory.All)
            _productsByCategory[product.Category].Add(product);
    }

    internal static void Clear()
    {
        _products.Clear();
        _productsByCategory.Clear();
    }

    public static IReadOnlyList<TabletShopProduct> GetProducts(TabletShopCategory category)
    {
        if (_productsByCategory.TryGetValue(category, out List<TabletShopProduct> products))
            return products;
        return Array.Empty<TabletShopProduct>();
    }

    public static bool TryGetByItemType(int itemType, out TabletShopProduct product)
    {
        for (int i = 0; i < _products.Count; i++)
        {
            if (_products[i].ItemType == itemType)
            {
                product = _products[i];
                return true;
            }
        }

        product = null;
        return false;
    }
}

public sealed class TabletShopCatalogSystem : ModSystem
{
    public override void PostSetupContent()
    {
        TabletShopCatalog.Rebuild(Mod);
    }

    public override void Unload()
    {
        TabletShopCatalog.Clear();
    }
}
