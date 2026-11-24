using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.ModLoader;
namespace KivotosMod.Content.Items.Vanity.Mika;

[Autoload(Side = ModSide.Client)]
public sealed class MikaSideLocksLayer : PlayerDrawLayer
{
    private static Asset<Texture2D> _texture;
    internal static bool Grounded(PlayerDrawSet info) => !info.drawPlayer.mount.Active
        && info.drawPlayer.itemAnimation <= 0 && System.Math.Abs(info.drawPlayer.velocity.Y) < .1f;
    internal static bool HasHair(PlayerDrawSet info) => !info.drawPlayer.dead && !info.drawPlayer.ghost
        && !info.drawPlayer.invis && !StudentSpriteVariants.Mika2x2(info.drawPlayer) && MikaVanitySet.HeadVisible(info.drawPlayer);
    public override bool GetDefaultVisibility(PlayerDrawSet info) => HasHair(info) && !Grounded(info);
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);
    public override bool IsHeadLayer => true;
    internal static void DrawHair(ref PlayerDrawSet info) => StudentSpriteDraw.Strip(ref info, ref _texture,
        "KivotosMod/Content/Items/Vanity/Mika/MikaSideLocks", head: true);
    protected override void Draw(ref PlayerDrawSet info) => DrawHair(ref info);
}

[Autoload(Side = ModSide.Client)]
public sealed class MikaIdleSideLocksLayer : PlayerDrawLayer
{
    public override bool GetDefaultVisibility(PlayerDrawSet info)
        => MikaSideLocksLayer.HasHair(info) && MikaSideLocksLayer.Grounded(info);
    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.ArmOverItem, PlayerDrawLayers.BladedGlove);
    public override bool IsHeadLayer => true;
    protected override void Draw(ref PlayerDrawSet info) => MikaSideLocksLayer.DrawHair(ref info);
}
