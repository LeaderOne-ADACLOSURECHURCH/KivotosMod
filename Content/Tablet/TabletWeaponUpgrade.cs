using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using KivotosMod.Globals.Classes;
using KivotosMod.Globals.Configs;

namespace KivotosMod.Content.Tablet;

public sealed class TabletWeaponLevel : GlobalItem
{
    public override bool InstancePerEntity => true;
    public int Level = 1;
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.ModItem is StudentWeaponClass;
    public override void SaveData(Item item, TagCompound tag) => tag["WeaponLevel"] = Level;
    public override void LoadData(Item item, TagCompound tag) => Level = Math.Clamp(tag.GetInt("WeaponLevel"), 1, 10);
    public override void NetSend(Item item, BinaryWriter writer) => writer.Write((byte)Level);
    public override void NetReceive(Item item, BinaryReader reader) => Level = Math.Clamp((int)reader.ReadByte(), 1, 10);
    public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
    {
        float scale = KivotosClientConfig.Instance?.StudentWeaponDamageMultiplier ?? 1f;
        if (!float.IsFinite(scale)) scale = 1f;
        damage *= Math.Clamp(scale, .3f, 20f) * (1f + TabletWeaponUpgrade.DamageBonus(item, Level));
    }
    public override void ModifyWeaponCrit(Item item, Player player, ref float crit) => crit += TabletWeaponUpgrade.CritBonus(item,Level);
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) =>
        tooltips.Add(new TooltipLine(Mod, "WeaponLevel", $"武器等级 {Level}/10"));
}

public readonly record struct WeaponUpgradeMaterial(int ItemType, int Count);

