using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using KivotosMod.Content.Tablet.Currency;

namespace KivotosMod.Content.Tablet;

public static class TabletWallet
{
    public const long CreditsPerCopper = 1;
    public const long CreditsPerSilver = 100;
    public const long CreditsPerGold = 10000;
    public const long CreditsPerPyroxene = 10000;
    public const long CreditsPerPlatinum = 1000000;

    internal static Item[][] Storages(Player player) => new[] { player.inventory, player.bank.item };

    public static long GetTotalCredits(Player player)
    {
        long total = 0;
        foreach (Item[] storage in Storages(player))
        foreach (Item item in storage)
            total += GetUnitValue(item.type) * item.stack;
        return total;
    }

    public static long GetPyroxeneEquivalent(Player player) => GetTotalCredits(player) / CreditsPerPyroxene;

    public static long GetUnitValue(int itemType)
    {
        if (itemType == ModContent.ItemType<UnionCredit>()) return 1;
        if (itemType == ItemID.CopperCoin) return CreditsPerCopper;
        if (itemType == ItemID.SilverCoin) return CreditsPerSilver;
        if (itemType == ItemID.GoldCoin) return CreditsPerGold;
        if (itemType == ModContent.ItemType<Pyroxene>()) return CreditsPerPyroxene;
        if (itemType == ItemID.PlatinumCoin) return CreditsPerPlatinum;
        return 0;
    }

    public static bool TrySpend(Player player, long amount)
    {
        if (amount <= 0) return true;
        if (GetTotalCredits(player) < amount) return false;

        long paid = 0;
        int[] denominationTypes =
        {
            ModContent.ItemType<UnionCredit>(),
            ModContent.ItemType<Pyroxene>(),
            ItemID.CopperCoin,
            ItemID.SilverCoin,
            ItemID.GoldCoin,
            ItemID.PlatinumCoin,
        };

        foreach (int type in denominationTypes)
        {
            long value = GetUnitValue(type);
            if (value <= 0) continue;

            bool convertRemainder = type == ModContent.ItemType<UnionCredit>() || type == ModContent.ItemType<Pyroxene>();
            foreach (Item[] storage in Storages(player))
            for (int i = 0; i < storage.Length && (paid < amount || convertRemainder); i++)
            {
                Item item = storage[i];
                if (item.type != type || item.stack <= 0) continue;

                long remaining = amount - paid;
                long needed = convertRemainder ? item.stack : (remaining + value - 1) / value;
                int take = (int)Math.Min(item.stack, needed);
                if (take <= 0) continue;

                item.stack -= take;
                paid += take * value;
                if (item.stack <= 0)
                    item.TurnToAir();
            }
        }

        long change = paid - amount;
        if (change > 0)
            GiveUnionCreditChange(player, change);

        if (Main.netMode == NetmodeID.Server)
        {
            for (int i = 0; i < player.inventory.Length; i++)
                NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, i);
        }

        return true;
    }

    private static void GiveUnionCreditChange(Player player, long change)
    {
        int[] types = { ItemID.PlatinumCoin, ItemID.GoldCoin, ItemID.SilverCoin, ItemID.CopperCoin };
        foreach (int type in types)
        {
            long value = GetUnitValue(type);
            long count = change / value;
            change %= value;
            while (count > 0)
            {
                int stack = (int)Math.Min(count, 9999);
                player.QuickSpawnItem(player.GetSource_Misc("KivotosTabletChange"), type, stack);
                count -= stack;
            }
        }
    }
}
