using Terraria;
using Terraria.ModLoader;

namespace KivotosMod.Globals.Instances.Projs
{
    public partial class KivotosGlobalProjs : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public bool FirstFrame = false;
        public override void AI(Projectile projectile)
        {
            if (!Main.dedServ && projectile.friendly && projectile.DamageType.CountsAsClass<RangedDamageClass>()
                && projectile.ModProjectile is global::KivotosMod.Globals.Classes.KivotosPlayerProjs)
                Lighting.AddLight(projectile.Center, .35f, .35f, .45f);
            if (!FirstFrame)
            {
                FirstFrame = true;
            }
        }
    }
}
