using KivotosMod.Content.Skills.BlackHole;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Tools;

// No recipe: obtain from an item browser for local visual testing.
public sealed class BlackHolePreviewItem : ModItem
{
    public override string LocalizationCategory => "Items";
    public override string Texture => "Terraria/Images/Item_" + ItemID.FallenStar;
    public override void SetDefaults()
    {
        Item.width = 20; Item.height = 20; Item.maxStack = 1;
        Item.rare = ItemRarityID.Purple; Item.value = 0;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.useTime = Item.useAnimation = 20;
    }
    public override bool? UseItem(Player player)
    {
        if (!Main.dedServ && player.whoAmI == Main.myPlayer)
        {
            BlackHolePreviewSystem.PaintDemo=false;
            BlackHolePreviewSystem.Toggle();
        }
        return true;
    }
}
