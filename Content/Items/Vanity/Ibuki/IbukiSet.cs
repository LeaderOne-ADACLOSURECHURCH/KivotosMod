using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Ibuki;

public static class IbukiVanitySet
{
    public static bool HeadVisible(Player player) => player.head == ModContent.GetInstance<IbukiHead>().Item.headSlot || player.head == ModContent.GetInstance<Ibuki2x2Head>().Item.headSlot;
    public static bool BodyVisible(Player player) => player.body == ModContent.GetInstance<IbukiBody>().Item.bodySlot || player.body == ModContent.GetInstance<Ibuki2x2Body>().Item.bodySlot;
    public static bool BootsVisible(Player player) => player.legs == ModContent.GetInstance<IbukiBoots>().Item.legSlot || player.legs == ModContent.GetInstance<Ibuki2x2Boots>().Item.legSlot;
    public static bool FullSetVisible(Player player) => HeadVisible(player) && BodyVisible(player) && BootsVisible(player);
}

[AutoloadEquip(EquipType.Head)]
public sealed class IbukiHead : ModItem
{
    public override void SetStaticDefaults()
    {
        ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
        ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = false;
        ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = false;
        ArmorIDs.Head.Sets.PreventBeardDraw[Item.headSlot] = true;
    }
    public override void SetDefaults()
    {
        Item.width = 24; Item.height = 28; Item.vanity = true;
        Item.rare = ItemRarityID.LightRed;
    }
}

[AutoloadEquip(EquipType.Body)]
public sealed class IbukiBody : ModItem
{
    public override void SetStaticDefaults()
    {
        ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = true;
        ArmorIDs.Body.Sets.HidesArms[Item.bodySlot] = true;
        ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = true;
    }
    public override void SetDefaults()
    {
        Item.width = 28; Item.height = 30; Item.vanity = true;
        Item.rare = ItemRarityID.LightRed;
    }
}

[AutoloadEquip(EquipType.Legs)]
public sealed class IbukiBoots : ModItem
{
    public override void SetStaticDefaults()
        => ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = true;
    public override void SetDefaults()
    {
        Item.width = 24; Item.height = 24; Item.vanity = true;
        Item.rare = ItemRarityID.LightRed;
    }
}
