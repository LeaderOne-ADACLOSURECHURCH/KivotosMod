using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace KivotosMod.Content.Items.Vanity.Hikari;

[AutoloadEquip(EquipType.Head)]
public sealed class Hikari2x2Head : ModItem
{
    public override string Texture => "KivotosMod/Content/Items/Vanity/Hikari/Hikari2x2Head";
    public override void SetStaticDefaults() { ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false; ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = false; ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = false; ArmorIDs.Head.Sets.PreventBeardDraw[Item.headSlot] = true; }
    public override void SetDefaults() { Item.width=24; Item.height=28; Item.vanity=true; Item.rare=ItemRarityID.LightRed; }
}

[AutoloadEquip(EquipType.Body)]
public sealed class Hikari2x2Body : ModItem
{
    public override string Texture => "KivotosMod/Content/Items/Vanity/Hikari/Hikari2x2Body";
    public override void SetStaticDefaults() { ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = true; ArmorIDs.Body.Sets.HidesArms[Item.bodySlot] = true; ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = true; }
    public override void SetDefaults() { Item.width=24; Item.height=28; Item.vanity=true; Item.rare=ItemRarityID.LightRed; }
}

[AutoloadEquip(EquipType.Legs)]
public sealed class Hikari2x2Boots : ModItem
{
    public override string Texture => "KivotosMod/Content/Items/Vanity/Hikari/Hikari2x2Boots";
    public override void SetStaticDefaults() { ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = true; }
    public override void SetDefaults() { Item.width=24; Item.height=28; Item.vanity=true; Item.rare=ItemRarityID.LightRed; }
}
