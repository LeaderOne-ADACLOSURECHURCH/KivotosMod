using KivotosMod.Globals.Classes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Tablet;

public sealed class MaxWeaponsCommand : ModCommand
{
    public override string Command => "maxweapons";
    public override CommandType Type => CommandType.World;
    public override string Usage => "/maxweapons";
    public override string Description => "作弊：将背包及快捷栏内全部学员武器直接升至10级，不消耗材料或信用点";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length != 0) { caller.Reply(Usage); return; }
        var player = caller.Player;
        if (Main.netMode == NetmodeID.MultiplayerClient || player == null || !player.active) return;
        int changed = 0;
        for (int slot = 0; slot < player.inventory.Length; slot++)
        {
            var item = player.inventory[slot];
            if (item == null || item.IsAir || item.ModItem is not StudentWeaponClass) continue;
            var level = item.GetGlobalItem<TabletWeaponLevel>();
            if (level.Level >= 10) continue;
            level.Level = 10;
            changed++;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, slot);
        }
        caller.Reply(changed > 0
            ? $"作弊升级完成：{changed} 把学员武器已升至 Lv.10，未消耗材料或信用点。"
            : "背包中没有需要升级的学员武器（已满级或不支持等级系统）。");
    }
}
