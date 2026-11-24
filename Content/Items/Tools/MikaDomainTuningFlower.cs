using KivotosMod.Content.Skills.BlackHole;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace KivotosMod.Content.Items.Tools;
public sealed class MikaDomainTuningFlower : ModItem
{
    public override string LocalizationCategory=>"Items";
    public override string Texture=>"Terraria/Images/Item_"+ItemID.Daybloom;
    public override void SetDefaults(){Item.width=24;Item.height=24;Item.useStyle=ItemUseStyleID.HoldUp;Item.useTime=Item.useAnimation=20;Item.rare=ItemRarityID.Purple;}
    public override bool? UseItem(Player player){if(!Main.dedServ && player.whoAmI==Main.myPlayer)MikaDomainTuning.Toggle();return true;}
}
