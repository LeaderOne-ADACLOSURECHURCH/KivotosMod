using KivotosMod.Content.Projs.Ranged.StudentWeapons.Millennium;
using KivotosMod.Globals.Classes;
using KivotosMod.Globals.Database.Enums;
using KivotosMod.Globals.Database.Lists;

namespace KivotosMod.Content.Items.Weapons.Ranged.StudentWeapons.Millennium
{
    /// <summary>
    /// 这玩意需要做一个双持效果，但是目前还没有实现，暂时先不做了
    /// </summary>
    public class YuukaWeapon : StudentWeaponClass
    {
        protected override void SetUpTextboxSettings(ref Color backgroundColor, ref Color backgroundEdgeColor, ref Color textColor, ref Color textEdgeColor)
        {
            backgroundColor = Color.DarkRed * .3f;
            backgroundEdgeColor = Color.DarkRed;
            textColor = Color.White;
            textEdgeColor = Color.Lerp(Color.Red, Color.DarkRed, .5f);
            //base.SetUpTextboxSettings(ref backgroundColor, ref backgroundEdgeColor, ref textColor, ref textEdgeColor);
        }

        public const int ShotIntervalTicks = 8;

        protected override string Owner => "Yuuka";
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            KivotosLists.ShinyRarityItemDictionary.Add(Type, KivotosRarityType.YuukaBlue);
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            // Summary7: 0.75 times the previous 6-tick firing rate => 8 ticks.
            Item.useTime = Item.useAnimation = ShotIntervalTicks;
            Item.damage = 21;
            Item.knockBack = 1;
            Item.shootSpeed = 10f;
            Item.shoot = ProjectileType<YuukaWeaponHeldProj>();
        }
    }

}
