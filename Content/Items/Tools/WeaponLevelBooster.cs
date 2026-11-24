using KivotosMod.Content.Tablet;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Tools;

// Developer utility, obtained from an item browser rather than crafting or shops.
public sealed class WeaponLevelBooster : ModItem
{
    public override string LocalizationCategory => "Items";
    public override string Texture => "Terraria/Images/Item_" + ItemID.IronHammer;
    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 28;
        Item.maxStack = 1;
        Item.rare = ItemRarityID.Purple;
        Item.value = 0;
    }
    public override bool CanRightClick() => true;
    public override bool ConsumeItem(Player player) => false;
    public override void RightClick(Player player) => TabletWeaponBoost.Request(player);
}
