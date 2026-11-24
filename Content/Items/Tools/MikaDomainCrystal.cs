using KivotosMod.Content.Skills.BlackHole;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Tools;

public sealed class MikaDomainCrystal : ModItem
{
    public override string LocalizationCategory => "Items";
    public override string Texture => "Terraria/Images/Item_" + ItemID.ManaCrystal;
    public override void SetDefaults()
    {
        Item.width = Item.height = 24;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.useTime = Item.useAnimation = 20;
        Item.rare = ItemRarityID.Purple;
    }
    public override bool CanUseItem(Player player) => MikaExDomain.Active ? MikaExDomain.Owner == player.whoAmI :
        !Main.dedServ && BlackHolePreviewSystem.Enabled;
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer) return true;
        if (MikaExDomain.Active) player.GetModPlayer<MikaExPlayer>().UseCrystal();
        else BlackHolePreviewSystem.TryPreviewCrystal();
        return true;
    }
}
