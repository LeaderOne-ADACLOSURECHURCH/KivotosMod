using KivotosMod.Content.Tablet;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Tools;

// Developer-only utility: no recipe or shop entry. Obtain through an item browser.
public sealed class WeaponLevelResetter : ModItem
{
    public override string LocalizationCategory => "Items";
    public override string Texture => "Terraria/Images/Item_" + ItemID.Wrench;
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
    public override void RightClick(Player player) => TabletWeaponReset.Request(player);
}
