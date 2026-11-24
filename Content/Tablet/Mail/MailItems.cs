using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.GameContent.ItemDropRules;

namespace KivotosMod.Content.Tablet.Mail;

public sealed class MailOrange : ModItem
{
    public override string Texture=>"Terraria/Images/Item_"+ItemID.BloodOrange;
    public override LocalizedText DisplayName=>Language.GetOrRegister("Mods.KivotosMod.Items.MailOrange.DisplayName",()=>"橘子");
    public override void SetDefaults()=>Item.CloneDefaults(ItemID.BloodOrange);
}
public sealed class YuukaApplePie : ModItem
{
    public override string Texture=>"Terraria/Images/Item_"+ItemID.ApplePie;
    public override LocalizedText DisplayName=>Language.GetOrRegister("Mods.KivotosMod.Items.YuukaApplePie.DisplayName",()=>"美味的苹果派");
    public override LocalizedText Tooltip=>Language.GetOrRegister("Mods.KivotosMod.Items.YuukaApplePie.Tooltip",()=>"优香亲手制作的苹果派\n所有属性中幅度提升，并获得生命回复效果，持续3分钟");
    public override void SetDefaults(){Item.DefaultToFood(28,24,BuffID.WellFed2,10800);Item.value=0;Item.maxStack=9999;}
    public override void OnConsumeItem(Player player)=>player.AddBuff(BuffID.Regeneration,10800);
}
public abstract class MailAccessoryBox : ModItem
{
    protected abstract int Stage {get;}
    public override string Texture=>"Terraria/Images/Item_"+ItemID.Present;
    public override LocalizedText DisplayName=>Language.GetOrRegister("Mods.KivotosMod.Items."+Name+".DisplayName",()=>new[]{"饰品礼盒（肉山前）","饰品礼盒（困难模式）","饰品礼盒（世纪之花后）"}[Stage]);
    public override LocalizedText Tooltip=>Language.GetOrRegister("Mods.KivotosMod.Items."+Name+".Tooltip",()=>"右键开启，随机获得本阶段列表中的一件饰品");
    public override void SetDefaults(){Item.width=24;Item.height=24;Item.maxStack=9999;Item.rare=ItemRarityID.Blue;}
    public override bool CanRightClick()=>true;
    public override void ModifyItemLoot(ItemLoot itemLoot)=>itemLoot.Add(ItemDropRule.OneFromOptions(1,TabletMailCatalog.Accessories[Stage]));
}
public sealed class PreHardmodeMailBox : MailAccessoryBox {protected override int Stage=>0;}
public sealed class HardmodeMailBox : MailAccessoryBox {protected override int Stage=>1;}
public sealed class PlanteraMailBox : MailAccessoryBox {protected override int Stage=>2;}
public sealed class MailResetter : ModItem
{
    public override string Texture=>"Terraria/Images/Item_"+ItemID.Wrench;
    public override LocalizedText DisplayName=>Language.GetOrRegister("Mods.KivotosMod.Items.MailResetter.DisplayName",()=>"邮件重置器（开发者）");
    public override LocalizedText Tooltip=>Language.GetOrRegister("Mods.KivotosMod.Items.MailResetter.Tooltip",()=>"右键清空自己的邮件及剧情接收记录；现实每日领取日期保留");
    public override void SetDefaults(){Item.width=28;Item.height=28;Item.maxStack=1;Item.rare=ItemRarityID.Purple;}
    public override bool CanRightClick()=>true;
    public override bool ConsumeItem(Player player)=>false;
    public override void RightClick(Player player)
    {
        if(player.whoAmI!=Main.myPlayer)return;
        if(Main.netMode==NetmodeID.MultiplayerClient)TabletMailPlayer.Request("",4);
        else player.GetModPlayer<TabletMailPlayer>().ResetMailbox();
    }
}
