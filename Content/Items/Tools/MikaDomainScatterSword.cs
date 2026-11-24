using KivotosMod.Content.Skills.BlackHole;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace KivotosMod.Content.Items.Tools;
public sealed class MikaDomainScatterSword : ModItem
{
    public override string LocalizationCategory=>"Items";
    public override string Texture=>"Terraria/Images/Item_"+ItemID.Starfury;
    public override void SetDefaults(){Item.width=32;Item.height=32;Item.useStyle=ItemUseStyleID.Swing;Item.useTime=Item.useAnimation=20;Item.noMelee=true;Item.rare=ItemRarityID.Purple;}
    public override bool? UseItem(Player player)
    {
        if(Main.dedServ || player.whoAmI!=Main.myPlayer)return true;
        if(MikaExDomain.Active)player.GetModPlayer<MikaExPlayer>().UseScatterSword();
        else MikaDomainStars.Scatter();
        return true;
    }
}
