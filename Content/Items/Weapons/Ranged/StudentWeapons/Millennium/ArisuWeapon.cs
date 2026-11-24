using System;
using System.Collections.Generic;
using KivotosMod.Content.Projs.Ranged.StudentWeapons.Millennium;
using KivotosMod.Globals.Classes;
using KivotosMod.Globals.Database.Enums;
using KivotosMod.Globals.Database.Lists;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace KivotosMod.Content.Items.Weapons.Ranged.StudentWeapons.Millennium;

public sealed class ArisuWeapon : StudentWeaponClass
{
    internal const int BaseDamage = 85;
    internal const int FullEnergy = 2;
    private const int ShotsPerLevel = 5;
    private int _shots;
    internal int Energy { get; private set; }
    protected override string Owner => "Arisu";
    public override float[] DamageUpgradeSteps { get; } = { 0, 0, 0, 0, 0, -1.30f, -.10f, 0, 0, 0, 0 };
    public override int[] CritUpgradeSteps { get; } = { 0, 0, 0, 0, 0, -7, 0, 0, 0, 0, 0 };

    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
        KivotosLists.ShinyRarityItemDictionary.Add(Type, KivotosRarityType.ArisuSkyBlue);
    }

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.damage = BaseDamage;
        Item.crit = 4;
        Item.DamageType = DamageClass.Magic;
        Item.width = 56;
        Item.height = 16;
        Item.useTime = Item.useAnimation = 60;
        Item.useAmmo = AmmoID.None;
        Item.shoot = ProjectileType<ArisuRailShot>();
        Item.shootSpeed = 60f;
        Item.knockBack = 6;
        Item.value = Item.buyPrice(gold: 1, silver: 20);
        Item.rare = ItemRarityID.Red;
        Item.mana = 20;
    }

    internal static int GrowthDamage(bool queen, int mechanicalBosses, bool plant, bool golem)
        => (queen ? 10 : 0) + (mechanicalBosses switch { 1 => 6, 2 => 16, 3 => 60, _ => 0 })
        + (plant ? 52 : 0) + (golem ? 88 : 0);

    private static int WorldGrowth => GrowthDamage(NPC.downedQueenSlime,
        (NPC.downedMechBoss1 ? 1 : 0) + (NPC.downedMechBoss2 ? 1 : 0) + (NPC.downedMechBoss3 ? 1 : 0),
        NPC.downedPlantBoss, NPC.downedGolemBoss);

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        => damage.Base += WorldGrowth * (Item.damage / (float)BaseDamage);

    public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
    {
        if (player.altFunctionUse == 2) { reduce = 0; mult = 0; }
        else if (NPC.downedPlantBoss) mult *= 32f / 20f;
    }

    public override void OnMissingMana(Player player, int neededMana)
    {
        // Let Mana Flower handle its own automatic use. The weapon only uses main-inventory supplies.
        if (player.whoAmI != Main.myPlayer || player.manaFlower) return;
        Item potion = player.QuickMana_GetItemToUse();
        if (potion != null && Array.IndexOf(player.inventory, potion, 0, 58) >= 0)
            player.QuickMana();
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
        => player.ownedProjectileCounts[ProjectileType<ArisuRailHeld>()] == 0 &&
            (player.altFunctionUse != 2 || Energy == FullEnergy);

    // ReSoLS draws the gun through attack projectiles rather than spawning a perpetual holdout.
    public override void HoldItem(Player player) { }

    internal bool RecordNormalShot()
    {
        if (Energy >= FullEnergy || ++_shots < ShotsPerLevel) return false;
        _shots = 0;
        Energy++;
        return true;
    }

    internal bool SpendEnergy()
    {
        if (Energy < FullEnergy) return false;
        Energy = _shots = 0;
        return true;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        Vector2 aim = velocity.SafeNormalize(new Vector2(player.direction, 0));
        int facing = aim.X < 0 ? -1 : 1;
        if (player.altFunctionUse == 2)
        {
            if (Energy != FullEnergy || player.ownedProjectileCounts[ProjectileType<ArisuRailHeld>()] > 0) return false;
            Projectile.NewProjectile(source, position, aim, ProjectileType<ArisuRailHeld>(), 0, 0, player.whoAmI);
            return false;
        }

        Vector2 shot = position + aim.RotatedBy(-.36 * facing) * 20f;
        Vector2 muzzle = position + aim.RotatedBy(-.10 * facing) * 96f;
        bool powered = Energy == FullEnergy;
        Projectile.NewProjectile(source, shot, velocity, ProjectileType<ArisuRailShot>(),
            powered ? damage * 135 / 100 : damage, knockback, player.whoAmI, powered ? 1 : 0);
        Projectile.NewProjectile(source, muzzle, aim, ProjectileType<ArisuRailFlash>(), 0, 0, player.whoAmI);
        Projectile.NewProjectile(source, position, aim, ProjectileType<ArisuRailRecoil>(), 0, 0, player.whoAmI,
            Math.Max(1, player.itemAnimationMax), 0, powered ? 1 : 0);
        if (RecordNormalShot())
            Projectile.NewProjectile(source, player.MountedCenter, aim, ProjectileType<ArisuRailStorage>(),
                0, 0, player.whoAmI, Energy);
        return false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        Color color = Color.Lerp(new Color(87, 227, 255), new Color(255, 245, 255),
            (MathF.Sin(Main.GlobalTimeWrappedHourly * 3) + 1) * .5f);
        tooltips.Add(new TooltipLine(Mod, "SupernovaGrowth", Language.GetTextValue(this.GetLocalizationKey("Growth"), WorldGrowth))
            { OverrideColor = color });
        tooltips.Add(new TooltipLine(Mod, "SupernovaEnergy", Language.GetTextValue(this.GetLocalizationKey("Energy"), Energy))
            { OverrideColor = color });
        base.ModifyTooltips(tooltips);
    }

    public override void AddRecipes()
    {
        foreach (int bar in new[] { ItemID.TitaniumBar, ItemID.AdamantiteBar })
            CreateRecipe().AddIngredient(ItemID.SoulofLight, 3).AddIngredient(bar, 25)
                .AddTile(TileID.MythrilAnvil).Register();
    }

    protected override void SetUpTextboxSettings(ref Color backgroundColor, ref Color backgroundEdgeColor,
        ref Color textColor, ref Color textEdgeColor)
    {
        backgroundColor = Color.Black * .44f;
        backgroundEdgeColor = textEdgeColor = Color.MidnightBlue;
        textColor = Color.LightCyan;
    }
}
