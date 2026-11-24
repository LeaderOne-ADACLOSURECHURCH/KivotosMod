using KivotosMod.Content.Items.Tools;
using KivotosMod.Globals.Classes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Tablet;

public static class TabletWeaponReset
{
    public static bool TryReset(Player player, int slot, int expectedType, int expectedLevel, out string message)
    {
        message = "重置失败：请选择快捷栏中的学员武器，并将开发者重置器放在背包中。";
        if (Main.netMode == NetmodeID.MultiplayerClient || player == null || !player.active || player.dead
            || slot < 0 || slot >= 10 || !player.HasItem(ModContent.ItemType<WeaponLevelResetter>())) return false;
        Item item = player.inventory[slot];
        if (item.type != expectedType || item.ModItem is not StudentWeaponClass) return false;
        var data = item.GetGlobalItem<TabletWeaponLevel>();
        if (data.Level != expectedLevel) return false;
        if (data.Level == 1)
        {
            message = "这把武器已经是 Lv.1。";
            return false;
        }
        data.Level = 1;
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, slot);
        message = $"{item.Name} 已恢复为 Lv.1。";
        return true;
    }

    public static void Request(Player player)
    {
        if (player.whoAmI != Main.myPlayer) return;
        int slot = player.selectedItem;
        if (slot < 0 || slot >= 10 || player.inventory[slot].ModItem is not StudentWeaponClass)
        {
            Main.NewText("请先选中快捷栏中的学员武器，再右键背包里的开发者重置器。");
            return;
        }
        Item item = player.inventory[slot];
        int level = item.GetGlobalItem<TabletWeaponLevel>().Level;
        if (Main.netMode == NetmodeID.MultiplayerClient) TabletNet.SendResetRequest(slot, item.type, level);
        else
        {
            TryReset(player, slot, item.type, level, out string message);
            Main.NewText(message);
        }
    }
}