public static class TabletWeaponUpgrade
{
    // Indices are target levels. All damage increments use the original level-one damage.
    public static readonly float[] DamageSteps = { 0, 0, .10f, .10f, .10f, 1.50f, .20f, .15f, .10f, .10f, .50f };
    public static readonly int[] CritSteps = { 0, 0, 1, 1, 2, 10, 1, 2, 1, 1, 7 };
    public static readonly string[] BossNames = { "", "", "克苏鲁之眼", "世界吞噬者 / 克苏鲁之脑", "骷髅王", "血肉墙", "机械三王全部", "世纪之花", "石巨人", "拜月教邪教徒", "月亮领主" };
    public static long Cost(int targetLevel) => 10000L * (targetLevel - 1) * (targetLevel - 1);
    public static float DamageBonus(Item item, int level)
    {
        float bonus = 0;
        for (int i = 2; i <= Math.Clamp(level, 1, 10); i++)
            bonus += DamageSteps[i] + ((StudentWeaponClass)item.ModItem).DamageUpgradeSteps[i];
        return bonus;
    }
    public static int CritBonus(Item item,int level)
    {
        int bonus = 0;
        for (int i = 2; i <= Math.Clamp(level, 1, 10); i++)
            bonus += CritSteps[i]+((StudentWeaponClass)item.ModItem).CritUpgradeSteps[i];
        return bonus;
    }
    public static bool BossUnlocked(int targetLevel) => targetLevel switch
    {
        2 => NPC.downedBoss1,
        3 => NPC.downedBoss2,
        4 => NPC.downedBoss3,
        5 => Main.hardMode,
        6 => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3,
        7 => NPC.downedPlantBoss,
        8 => NPC.downedGolemBoss,
        9 => NPC.downedAncientCultist,
        10 => NPC.downedMoonlord,
        _ => false,
    };
    // Initial common recipes: each uses a drop/material from its corresponding stage.
    // Per-weapon recipes can be supplied by overriding StudentWeaponClass.UpgradeMaterials.
    public static WeaponUpgradeMaterial[] CommonMaterials(int targetLevel) => targetLevel switch
    {
        2 => new[] { new WeaponUpgradeMaterial(ItemID.Lens, 6), new WeaponUpgradeMaterial(ItemID.IronBar, 10) },
        3 => new[] { new WeaponUpgradeMaterial(WorldGen.crimson ? ItemID.TissueSample : ItemID.ShadowScale, 10) },
        4 => new[] { new WeaponUpgradeMaterial(ItemID.Bone, 30) },
        5 => new[] { new WeaponUpgradeMaterial(ItemID.HellstoneBar, 15) },
        6 => new[] { new WeaponUpgradeMaterial(ItemID.HallowedBar, 15), new WeaponUpgradeMaterial(ItemID.SoulofMight, 5), new WeaponUpgradeMaterial(ItemID.SoulofSight, 5), new WeaponUpgradeMaterial(ItemID.SoulofFright, 5) },
        7 => new[] { new WeaponUpgradeMaterial(ItemID.ChlorophyteBar, 15) },
        8 => new[] { new WeaponUpgradeMaterial(ItemID.BeetleHusk, 8) },
        9 => new[] { new WeaponUpgradeMaterial(ItemID.FragmentSolar, 5), new WeaponUpgradeMaterial(ItemID.FragmentVortex, 5), new WeaponUpgradeMaterial(ItemID.FragmentNebula, 5), new WeaponUpgradeMaterial(ItemID.FragmentStardust, 5) },
        10 => new[] { new WeaponUpgradeMaterial(ItemID.LunarBar, 10) },
        _ => Array.Empty<WeaponUpgradeMaterial>(),
    };
    public static int Count(Player player, int type)
    {
        int count = 0;
        for (int i = 0; i < 58; i++) if (player.inventory[i].type == type) count += player.inventory[i].stack;
        return count;
    }
    public static bool CanUpgrade(Player player, Item item)
    {
        if (item.ModItem is not StudentWeaponClass weapon) return false;
        int next = item.GetGlobalItem<TabletWeaponLevel>().Level + 1;
        if (next > 10 || !BossUnlocked(next) || TabletWallet.GetTotalCredits(player) < Cost(next)) return false;
        foreach (var m in weapon.UpgradeMaterials(next)) if (Count(player, m.ItemType) < m.Count) return false;
        return true;
    }
    public static bool TryUpgrade(Player player, int slot, int expectedType, int expectedLevel, out string message)
    {
        message = "升级失败：请检查武器、Boss 进度、材料和余额。";
        if (Main.netMode == NetmodeID.MultiplayerClient || player == null || !player.active || player.dead || slot < 0 || slot >= 58) return false;
        Item item = player.inventory[slot];
        if (item.type != expectedType || item.ModItem is not StudentWeaponClass weapon) return false;
        var data = item.GetGlobalItem<TabletWeaponLevel>();
        if (data.Level != expectedLevel || !CanUpgrade(player, item)) return false;
        int next = data.Level + 1;
        if (!TabletWallet.TrySpend(player, Cost(next))) return false;
        foreach (var m in weapon.UpgradeMaterials(next))
        {
            int remaining = m.Count;
            for (int i = 0; i < 58 && remaining > 0; i++)
            {
                Item material = player.inventory[i];
                if (material.type != m.ItemType) continue;
                int take = Math.Min(material.stack, remaining);
                material.stack -= take;
                remaining -= take;
                if (material.stack <= 0) material.TurnToAir();
            }
        }
        data.Level = next;
        if (Main.netMode == NetmodeID.Server)
            for (int i = 0; i < player.inventory.Length; i++)
                NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, i);
        message = $"{item.Name} 已升级至 Lv.{next}。";
        return true;
    }
    public static void Request(int slot)
    {
        Item item = Main.LocalPlayer.inventory[slot];
        int level = item.GetGlobalItem<TabletWeaponLevel>().Level;
        if (Main.netMode == NetmodeID.MultiplayerClient) TabletNet.SendUpgradeRequest(slot, item.type, level);
        else
        {
            TryUpgrade(Main.LocalPlayer, slot, item.type, level, out string message);
            Main.NewText(message);
        }
    }
}
