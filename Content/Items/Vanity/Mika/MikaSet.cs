using KivotosMod.Content.Tablet;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Vanity.Mika;

public static class MikaVanitySet
{
    public static bool HeadVisible(Player p) => p.head == ModContent.GetInstance<MikaHead>().Item.headSlot || p.head == ModContent.GetInstance<Mika2x2Head>().Item.headSlot;
    public static bool BodyVisible(Player p) => p.body == ModContent.GetInstance<MikaBody>().Item.bodySlot || p.body == ModContent.GetInstance<Mika2x2Body>().Item.bodySlot;
    public static bool BootsVisible(Player p) => p.legs == ModContent.GetInstance<MikaBoots>().Item.legSlot || p.legs == ModContent.GetInstance<Mika2x2Boots>().Item.legSlot;
    public static bool FullSetVisible(Player p) => HeadVisible(p) && BodyVisible(p) && BootsVisible(p);

    // Gameplay hooks may precede FrameEffects: inspect selection/equipment,
    // not last frame's visual slots, when selecting Mika's voice/wing skin.
    public static bool IsMika(Player p)
    {
        var appearance = p.GetModPlayer<TabletStudentAppearancePlayer>();
        if (appearance.HasSelection) return appearance.IsMikaAppearance;
        int VisibleType(int slot) => p.armor[slot + 10].IsAir ? p.armor[slot].type : p.armor[slot + 10].type;
        return (VisibleType(0) == ModContent.ItemType<MikaHead>() || VisibleType(0) == ModContent.ItemType<Mika2x2Head>())
            && (VisibleType(1) == ModContent.ItemType<MikaBody>() || VisibleType(1) == ModContent.ItemType<Mika2x2Body>())
            && (VisibleType(2) == ModContent.ItemType<MikaBoots>() || VisibleType(2) == ModContent.ItemType<Mika2x2Boots>());
    }
}

[AutoloadEquip(EquipType.Head)]
public sealed class MikaHead : ModItem
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
        Item.width = 28; Item.height = 26; Item.vanity = true;
        Item.rare = ItemRarityID.LightRed;
    }
}

[AutoloadEquip(EquipType.Body)]
public sealed class MikaBody : ModItem
{

    public override void SetStaticDefaults()
    {
        ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = true;
        ArmorIDs.Body.Sets.HidesArms[Item.bodySlot] = true;
        ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = true;
    }
    public override void SetDefaults()
    {
        Item.width = 16; Item.height = 16; Item.vanity = true;
        Item.rare = ItemRarityID.LightRed;
    }
}

[AutoloadEquip(EquipType.Legs)]
public sealed class MikaBoots : ModItem
{

    public override void SetStaticDefaults() => ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = true;
    public override void SetDefaults()
    {
        Item.width = 24; Item.height = 24; Item.vanity = true;
        Item.rare = ItemRarityID.LightRed;
    }
}
